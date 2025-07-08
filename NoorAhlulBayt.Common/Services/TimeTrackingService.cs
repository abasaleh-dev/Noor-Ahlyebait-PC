using Microsoft.EntityFrameworkCore;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;

namespace NoorAhlulBayt.Common.Services;

/// <summary>
/// Service for tracking daily usage time and enforcing time limits
/// </summary>
public class TimeTrackingService
{
    private readonly ApplicationDbContext _context;
    private DailyUsageSession? _currentSession;
    private readonly System.Timers.Timer _updateTimer;
    private DateTime _sessionStartTime;

    public TimeTrackingService(ApplicationDbContext context)
    {
        _context = context;
        
        // Update session every minute
        _updateTimer = new System.Timers.Timer(60000); // 60 seconds
        _updateTimer.Elapsed += UpdateCurrentSession;
        _updateTimer.AutoReset = true;
    }

    /// <summary>
    /// Start tracking time for a user profile
    /// </summary>
    /// <param name="userProfileId">User profile ID</param>
    public async Task StartTrackingAsync(int userProfileId)
    {
        try
        {
            // End any existing active sessions for this profile
            await EndActiveSessionsAsync(userProfileId);

            // Create new session for today
            _currentSession = new DailyUsageSession
            {
                UserProfileId = userProfileId,
                Date = DateTime.Today,
                SessionStart = DateTime.Now,
                IsActive = true
            };

            _context.DailyUsageSessions.Add(_currentSession);
            await _context.SaveChangesAsync();

            _sessionStartTime = DateTime.Now;
            _updateTimer.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting time tracking: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop tracking time for the current session
    /// </summary>
    public async Task StopTrackingAsync()
    {
        try
        {
            _updateTimer.Stop();

            if (_currentSession != null)
            {
                _currentSession.EndSession();
                _context.DailyUsageSessions.Update(_currentSession);
                await _context.SaveChangesAsync();
                _currentSession = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping time tracking: {ex.Message}");
        }
    }

    /// <summary>
    /// Get total minutes used today for a user profile
    /// </summary>
    /// <param name="userProfileId">User profile ID</param>
    /// <returns>Total minutes used today</returns>
    public async Task<int> GetTodayUsageMinutesAsync(int userProfileId)
    {
        try
        {
            var today = DateTime.Today;
            var sessions = await _context.DailyUsageSessions
                .Where(s => s.UserProfileId == userProfileId && s.Date == today)
                .ToListAsync();

            var totalMinutes = sessions.Sum(s => s.IsActive ? s.GetCurrentDurationMinutes() : s.DurationMinutes);
            return totalMinutes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting today's usage: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Check if user has exceeded their daily time limit
    /// </summary>
    /// <param name="userProfile">User profile with time limit settings</param>
    /// <returns>True if time limit exceeded</returns>
    public async Task<bool> IsTimeLimitExceededAsync(UserProfile userProfile)
    {
        if (userProfile.DailyTimeLimitMinutes <= 0)
            return false; // No limit set

        var usedMinutes = await GetTodayUsageMinutesAsync(userProfile.Id);
        return usedMinutes >= userProfile.DailyTimeLimitMinutes;
    }

    /// <summary>
    /// Get remaining minutes for today
    /// </summary>
    /// <param name="userProfile">User profile with time limit settings</param>
    /// <returns>Remaining minutes, or -1 if no limit</returns>
    public async Task<int> GetRemainingMinutesAsync(UserProfile userProfile)
    {
        if (userProfile.DailyTimeLimitMinutes <= 0)
            return -1; // No limit

        var usedMinutes = await GetTodayUsageMinutesAsync(userProfile.Id);
        var remaining = userProfile.DailyTimeLimitMinutes - usedMinutes;
        return Math.Max(0, remaining);
    }

    /// <summary>
    /// Check if current time is within allowed hours
    /// </summary>
    /// <param name="userProfile">User profile with time window settings</param>
    /// <returns>True if within allowed hours</returns>
    public bool IsWithinAllowedHours(UserProfile userProfile)
    {
        if (!userProfile.AllowedStartTime.HasValue || !userProfile.AllowedEndTime.HasValue)
            return true; // No time window restriction

        var now = DateTime.Now.TimeOfDay;
        var startTime = userProfile.AllowedStartTime.Value;
        var endTime = userProfile.AllowedEndTime.Value;

        // Handle cases where end time is next day (e.g., 22:00 to 06:00)
        if (startTime <= endTime)
        {
            return now >= startTime && now <= endTime;
        }
        else
        {
            return now >= startTime || now <= endTime;
        }
    }

    /// <summary>
    /// Get time tracking statistics for a user profile
    /// </summary>
    /// <param name="userProfileId">User profile ID</param>
    /// <param name="days">Number of days to look back</param>
    /// <returns>Usage statistics</returns>
    public async Task<TimeTrackingStats> GetUsageStatsAsync(int userProfileId, int days = 7)
    {
        try
        {
            var startDate = DateTime.Today.AddDays(-days + 1);
            var sessions = await _context.DailyUsageSessions
                .Where(s => s.UserProfileId == userProfileId && s.Date >= startDate)
                .GroupBy(s => s.Date)
                .Select(g => new DailyUsage
                {
                    Date = g.Key,
                    TotalMinutes = g.Sum(s => s.IsActive ? s.GetCurrentDurationMinutes() : s.DurationMinutes)
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            return new TimeTrackingStats
            {
                DailyUsages = sessions,
                TotalMinutes = sessions.Sum(s => s.TotalMinutes),
                AverageMinutesPerDay = sessions.Any() ? sessions.Average(s => s.TotalMinutes) : 0
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting usage stats: {ex.Message}");
            return new TimeTrackingStats();
        }
    }

    /// <summary>
    /// End all active sessions for a user profile
    /// </summary>
    private async Task EndActiveSessionsAsync(int userProfileId)
    {
        var activeSessions = await _context.DailyUsageSessions
            .Where(s => s.UserProfileId == userProfileId && s.IsActive)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.EndSession();
        }

        if (activeSessions.Any())
        {
            _context.DailyUsageSessions.UpdateRange(activeSessions);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Timer event to update current session duration
    /// </summary>
    private async void UpdateCurrentSession(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            if (_currentSession != null && _currentSession.IsActive)
            {
                _currentSession.DurationMinutes = _currentSession.GetCurrentDurationMinutes();
                _currentSession.UpdatedAt = DateTime.UtcNow;
                
                _context.DailyUsageSessions.Update(_currentSession);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating session: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}

/// <summary>
/// Time tracking statistics
/// </summary>
public class TimeTrackingStats
{
    public List<DailyUsage> DailyUsages { get; set; } = new();
    public int TotalMinutes { get; set; }
    public double AverageMinutesPerDay { get; set; }
}

/// <summary>
/// Daily usage information
/// </summary>
public class DailyUsage
{
    public DateTime Date { get; set; }
    public int TotalMinutes { get; set; }
}
