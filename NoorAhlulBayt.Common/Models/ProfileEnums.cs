namespace NoorAhlulBayt.Common.Models;

/// <summary>
/// Age-appropriate filtering levels for profiles
/// </summary>
public enum FilteringLevel
{
    /// <summary>
    /// Strict filtering for children (8-12 years)
    /// </summary>
    Child = 1,
    
    /// <summary>
    /// Moderate filtering for teenagers (13-17 years)
    /// </summary>
    Teen = 2,
    
    /// <summary>
    /// Minimal filtering for adults (18+ years)
    /// </summary>
    Adult = 3,
    
    /// <summary>
    /// Custom filtering with user-defined settings
    /// </summary>
    Custom = 4
}

/// <summary>
/// Profile status enumeration
/// </summary>
public enum ProfileStatus
{
    /// <summary>
    /// Profile is active and can be used
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Profile is temporarily disabled
    /// </summary>
    Disabled = 2,
    
    /// <summary>
    /// Profile is suspended due to violations
    /// </summary>
    Suspended = 3,
    
    /// <summary>
    /// Profile is archived (soft delete)
    /// </summary>
    Archived = 4
}

/// <summary>
/// Time restriction types
/// </summary>
public enum TimeRestrictionType
{
    /// <summary>
    /// No time restrictions
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Daily time limit
    /// </summary>
    DailyLimit = 1,
    
    /// <summary>
    /// Scheduled access times
    /// </summary>
    ScheduledAccess = 2,
    
    /// <summary>
    /// Both daily limit and scheduled access
    /// </summary>
    Both = 3
}

/// <summary>
/// Days of the week for scheduling
/// </summary>
[Flags]
public enum DaysOfWeek
{
    None = 0,
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekends = Saturday | Sunday,
    All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}

/// <summary>
/// Content filtering categories
/// </summary>
[Flags]
public enum ContentFilteringCategories
{
    None = 0,
    
    // Basic categories
    Adult = 1,
    Violence = 2,
    Profanity = 4,
    Gambling = 8,
    Drugs = 16,
    
    // Social media and communication
    SocialMedia = 32,
    Chat = 64,
    Forums = 128,
    
    // Entertainment
    Gaming = 256,
    Streaming = 512,
    Music = 1024,
    
    // Educational exceptions
    Educational = 2048,
    News = 4096,
    Reference = 8192,
    
    // Islamic content
    IslamicContent = 16384,
    
    // Presets
    ChildSafe = Adult | Violence | Profanity | Gambling | Drugs | SocialMedia | Chat | Forums,
    TeenSafe = Adult | Violence | Gambling | Drugs,
    AdultMinimal = Adult | Violence
}
