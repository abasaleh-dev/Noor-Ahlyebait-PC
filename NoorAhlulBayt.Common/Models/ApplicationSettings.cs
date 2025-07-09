using System.ComponentModel;

namespace NoorAhlulBayt.Common.Models;

/// <summary>
/// Centralized application settings for the Islamic family-safe browser system
/// </summary>
public class ApplicationSettings
{
    public GeneralSettings General { get; set; } = new();
    public PrayerTimeSettings PrayerTimes { get; set; } = new();
    public ContentFilteringSettings ContentFiltering { get; set; } = new();
    public MonitoringSettings Monitoring { get; set; } = new();
    public ThemeSettings Theme { get; set; } = new();
    public Dictionary<int, ProfileSpecificSettings> ProfileSettings { get; set; } = new();
}

/// <summary>
/// General application settings
/// </summary>
public class GeneralSettings
{
    // Startup & System Integration
    public bool StartWithWindows { get; set; } = true;
    public bool MinimizeToSystemTray { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public bool StartMinimized { get; set; } = false;

    // Language & Localization
    public string Language { get; set; } = "en-US";
    public bool UseSystemLanguage { get; set; } = false;
    public string DateFormat { get; set; } = "MM/dd/yyyy";
    public string TimeFormat { get; set; } = "12"; // 12 or 24 hour format

    // Profile Management
    public int DefaultProfileId { get; set; } = 0;
    public bool RememberLastProfile { get; set; } = true;
    public bool RequireProfileSelection { get; set; } = false;

    // Notification Settings
    public bool NotifyOnContentBlock { get; set; } = true;
    public bool NotifyOnTimeLimit { get; set; } = true;
    public bool NotifyOnPrayerTime { get; set; } = true;
    public bool NotifyOnBrowserAttempt { get; set; } = true;
    public int NotificationDuration { get; set; } = 5; // seconds

    // Performance & Behavior
    public bool EnableLogging { get; set; } = true;
    public int LogRetentionDays { get; set; } = 30;
    public bool AutoSaveSettings { get; set; } = true;
    public int SettingsBackupCount { get; set; } = 5;
}

/// <summary>
/// Islamic prayer time settings
/// </summary>
public class PrayerTimeSettings
{
    public bool EnablePrayerNotifications { get; set; } = true;
    public LocationSettings Location { get; set; } = new();
    public CalculationMethodType CalculationMethod { get; set; } = CalculationMethodType.IslamicSocietyOfNorthAmerica;
    public PrayerAdjustments Adjustments { get; set; } = new();
    public AzanSettings Azan { get; set; } = new();
    public PrayerBreakSettings Break { get; set; } = new();
}

/// <summary>
/// Geographic location for prayer time calculations
/// </summary>
public class LocationSettings
{
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public double Latitude { get; set; } = 0.0;
    public double Longitude { get; set; } = 0.0;
    public string TimeZone { get; set; } = "";
    public bool AutoDetectLocation { get; set; } = true;
}

/// <summary>
/// Prayer time calculation methods
/// </summary>
public enum CalculationMethodType
{
    [Description("Islamic Society of North America (ISNA)")]
    IslamicSocietyOfNorthAmerica = 0,
    
    [Description("Muslim World League")]
    MuslimWorldLeague = 1,
    
    [Description("Egyptian General Authority of Survey")]
    EgyptianGeneralAuthority = 2,
    
    [Description("Umm Al-Qura University, Makkah")]
    UmmAlQura = 3,
    
    [Description("University of Islamic Sciences, Karachi")]
    UniversityOfIslamicSciences = 4,
    
    [Description("Institute of Geophysics, University of Tehran")]
    InstituteOfGeophysics = 5,
    
    [Description("Shia Ithna-Ashari, Leva Institute, Qum")]
    ShiaIthnaAshari = 6,
    
    [Description("Gulf Region")]
    GulfRegion = 7,
    
    [Description("Kuwait")]
    Kuwait = 8,
    
    [Description("Qatar")]
    Qatar = 9,
    
    [Description("Majlis Ugama Islam Singapura, Singapore")]
    Singapore = 10,
    
    [Description("Union Organization islamic de France")]
    France = 11,
    
    [Description("Diyanet İşleri Başkanlığı, Turkey")]
    Turkey = 12,
    
