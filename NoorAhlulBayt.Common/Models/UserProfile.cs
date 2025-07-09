using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NoorAhlulBayt.Common.Models;

public class UserProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    // Profile Information
    public int Age { get; set; } = 0; // 0 = not specified

    [MaxLength(50)]
    public string? AvatarIcon { get; set; } = "👤"; // Unicode emoji for avatar

    public FilteringLevel FilteringLevel { get; set; } = FilteringLevel.Child;
    public ProfileStatus Status { get; set; } = ProfileStatus.Active;

    // PIN Protection
    public string? EncryptedPin { get; set; }
    public bool RequirePinForSettings { get; set; } = true;
    public bool RequirePinForSwitching { get; set; } = false;

    // Content Filtering Settings
    public bool EnableProfanityFilter { get; set; } = true;
    public bool EnableNsfwFilter { get; set; } = true;
    public bool EnableAdBlocker { get; set; } = true;
    public bool EnableSafeSearch { get; set; } = true;

    // Advanced Content Filtering
    public ContentFilteringCategories FilteringCategories { get; set; } = ContentFilteringCategories.ChildSafe;
    public int ContentFilteringStrength { get; set; } = 80; // 0-100 scale

    // Time Management
    public TimeRestrictionType TimeRestrictionType { get; set; } = TimeRestrictionType.None;
    public int DailyTimeLimitMinutes { get; set; } = 0; // 0 = no limit
    public TimeSpan? AllowedStartTime { get; set; }
    public TimeSpan? AllowedEndTime { get; set; }
    public DaysOfWeek AllowedDays { get; set; } = DaysOfWeek.All;

    // Break and notification settings
    public int BreakReminderMinutes { get; set; } = 60; // Remind for breaks every hour
    public int WarningBeforeTimeoutMinutes { get; set; } = 10; // Warn 10 minutes before timeout

    // Prayer Time Settings
    public bool EnableAzanBlocking { get; set; } = true;
    public string? City { get; set; }
    public string? Country { get; set; }
    public int AzanBlockingDurationMinutes { get; set; } = 10;

    // Whitelist/Blacklist
    public string WhitelistedDomains { get; set; } = string.Empty; // JSON array
    public string BlacklistedDomains { get; set; } = string.Empty; // JSON array

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    public virtual ICollection<BrowsingHistory> BrowsingHistory { get; set; } = new List<BrowsingHistory>();
    public virtual ICollection<DailyUsageSession> DailyUsageSessions { get; set; } = new List<DailyUsageSession>();
    public virtual ICollection<BrowserAttemptLog> BrowserAttemptLogs { get; set; } = new List<BrowserAttemptLog>();

    // Helper Methods

    /// <summary>
    /// Get age category based on age
    /// </summary>
    public string GetAgeCategory()
    {
        return Age switch
        {
            >= 0 and <= 7 => "Young Child",
            >= 8 and <= 12 => "Child",
            >= 13 and <= 17 => "Teen",
            >= 18 => "Adult",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get recommended filtering level based on age
    /// </summary>
    public FilteringLevel GetRecommendedFilteringLevel()
    {
        return Age switch
        {
            >= 0 and <= 12 => FilteringLevel.Child,
            >= 13 and <= 17 => FilteringLevel.Teen,
            >= 18 => FilteringLevel.Adult,
            _ => FilteringLevel.Child // Default to strictest
        };
    }

    /// <summary>
    /// Check if profile has time restrictions active today
    /// </summary>
    public bool HasTimeRestrictionsToday()
    {
        if (TimeRestrictionType == TimeRestrictionType.None) return false;

        var today = (DaysOfWeek)(1 << (int)DateTime.Today.DayOfWeek);
        return AllowedDays.HasFlag(today);
    }

    /// <summary>
    /// Check if current time is within allowed hours
    /// </summary>
    public bool IsCurrentTimeAllowed()
    {
        if (!HasTimeRestrictionsToday()) return true;
        if (TimeRestrictionType == TimeRestrictionType.DailyLimit) return true;

        var now = DateTime.Now.TimeOfDay;

        if (AllowedStartTime.HasValue && AllowedEndTime.HasValue)
        {
            var start = AllowedStartTime.Value;
            var end = AllowedEndTime.Value;

            // Handle overnight periods (e.g., 22:00 to 06:00)
            if (start > end)
            {
                return now >= start || now <= end;
            }
            else
            {
                return now >= start && now <= end;
            }
        }

        return true;
    }

    /// <summary>
    /// Get whitelisted domains as list
    /// </summary>
    public List<string> GetWhitelistedDomains()
    {
        if (string.IsNullOrEmpty(WhitelistedDomains)) return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(WhitelistedDomains) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Get blacklisted domains as list
    /// </summary>
    public List<string> GetBlacklistedDomains()
    {
        if (string.IsNullOrEmpty(BlacklistedDomains)) return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(BlacklistedDomains) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Set whitelisted domains from list
    /// </summary>
    public void SetWhitelistedDomains(List<string> domains)
    {
        WhitelistedDomains = JsonSerializer.Serialize(domains);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set blacklisted domains from list
    /// </summary>
    public void SetBlacklistedDomains(List<string> domains)
    {
        BlacklistedDomains = JsonSerializer.Serialize(domains);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Apply filtering level preset
    /// </summary>
    public void ApplyFilteringLevelPreset()
    {
        switch (FilteringLevel)
        {
            case FilteringLevel.Child:
                FilteringCategories = ContentFilteringCategories.ChildSafe;
                ContentFilteringStrength = 90;
                EnableProfanityFilter = true;
                EnableNsfwFilter = true;
                EnableSafeSearch = true;
                break;

            case FilteringLevel.Teen:
                FilteringCategories = ContentFilteringCategories.TeenSafe;
                ContentFilteringStrength = 70;
                EnableProfanityFilter = true;
                EnableNsfwFilter = true;
                EnableSafeSearch = true;
                break;

            case FilteringLevel.Adult:
                FilteringCategories = ContentFilteringCategories.AdultMinimal;
                ContentFilteringStrength = 50;
                EnableProfanityFilter = false;
                EnableNsfwFilter = true;
                EnableSafeSearch = false;
                break;

            case FilteringLevel.Custom:
                // Keep existing settings
                break;
        }

        UpdatedAt = DateTime.UtcNow;
    }
}