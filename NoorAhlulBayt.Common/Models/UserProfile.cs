using System.ComponentModel.DataAnnotations;

namespace NoorAhlulBayt.Common.Models;

public class UserProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    // PIN Protection
    public string? EncryptedPin { get; set; }
    public bool RequirePinForSettings { get; set; } = true;

    // Content Filtering Settings
    public bool EnableProfanityFilter { get; set; } = true;
    public bool EnableNsfwFilter { get; set; } = true;
    public bool EnableAdBlocker { get; set; } = true;
    public bool EnableSafeSearch { get; set; } = true;

    // Time Management
    public int DailyTimeLimitMinutes { get; set; } = 0; // 0 = no limit
    public TimeSpan? AllowedStartTime { get; set; }
    public TimeSpan? AllowedEndTime { get; set; }

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
}