    [Description("Spiritual Administration of Muslims of Russia")]
    Russia = 13
}

/// <summary>
/// Individual prayer time adjustments in minutes
/// </summary>
public class PrayerAdjustments
{
    public int Fajr { get; set; } = 0;
    public int Sunrise { get; set; } = 0;
    public int Dhuhr { get; set; } = 0;
    public int Asr { get; set; } = 0;
    public int Maghrib { get; set; } = 0;
    public int Isha { get; set; } = 0;
}

/// <summary>
/// Azan (call to prayer) audio settings
/// </summary>
public class AzanSettings
{
    public bool PlayAzan { get; set; } = true;
    public string AzanAudioFile { get; set; } = "";
    public int Volume { get; set; } = 70;
    public bool PlayForAllPrayers { get; set; } = true;
    public Dictionary<string, bool> PrayerSpecificAzan { get; set; } = new()
    {
        { "Fajr", true },
        { "Dhuhr", true },
        { "Asr", true },
        { "Maghrib", true },
        { "Isha", true }
    };
}

/// <summary>
/// Prayer break overlay settings
/// </summary>
public class PrayerBreakSettings
{
    public bool EnablePrayerBreak { get; set; } = true;
    public int BreakDurationMinutes { get; set; } = 10;
    public bool ShowCountdownTimer { get; set; } = true;
    public bool AllowEarlyResume { get; set; } = false;
    public string BreakMessage { get; set; } = "It's time for prayer. Take a moment to connect with Allah.";
    public bool DimScreen { get; set; } = true;
    public int ScreenDimLevel { get; set; } = 30;
}

/// <summary>
/// Content filtering settings
/// </summary>
public class ContentFilteringSettings
{
    public bool EnableNsfwDetection { get; set; } = true;
    public double NsfwThreshold { get; set; } = 0.8;
    public bool EnableTextFiltering { get; set; } = true;
    public bool EnableDomainBlocking { get; set; } = true;
    public bool EnableSocialMediaFiltering { get; set; } = true;
    public List<string> CustomBlockedDomains { get; set; } = new();
    public List<string> CustomAllowedDomains { get; set; } = new();
    public List<string> CustomBlockedKeywords { get; set; } = new();
}

/// <summary>
/// Monitoring and logging settings
/// </summary>
public class MonitoringSettings
{
    public bool EnableActivityLogging { get; set; } = true;
    public bool EnableScreenshotMonitoring { get; set; } = false;
    public int ScreenshotInterval { get; set; } = 30;
    public bool LogBlockedAttempts { get; set; } = true;
    public int LogRetentionDays { get; set; } = 30;
    public bool EnableRealTimeAlerts { get; set; } = true;
}

/// <summary>
/// Theme and appearance settings
/// </summary>
public class ThemeSettings
{
    public string PrimaryColor { get; set; } = "#2E7D32";
    public string AccentColor { get; set; } = "#FFD700";
    public string BackgroundColor { get; set; } = "#2C2C2C";
    public bool DarkMode { get; set; } = true;
    public double FontScale { get; set; } = 1.0;
    public bool ShowAnimations { get; set; } = true;
}

/// <summary>
/// Profile-specific settings that override global settings
/// </summary>
public class ProfileSpecificSettings
{
    public int ProfileId { get; set; }
    public PrayerTimeSettings? PrayerTimeOverrides { get; set; }
    public ContentFilteringSettings? ContentFilteringOverrides { get; set; }
    public MonitoringSettings? MonitoringOverrides { get; set; }
    public bool InheritGlobalSettings { get; set; } = true;
}

/// <summary>
/// Current prayer times for display
/// </summary>
public class DailyPrayerTimes
{
    public DateTime Date { get; set; }
    public TimeSpan Fajr { get; set; }
    public TimeSpan Sunrise { get; set; }
    public TimeSpan Dhuhr { get; set; }
    public TimeSpan Asr { get; set; }
    public TimeSpan Maghrib { get; set; }
    public TimeSpan Isha { get; set; }
    
    public Dictionary<string, TimeSpan> GetAllPrayerTimes()
    {
        return new Dictionary<string, TimeSpan>
        {
            { "Fajr", Fajr },
            { "Sunrise", Sunrise },
            { "Dhuhr", Dhuhr },
            { "Asr", Asr },
            { "Maghrib", Maghrib },
            { "Isha", Isha }
        };
    }
    
    public (string PrayerName, TimeSpan Time)? GetNextPrayer()
    {
        var now = DateTime.Now.TimeOfDay;
        var prayers = GetAllPrayerTimes().Where(p => p.Key != "Sunrise");
        
        foreach (var prayer in prayers.OrderBy(p => p.Value))
        {
            if (prayer.Value > now)
                return (prayer.Key, prayer.Value);
        }
        
        // If no prayer today, return tomorrow's Fajr
        return ("Fajr", Fajr.Add(TimeSpan.FromDays(1)));
    }
    
    public (string PrayerName, TimeSpan Time)? GetCurrentPrayer()
    {
        var now = DateTime.Now.TimeOfDay;
        var prayers = GetAllPrayerTimes().Where(p => p.Key != "Sunrise");
        
        foreach (var prayer in prayers.OrderByDescending(p => p.Value))
        {
            if (prayer.Value <= now)
                return (prayer.Key, prayer.Value);
        }
        
        return null;
    }
}
