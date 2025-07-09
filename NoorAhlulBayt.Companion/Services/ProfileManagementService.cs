using System.Security.Cryptography;
using System.Text;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;
using NoorAhlulBayt.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace NoorAhlulBayt.Companion.Services;

/// <summary>
/// Service for comprehensive profile management operations
/// </summary>
public class ProfileManagementService
{
    private readonly ApplicationDbContext _context;

    public ProfileManagementService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all profiles with usage statistics
    /// </summary>
    public async Task<List<ProfileInfo>> GetAllProfilesAsync()
    {
        try
        {
            Console.WriteLine("ProfileManagementService: Starting GetAllProfilesAsync");

            var profiles = await _context.UserProfiles
                .Include(p => p.DailyUsageSessions)
                .Include(p => p.BrowserAttemptLogs)
                .Where(p => p.Status != ProfileStatus.Archived) // Filter out archived profiles
                .OrderBy(p => p.IsDefault ? 0 : 1)
                .ThenBy(p => p.Name)
                .ToListAsync();

            Console.WriteLine($"ProfileManagementService: Found {profiles.Count} profiles in database");

            var profileInfos = new List<ProfileInfo>();

            foreach (var profile in profiles)
            {
                var todayUsage = await GetTodayUsageAsync(profile.Id);
                var weeklyUsage = await GetWeeklyUsageAsync(profile.Id);

                profileInfos.Add(new ProfileInfo
                {
                    Profile = profile,
                    TodayUsageMinutes = todayUsage,
                    WeeklyUsageMinutes = weeklyUsage,
                    IsCurrentlyActive = await IsProfileActiveAsync(profile.Id),
                    LastUsed = await GetLastUsedAsync(profile.Id),
                    TotalSessions = profile.DailyUsageSessions.Count,
                    BlockedAttempts = profile.BrowserAttemptLogs.Count(b => b.Action == "BLOCKED" || b.Action == "TERMINATED")
                });
            }

            return profileInfos;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting profiles: {ex.Message}");
            return new List<ProfileInfo>();
        }
    }

