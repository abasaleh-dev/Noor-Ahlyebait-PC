using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NoorAhlulBayt.Common.Services;
using System.Text.Json;
using System.Windows;

namespace NoorAhlulBayt.Browser.Services;

/// <summary>
/// Service for integrating content filtering with WebView2
/// </summary>
public class WebViewContentFilterService
{
    private readonly ContentFilterService _contentFilterService;
    private readonly ContentInjectionService _contentInjectionService;
    private WebView2? _webView;
    private bool _isInitialized = false;

    // Loop prevention mechanism - less aggressive to allow legitimate navigations
    private string? _lastNavigationUrl;
    private DateTime _lastNavigationTime = DateTime.MinValue;
    private const int NAVIGATION_COOLDOWN_MS = 200; // Reduced cooldown to prevent only rapid loops
    private int _navigationCount = 0;
    private const int MAX_RAPID_NAVIGATIONS = 5; // Allow up to 5 rapid navigations before blocking

    public WebViewContentFilterService(ContentFilterService contentFilterService)
    {
        _contentFilterService = contentFilterService;
        _contentInjectionService = new ContentInjectionService();
    }

    /// <summary>
    /// Initializes the content filter service with a WebView2 control
    /// </summary>
    /// <param name="webView">WebView2 control to integrate with</param>
    public async Task InitializeAsync(WebView2 webView)
    {
        if (_isInitialized) return;

        _webView = webView;

        try
        {
            // Wait for WebView2 to be ready
            await _webView.EnsureCoreWebView2Async();

            // Set up event handlers
            _webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
            _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            _webView.CoreWebView2.DOMContentLoaded += OnDOMContentLoaded;
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            _webView.CoreWebView2.WebResourceRequested += OnWebResourceRequested;

            // Add filter for all resources to enable ad blocking
            _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize content filter: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles navigation starting event
    /// </summary>
    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        // Enhanced loop prevention: Check for rapid successive navigations to the same URL
        var currentTime = DateTime.Now;
        var timeSinceLastNavigation = (currentTime - _lastNavigationTime).TotalMilliseconds;

        if (_lastNavigationUrl == e.Uri && timeSinceLastNavigation < NAVIGATION_COOLDOWN_MS)
        {
            _navigationCount++;
            if (_navigationCount >= MAX_RAPID_NAVIGATIONS)
            {
                Console.WriteLine($"Navigation loop prevented: {e.Uri} (too many rapid navigations: {_navigationCount})");
                e.Cancel = true;
                return;
            }
        }
        else
        {
            // Reset counter for different URLs or after cooldown period
            _navigationCount = 0;
        }

        // Update navigation tracking
        _lastNavigationUrl = e.Uri;
        _lastNavigationTime = currentTime;

        // Note: SafeSearch enforcement moved back to NavigateToUrl method to prevent
        // navigation cancellation issues that were causing page load failures

        // Check if the main navigation URL should be blocked
        var adBlockResult = _contentFilterService.CheckAdBlock(e.Uri);
        if (adBlockResult.IsBlocked)
        {
            e.Cancel = true;
            ShowBlockedPage($"Navigation blocked: {adBlockResult.Reason}");
            return;
        }

        // Check domain whitelist/blacklist
        var domainResult = _contentFilterService.CheckDomain(e.Uri);
        if (domainResult.IsBlocked)
        {
            e.Cancel = true;
            ShowBlockedPage($"Domain blocked: {domainResult.Reason}");
            return;
        }
    }

    /// <summary>
    /// Handles navigation completed event
    /// </summary>
    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess || _webView?.CoreWebView2 == null) return;

