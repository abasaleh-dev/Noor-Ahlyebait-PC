using System.ComponentModel.DataAnnotations;

namespace NoorAhlulBayt.Common.Models;

public class Settings
{
    [Key]
    public int Id { get; set; }

    // Application Settings
    public string Theme { get; set; } = "Islamic"; // Islamic, Light, Dark
    public string Language { get; set; } = "en"; // en, ar
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;

    // Default Profile
    public int? DefaultProfileId { get; set; }
    public bool RememberLastProfile { get; set; } = true;

    // Admin Settings
    public string? AdminEncryptedPin { get; set; }
    public bool RequireAdminPinForProfileSwitch { get; set; } = false;

    // Master Password Settings (for Companion App)
    public string? MasterPasswordHash { get; set; }
    public string? MasterPasswordSalt { get; set; }
    public bool RequireMasterPassword { get; set; } = true;

    // Companion App Settings
    public bool EnableCompanionApp { get; set; } = true;
    public bool BlockOtherBrowsers { get; set; } = true;
    public bool NotifyOnBrowserAttempt { get; set; } = true;
    public string BlockedBrowsers { get; set; } = "chrome,firefox,msedge,opera,brave"; // comma-separated process names

    // Other Browser Monitoring Settings
    public bool MonitorOtherBrowsers { get; set; } = true;
    public bool AutoTerminateOtherBrowsers { get; set; } = true;
    public string AllowedBrowsers { get; set; } = string.Empty; // comma-separated whitelist

    // Filter Lists
    public string AdBlockFilterListsUrls { get; set; } = "https://easylist.to/easylist/easylist.txt,https://easylist.to/easylist/easyprivacy.txt";
    public DateTime? LastFilterListUpdate { get; set; }
    public bool AutoUpdateFilterLists { get; set; } = true;

    // Prayer Time Settings
    public string? DefaultCity { get; set; }
    public string? DefaultCountry { get; set; }
    public int DefaultCalculationMethod { get; set; } = 2; // Aladhan API method

    // Notifications
    public bool EnableToastNotifications { get; set; } = true;
    public bool NotifyOnTimeLimit { get; set; } = true;
    public bool NotifyOnAzan { get; set; } = true;
    public bool NotifyOnContentBlock { get; set; } = false;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}