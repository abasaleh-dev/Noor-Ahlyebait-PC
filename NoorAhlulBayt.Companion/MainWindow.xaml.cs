using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Services;
using NoorAhlulBayt.Companion.Services;
using NoorAhlulBayt.Companion.Dialogs;
using Microsoft.EntityFrameworkCore;

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
    private readonly System.Timers.Timer _uiUpdateTimer;
    private bool _isClosing = false;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize database context
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=companion.db")
            .Options;

        _context = new ApplicationDbContext(options);
        _masterPasswordService = new MasterPasswordService(_context);
        _browserMonitor = new BrowserMonitoringService(_context);
        _otherBrowserMonitor = new OtherBrowserMonitoringService(_context);
        _dashboardService = new DashboardService(_context, _browserMonitor, _otherBrowserMonitor);
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

            // Hide to tray on startup
            WindowState = WindowState.Minimized;
            Hide();
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

                _systemTray.ShowNotification("Emergency Block", message, ToolTipIcon.Info);

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
                "The Islamic browser has been started.", ToolTipIcon.Info);

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
}