        try
        {
            // Note: Removed SafeSearch enforcement from NavigationCompleted to prevent infinite reload loops
            // SafeSearch is now enforced in NavigationStarting event instead
            Console.WriteLine($"Navigation completed successfully for: {_webView.CoreWebView2.Source}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in navigation completed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles DOM content loaded event
    /// </summary>
    private async void OnDOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
    {
        if (_webView?.CoreWebView2 == null) return;

        try
        {
            // Inject content filter JavaScript
            var filterScript = _contentInjectionService.GetContentFilterScript();
            await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(filterScript);

            // Inject content filter styles
            var filterStyles = _contentInjectionService.GetContentFilterStyles();
            await _webView.CoreWebView2.ExecuteScriptAsync($"document.head.insertAdjacentHTML('beforeend', `{filterStyles}`);");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error injecting content filter: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles web messages from injected JavaScript
    /// </summary>
    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var messageJson = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(messageJson)) return;

            var message = JsonSerializer.Deserialize<ContentFilterMessage>(messageJson);
            if (message == null || message.Type != "contentFilter") return;

            FilterResult result;

            switch (message.SubType?.ToLowerInvariant())
            {
                case "nsfw":
                    result = await _contentFilterService.CheckNsfwImageAsync(message.Url ?? "");
                    break;

                case "video":
                    // For now, use basic URL filtering for videos
                    result = _contentFilterService.CheckProfanity(message.Url ?? "");
                    break;

                case "adblock":
                    result = _contentFilterService.CheckAdBlock(message.Url ?? "");
                    break;

                default:
                    result = new FilterResult { IsBlocked = false, Reason = "Unknown filter type" };
                    break;
            }

            // Send response back to JavaScript
            var response = new
            {
                messageId = message.MessageId,
                result = new
                {
                    isBlocked = result.IsBlocked,
                    reason = result.Reason,
                    confidence = result.Confidence
                }
            };

            var responseJson = JsonSerializer.Serialize(response);
            _webView.CoreWebView2.PostWebMessageAsString(responseJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling web message: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles web resource requests for ad blocking
    /// </summary>
    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        try
        {
            var url = e.Request.Uri;
            var adBlockResult = _contentFilterService.CheckAdBlock(url);

            if (adBlockResult.IsBlocked)
            {
                // Block the request by creating an empty response
                var environment = _webView?.CoreWebView2.Environment;
                if (environment != null)
                {
                    var response = environment.CreateWebResourceResponse(
                        null, 200, "OK", "");
                    e.Response = response;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in web resource request: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows a blocked page with the given message
    /// </summary>
    private void ShowBlockedPage(string message)
    {
        if (_webView?.CoreWebView2 == null) return;

        var blockedPageHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Content Blocked - Noor AhlulBayt Browser</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            margin: 0;
            padding: 0;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            color: white;
        }}
        .container {{
            text-align: center;
            background: rgba(255, 255, 255, 0.1);
            padding: 40px;
            border-radius: 15px;
            backdrop-filter: blur(10px);
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
            max-width: 500px;
        }}
        .icon {{
            font-size: 64px;
            margin-bottom: 20px;
        }}
        h1 {{
            margin: 0 0 20px 0;
            font-size: 28px;
            font-weight: 300;
        }}
        p {{
            margin: 0 0 30px 0;
            font-size: 16px;
            line-height: 1.6;
            opacity: 0.9;
        }}
        .message {{
            background: rgba(255, 255, 255, 0.1);
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
            font-family: monospace;
            font-size: 14px;
        }}
        button {{
            background: rgba(255, 255, 255, 0.2);
            border: 1px solid rgba(255, 255, 255, 0.3);
            color: white;
            padding: 12px 24px;
            border-radius: 6px;
            cursor: pointer;
            font-size: 16px;
            transition: all 0.3s ease;
        }}
        button:hover {{
            background: rgba(255, 255, 255, 0.3);
            transform: translateY(-2px);
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>🛡️</div>
        <h1>Content Blocked</h1>
        <p>This content has been blocked by the Islamic family-safe browser to protect you and your family.</p>
        <div class='message'>{message}</div>
        <button onclick='history.back()'>Go Back</button>
    </div>
</body>
</html>";

        _webView.NavigateToString(blockedPageHtml);
    }

    /// <summary>
    /// Disposes of resources
    /// </summary>
    public void Dispose()
    {
        if (_webView?.CoreWebView2 != null)
        {
            _webView.CoreWebView2.NavigationStarting -= OnNavigationStarting;
            _webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
            _webView.CoreWebView2.DOMContentLoaded -= OnDOMContentLoaded;
            _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            _webView.CoreWebView2.WebResourceRequested -= OnWebResourceRequested;
        }
    }
}

/// <summary>
/// Message structure for communication between JavaScript and C#
/// </summary>
public class ContentFilterMessage
{
    public string? Type { get; set; }
    public string? SubType { get; set; }
    public string? Url { get; set; }
    public string? MessageId { get; set; }
}
