using System.Diagnostics;
using System.Timers;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace NoorAhlulBayt.Companion.Services;

/// <summary>
/// Service for monitoring and controlling other browsers (Chrome, Firefox, Edge, etc.)
/// </summary>
public class OtherBrowserMonitoringService : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly System.Timers.Timer _monitoringTimer;
    private readonly Dictionary<string, DateTime> _lastDetectionTime;
    private readonly HashSet<string> _whitelistedBrowsers;
    private Settings? _settings;

    // Default browsers to monitor
    private readonly Dictionary<string, string> _browsersToMonitor = new()
    {
        { "chrome", "Google Chrome" },
        { "firefox", "Mozilla Firefox" },
        { "msedge", "Microsoft Edge" },
        { "opera", "Opera Browser" },
        { "brave", "Brave Browser" },
        { "vivaldi", "Vivaldi Browser" },
        { "iexplore", "Internet Explorer" },
        { "safari", "Safari Browser" }
    };

    // Events
    public event EventHandler<BrowserDetectedEventArgs>? BrowserDetected;
    public event EventHandler<BrowserTerminatedEventArgs>? BrowserTerminated;

    public OtherBrowserMonitoringService(ApplicationDbContext context)
    {
        _context = context;
        _lastDetectionTime = new Dictionary<string, DateTime>();
        _whitelistedBrowsers = new HashSet<string>();
        
        // Monitor every 5 seconds for responsive detection
        _monitoringTimer = new System.Timers.Timer(5000);
        _monitoringTimer.Elapsed += OnMonitoringTimerElapsed;
        _monitoringTimer.AutoReset = true;
    }

    /// <summary>
    /// Start monitoring other browsers
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        try
        {
            await LoadSettingsAsync();
            _monitoringTimer.Start();
            Console.WriteLine("Other browser monitoring started");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting other browser monitoring: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop monitoring other browsers
    /// </summary>
    public void StopMonitoring()
    {
        _monitoringTimer.Stop();
        Console.WriteLine("Other browser monitoring stopped");
    }

    /// <summary>
    /// Check if monitoring is enabled
    /// </summary>
    public bool IsMonitoringEnabled => _settings?.MonitorOtherBrowsers ?? true;

    /// <summary>
    /// Check if auto-termination is enabled
    /// </summary>
    public bool IsAutoTerminationEnabled => _settings?.AutoTerminateOtherBrowsers ?? true;

    /// <summary>
    /// Get list of currently running other browsers
    /// </summary>
    public List<BrowserProcessInfo> GetRunningBrowsers()
    {
        var runningBrowsers = new List<BrowserProcessInfo>();

        foreach (var browser in _browsersToMonitor)
        {
            try
            {
                var processes = Process.GetProcessesByName(browser.Key);
                foreach (var process in processes)
                {
                    runningBrowsers.Add(new BrowserProcessInfo
                    {
                        ProcessName = browser.Key,
                        DisplayName = browser.Value,
                        ProcessId = process.Id,
                        StartTime = process.StartTime,
                        IsWhitelisted = _whitelistedBrowsers.Contains(browser.Key)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking {browser.Value}: {ex.Message}");
            }
        }

        return runningBrowsers;
    }

    /// <summary>
    /// Add browser to whitelist
    /// </summary>
    public async Task AddToWhitelistAsync(string browserName)
    {
        try
        {
            _whitelistedBrowsers.Add(browserName.ToLower());
            await UpdateWhitelistSettingsAsync();
            Console.WriteLine($"Added {browserName} to whitelist");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding {browserName} to whitelist: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove browser from whitelist
    /// </summary>
    public async Task RemoveFromWhitelistAsync(string browserName)
    {
        try
        {
            _whitelistedBrowsers.Remove(browserName.ToLower());
            await UpdateWhitelistSettingsAsync();
            Console.WriteLine($"Removed {browserName} from whitelist");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing {browserName} from whitelist: {ex.Message}");
        }
    }

    /// <summary>
    /// Terminate specific browser process
    /// </summary>
    public async Task<bool> TerminateBrowserAsync(string browserName, int? specificProcessId = null)
    {
        try
        {
            var processes = specificProcessId.HasValue 
                ? new[] { Process.GetProcessById(specificProcessId.Value) }
                : Process.GetProcessesByName(browserName);

            var terminatedCount = 0;
            foreach (var process in processes)
            {
                try
                {
                    var displayName = _browsersToMonitor.GetValueOrDefault(browserName, browserName);
                    
                    // Log the termination attempt
                    await LogBrowserAttemptAsync(browserName, displayName, "TERMINATED");
                    
                    process.Kill();
                    process.WaitForExit(5000);
                    terminatedCount++;
                    
                    Console.WriteLine($"Terminated {displayName} (PID: {process.Id})");
                    
                    // Raise event
                    BrowserTerminated?.Invoke(this, new BrowserTerminatedEventArgs
                    {
                        BrowserName = browserName,
                        DisplayName = displayName,
                        ProcessId = process.Id,
                        TerminationTime = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error terminating {browserName} process {process.Id}: {ex.Message}");
                }
            }

            return terminatedCount > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error terminating {browserName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Timer event handler for monitoring
    /// </summary>
    private async void OnMonitoringTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!IsMonitoringEnabled) return;

        try
        {
            await CheckForOtherBrowsersAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during browser monitoring: {ex.Message}");
        }
    }

    /// <summary>
    /// Check for running other browsers and take action
    /// </summary>
    private async Task CheckForOtherBrowsersAsync()
    {
        foreach (var browser in _browsersToMonitor)
        {
            try
            {
                // Skip if whitelisted
                if (_whitelistedBrowsers.Contains(browser.Key)) continue;

                var processes = Process.GetProcessesByName(browser.Key);
                if (processes.Length > 0)
                {
                    // Check if we recently detected this browser (avoid spam)
                    var lastDetection = _lastDetectionTime.GetValueOrDefault(browser.Key, DateTime.MinValue);
                    if (DateTime.Now - lastDetection < TimeSpan.FromSeconds(30))
                    {
                        continue; // Skip if detected recently
                    }

                    _lastDetectionTime[browser.Key] = DateTime.Now;

                    // Log the detection
                    await LogBrowserAttemptAsync(browser.Key, browser.Value, "DETECTED");

                    // Raise detection event
                    BrowserDetected?.Invoke(this, new BrowserDetectedEventArgs
                    {
                        BrowserName = browser.Key,
                        DisplayName = browser.Value,
                        ProcessCount = processes.Length,
                        DetectionTime = DateTime.Now
                    });

                    // Auto-terminate if enabled
                    if (IsAutoTerminationEnabled)
                    {
                        await TerminateBrowserAsync(browser.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking {browser.Value}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Load settings from database
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        try
        {
            _settings = await _context.Settings.FirstOrDefaultAsync();
            if (_settings == null)
            {
                _settings = new Settings();
                _context.Settings.Add(_settings);
                await _context.SaveChangesAsync();
            }

            // Load whitelist
            _whitelistedBrowsers.Clear();
            if (!string.IsNullOrEmpty(_settings.AllowedBrowsers))
            {
                var allowed = _settings.AllowedBrowsers.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var browser in allowed)
                {
                    _whitelistedBrowsers.Add(browser.Trim().ToLower());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Update whitelist settings in database
    /// </summary>
    private async Task UpdateWhitelistSettingsAsync()
    {
        try
        {
            if (_settings != null)
            {
                _settings.AllowedBrowsers = string.Join(",", _whitelistedBrowsers);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating whitelist settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Log browser attempt to database
    /// </summary>
    private async Task LogBrowserAttemptAsync(string browserName, string displayName, string action)
    {
        try
        {
            var log = new BrowserAttemptLog
            {
                BrowserName = displayName,
                ProcessPath = browserName,
                AttemptTime = DateTime.UtcNow,
                Action = action,
                UserProfileId = null // Will be set when profile management is implemented
            };

            _context.BrowserAttemptLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging browser attempt: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
    }
}

/// <summary>
/// Information about a browser process
/// </summary>
public class BrowserProcessInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsWhitelisted { get; set; }
}

/// <summary>
/// Event args for browser detection
/// </summary>
public class BrowserDetectedEventArgs : EventArgs
{
    public string BrowserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ProcessCount { get; set; }
    public DateTime DetectionTime { get; set; }
}

/// <summary>
/// Event args for browser termination
/// </summary>
public class BrowserTerminatedEventArgs : EventArgs
{
    public string BrowserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public DateTime TerminationTime { get; set; }
}
