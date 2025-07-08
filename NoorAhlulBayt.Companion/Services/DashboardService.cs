using System.Collections.ObjectModel;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace NoorAhlulBayt.Companion.Services;

/// <summary>
/// Service for managing dashboard data and statistics
/// </summary>
public class DashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly BrowserMonitoringService _browserMonitor;
    private readonly OtherBrowserMonitoringService _otherBrowserMonitor;

    public DashboardService(
        ApplicationDbContext context,
        BrowserMonitoringService browserMonitor,
        OtherBrowserMonitoringService otherBrowserMonitor)
    {
        _context = context;
        _browserMonitor = browserMonitor;
        _otherBrowserMonitor = otherBrowserMonitor;
    }

    /// <summary>
    /// Get family status information
    /// </summary>
    public async Task<FamilyStatusInfo> GetFamilyStatusAsync()
    {
        try
        {
            var profiles = await _context.UserProfiles.ToListAsync();
            var activeSessions = await _context.DailyUsageSessions
                .Where(s => s.IsActive && s.Date.Date == DateTime.Today)
                .ToListAsync();

            var browserStatus = _browserMonitor.GetCurrentStatus();
            var runningOtherBrowsers = _otherBrowserMonitor.GetRunningBrowsers();

            return new FamilyStatusInfo
            {
                TotalProfiles = profiles.Count,
                ActiveProfiles = activeSessions.Count,
                IslamicBrowserRunning = browserStatus.IsRunning,
                IslamicBrowserBlocked = browserStatus.IsBlocked,
                OtherBrowsersDetected = runningOtherBrowsers.Count,
                OverallStatus = DetermineOverallStatus(browserStatus, runningOtherBrowsers.Count),
                LastUpdated = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting family status: {ex.Message}");
            return new FamilyStatusInfo
            {
                TotalProfiles = 1,
                ActiveProfiles = 0,
                OverallStatus = "Error",
                LastUpdated = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Get today's usage statistics
    /// </summary>
    public async Task<List<UsageStatistic>> GetTodayUsageAsync()
    {
        try
        {
            var today = DateTime.Today;
            var sessions = await _context.DailyUsageSessions
                .Include(s => s.UserProfile)
                .Where(s => s.Date.Date == today)
                .ToListAsync();

            var statistics = new List<UsageStatistic>();

            foreach (var session in sessions)
            {
                var totalMinutes = session.IsActive
                    ? (int)(DateTime.Now - session.SessionStart).TotalMinutes
                    : session.DurationMinutes;

                statistics.Add(new UsageStatistic
                {
                    ProfileName = session.UserProfile?.Name ?? "Default",
                    TotalMinutes = totalMinutes,
                    IsActive = session.IsActive,
                    StartTime = session.SessionStart,
                    LastActivity = session.UpdatedAt
                });
            }

            // Add default if no sessions
            if (!statistics.Any())
            {
                statistics.Add(new UsageStatistic
                {
                    ProfileName = "Default Profile",
                    TotalMinutes = 0,
                    IsActive = false,
                    StartTime = DateTime.Now,
                    LastActivity = DateTime.Now
                });
            }

            return statistics;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting usage statistics: {ex.Message}");
            return new List<UsageStatistic>
            {
                new UsageStatistic
                {
                    ProfileName = "Default Profile",
                    TotalMinutes = 0,
                    IsActive = false,
                    StartTime = DateTime.Now,
                    LastActivity = DateTime.Now
                }
            };
        }
    }

    /// <summary>
    /// Get recent activity events
    /// </summary>
    public async Task<List<ActivityEvent>> GetRecentActivityAsync(int maxEvents = 10)
    {
        try
        {
            var activities = new List<ActivityEvent>();

            // Get recent browser attempt logs
            var browserAttempts = await _context.BrowserAttemptLogs
                .OrderByDescending(b => b.AttemptTime)
                .Take(maxEvents / 2)
                .ToListAsync();

            foreach (var attempt in browserAttempts)
            {
                var icon = attempt.Action switch
                {
                    "DETECTED" => "🔍",
                    "TERMINATED" => "🔴",
                    "BLOCKED" => "🚫",
                    "ALLOWED" => "✅",
                    _ => "📊"
                };

                var message = attempt.Action switch
                {
                    "DETECTED" => $"{attempt.BrowserName} detected",
                    "TERMINATED" => $"{attempt.BrowserName} automatically closed",
                    "BLOCKED" => $"{attempt.BrowserName} blocked by parental controls",
                    "ALLOWED" => $"{attempt.BrowserName} allowed to run",
                    _ => $"{attempt.BrowserName} - {attempt.Action}"
                };

                activities.Add(new ActivityEvent
                {
                    Icon = icon,
                    Message = message,
                    Timestamp = attempt.AttemptTime.ToLocalTime(),
                    Type = attempt.Action,
                    Severity = GetSeverityForAction(attempt.Action)
                });
            }

            // Add current browser monitoring status
            var browserStatus = _browserMonitor.GetCurrentStatus();
            var runningBrowsers = _otherBrowserMonitor.GetRunningBrowsers();

            if (browserStatus.IsRunning)
            {
                activities.Add(new ActivityEvent
                {
                    Icon = "✅",
                    Message = "Islamic browser is running safely",
                    Timestamp = DateTime.Now,
                    Type = "STATUS",
                    Severity = "Good"
                });
            }

            if (runningBrowsers.Any())
            {
                foreach (var browser in runningBrowsers.Take(3))
                {
                    activities.Add(new ActivityEvent
                    {
                        Icon = "⚠️",
                        Message = $"{browser.DisplayName} detected and being monitored",
                        Timestamp = DateTime.Now,
                        Type = "MONITORING",
                        Severity = "Warning"
                    });
                }
            }
            else if (_otherBrowserMonitor.IsMonitoringEnabled)
            {
                activities.Add(new ActivityEvent
                {
                    Icon = "🛡️",
                    Message = "No unauthorized browsers detected",
                    Timestamp = DateTime.Now,
                    Type = "STATUS",
                    Severity = "Good"
                });
            }

            return activities
                .OrderByDescending(a => a.Timestamp)
                .Take(maxEvents)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting recent activity: {ex.Message}");
            return new List<ActivityEvent>
            {
                new ActivityEvent
                {
                    Icon = "📊",
                    Message = "Dashboard monitoring active",
                    Timestamp = DateTime.Now,
                    Type = "STATUS",
                    Severity = "Good"
                }
            };
        }
    }

    /// <summary>
    /// Get quick action status
    /// </summary>
    public QuickActionStatus GetQuickActionStatus()
    {
        var browserStatus = _browserMonitor.GetCurrentStatus();
        var runningBrowsers = _otherBrowserMonitor.GetRunningBrowsers();

        return new QuickActionStatus
        {
            CanBlockBrowsers = runningBrowsers.Any() || browserStatus.IsRunning,
            CanLaunchBrowser = !browserStatus.IsRunning,
            MonitoringEnabled = _otherBrowserMonitor.IsMonitoringEnabled,
            AutoTerminateEnabled = _otherBrowserMonitor.IsAutoTerminationEnabled,
            TotalThreats = runningBrowsers.Count
        };
    }

    private string DetermineOverallStatus(BrowserStatus browserStatus, int otherBrowserCount)
    {
        if (browserStatus.IsBlocked)
            return "Blocked";
        
        if (otherBrowserCount > 0)
            return "Monitoring";
        
        if (browserStatus.IsRunning)
            return "Active & Safe";
        
        return "Protected";
    }

    private string GetSeverityForAction(string action)
    {
        return action switch
        {
            "DETECTED" => "Warning",
            "TERMINATED" => "Good",
            "BLOCKED" => "Good",
            "ALLOWED" => "Warning",
            _ => "Info"
        };
    }
}

/// <summary>
/// Family status information
/// </summary>
public class FamilyStatusInfo
{
    public int TotalProfiles { get; set; }
    public int ActiveProfiles { get; set; }
    public bool IslamicBrowserRunning { get; set; }
    public bool IslamicBrowserBlocked { get; set; }
    public int OtherBrowsersDetected { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Usage statistic for a profile
/// </summary>
public class UsageStatistic
{
    public string ProfileName { get; set; } = string.Empty;
    public int TotalMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastActivity { get; set; }
    
    public string FormattedDuration => 
        TotalMinutes < 60 
            ? $"{TotalMinutes}m" 
            : $"{TotalMinutes / 60}h {TotalMinutes % 60}m";
}

/// <summary>
/// Activity event for the dashboard feed
/// </summary>
public class ActivityEvent
{
    public string Icon { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    
    public string FormattedTime => 
        DateTime.Now - Timestamp < TimeSpan.FromMinutes(1) 
            ? "Just now" 
            : DateTime.Now - Timestamp < TimeSpan.FromHours(1)
                ? $"{(int)(DateTime.Now - Timestamp).TotalMinutes} min ago"
                : Timestamp.ToString("HH:mm");
}

/// <summary>
/// Quick action status information
/// </summary>
public class QuickActionStatus
{
    public bool CanBlockBrowsers { get; set; }
    public bool CanLaunchBrowser { get; set; }
    public bool MonitoringEnabled { get; set; }
    public bool AutoTerminateEnabled { get; set; }
    public int TotalThreats { get; set; }
}
