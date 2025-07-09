using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;
using NoorAhlulBayt.Common.Services;
using NoorAhlulBayt.Companion.Services;
using NoorAhlulBayt.Companion.Dialogs;

using Microsoft.EntityFrameworkCore;
using WinForms = System.Windows.Forms;

namespace NoorAhlulBayt.Companion;

/// <summary>
/// Interaction logic for MainWindow.xaml - Modern Parent Dashboard
/// </summary>
public partial class MainWindow : Window
{
    private readonly ApplicationDbContext _context;
    private readonly BrowserMonitoringService _browserMonitor;
    private readonly OtherBrowserMonitoringService _otherBrowserMonitor;
    private readonly SystemTrayService _systemTray;
    private readonly MasterPasswordService _masterPasswordService;
    private readonly DashboardService _dashboardService;
    private readonly ProfileManagementService _profileManagementService;
    private readonly SettingsService _settingsService;
    private readonly PrayerTimeService _prayerTimeService;
    private readonly System.Timers.Timer _uiUpdateTimer;
    private string _currentSettingsPanel = "";
    private bool _isClosing = false;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize database context
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=companion.db")
            .Options;

        _context = new ApplicationDbContext(options);

        // Ensure database is created and up to date
        try
        {
            _context.Database.EnsureCreated();
            Console.WriteLine("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
        }

        _masterPasswordService = new MasterPasswordService(_context);
        _browserMonitor = new BrowserMonitoringService(_context);
        _otherBrowserMonitor = new OtherBrowserMonitoringService(_context);
        _dashboardService = new DashboardService(_context, _browserMonitor, _otherBrowserMonitor);
        _profileManagementService = new ProfileManagementService(_context);
        _settingsService = new SettingsService();
        _prayerTimeService = new PrayerTimeService(new HttpClient(), _context, _settingsService);
        _systemTray = new SystemTrayService(_browserMonitor);

        // Subscribe to browser monitoring events
        _browserMonitor.BrowserStatusChanged += OnBrowserStatusChanged;
        _otherBrowserMonitor.BrowserDetected += OnOtherBrowserDetected;
        _otherBrowserMonitor.BrowserTerminated += OnOtherBrowserTerminated;

        // Setup UI update timer
        _uiUpdateTimer = new System.Timers.Timer(2000); // Update every 2 seconds
        _uiUpdateTimer.Elapsed += (s, e) => Dispatcher.Invoke(UpdateDashboard);
        _uiUpdateTimer.AutoReset = true;

        // Initialize the application
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Check master password authentication
            if (!await HandleMasterPasswordAuthenticationAsync())
            {
                // Authentication failed or cancelled, close app
                _isClosing = true;
                System.Windows.Application.Current.Shutdown();
                return;
            }

            // Start browser monitoring
            await _browserMonitor.StartMonitoringAsync();
            await _otherBrowserMonitor.StartMonitoringAsync();

            // Start UI updates
            _uiUpdateTimer.Start();

            // Update initial dashboard
            UpdateDashboard();

            // Load profiles
            await LoadProfilesAsync();

            // Initialize settings panel with prayer settings as default
            ShowSettingsPanel("PrayerSettings");

            // Check if we should minimize to tray on startup
            var settings = _settingsService.GetSettings();
            if (settings.General.StartMinimized)
            {
                WindowState = WindowState.Minimized;
                Hide();
            }
            else
            {
                // Show the window normally after successful authentication
                Show();
                WindowState = WindowState.Normal;
                Activate();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error initializing companion app: {ex.Message}",
                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _isClosing = true;
            System.Windows.Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// Handle master password authentication flow
    /// </summary>
    private async Task<bool> HandleMasterPasswordAuthenticationAsync()
    {
        try
        {
            // Check if master password is required
            if (!await _masterPasswordService.IsMasterPasswordRequiredAsync())
            {
                return true; // No authentication required
            }

            // Check if master password is set up
            if (!await _masterPasswordService.IsMasterPasswordSetAsync())
            {
                // First time setup - show setup dialog
                Console.WriteLine("Master password not set up, showing setup dialog");
                
                // Show the window temporarily for the dialog
                Show();
                WindowState = WindowState.Normal;
                
                var setupSuccess = MasterPasswordSetupDialog.ShowDialog(this, _context);
                
                if (!setupSuccess)
                {
                    System.Windows.MessageBox.Show(
                        "Master password setup is required to use the companion app.",
                        "Setup Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                
                Console.WriteLine("Master password setup completed successfully");
            }

            // Verify master password
            Console.WriteLine("Requesting master password verification");

            // Show the window temporarily for the dialog
            Show();
            WindowState = WindowState.Normal;

            var verificationSuccess = MasterPasswordVerificationDialog.ShowDialog(this, _context);

            if (!verificationSuccess)
            {
                System.Windows.MessageBox.Show(
                    "Master password verification failed. Access denied.",
                    "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            Console.WriteLine("Master password verification successful");
            // Keep window visible after successful authentication
            // Window visibility will be controlled by startup settings later
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error during master password authentication: {ex.Message}",
                "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>
    /// Update dashboard with current information
    /// </summary>
    private async void UpdateDashboard()
    {
        try
        {
            // Update family status
            await UpdateFamilyStatusAsync();

            // Update usage statistics
            await UpdateUsageStatisticsAsync();

            // Update activity feed
            await UpdateActivityFeedAsync();

            // Update monitoring status
            UpdateMonitoringStatus();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating dashboard: {ex.Message}");
        }
    }

    private async Task UpdateFamilyStatusAsync()
    {
        try
        {
            var familyStatus = await _dashboardService.GetFamilyStatusAsync();

            // Update profile count
            ProfileCountText.Text = familyStatus.TotalProfiles == 1
                ? "1 Profile"
                : $"{familyStatus.TotalProfiles} Profiles";

            // Update active profiles
            ActiveProfilesText.Text = familyStatus.ActiveProfiles == 0
                ? "None Active"
                : familyStatus.ActiveProfiles == 1
                    ? "1 Active"
                    : $"{familyStatus.ActiveProfiles} Active";

            // Update overall status with color coding
            FamilyStatusText.Text = familyStatus.OverallStatus;
            FamilyStatusText.Foreground = familyStatus.OverallStatus switch
            {
                "Active & Safe" => new SolidColorBrush(Colors.LightGreen),
                "Protected" => new SolidColorBrush(Colors.LightGreen),
                "Monitoring" => new SolidColorBrush(Colors.Orange),
                "Blocked" => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.White)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating family status: {ex.Message}");
            ProfileCountText.Text = "Error";
            ActiveProfilesText.Text = "Error";
            FamilyStatusText.Text = "Error";
            FamilyStatusText.Foreground = new SolidColorBrush(Colors.Red);
        }
    }

    private async Task UpdateUsageStatisticsAsync()
    {
        try
        {
            // Clear existing stats
            UsageStatsPanel.Children.Clear();

            var usageStats = await _dashboardService.GetTodayUsageAsync();

            foreach (var stat in usageStats.Take(3)) // Show max 3 profiles
            {
                var statGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
                statGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                statGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var labelText = new TextBlock
                {
                    Text = $"{stat.ProfileName}:",
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(labelText, 0);

                var statusText = stat.IsActive ? " (Active)" : "";
                var valueText = new TextBlock
                {
                    Text = $"{stat.FormattedDuration}{statusText}",
                    Foreground = stat.IsActive
                        ? new SolidColorBrush(Colors.LightGreen)
                        : new SolidColorBrush(Colors.LightBlue),
                    FontWeight = FontWeights.SemiBold
                };
                Grid.SetColumn(valueText, 1);

                statGrid.Children.Add(labelText);
                statGrid.Children.Add(valueText);
                UsageStatsPanel.Children.Add(statGrid);
            }

            // Add summary if no active sessions
            if (!usageStats.Any(s => s.IsActive))
            {
                var summaryGrid = new Grid { Margin = new Thickness(0, 5, 0, 0) };
                summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var summaryLabel = new TextBlock
                {
                    Text = "Status:",
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(summaryLabel, 0);

                var summaryValue = new TextBlock
                {
                    Text = "No Active Sessions",
                    Foreground = new SolidColorBrush(Colors.Orange),
                    FontWeight = FontWeights.SemiBold
                };
                Grid.SetColumn(summaryValue, 1);

                summaryGrid.Children.Add(summaryLabel);
                summaryGrid.Children.Add(summaryValue);
                UsageStatsPanel.Children.Add(summaryGrid);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating usage statistics: {ex.Message}");

            // Add error message
            var errorText = new TextBlock
            {
                Text = "Error loading usage data",
                Foreground = new SolidColorBrush(Colors.Red),
                FontWeight = FontWeights.SemiBold
            };
            UsageStatsPanel.Children.Add(errorText);
        }
    }

    private async Task UpdateActivityFeedAsync()
    {
        try
        {
            // Clear existing activity
            ActivityFeedPanel.Children.Clear();

            var activities = await _dashboardService.GetRecentActivityAsync(8);

            foreach (var activity in activities)
            {
                var activityGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                activityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                activityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                activityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Icon
                var iconText = new TextBlock
                {
                    Text = activity.Icon,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(iconText, 0);

                // Message
                var messageText = new TextBlock
                {
                    Text = activity.Message,
                    Foreground = GetColorForSeverity(activity.Severity),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12
                };
                Grid.SetColumn(messageText, 1);

                // Timestamp
                var timeText = new TextBlock
                {
                    Text = activity.FormattedTime,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(timeText, 2);

                activityGrid.Children.Add(iconText);
                activityGrid.Children.Add(messageText);
                activityGrid.Children.Add(timeText);
                ActivityFeedPanel.Children.Add(activityGrid);
            }

            // Add "no activity" message if empty
            if (!activities.Any())
            {
                var noActivityText = new TextBlock
                {
                    Text = "📊 No recent activity",
                    Foreground = new SolidColorBrush(Colors.Gray),
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                ActivityFeedPanel.Children.Add(noActivityText);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating activity feed: {ex.Message}");

            var errorText = new TextBlock
            {
                Text = "❌ Error loading activity feed",
                Foreground = new SolidColorBrush(Colors.Red),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            ActivityFeedPanel.Children.Add(errorText);
        }
    }

    private SolidColorBrush GetColorForSeverity(string severity)
    {
        return severity switch
        {
            "Good" => new SolidColorBrush(Colors.LightGreen),
            "Warning" => new SolidColorBrush(Colors.Orange),
            "Error" => new SolidColorBrush(Colors.Red),
            _ => new SolidColorBrush(Colors.White)
        };
    }

    private void UpdateMonitoringStatus()
    {
        try
        {
            var browserStatus = _browserMonitor.GetCurrentStatus();
            var runningBrowsers = _otherBrowserMonitor.GetRunningBrowsers();

            // Update Islamic Browser status
            if (browserStatus.IsBlocked)
            {
                IslamicBrowserStatusText.Text = "Blocked";
                IslamicBrowserStatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
            else if (browserStatus.IsRunning)
            {
                IslamicBrowserStatusText.Text = "Running";
                IslamicBrowserStatusText.Foreground = new SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                IslamicBrowserStatusText.Text = "Not Running";
                IslamicBrowserStatusText.Foreground = new SolidColorBrush(Colors.Orange);
            }

            // Update Other Browsers status
            if (runningBrowsers.Count == 0)
            {
                OtherBrowserStatusText.Text = "None Detected";
                OtherBrowserStatusText.Foreground = new SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                OtherBrowserStatusText.Text = $"{runningBrowsers.Count} Detected";
                OtherBrowserStatusText.Foreground = new SolidColorBrush(Colors.Red);
            }

            // Update Auto-Terminate status
            AutoTerminateStatusText.Text = _otherBrowserMonitor.IsAutoTerminationEnabled ? "Enabled" : "Disabled";
            AutoTerminateStatusText.Foreground = _otherBrowserMonitor.IsAutoTerminationEnabled
                ? new SolidColorBrush(Colors.LightGreen)
                : new SolidColorBrush(Colors.Orange);

            // Update Launch Browser button state
            LaunchBrowserQuickButton.IsEnabled = !browserStatus.IsRunning;
            LaunchBrowserQuickButton.Content = browserStatus.IsRunning
                ? "🚀 Browser Running"
                : "🚀 Launch Islamic Browser";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating monitoring status: {ex.Message}");
        }
    }

    // Event Handlers for Browser Monitoring
    private void OnBrowserStatusChanged(object? sender, BrowserStatusEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateDashboard();
        });
    }

    private void OnOtherBrowserDetected(object? sender, BrowserDetectedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            Console.WriteLine($"Other browser detected: {e.DisplayName}");
            UpdateDashboard();
        });
    }

    private void OnOtherBrowserTerminated(object? sender, BrowserTerminatedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            Console.WriteLine($"Other browser terminated: {e.DisplayName}");
            UpdateDashboard();
        });
    }

    // UI Event Handlers
    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle tab changes if needed
        if (e.Source is System.Windows.Controls.TabControl tabControl)
        {
            var selectedTab = tabControl.SelectedItem as System.Windows.Controls.TabItem;
            Console.WriteLine($"Switched to tab: {selectedTab?.Header}");
        }
    }

    private void LockDashboard_Click(object sender, RoutedEventArgs e)
    {
        // Lock the dashboard and require master password re-authentication
        Hide();
        WindowState = WindowState.Minimized;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to exit the Parent Dashboard?\n\nThis will stop all browser monitoring and parental controls.",
            "Exit Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _isClosing = true;
            Close();
        }
    }

    private async void BlockAllBrowsers_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = System.Windows.MessageBox.Show(
                "This will immediately terminate all running browsers and block the Islamic browser.\n\nAre you sure you want to continue?",
                "Block All Browsers", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var blockedCount = 0;

                // Block Islamic browser if running
                var browserStatus = _browserMonitor.GetCurrentStatus();
                if (browserStatus.IsRunning)
                {
                    _browserMonitor.BlockBrowser("Emergency block requested by parent");
                    blockedCount++;
                }

                // Terminate all other browsers
                var runningBrowsers = _otherBrowserMonitor.GetRunningBrowsers();
                foreach (var browser in runningBrowsers)
                {
                    if (await _otherBrowserMonitor.TerminateBrowserAsync(browser.ProcessName, browser.ProcessId))
                    {
                        blockedCount++;
                    }
                }

                // Show result
                var message = blockedCount == 0
                    ? "No browsers were running to block."
                    : $"Successfully blocked {blockedCount} browser(s).";

                _systemTray.ShowNotification("Emergency Block", message, WinForms.ToolTipIcon.Info);

                System.Windows.MessageBox.Show(message, "Block Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Update dashboard immediately
                UpdateDashboard();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error blocking browsers: {ex.Message}",
                "Block Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ManageProfiles_Click(object sender, RoutedEventArgs e)
    {
        // Switch to Profiles tab
        MainTabControl.SelectedIndex = 1;
    }

    private void ViewReports_Click(object sender, RoutedEventArgs e)
    {
        // Switch to Reports tab
        MainTabControl.SelectedIndex = 4;
    }

    private void LaunchBrowser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _browserMonitor.LaunchBrowser();
            _systemTray.ShowNotification("Browser Launched",
                "The Islamic browser has been started.", WinForms.ToolTipIcon.Info);

            // Update dashboard immediately
            UpdateDashboard();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error launching browser: {ex.Message}",
                "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            Hide();
            WindowState = WindowState.Minimized;
        }
        else
        {
            // Cleanup resources
            _uiUpdateTimer?.Stop();
            _uiUpdateTimer?.Dispose();
            _browserMonitor?.StopMonitoring();
            _otherBrowserMonitor?.StopMonitoring();
            _systemTray?.Dispose();
            _context?.Dispose();
        }
    }

    // Profile Management Methods

    /// <summary>
    /// Load and display all profiles
    /// </summary>
    private async Task LoadProfilesAsync()
    {
        try
        {
            Console.WriteLine("Starting LoadProfilesAsync...");
            ProfilesPanel.Children.Clear();
            NoProfilesMessage.Text = "Loading profiles...";
            NoProfilesMessage.Visibility = Visibility.Visible;

            Console.WriteLine("Calling ProfileManagementService.GetAllProfilesAsync...");
            var profiles = await _profileManagementService.GetAllProfilesAsync();
            Console.WriteLine($"Retrieved {profiles.Count} profiles from service");

            if (!profiles.Any())
            {
                Console.WriteLine("No profiles found, showing message");
                NoProfilesMessage.Text = "No profiles found. Click 'Add Profile' to create your first family profile.";
                return;
            }

            Console.WriteLine("Hiding no profiles message and creating profile cards");
            NoProfilesMessage.Visibility = Visibility.Collapsed;

            foreach (var profileInfo in profiles)
            {
                Console.WriteLine($"Creating card for profile: {profileInfo.Profile.Name}");
                var profileCard = CreateProfileCard(profileInfo);
                ProfilesPanel.Children.Add(profileCard);
            }

            Console.WriteLine($"Successfully loaded {profiles.Count} profiles");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading profiles: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            NoProfilesMessage.Text = "Error loading profiles. Please try refreshing.";
        }
    }

    /// <summary>
    /// Create a profile card UI element
    /// </summary>
    private Border CreateProfileCard(ProfileInfo profileInfo)
    {
        var profile = profileInfo.Profile;

        var card = new Border
        {
            Style = (Style)FindResource("CardStyle"),
            Margin = new Thickness(0, 0, 0, 15)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Avatar and basic info
        var avatarPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Top
        };

        var avatarText = new TextBlock
        {
            Text = profile.AvatarIcon ?? "👤",
            FontSize = 32,
            Margin = new Thickness(0, 0, 15, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var infoPanel = new StackPanel();

        var nameText = new TextBlock
        {
            Text = profile.Name,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            Margin = new Thickness(0, 0, 0, 5)
        };

        var statusText = new TextBlock
        {
            Text = GetProfileStatusText(profile, profileInfo),
            FontSize = 12,
            Foreground = GetProfileStatusColor(profile, profileInfo),
            Margin = new Thickness(0, 0, 0, 5)
        };

        var descriptionText = new TextBlock
        {
            Text = profile.Description ?? $"{profile.GetAgeCategory()} • {profile.FilteringLevel} Filtering",
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.LightGray),
            TextWrapping = TextWrapping.Wrap
        };

        infoPanel.Children.Add(nameText);
        infoPanel.Children.Add(statusText);
        infoPanel.Children.Add(descriptionText);

        avatarPanel.Children.Add(avatarText);
        avatarPanel.Children.Add(infoPanel);

        Grid.SetColumn(avatarPanel, 0);

        // Usage statistics
        var statsPanel = new StackPanel
        {
            Margin = new Thickness(20, 0, 20, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var todayUsageText = new TextBlock
        {
            Text = $"Today: {profileInfo.FormattedTodayUsage}",
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.LightBlue),
            Margin = new Thickness(0, 0, 0, 3)
        };

        var weeklyUsageText = new TextBlock
        {
            Text = $"This week: {profileInfo.FormattedWeeklyUsage}",
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.LightBlue),
            Margin = new Thickness(0, 0, 0, 3)
        };

        var lastUsedText = new TextBlock
        {
            Text = $"Last used: {profileInfo.FormattedLastUsed}",
            FontSize = 10,
            Foreground = new SolidColorBrush(Colors.Gray)
        };

        statsPanel.Children.Add(todayUsageText);
        statsPanel.Children.Add(weeklyUsageText);
        statsPanel.Children.Add(lastUsedText);

        Grid.SetColumn(statsPanel, 1);

        // Action buttons
        var buttonPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var deleteButton = new System.Windows.Controls.Button
        {
            Content = "🗑️ Delete",
            Style = (Style)FindResource("ModernButtonStyle"),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 47, 47)), // Red
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(12, 6, 12, 6),
            FontSize = 11,
            IsEnabled = !profile.IsDefault
        };
        deleteButton.Click += (s, e) => DeleteProfile_Click(profile.Id, profile.Name);

        var switchButton = new System.Windows.Controls.Button
        {
            Content = profileInfo.IsCurrentlyActive ? "🔄 Active" : "🔄 Switch",
            Style = (Style)FindResource("ModernButtonStyle"),
            Background = profileInfo.IsCurrentlyActive
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)) // Green
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)), // Blue
            Padding = new Thickness(12, 6, 12, 6),
            FontSize = 11,
            IsEnabled = profile.Status == ProfileStatus.Active
        };
        switchButton.Click += (s, e) => SwitchProfile_Click(profile.Id, profile.Name);

        buttonPanel.Children.Add(deleteButton);
        buttonPanel.Children.Add(switchButton);

        Grid.SetColumn(buttonPanel, 2);

        grid.Children.Add(avatarPanel);
        grid.Children.Add(statsPanel);
        grid.Children.Add(buttonPanel);

        card.Child = grid;
        return card;
    }

    private string GetProfileStatusText(UserProfile profile, ProfileInfo profileInfo)
    {
        if (profileInfo.IsCurrentlyActive)
            return "🟢 Currently Active";

        return profile.Status switch
        {
            ProfileStatus.Active => "⚪ Ready",
            ProfileStatus.Disabled => "🟡 Disabled",
            ProfileStatus.Suspended => "🔴 Suspended",
            ProfileStatus.Archived => "⚫ Archived",
            _ => "❓ Unknown"
        };
    }

    private SolidColorBrush GetProfileStatusColor(UserProfile profile, ProfileInfo profileInfo)
    {
        if (profileInfo.IsCurrentlyActive)
            return new SolidColorBrush(Colors.LightGreen);

        return profile.Status switch
        {
            ProfileStatus.Active => new SolidColorBrush(Colors.LightBlue),
            ProfileStatus.Disabled => new SolidColorBrush(Colors.Orange),
            ProfileStatus.Suspended => new SolidColorBrush(Colors.Red),
            ProfileStatus.Archived => new SolidColorBrush(Colors.Gray),
            _ => new SolidColorBrush(Colors.White)
        };
    }

    // Profile Management Event Handlers

    private async void AddProfile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ProfileWizardDialog(_profileManagementService);
            var result = dialog.ShowDialog();

            if (result == true && dialog.ProfileCreated)
            {
                // Refresh the profiles list
                await LoadProfilesAsync();

                System.Windows.MessageBox.Show($"Profile '{dialog.ProfileRequest.Name}' has been created successfully!",
                    "Profile Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"An error occurred while opening the profile creation dialog: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshProfiles_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadProfilesAsync();
    }

    private void EditProfile_Click(int profileId)
    {
        // TODO: Open Edit Profile dialog
        System.Windows.MessageBox.Show($"Edit Profile dialog for ID {profileId} - Coming in next update!",
            "Profile Management", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void DeleteProfile_Click(int profileId, string profileName)
    {
        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete the profile '{profileName}'?\n\nThis action cannot be undone and will remove all associated data.",
            "Delete Profile", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var (success, message) = await _profileManagementService.DeleteProfileAsync(profileId);

            if (success)
            {
                System.Windows.MessageBox.Show("Profile deleted successfully.",
                    "Profile Management", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadProfilesAsync();
            }
            else
            {
                System.Windows.MessageBox.Show($"Error deleting profile: {message}",
                    "Profile Management", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void SwitchProfile_Click(int profileId, string profileName)
    {
        try
        {
            var dialog = new ProfileSwitchDialog(_profileManagementService);
            var result = dialog.ShowDialog();

            if (result == true && dialog.ProfileSwitched)
            {
                // Refresh the profiles list to show updated active status
                await LoadProfilesAsync();

                // Update dashboard to reflect new active profile
                UpdateDashboard();

                System.Windows.MessageBox.Show($"Successfully switched to profile '{dialog.NewActiveProfile?.Name}'!",
                    "Profile Switched", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error opening profile switch dialog: {ex.Message}",
                "Profile Management", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region Settings Navigation

    public string CurrentSettingsPanel
    {
        get => _currentSettingsPanel;
        set
        {
            _currentSettingsPanel = value;
            UpdateNavigationButtonStyles();
        }
    }

    private void NavPrayerSettings_Click(object sender, RoutedEventArgs e)
    {
        ShowSettingsPanel("PrayerSettings");
    }

    private void NavContentFiltering_Click(object sender, RoutedEventArgs e)
    {
        ShowSettingsPanel("ContentFiltering");
    }

    private void NavGeneralSettings_Click(object sender, RoutedEventArgs e)
    {
        ShowSettingsPanel("GeneralSettings");
    }

    private void NavSettingsManagement_Click(object sender, RoutedEventArgs e)
    {
        ShowSettingsPanel("SettingsManagement");
    }

    private void ShowSettingsPanel(string panelName)
    {
        try
        {
            CurrentSettingsPanel = panelName;

            System.Windows.Controls.UserControl? content = panelName switch
            {
                "PrayerSettings" => new Controls.PrayerTimeSettingsControl(_settingsService, _prayerTimeService),
                "ContentFiltering" => CreateContentFilteringPanel(),
                "GeneralSettings" => CreateGeneralSettingsPanel(),
                "SettingsManagement" => CreateSettingsManagementPanel(),
                _ => null
            };

            if (content != null)
            {
                SettingsContentPresenter.Content = content;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading settings panel: {ex.Message}",
                "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateNavigationButtonStyles()
    {
        // Update button styles based on current panel
        var buttons = new[]
        {
            (NavPrayerSettings, "PrayerSettings"),
            (NavContentFiltering, "ContentFiltering"),
            (NavGeneralSettings, "GeneralSettings"),
            (NavSettingsManagement, "SettingsManagement")
        };

        foreach (var (button, panelName) in buttons)
        {
            if (panelName == CurrentSettingsPanel)
            {
                button.Background = (System.Windows.Media.Brush)FindResource("IslamicGreenBrush");
                button.FontWeight = FontWeights.Bold;
            }
            else
            {
                button.Background = System.Windows.Media.Brushes.Transparent;
                button.FontWeight = FontWeights.Normal;
            }
        }
    }

    private System.Windows.Controls.UserControl CreateContentFilteringPanel()
    {
        return new Controls.ContentFilteringSettingsControl(_settingsService);
    }

    private System.Windows.Controls.UserControl CreateGeneralSettingsPanel()
    {
        return new Controls.GeneralSettingsControl(_settingsService, _context);
    }



    private System.Windows.Controls.UserControl CreateSettingsManagementPanel()
    {
        var panel = new System.Windows.Controls.UserControl();
        var stackPanel = new StackPanel();

        var header = new TextBlock
        {
            Text = "💾 Settings Management",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = (System.Windows.Media.Brush)FindResource("IslamicGoldBrush"),
            Margin = new Thickness(0, 0, 0, 20)
        };
        stackPanel.Children.Add(header);

        // Export/Import/Reset buttons
        var buttonPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 20, 0, 0) };

        var exportButton = new System.Windows.Controls.Button
        {
            Content = "📤 Export Settings",
            Style = (Style)FindResource("ModernButtonStyle"),
            Margin = new Thickness(0, 0, 10, 0)
        };
        exportButton.Click += ExportSettings_Click;
        buttonPanel.Children.Add(exportButton);

        var importButton = new System.Windows.Controls.Button
        {
            Content = "📥 Import Settings",
            Style = (Style)FindResource("ModernButtonStyle"),
            Margin = new Thickness(0, 0, 10, 0)
        };
        importButton.Click += ImportSettings_Click;
        buttonPanel.Children.Add(importButton);

        var resetButton = new System.Windows.Controls.Button
        {
            Content = "🔄 Reset to Defaults",
            Style = (Style)FindResource("SecondaryButtonStyle")
        };
        resetButton.Click += ResetSettings_Click;
        buttonPanel.Children.Add(resetButton);

        stackPanel.Children.Add(buttonPanel);

        panel.Content = stackPanel;
        return panel;
    }

    private async void ExportSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Settings",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"NoorAhlulBayt_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var success = await _settingsService.ExportSettingsAsync(saveDialog.FileName);
                if (success)
                {
                    System.Windows.MessageBox.Show($"Settings exported successfully to:\n{saveDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to export settings. Please try again.",
                        "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error exporting settings: {ex.Message}",
                "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ImportSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Settings",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openDialog.ShowDialog() == true)
            {
                var result = System.Windows.MessageBox.Show(
                    "Importing settings will overwrite your current configuration. Do you want to continue?",
                    "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var success = await _settingsService.ImportSettingsAsync(openDialog.FileName);
                    if (success)
                    {
                        System.Windows.MessageBox.Show("Settings imported successfully! The application will restart to apply changes.",
                            "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Restart application
                        System.Windows.Forms.Application.Restart();
                        System.Windows.Application.Current.Shutdown();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to import settings. Please check the file format.",
                            "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error importing settings: {ex.Message}",
                "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ResetSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = System.Windows.MessageBox.Show(
                "This will reset all settings to their default values. This action cannot be undone. Do you want to continue?",
                "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var success = await _settingsService.ResetToDefaultsAsync();
                if (success)
                {
                    System.Windows.MessageBox.Show("Settings have been reset to defaults successfully!",
                        "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to reset settings. Please try again.",
                        "Reset Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error resetting settings: {ex.Message}",
                "Reset Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplySettings_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("All settings are applied automatically when saved. No manual application needed.",
            "Settings Applied", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion
}