    /// <summary>
    /// Create a new profile with validation
    /// </summary>
    public async Task<(bool Success, string Message, UserProfile? Profile)> CreateProfileAsync(CreateProfileRequest request)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Name))
                return (false, "Profile name is required.", null);

            if (request.Name.Length > 100)
                return (false, "Profile name must be 100 characters or less.", null);

            // Check for duplicate names
            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Name.ToLower() == request.Name.ToLower());
            
            if (existingProfile != null)
                return (false, "A profile with this name already exists.", null);

            // Create new profile
            var profile = new UserProfile
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Age = request.Age,
                AvatarIcon = request.AvatarIcon ?? GetRandomAvatar(),
                FilteringLevel = request.FilteringLevel,
                Status = ProfileStatus.Active,
                IsDefault = false,
                RequirePinForSwitching = request.RequirePinForSwitching,
                TimeRestrictionType = request.TimeRestrictionType,
                DailyTimeLimitMinutes = request.DailyTimeLimitMinutes,
                AllowedStartTime = request.AllowedStartTime,
                AllowedEndTime = request.AllowedEndTime,
                AllowedDays = request.AllowedDays,
                BreakReminderMinutes = request.BreakReminderMinutes,
                WarningBeforeTimeoutMinutes = request.WarningBeforeTimeoutMinutes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Apply filtering level preset
            profile.ApplyFilteringLevelPreset();

            // Set PIN if provided
            if (!string.IsNullOrEmpty(request.Pin))
            {
                profile.EncryptedPin = CryptographyService.EncryptPin(request.Pin);
            }

            // Set domain lists if provided
            if (request.WhitelistedDomains?.Any() == true)
            {
                profile.SetWhitelistedDomains(request.WhitelistedDomains);
            }

            if (request.BlacklistedDomains?.Any() == true)
            {
                profile.SetBlacklistedDomains(request.BlacklistedDomains);
            }

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            return (true, "Profile created successfully.", profile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating profile: {ex.Message}");
            return (false, $"Error creating profile: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Update an existing profile
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateProfileAsync(int profileId, UpdateProfileRequest request)
    {
        try
        {
            var profile = await _context.UserProfiles.FindAsync(profileId);
            if (profile == null)
                return (false, "Profile not found.");

            // Validation
            if (string.IsNullOrWhiteSpace(request.Name))
                return (false, "Profile name is required.");

            if (request.Name.Length > 100)
                return (false, "Profile name must be 100 characters or less.");

            // Check for duplicate names (excluding current profile)
            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Name.ToLower() == request.Name.ToLower() && p.Id != profileId);
            
            if (existingProfile != null)
                return (false, "A profile with this name already exists.");

            // Update profile properties
            profile.Name = request.Name.Trim();
            profile.Description = request.Description?.Trim();
            profile.Age = request.Age;
            profile.AvatarIcon = request.AvatarIcon ?? profile.AvatarIcon;
            profile.FilteringLevel = request.FilteringLevel;
            profile.Status = request.Status;
            profile.RequirePinForSwitching = request.RequirePinForSwitching;
            profile.TimeRestrictionType = request.TimeRestrictionType;
            profile.DailyTimeLimitMinutes = request.DailyTimeLimitMinutes;
            profile.AllowedStartTime = request.AllowedStartTime;
            profile.AllowedEndTime = request.AllowedEndTime;
            profile.AllowedDays = request.AllowedDays;
            profile.BreakReminderMinutes = request.BreakReminderMinutes;
            profile.WarningBeforeTimeoutMinutes = request.WarningBeforeTimeoutMinutes;
            profile.UpdatedAt = DateTime.UtcNow;

            // Apply filtering level preset if changed
            if (request.ApplyFilteringPreset)
            {
                profile.ApplyFilteringLevelPreset();
            }

            // Update PIN if provided
            if (!string.IsNullOrEmpty(request.Pin))
            {
                profile.EncryptedPin = CryptographyService.EncryptPin(request.Pin);
            }
            else if (request.ClearPin)
            {
                profile.EncryptedPin = null;
            }

            // Update domain lists if provided
            if (request.WhitelistedDomains != null)
            {
                profile.SetWhitelistedDomains(request.WhitelistedDomains);
            }

            if (request.BlacklistedDomains != null)
            {
                profile.SetBlacklistedDomains(request.BlacklistedDomains);
            }

            await _context.SaveChangesAsync();

            return (true, "Profile updated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating profile: {ex.Message}");
            return (false, $"Error updating profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a profile (soft delete by archiving)
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteProfileAsync(int profileId)
    {
        try
        {
            var profile = await _context.UserProfiles.FindAsync(profileId);
            if (profile == null)
                return (false, "Profile not found.");

            if (profile.IsDefault)
                return (false, "Cannot delete the default profile.");

            // Soft delete by archiving
            profile.Status = ProfileStatus.Archived;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Profile deleted successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting profile: {ex.Message}");
            return (false, $"Error deleting profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Verify profile PIN
    /// </summary>
    public async Task<bool> VerifyProfilePinAsync(int profileId, string pin)
    {
        try
        {
            var profile = await _context.UserProfiles.FindAsync(profileId);
            if (profile?.EncryptedPin == null) return true; // No PIN set

            var decryptedPin = CryptographyService.DecryptPin(profile.EncryptedPin);
            return decryptedPin == pin;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying PIN: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get profile templates for quick creation
    /// </summary>
    public List<ProfileTemplate> GetProfileTemplates()
    {
        return new List<ProfileTemplate>
        {
            new ProfileTemplate
            {
                Name = "Child (8-12 years)",
                Description = "Safe browsing for children with strict content filtering",
                AvatarIcon = "👶",
                FilteringLevel = FilteringLevel.Child,
                DailyTimeLimitMinutes = 120, // 2 hours
                AllowedStartTime = new TimeSpan(16, 0, 0), // 4 PM
                AllowedEndTime = new TimeSpan(19, 0, 0), // 7 PM
                AllowedDays = DaysOfWeek.Weekdays,
                TimeRestrictionType = TimeRestrictionType.Both
            },
            new ProfileTemplate
            {
                Name = "Teen (13-17 years)",
                Description = "Balanced filtering for teenagers with moderate restrictions",
                AvatarIcon = "🧑",
                FilteringLevel = FilteringLevel.Teen,
                DailyTimeLimitMinutes = 240, // 4 hours
                AllowedStartTime = new TimeSpan(15, 0, 0), // 3 PM
                AllowedEndTime = new TimeSpan(21, 0, 0), // 9 PM
                AllowedDays = DaysOfWeek.All,
                TimeRestrictionType = TimeRestrictionType.DailyLimit
            },
            new ProfileTemplate
            {
                Name = "Adult",
                Description = "Minimal filtering for adult users",
                AvatarIcon = "👨",
                FilteringLevel = FilteringLevel.Adult,
                DailyTimeLimitMinutes = 0, // No limit
                TimeRestrictionType = TimeRestrictionType.None
            }
        };
    }

    // Helper methods
    private async Task<int> GetTodayUsageAsync(int profileId)
    {
        var today = DateTime.Today;
        var sessions = await _context.DailyUsageSessions
            .Where(s => s.UserProfileId == profileId && s.Date.Date == today)
            .ToListAsync();

        return sessions.Sum(s => s.IsActive 
            ? (int)(DateTime.Now - s.SessionStart).TotalMinutes 
            : s.DurationMinutes);
    }

    private async Task<int> GetWeeklyUsageAsync(int profileId)
    {
        var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        var sessions = await _context.DailyUsageSessions
            .Where(s => s.UserProfileId == profileId && s.Date.Date >= weekStart)
            .ToListAsync();

        return sessions.Sum(s => s.IsActive 
            ? (int)(DateTime.Now - s.SessionStart).TotalMinutes 
            : s.DurationMinutes);
    }

    private async Task<bool> IsProfileActiveAsync(int profileId)
    {
        return await _context.DailyUsageSessions
            .AnyAsync(s => s.UserProfileId == profileId && s.IsActive);
    }

    private async Task<DateTime?> GetLastUsedAsync(int profileId)
    {
        var lastSession = await _context.DailyUsageSessions
            .Where(s => s.UserProfileId == profileId)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync();

        return lastSession?.UpdatedAt;
    }

    private string GetRandomAvatar()
    {
        var avatars = new[] { "👤", "👶", "🧒", "👦", "👧", "🧑", "👨", "👩", "🧔", "👱" };
        var random = new Random();
        return avatars[random.Next(avatars.Length)];
    }

    /// <summary>
    /// Get the currently active profile
    /// </summary>
    public async Task<UserProfile?> GetCurrentActiveProfileAsync()
    {
        try
        {
            // For now, we'll use the first non-archived profile as active
            // In a full implementation, you'd track the active profile in settings or database
            var activeProfile = await _context.UserProfiles
                .Where(p => p.Status == ProfileStatus.Active)
                .OrderBy(p => p.IsDefault ? 0 : 1)
                .FirstOrDefaultAsync();

            return activeProfile;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting current active profile: {ex.Message}");
            return null;
        }
    }



    /// <summary>
    /// Switch to a specific profile
    /// </summary>
    public async Task<bool> SwitchToProfileAsync(int profileId)
    {
        try
        {
            var profile = await _context.UserProfiles.FindAsync(profileId);
            if (profile == null || profile.Status != ProfileStatus.Active)
                return false;

            // In a full implementation, you would:
            // 1. Update application settings to track current profile
            // 2. Apply profile-specific browser settings
            // 3. Update monitoring configurations
            // 4. Log the profile switch event

            Console.WriteLine($"Switched to profile: {profile.Name} (ID: {profile.Id})");

            // For now, we'll just log the switch
            // TODO: Implement actual profile switching logic

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error switching to profile: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// Profile information with usage statistics
/// </summary>
public class ProfileInfo
{
    public UserProfile Profile { get; set; } = null!;
    public int TodayUsageMinutes { get; set; }
    public int WeeklyUsageMinutes { get; set; }
    public bool IsCurrentlyActive { get; set; }
    public DateTime? LastUsed { get; set; }
    public int TotalSessions { get; set; }
    public int BlockedAttempts { get; set; }

    public string FormattedTodayUsage => FormatMinutes(TodayUsageMinutes);
    public string FormattedWeeklyUsage => FormatMinutes(WeeklyUsageMinutes);
    public string FormattedLastUsed => LastUsed?.ToString("MMM dd, HH:mm") ?? "Never";

    private string FormatMinutes(int minutes)
    {
        if (minutes == 0) return "0m";
        if (minutes < 60) return $"{minutes}m";
        return $"{minutes / 60}h {minutes % 60}m";
    }
}

/// <summary>
/// Request model for creating a new profile
/// </summary>
public class CreateProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Age { get; set; }
    public string? AvatarIcon { get; set; }
    public FilteringLevel FilteringLevel { get; set; } = FilteringLevel.Child;
    public bool RequirePinForSwitching { get; set; }
    public string? Pin { get; set; }

    // Time management
    public TimeRestrictionType TimeRestrictionType { get; set; } = TimeRestrictionType.None;
    public int DailyTimeLimitMinutes { get; set; }
    public TimeSpan? AllowedStartTime { get; set; }
    public TimeSpan? AllowedEndTime { get; set; }
    public DaysOfWeek AllowedDays { get; set; } = DaysOfWeek.All;
    public int BreakReminderMinutes { get; set; } = 60;
    public int WarningBeforeTimeoutMinutes { get; set; } = 10;

    // Domain management
    public List<string>? WhitelistedDomains { get; set; }
    public List<string>? BlacklistedDomains { get; set; }
}

/// <summary>
/// Request model for updating a profile
/// </summary>
public class UpdateProfileRequest : CreateProfileRequest
{
    public ProfileStatus Status { get; set; } = ProfileStatus.Active;
    public bool ApplyFilteringPreset { get; set; }
    public bool ClearPin { get; set; }
}

/// <summary>
/// Profile template for quick creation
/// </summary>
public class ProfileTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AvatarIcon { get; set; } = "👤";
    public FilteringLevel FilteringLevel { get; set; }
    public TimeRestrictionType TimeRestrictionType { get; set; }
    public int DailyTimeLimitMinutes { get; set; }
    public TimeSpan? AllowedStartTime { get; set; }
    public TimeSpan? AllowedEndTime { get; set; }
    public DaysOfWeek AllowedDays { get; set; } = DaysOfWeek.All;
}
