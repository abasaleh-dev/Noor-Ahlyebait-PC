using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using MaterialDesignThemes.Wpf;
using NoorAhlulBayt.Browser.Services;
using NoorAhlulBayt.Common.Services;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;
using Microsoft.EntityFrameworkCore;
using System.Timers;
using System.Net.Http;

namespace NoorAhlulBayt.Browser;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ApplicationDbContext _context;
    private readonly ContentFilterService _contentFilter;
    private readonly PrayerTimeService _prayerTimeService;
    private readonly WebViewContentFilterService _webViewContentFilter;
    private readonly HttpClient _httpClient;
    private readonly System.Timers.Timer _prayerTimer;
    private UserProfile? _currentProfile;
    private Settings? _appSettings;
    private string _homeUrl = "https://www.google.com";
    private bool _isIncognitoMode = false;
    private bool _navigationBlockedByFilter = false;

    public MainWindow()
    {
        try
        {
            DiagnosticLogger.LogStartupStep("MainWindow constructor started");
            DiagnosticLogger.LogWindowEvent("Constructor", "Initializing MainWindow");

            DiagnosticLogger.LogStartupStep("Calling InitializeComponent");
            InitializeComponent();
            DiagnosticLogger.LogStartupStep("InitializeComponent completed");

            // Log window properties
            DiagnosticLogger.LogWindowEvent("Window Properties", $"Title: {Title}, Width: {Width}, Height: {Height}");
            DiagnosticLogger.LogWindowEvent("Window State", $"WindowState: {WindowState}, Visibility: {Visibility}");

            // Initialize services
            DiagnosticLogger.LogStartupStep("Initializing HttpClient");
            _httpClient = new HttpClient();
            DiagnosticLogger.LogStartupStep("HttpClient initialized");

            // Configure DbContext with SQLite
            DiagnosticLogger.LogStartupStep("Configuring database context");
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var dbPath = "Data Source=noor_browser.db";
            optionsBuilder.UseSqlite(dbPath);
            _context = new ApplicationDbContext(optionsBuilder.Options);
            DiagnosticLogger.LogStartupStep("Database context configured", $"Connection: {dbPath}");

            DiagnosticLogger.LogStartupStep("Initializing ContentFilterService");
            // Initialize content filter with NSFW model path (REQUIRED)
            var nsfwModelPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "AI", "nsfw-model.onnx");

            if (!File.Exists(nsfwModelPath))
            {
                DiagnosticLogger.LogError("Startup", "Critical component missing: NSFW detection model not found",
                    new FileNotFoundException($"Required NSFW model not found at: {nsfwModelPath}"));

                var result = MessageBox.Show(
                    "Critical Component Missing\n\n" +
                    "The NSFW (inappropriate content) detection model is required for this Islamic family-safe browser to function properly.\n\n" +
                    "The model file 'nsfw-model.onnx' was not found in:\n" +
                    $"{nsfwModelPath}\n\n" +
                    "Please download the model from:\n" +
                    "https://github.com/iola1999/nsfw-detect-onnx/releases\n\n" +
                    "Extract and place 'nsfw-model.onnx' in the Models/AI folder, then restart the application.\n\n" +
                    "Would you like to continue without the NSFW model? (NOT RECOMMENDED for family safety)",
                    "Noor-e-AhlulBayt Browser - Critical Component Missing",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    DiagnosticLogger.LogInfo("Startup", "User chose to exit due to missing NSFW model");
                    Application.Current.Shutdown();
                    return;
                }
                else
                {
                    DiagnosticLogger.LogWarning("Startup", "User chose to continue without NSFW model - REDUCED SAFETY");
                }
            }

            _contentFilter = new ContentFilterService(File.Exists(nsfwModelPath) ? nsfwModelPath : null);
            DiagnosticLogger.LogStartupStep("ContentFilterService initialized", $"NSFW Model: {(File.Exists(nsfwModelPath) ? "Loaded" : "Not found - REDUCED SAFETY MODE")}");

            DiagnosticLogger.LogStartupStep("Initializing PrayerTimeService");
            _prayerTimeService = new PrayerTimeService(_httpClient, _context);
            DiagnosticLogger.LogStartupStep("PrayerTimeService initialized");

            DiagnosticLogger.LogStartupStep("Initializing WebViewContentFilterService");
            _webViewContentFilter = new WebViewContentFilterService(_contentFilter);
            DiagnosticLogger.LogStartupStep("WebViewContentFilterService initialized");

            // Initialize prayer timer (check every minute)
            DiagnosticLogger.LogStartupStep("Setting up prayer timer");
            _prayerTimer = new System.Timers.Timer(60000);
            _prayerTimer.Elapsed += PrayerTimer_Elapsed;
            _prayerTimer.Start();
            DiagnosticLogger.LogStartupStep("Prayer timer started", "Interval: 60 seconds");

            // Initialize the browser after window is loaded
            DiagnosticLogger.LogStartupStep("Registering Loaded event handler");
            this.Loaded += MainWindow_Loaded;

            // Register additional window events for debugging
            this.Activated += MainWindow_Activated;
            this.Deactivated += MainWindow_Deactivated;
            this.StateChanged += MainWindow_StateChanged;
            this.SizeChanged += MainWindow_SizeChanged;
            this.LocationChanged += MainWindow_LocationChanged;
            this.IsVisibleChanged += MainWindow_IsVisibleChanged;

            DiagnosticLogger.LogStartupStep("MainWindow constructor completed successfully");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogFatal("MainWindow", "Critical error in constructor", ex);
            MessageBox.Show($"Error initializing browser: {ex.Message}\n\nStack trace: {ex.StackTrace}",
                          "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            DiagnosticLogger.LogWindowEvent("Loaded", "MainWindow loaded event triggered");
            DiagnosticLogger.LogWindowEvent("Window State", $"ActualWidth: {ActualWidth}, ActualHeight: {ActualHeight}");
            DiagnosticLogger.LogWindowEvent("Window State", $"IsVisible: {IsVisible}, IsActive: {IsActive}");
            DiagnosticLogger.LogWindowEvent("Window State", $"WindowState: {WindowState}, Topmost: {Topmost}");

            await InitializeBrowser();

            DiagnosticLogger.LogWindowEvent("Loaded", "MainWindow loaded event completed successfully");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("MainWindow", "Error in Loaded event", ex);
            MessageBox.Show($"Error loading browser: {ex.Message}", "Load Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_Activated(object sender, EventArgs e)
    {
        DiagnosticLogger.LogWindowEvent("Activated", "Window activated");
    }

    private void MainWindow_Deactivated(object sender, EventArgs e)
    {
        DiagnosticLogger.LogWindowEvent("Deactivated", "Window deactivated");
    }

    private void MainWindow_StateChanged(object sender, EventArgs e)
    {
        DiagnosticLogger.LogWindowEvent("StateChanged", $"New state: {WindowState}");
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DiagnosticLogger.LogWindowEvent("SizeChanged", $"New size: {e.NewSize.Width}x{e.NewSize.Height}");
    }

    private void MainWindow_LocationChanged(object sender, EventArgs e)
    {
        DiagnosticLogger.LogWindowEvent("LocationChanged", $"New location: {Left}, {Top}");
    }

    private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DiagnosticLogger.LogWindowEvent("IsVisibleChanged", $"Visible: {e.NewValue}");
    }

    private async Task InitializeBrowser()
    {
        try
        {
            DiagnosticLogger.LogStartupStep("InitializeBrowser started");

            // Ensure database is created
            DiagnosticLogger.LogStartupStep("Creating database if needed");
            await _context.Database.EnsureCreatedAsync();
            DiagnosticLogger.LogStartupStep("Database creation completed");

            // Load current profile and settings
            DiagnosticLogger.LogStartupStep("Loading profile and settings");
            await LoadProfileAndSettings();
            DiagnosticLogger.LogStartupStep("Profile and settings loaded");

            // Check WebView2 availability
            DiagnosticLogger.LogWebView2Event("Checking WebView2 availability");
            try
            {
                var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                DiagnosticLogger.LogWebView2Event("WebView2 version available", version ?? "Unknown");
            }
            catch (Exception versionEx)
            {
                DiagnosticLogger.LogError("WebView2", "Failed to get WebView2 version", versionEx);
            }

            // Initialize WebView2
            DiagnosticLogger.LogWebView2Event("Initializing WebView2 core");
            DiagnosticLogger.LogWebView2Event("WebView2 control state", $"IsLoaded: {WebView.IsLoaded}, Visibility: {WebView.Visibility}");

            await WebView.EnsureCoreWebView2Async();

            DiagnosticLogger.LogWebView2Event("WebView2 core initialized");
            DiagnosticLogger.LogWebView2Event("WebView2 environment", $"BrowserVersionString: {WebView.CoreWebView2.Environment.BrowserVersionString}");
            DiagnosticLogger.LogWebView2Event("WebView2 environment", $"UserDataFolder: {WebView.CoreWebView2.Environment.UserDataFolder}");

            // Configure WebView2 settings
            DiagnosticLogger.LogWebView2Event("Configuring WebView2 settings");
            await ConfigureWebView();
            DiagnosticLogger.LogWebView2Event("WebView2 settings configured");

            // Navigate to home page
            DiagnosticLogger.LogWebView2Event("Navigating to home page", _homeUrl);
            NavigateToHome();

            // Update prayer time display
            DiagnosticLogger.LogStartupStep("Updating prayer time display");
            await UpdatePrayerTimeDisplay();
            DiagnosticLogger.LogStartupStep("Prayer time display updated");

            // Update status
            DiagnosticLogger.LogStartupStep("Updating status bar");
            UpdateStatusBar();
            DiagnosticLogger.LogStartupStep("Status bar updated");

            DiagnosticLogger.LogStartupStep("InitializeBrowser completed successfully");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogFatal("MainWindow", "Failed to initialize browser", ex);
            MessageBox.Show($"Failed to initialize browser: {ex.Message}", "Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadProfileAndSettings()
    {
        // Load default profile
        _currentProfile = await _context.UserProfiles
            .Include(p => p.Bookmarks)
            .Include(p => p.BrowsingHistory)
            .FirstOrDefaultAsync(p => p.IsDefault) ??
            new UserProfile { Name = "Default", IsDefault = true };

        // Load app settings
        _appSettings = await _context.Settings.FirstOrDefaultAsync() ??
            new Settings();

        // Set home URL - using Google as default since no DefaultHomePage property exists
        _homeUrl = "https://www.google.com";
    }

    private async Task ConfigureWebView()
    {
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Starting WebView2 configuration");

        if (WebView.CoreWebView2 == null)
        {
            DiagnosticLogger.LogError("WebView2", "CoreWebView2 is null, cannot configure");
            return;
        }

        var settings = WebView.CoreWebView2.Settings;
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Retrieved WebView2 settings");

        // Configure security settings
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Configuring security settings");
        settings.IsGeneralAutofillEnabled = false; // Always disabled for family safety
        settings.IsPasswordAutosaveEnabled = false; // Always disabled for security
        settings.AreDevToolsEnabled = false; // Disable dev tools for family safety
        settings.AreDefaultContextMenusEnabled = true;
        settings.AreHostObjectsAllowed = false;
        settings.IsWebMessageEnabled = false;

        // Note: Removed CSS injection that was interfering with web page styling
        // WebView2 DefaultBackgroundColor property handles background color properly
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Security settings configured");

        // Block dangerous content
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Configuring content settings");
        settings.IsScriptEnabled = true; // Needed for modern web
        settings.AreDefaultScriptDialogsEnabled = false; // Block alert/confirm dialogs
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Content settings configured");

        // Add content filtering
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Setting up content filtering");
        WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        WebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Content filtering configured");

        // Initialize advanced content filtering with NSFW detection
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Initializing advanced content filtering");
        try
        {
            await _webViewContentFilter.InitializeAsync(WebView);
            DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Advanced content filtering initialized successfully");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("WebView2", "Failed to initialize advanced content filtering", ex);
        }

        // Note: Removed DOMContentLoaded event handler that was causing CSS injection
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Skipping DOMContentLoaded event to prevent styling interference");

        // Handle new window requests
        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "Setting up new window handling");
        WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

        DiagnosticLogger.LogWebView2Event("ConfigureWebView", "WebView2 configuration completed successfully");
    }

    private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        try
        {
            var url = e.Request.Uri;

            // Note: Removed CSS injection code that was causing visual corruption
            // Web resources should load with their original styling

            // Check if URL should be blocked by ad blocker
            if (_contentFilter.ShouldBlockUrl(url))
            {
                e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 200, "OK", "");
                return;
            }

            // Check hardcoded blacklist for critical domains
            var domainResult = _contentFilter.CheckDomain(url);
            if (domainResult.IsBlocked)
            {
                DiagnosticLogger.LogWebView2Event("ContentFilter", $"Resource blocked - hardcoded blacklist: {url}");
                e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 200, "OK", "");
                return;
            }

            // Check user profile blacklist
            if (_currentProfile != null && _contentFilter.IsBlacklisted(url, _currentProfile.BlacklistedDomains))
            {
                DiagnosticLogger.LogWebView2Event("ContentFilter", $"Resource blocked - user blacklist: {url}");
                e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 200, "OK", "");
                return;
            }

            // Check for image resources and apply NSFW filtering
            if (IsImageResource(url))
            {
                // Use synchronous URL-based filtering for performance
                var imageResult = _contentFilter.CheckProfanity(url);
                if (imageResult.IsBlocked)
                {
                    DiagnosticLogger.LogWebView2Event("ContentFilter", $"Image blocked - inappropriate URL: {url}");
                    e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                        null, 200, "OK", "");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't block the request
            Console.WriteLine($"Error in web resource filter: {ex.Message}");
        }
    }

    private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        // Handle new window requests - open in same window for family safety
        e.Handled = true;
        WebView.CoreWebView2.Navigate(e.Uri);
    }

    #region Navigation Methods

    private void NavigateToHome()
    {
        if (WebView.CoreWebView2 != null)
        {
            WebView.CoreWebView2.Navigate(_homeUrl);
        }
    }

    private void NavigateToUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        // Add protocol if missing
        if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://"))
        {
            // Check if it looks like a URL
            if (url.Contains(".") && !url.Contains(" "))
            {
                url = "https://" + url;
            }
            else
            {
                // Treat as search query
                url = $"https://www.google.com/search?q={Uri.EscapeDataString(url)}";
            }
        }

        // Apply SafeSearch if enabled for current profile
        if (_currentProfile?.EnableSafeSearch == true)
        {
            url = _contentFilter.EnforceSafeSearch(url);
        }

        if (WebView.CoreWebView2 != null)
        {
            WebView.CoreWebView2.Navigate(url);
        }
    }

    private void BlockContent(string title, string reason)
    {
        Dispatcher.Invoke(() =>
        {
            _navigationBlockedByFilter = true;
            BlockTitle.Text = title;
            BlockReason.Text = reason;
            BlockOverlay.Visibility = Visibility.Visible;
        });
    }

    private void UnblockContent()
    {
        Dispatcher.Invoke(() =>
        {
            _navigationBlockedByFilter = false;
            BlockOverlay.Visibility = Visibility.Collapsed;
        });
    }

    private bool IsImageResource(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        var lowerUrl = url.ToLowerInvariant();
        return lowerUrl.Contains(".jpg") || lowerUrl.Contains(".jpeg") ||
               lowerUrl.Contains(".png") || lowerUrl.Contains(".gif") ||
               lowerUrl.Contains(".webp") || lowerUrl.Contains(".bmp") ||
               lowerUrl.Contains(".svg") || lowerUrl.Contains("image/");
    }

    #endregion

    #region WebView Event Handlers

    private async void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        try
        {
            var url = e.Uri;
            DiagnosticLogger.LogWebView2Event("NavigationStarting", $"URL: {url}");

            // Reset the navigation blocked flag for new navigation attempts
            _navigationBlockedByFilter = false;
            DiagnosticLogger.LogWebView2Event("NavigationStarting", $"IsUserInitiated: {e.IsUserInitiated}, IsRedirected: {e.IsRedirected}");

            // Show loading overlay
            LoadingOverlay.Visibility = Visibility.Visible;
            DiagnosticLogger.LogWebView2Event("UI", "Loading overlay shown");

            // Update address bar
            AddressBar.Text = url;
            UpdateSecurityIcon(url);
            DiagnosticLogger.LogWebView2Event("UI", "Address bar updated");

            // Check time limits
            if (_currentProfile != null && _currentProfile.AllowedStartTime.HasValue && _currentProfile.AllowedEndTime.HasValue)
            {
                DiagnosticLogger.LogWebView2Event("ContentFilter", "Checking time limits");
                var now = DateTime.Now.TimeOfDay;
                if (now < _currentProfile.AllowedStartTime || now > _currentProfile.AllowedEndTime)
                {
                    e.Cancel = true;
                    DiagnosticLogger.LogWebView2Event("ContentFilter", $"Navigation blocked - outside allowed time: {now}");
                    BlockContent("Time Limit", "Browsing is not allowed at this time.");
                    return;
                }
            }

            // Check if it's prayer time and blocking is enabled
            if (_currentProfile?.EnableAzanBlocking == true && !string.IsNullOrEmpty(_currentProfile.City))
            {
                DiagnosticLogger.LogWebView2Event("ContentFilter", "Checking prayer time blocking");
                var isAzanTime = await _prayerTimeService.IsCurrentlyAzanTimeAsync(
                    _currentProfile.City, _currentProfile.Country ?? "US",
                    _currentProfile.AzanBlockingDurationMinutes);

                if (isAzanTime)
                {
                    e.Cancel = true;
                    DiagnosticLogger.LogWebView2Event("ContentFilter", "Navigation blocked - prayer time");
                    BlockContent("Prayer Time", "Browsing is blocked during Azan. Please take time for prayer.");
                    return;
                }
            }

            // Check hardcoded blacklist first (critical domains)
            var domainResult = _contentFilter.CheckDomain(url);
            if (domainResult.IsBlocked)
            {
                e.Cancel = true;
                DiagnosticLogger.LogWebView2Event("ContentFilter", $"Navigation blocked - hardcoded blacklist: {url}");
                BlockContent("Domain Blocked", $"This website is blocked for family safety: {domainResult.Reason}");
                return;
            }

            // Check user profile blacklist
            if (_currentProfile != null && _contentFilter.IsBlacklisted(url, _currentProfile.BlacklistedDomains))
            {
                e.Cancel = true;
                DiagnosticLogger.LogWebView2Event("ContentFilter", $"Navigation blocked - user blacklist: {url}");
                BlockContent("Domain Blocked", "This website is in your blocked list.");
                return;
            }

            // Check URL for inappropriate content patterns
            var urlContentResult = _contentFilter.CheckProfanity(url);
            if (urlContentResult.IsBlocked)
            {
                e.Cancel = true;
                DiagnosticLogger.LogWebView2Event("ContentFilter", $"Navigation blocked - inappropriate URL content: {url}");
                BlockContent("Inappropriate Content", $"URL contains inappropriate content: {urlContentResult.Reason}");
                return;
            }

            // Update status
            StatusText.Text = $"Loading {url}...";
            DiagnosticLogger.LogWebView2Event("UI", "Status text updated");

            DiagnosticLogger.LogWebView2Event("NavigationStarting", "Completed successfully");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("WebView2", "Error in navigation starting", ex);
        }
    }

    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        try
        {
            DiagnosticLogger.LogWebView2Event("NavigationCompleted", $"IsSuccess: {e.IsSuccess}");
            if (WebView.CoreWebView2 != null)
            {
                DiagnosticLogger.LogWebView2Event("NavigationCompleted", $"Final URL: {WebView.CoreWebView2.Source}");
                DiagnosticLogger.LogWebView2Event("NavigationCompleted", $"Document Title: {WebView.CoreWebView2.DocumentTitle}");
            }

            // Hide loading overlay
            LoadingOverlay.Visibility = Visibility.Collapsed;
            DiagnosticLogger.LogWebView2Event("UI", "Loading overlay hidden");

            if (e.IsSuccess)
            {
                DiagnosticLogger.LogWebView2Event("NavigationCompleted", "Navigation successful");

                // Note: Removed aggressive CSS injection that was causing visual corruption
                // The WebView2 control now uses its natural DefaultBackgroundColor="White" setting
                DiagnosticLogger.LogWebView2Event("CSS", "Skipping CSS injection to prevent web page styling interference");

                // Update navigation buttons
                BackButton.IsEnabled = WebView.CoreWebView2?.CanGoBack ?? false;
                ForwardButton.IsEnabled = WebView.CoreWebView2?.CanGoForward ?? false;
                DiagnosticLogger.LogWebView2Event("UI", $"Navigation buttons updated - Back: {BackButton.IsEnabled}, Forward: {ForwardButton.IsEnabled}");

                // Update address bar with final URL
                if (WebView.CoreWebView2 != null)
                {
                    AddressBar.Text = WebView.CoreWebView2.Source;
                    UpdateSecurityIcon(WebView.CoreWebView2.Source);
                    DiagnosticLogger.LogWebView2Event("UI", "Address bar updated with final URL");
                }

                // Save to browsing history (if not incognito)
                if (!_isIncognitoMode && _currentProfile != null && WebView.CoreWebView2 != null)
                {
                    DiagnosticLogger.LogWebView2Event("History", "Saving to browsing history");
                    await SaveToHistory(WebView.CoreWebView2.Source, WebView.CoreWebView2.DocumentTitle);
                }
                else
                {
                    DiagnosticLogger.LogWebView2Event("History", $"Not saving to history - Incognito: {_isIncognitoMode}, Profile: {_currentProfile?.Name ?? "None"}");
                }

                // Check page content for profanity after page loads
                DiagnosticLogger.LogWebView2Event("ContentFilter", "Starting page content check");
                await Task.Delay(1000); // Wait for page to fully load
                await CheckPageContent();

                StatusText.Text = "Ready";
                DiagnosticLogger.LogWebView2Event("UI", "Status set to Ready");
            }
            else
            {
                DiagnosticLogger.LogWebView2Event("NavigationCompleted", $"Navigation failed - WebErrorStatus: {e.WebErrorStatus}");

                // Don't show generic error message if navigation was blocked by content filter
                if (!_navigationBlockedByFilter)
                {
                    StatusText.Text = "Failed to load page";
                    MessageBox.Show("Failed to load the webpage.", "Navigation Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    StatusText.Text = "Content blocked for your protection";
                    DiagnosticLogger.LogWebView2Event("NavigationCompleted", "Navigation blocked by content filter - showing block overlay instead of error message");
                }
            }

            DiagnosticLogger.LogWebView2Event("NavigationCompleted", "Event handling completed");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("WebView2", "Error in navigation completed", ex);
        }
    }

    private async void WebView_DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
    {
        try
        {
            DiagnosticLogger.LogWebView2Event("DOMContentLoaded", "DOM content loaded - skipping CSS injection to prevent styling interference");
            // Note: Removed CSS injection that was causing visual corruption
            // Web pages should display with their original styling
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("WebView2", "Error in DOMContentLoaded", ex);
        }
    }

    private async Task CheckPageContent()
    {
        try
        {
            if (WebView.CoreWebView2 == null) return;

            // Check page content for profanity
            var pageContent = await WebView.CoreWebView2.ExecuteScriptAsync("document.body.innerText");
            if (!string.IsNullOrEmpty(pageContent))
            {
                // Remove quotes from script result
                pageContent = pageContent.Trim('"').Replace("\\n", "\n").Replace("\\\"", "\"");

                var filterResult = _contentFilter.CheckProfanity(pageContent);
                if (filterResult.IsBlocked)
                {
                    BlockContent("Inappropriate Content", filterResult.Reason);
                    return;
                }
            }

            // Inject custom CSS for Islamic theme if enabled
            if (_appSettings?.Theme == "Islamic")
            {
                await InjectIslamicTheme();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking page content: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private async Task SaveToHistory(string url, string title)
    {
        try
        {
            if (_currentProfile == null) return;

            var historyEntry = new BrowsingHistory
            {
                UserProfileId = _currentProfile.Id,
                Url = url,
                Title = title,
                VisitedAt = DateTime.Now,
                IsIncognito = _isIncognitoMode
            };

            _context.BrowsingHistory.Add(historyEntry);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving to history: {ex.Message}");
        }
    }

    private async Task InjectIslamicTheme()
    {
        try
        {
            if (WebView.CoreWebView2 == null) return;

            // Apply very subtle Islamic theme that doesn't interfere with website functionality
            // Only apply minimal styling that enhances readability without breaking layouts
            var css = @"
                /* Very subtle Islamic theme - minimal interference */
                body {
                    /* Subtle warm tone filter - much less aggressive */
                    filter: sepia(2%) hue-rotate(15deg) saturate(105%);
                }
                /* Only style generic elements, avoid breaking specific website designs */
                a:not([class]):not([id]) {
                    color: #2E7D32;
                }
            ";

            await WebView.CoreWebView2.ExecuteScriptAsync($@"
                // Only inject if no existing Islamic theme style
                if (!document.getElementById('noor-islamic-theme')) {{
                    var style = document.createElement('style');
                    style.id = 'noor-islamic-theme';
                    style.textContent = `{css}`;
                    document.head.appendChild(style);
                    console.log('Noor Browser: Subtle Islamic theme applied');
                }}
            ");

            DiagnosticLogger.LogWebView2Event("Theme", "Subtle Islamic theme applied");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("Theme", "Error injecting Islamic theme", ex);
        }
    }

    private async void PrayerTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await UpdatePrayerTimeDisplay();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating prayer time: {ex.Message}");
        }
    }

    private async Task UpdatePrayerTimeDisplay()
    {
        try
        {
            if (_currentProfile == null || string.IsNullOrEmpty(_currentProfile.City))
            {
                Dispatcher.Invoke(() => NextPrayerText.Text = "Prayer times not set");
                return;
            }

            var nextPrayer = await _prayerTimeService.GetNextPrayerAsync(
                _currentProfile.City, _currentProfile.Country ?? "US");

            if (nextPrayer.HasValue)
            {
                var timeUntil = nextPrayer.Value.Time - DateTime.Now.TimeOfDay;
                if (timeUntil.TotalSeconds < 0)
                    timeUntil = timeUntil.Add(TimeSpan.FromDays(1));

                Dispatcher.Invoke(() =>
                {
                    NextPrayerText.Text = $"{nextPrayer.Value.PrayerName}: {timeUntil:hh\\:mm}";
                });
            }
            else
            {
                Dispatcher.Invoke(() => NextPrayerText.Text = "Prayer times unavailable");
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => NextPrayerText.Text = "Prayer time error");
            Console.WriteLine($"Error updating prayer display: {ex.Message}");
        }
    }

    private void UpdateStatusBar()
    {
        if (_currentProfile != null)
        {
            ProfileText.Text = $"Profile: {_currentProfile.Name}";
        }

        FilterStatusText.Text = _currentProfile?.EnableProfanityFilter == true ?
            "Filters: Active" : "Filters: Disabled";

        // Update time remaining if time limits are enabled
        if (_currentProfile?.AllowedEndTime.HasValue == true)
        {
            var now = DateTime.Now.TimeOfDay;
            var endTime = _currentProfile.AllowedEndTime.Value;

            if (now < endTime)
            {
                var remaining = endTime - now;
                TimeRemainingText.Text = $"Time: {remaining:hh\\:mm}";
            }
            else
            {
                TimeRemainingText.Text = "Time: Expired";
            }
        }
        else
        {
            TimeRemainingText.Text = "";
        }
    }

    #endregion

    #region UI Event Handlers

    // Navigation Bar Events
    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2?.CanGoBack == true)
        {
            WebView.CoreWebView2.GoBack();
        }
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2?.CanGoForward == true)
        {
            WebView.CoreWebView2.GoForward();
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        WebView.CoreWebView2?.Reload();
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        NavigateToHome();
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateToUrl(AddressBar.Text);
        }
    }

    private void Go_Click(object sender, RoutedEventArgs e)
    {
        NavigateToUrl(AddressBar.Text);
    }

    private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
    {
        // Select all text when address bar gets focus for easy editing
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
    {
        // Update address bar with current URL when focus is lost
        if (WebView.CoreWebView2 != null && !string.IsNullOrEmpty(WebView.CoreWebView2.Source))
        {
            AddressBar.Text = WebView.CoreWebView2.Source;
        }
    }

    private void AddressBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Update security icon based on URL protocol
        if (sender is TextBox textBox)
        {
            UpdateSecurityIcon(textBox.Text);
        }
    }

    private void UpdateSecurityIcon(string? url)
    {
        var text = url?.ToLower() ?? "";

        if (text.StartsWith("https://"))
        {
            SecurityIcon.Kind = PackIconKind.Lock;
            SecurityIcon.Foreground = new SolidColorBrush(Colors.Green);
            SecurityIcon.ToolTip = "Secure Connection (HTTPS)";
        }
        else if (text.StartsWith("http://"))
        {
            SecurityIcon.Kind = PackIconKind.LockOpenVariant;
            SecurityIcon.Foreground = new SolidColorBrush(Colors.Orange);
            SecurityIcon.ToolTip = "Insecure Connection (HTTP)";
        }
        else if (text.Contains(".") && !text.Contains(" "))
        {
            SecurityIcon.Kind = PackIconKind.Web;
            SecurityIcon.Foreground = (SolidColorBrush)FindResource("IslamicGreenBrush");
            SecurityIcon.ToolTip = "Website";
        }
        else
        {
            SecurityIcon.Kind = PackIconKind.Magnify;
            SecurityIcon.Foreground = (SolidColorBrush)FindResource("IslamicGreenBrush");
            SecurityIcon.ToolTip = "Search Query";
        }
    }

    private void AddressBar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-click anywhere in the address bar to select all text
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
            e.Handled = true;
        }
    }

    private void AddressBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Handle single click behavior - if not focused, focus and select all
        if (sender is TextBox textBox && !textBox.IsFocused)
        {
            textBox.Focus();
            textBox.SelectAll();
            e.Handled = true;
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // Handle Ctrl+L to focus address bar (common browser shortcut)
        if (e.Key == Key.L && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            AddressBar.Focus();
            AddressBar.SelectAll();
            e.Handled = true;
            return;
        }

        // Handle F5 for refresh
        if (e.Key == Key.F5)
        {
            Refresh_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        // Handle Ctrl+R for refresh
        if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            Refresh_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        // Handle Alt+Left for back
        if (e.Key == Key.Left && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            if (BackButton.IsEnabled)
                Back_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        // Handle Alt+Right for forward
        if (e.Key == Key.Right && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            if (ForwardButton.IsEnabled)
                Forward_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        // Handle Alt+Home for home
        if (e.Key == Key.Home && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            Home_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        // Handle Ctrl+D for bookmark
        if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            Bookmark_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        // Handle F11 for fullscreen toggle
        if (e.Key == Key.F11)
        {
            FullScreen_Click(sender, e);
            e.Handled = true;
        }
    }

    private async void Bookmark_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (WebView.CoreWebView2 == null || _currentProfile == null) return;

            var bookmark = new Bookmark
            {
                UserProfileId = _currentProfile.Id,
                Title = WebView.CoreWebView2.DocumentTitle,
                Url = WebView.CoreWebView2.Source,
                CreatedAt = DateTime.Now,
                FolderPath = "Default"
            };

            _context.Bookmarks.Add(bookmark);
            await _context.SaveChangesAsync();

            MessageBox.Show("Bookmark added successfully!", "Bookmark",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to add bookmark: {ex.Message}", "Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Block Overlay Events
    private void GoBack_Click(object sender, RoutedEventArgs e)
    {
        UnblockContent();
        if (WebView.CoreWebView2?.CanGoBack == true)
        {
            WebView.CoreWebView2.GoBack();
        }
        else
        {
            NavigateToHome();
        }
    }

    private void Override_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if PIN is set
            if (_currentProfile == null || string.IsNullOrEmpty(_currentProfile.EncryptedPin))
            {
                MessageBox.Show("No PIN is set. Please set a PIN in settings first.", "Override",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verify PIN
            bool pinVerified = PinVerificationDialog.ShowDialog(this, _currentProfile.EncryptedPin, "override content blocking");
            if (pinVerified)
            {
                UnblockContent();
                StatusText.Text = "Content blocking overridden";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during PIN verification: {ex.Message}", "Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Menu Event Handlers

    // File Menu
    private void NewTab_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement tabbed browsing
        MessageBox.Show("Tabbed browsing not yet implemented.", "New Tab",
                      MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void NewIncognitoTab_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement incognito mode
        MessageBox.Show("Incognito mode not yet implemented.", "Incognito",
                      MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if PIN verification is required
            if (_currentProfile?.RequirePinForSettings == true && !string.IsNullOrEmpty(_currentProfile.EncryptedPin))
            {
                bool pinVerified = PinVerificationDialog.ShowDialog(this, _currentProfile.EncryptedPin, "access settings");
                if (!pinVerified)
                {
                    return; // User cancelled or failed PIN verification
                }
            }

            // Open settings window
            var settingsWindow = new SettingsWindow(_context, _currentProfile!, _appSettings!)
            {
                Owner = this
            };

            var result = settingsWindow.ShowDialog();
            if (result == true)
            {
                // Settings were saved, reload profile and update UI
                await LoadProfileAndSettings();
                UpdateStatusBar();
                await UpdatePrayerTimeDisplay();

                StatusText.Text = "Settings updated successfully";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening settings: {ex.Message}", "Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    // Edit Menu
    private async void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2 != null)
        {
            await WebView.CoreWebView2.ExecuteScriptAsync("document.execCommand('copy')");
        }
    }

    private async void Paste_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2 != null)
        {
            await WebView.CoreWebView2.ExecuteScriptAsync("document.execCommand('paste')");
        }
    }

    private async void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2 != null)
        {
            await WebView.CoreWebView2.ExecuteScriptAsync("document.execCommand('selectAll')");
        }
    }

    // View Menu
    private void FullScreen_Click(object sender, RoutedEventArgs e)
    {
        if (WindowStyle == WindowStyle.None)
        {
            // Exit fullscreen
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            ResizeMode = ResizeMode.CanResize;
        }
        else
        {
            // Enter fullscreen
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
        }
    }

    private void Bookmarks_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open bookmarks manager
        MessageBox.Show("Bookmarks manager not yet implemented.", "Bookmarks",
                      MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void History_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open history window
        MessageBox.Show("History window not yet implemented.", "History",
                      MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // Islamic Menu
    private void PrayerTimes_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open prayer times window
        MessageBox.Show("Prayer times window not yet implemented.", "Prayer Times",
                      MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void QiblaDirection_Click(object sender, RoutedEventArgs e)
    {
        NavigateToUrl("https://qiblafinder.withgoogle.com/");
    }

    private void IslamicResources_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open Islamic resources page
        NavigateToUrl("https://quran.com/");
    }

    // Help Menu
    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Noor-e-AhlulBayt Islamic Family-Safe Browser\n" +
            "Version 1.0.0\n\n" +
            "A family-safe browser with Islamic features including:\n" +
            "• Content filtering and ad blocking\n" +
            "• Prayer time integration\n" +
            "• Time management controls\n" +
            "• Islamic theming\n\n" +
            "Built with love for the Muslim community.",
            "About Noor-e-AhlulBayt Browser",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    #endregion

    #region Cleanup

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            _prayerTimer?.Stop();
            _prayerTimer?.Dispose();
            _httpClient?.Dispose();
            _context?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup: {ex.Message}");
        }

        base.OnClosed(e);
    }

    #endregion

    #region Window Controls

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click to maximize/restore
            MaximizeButton_Click(sender, new RoutedEventArgs());
        }
        else
        {
            // Single click to drag
            this.DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
            MaximizeButton.Content = "🗖";
        }
        else
        {
            this.WindowState = WindowState.Maximized;
            MaximizeButton.Content = "🗗";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    #endregion
}