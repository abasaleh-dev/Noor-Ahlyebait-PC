using System.Diagnostics;
using System.Timers;
using System.IO;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;
using NoorAhlulBayt.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace NoorAhlulBayt.Companion.Services;

/// <summary>
/// Service for monitoring the Islamic browser application
/// </summary>
public class BrowserMonitoringService : IDisposable
{
    private readonly System.Timers.Timer _monitoringTimer;
    private readonly ApplicationDbContext _context;
    private readonly TimeTrackingService _timeTrackingService;
    private bool _lastBrowserRunningState = false;
    private bool _isBrowserBlocked = false;
    private DateTime _lastStatusCheck = DateTime.MinValue;
    private UserProfile? _currentProfile;

    // Events
    public event EventHandler<BrowserStatusEventArgs>? BrowserStatusChanged;

    public BrowserMonitoringService(ApplicationDbContext context)
    {
        _context = context;
        _timeTrackingService = new TimeTrackingService(context);
        
        // Monitor every 10 seconds
        _monitoringTimer = new System.Timers.Timer(10000);
        _monitoringTimer.Elapsed += OnMonitoringTimerElapsed;
        _monitoringTimer.AutoReset = true;
    }

    /// <summary>
    /// Start monitoring the browser
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        try
        {
            // Load current profile
            await LoadCurrentProfileAsync();
            
            _monitoringTimer.Start();
            Console.WriteLine("Browser monitoring started");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting browser monitoring: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop monitoring the browser
    /// </summary>
    public void StopMonitoring()
    {
        _monitoringTimer.Stop();
        Console.WriteLine("Browser monitoring stopped");
    }

    /// <summary>
    /// Get current browser status
    /// </summary>
    public BrowserStatus GetCurrentStatus()
    {
        var isRunning = IsBrowserRunning();
        return new BrowserStatus
        {
            IsRunning = isRunning,
            IsBlocked = _isBrowserBlocked,
            LastChecked = _lastStatusCheck,
            ProcessCount = GetBrowserProcessCount()
        };
    }

    /// <summary>
    /// Launch the browser application
    /// </summary>
    public void LaunchBrowser()
    {
        try
        {
            var browserPath = GetBrowserExecutablePath();
            if (File.Exists(browserPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = browserPath,
                    UseShellExecute = true
                });
                Console.WriteLine("Browser launched successfully");
            }
            else
            {
                throw new FileNotFoundException($"Browser executable not found at: {browserPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error launching browser: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Block the browser (kill processes)
    /// </summary>
    public void BlockBrowser(string reason)
    {
        try
        {
            var processes = Process.GetProcessesByName("NoorAhlulBayt.Browser");
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error killing browser process {process.Id}: {ex.Message}");
                }
            }

            _isBrowserBlocked = true;
            Console.WriteLine($"Browser blocked: {reason}");
            
            // Raise event
            BrowserStatusChanged?.Invoke(this, new BrowserStatusEventArgs
            {
                IsRunning = false,
                IsBlocked = true,
                WasRunning = _lastBrowserRunningState,
                Reason = reason
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error blocking browser: {ex.Message}");
        }
    }

    /// <summary>
    /// Unblock the browser
    /// </summary>
    public void UnblockBrowser()
    {
        _isBrowserBlocked = false;
        Console.WriteLine("Browser unblocked");
    }

    /// <summary>
    /// Timer event for monitoring
    /// </summary>
    private async void OnMonitoringTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await CheckBrowserStatusAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during browser monitoring: {ex.Message}");
        }
    }

    /// <summary>
    /// Check browser status and enforce restrictions
    /// </summary>
    private async Task CheckBrowserStatusAsync()
    {
        _lastStatusCheck = DateTime.Now;
        var isCurrentlyRunning = IsBrowserRunning();
        var wasRunning = _lastBrowserRunningState;

        // Check if we need to enforce restrictions
        if (isCurrentlyRunning && _currentProfile != null)
        {
            var shouldBlock = await ShouldBlockBrowserAsync();
            if (shouldBlock.ShouldBlock)
            {
                BlockBrowser(shouldBlock.Reason);
                return;
            }
        }

        // Check for status changes
        if (isCurrentlyRunning != wasRunning)
        {
            BrowserStatusChanged?.Invoke(this, new BrowserStatusEventArgs
            {
                IsRunning = isCurrentlyRunning,
                IsBlocked = _isBrowserBlocked,
                WasRunning = wasRunning,
                Reason = isCurrentlyRunning ? "Browser started" : "Browser stopped"
            });
        }

        _lastBrowserRunningState = isCurrentlyRunning;
    }

    /// <summary>
    /// Check if browser should be blocked based on current restrictions
    /// </summary>
    private async Task<(bool ShouldBlock, string Reason)> ShouldBlockBrowserAsync()
    {
        if (_currentProfile == null)
            return (false, "");

        try
        {
            // Check time window restrictions
            if (_currentProfile.AllowedStartTime.HasValue && _currentProfile.AllowedEndTime.HasValue)
            {
                if (!_timeTrackingService.IsWithinAllowedHours(_currentProfile))
                {
                    return (true, "Outside allowed time window");
                }
            }

            // Check daily time limits
            if (_currentProfile.DailyTimeLimitMinutes > 0)
            {
                var isLimitExceeded = await _timeTrackingService.IsTimeLimitExceededAsync(_currentProfile);
                if (isLimitExceeded)
                {
                    return (true, "Daily time limit exceeded");
                }
            }

            return (false, "");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking browser restrictions: {ex.Message}");
            return (false, "");
        }
    }

    /// <summary>
    /// Check if the browser is currently running
    /// </summary>
    private bool IsBrowserRunning()
    {
        try
        {
            var processes = Process.GetProcessesByName("NoorAhlulBayt.Browser");
            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the number of browser processes running
    /// </summary>
    private int GetBrowserProcessCount()
    {
        try
        {
            var processes = Process.GetProcessesByName("NoorAhlulBayt.Browser");
            return processes.Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Get the path to the browser executable
    /// </summary>
    private string GetBrowserExecutablePath()
    {
        // Assume browser is in the same directory or a sibling directory
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var browserPath = Path.Combine(currentDir, "..", "NoorAhlulBayt.Browser", "bin", "Debug", "net9.0-windows", "NoorAhlulBayt.Browser.exe");
        
        if (!File.Exists(browserPath))
        {
            // Try relative to current directory
            browserPath = Path.Combine(currentDir, "NoorAhlulBayt.Browser.exe");
        }

        return Path.GetFullPath(browserPath);
    }

    /// <summary>
    /// Load the current user profile
    /// </summary>
    private async Task LoadCurrentProfileAsync()
    {
        try
        {
            _currentProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.IsDefault);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading current profile: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
        _timeTrackingService?.Dispose();
    }
}

/// <summary>
/// Browser status information
/// </summary>
public class BrowserStatus
{
    public bool IsRunning { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime LastChecked { get; set; }
    public int ProcessCount { get; set; }
}

/// <summary>
/// Event arguments for browser status changes
/// </summary>
public class BrowserStatusEventArgs : EventArgs
{
    public bool IsRunning { get; set; }
    public bool IsBlocked { get; set; }
    public bool WasRunning { get; set; }
    public string Reason { get; set; } = "";
}
