using Microsoft.EntityFrameworkCore;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;
using NoorAhlulBayt.Common.Services;
using System.IO;

namespace NoorAhlulBayt.Browser.Services;

/// <summary>
/// Service for handling profile selection and authentication in the browser
/// </summary>
public class ProfileSelectionService : IDisposable
{
    private readonly ApplicationDbContext _context;
    private static int? _currentProfileId;
    private bool _disposed = false;

    public ProfileSelectionService()
    {
        try
        {
            // Configure DbContext with SQLite - Use same database as Companion app
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var dbPath = GetSharedDatabasePath();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            _context = new ApplicationDbContext(optionsBuilder.Options);

            // Ensure database is created
            _context.Database.EnsureCreated();

            DiagnosticLogger.LogStartupStep($"ProfileSelectionService initialized with database: {dbPath}");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ProfileSelectionService", "Failed to initialize database", ex);
            throw new InvalidOperationException("Failed to initialize profile database. Please ensure the Companion app has been run at least once.", ex);
        }
    }

    /// <summary>
    /// Get the shared database path used by both Companion and Browser applications
    /// </summary>
    private static string GetSharedDatabasePath()
    {
        // Use a shared location that both applications can access
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "NoorAhlulBayt");
        Directory.CreateDirectory(appFolder);

        return Path.Combine(appFolder, "noor_family_browser.db");
    }

    /// <summary>
    /// Get all available profiles (excluding default profile)
    /// </summary>
    public async Task<List<UserProfile>> GetAvailableProfilesAsync()
    {
        try
        {
            DiagnosticLogger.LogStartupStep("ProfileSelectionService: Loading available profiles");
            
            var profiles = await _context.UserProfiles
                .Where(p => !p.IsDefault && p.Status == ProfileStatus.Active)
                .OrderBy(p => p.Name)
                .ToListAsync();

            DiagnosticLogger.LogStartupStep($"ProfileSelectionService: Found {profiles.Count} available profiles");
            
            return profiles;
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ProfileSelectionService", "Error loading profiles", ex);
            throw;
        }
    }

    /// <summary>
    /// Authenticate a profile with optional PIN
    /// </summary>
    public async Task<bool> AuthenticateProfileAsync(int profileId, string? pin)
    {
        try
        {
            DiagnosticLogger.LogStartupStep($"ProfileSelectionService: Authenticating profile {profileId}");
            
            var profile = await _context.UserProfiles.FindAsync(profileId);
            if (profile == null || profile.Status != ProfileStatus.Active)
            {
                DiagnosticLogger.LogStartupStep($"ProfileSelectionService: Profile {profileId} not found or inactive");
                return false;
            }

            // Check if PIN is required
            if (!string.IsNullOrEmpty(profile.EncryptedPin))
            {
                if (string.IsNullOrEmpty(pin))
                {
                    DiagnosticLogger.LogStartupStep($"ProfileSelectionService: PIN required but not provided for profile {profileId}");
                    return false;
                }

                // Verify PIN
                bool pinValid = CryptographyService.VerifyPin(pin, profile.EncryptedPin);
                if (!pinValid)
                {
                    DiagnosticLogger.LogStartupStep($"ProfileSelectionService: Invalid PIN for profile {profileId}");
                    return false;
                }
            }

            DiagnosticLogger.LogStartupStep($"ProfileSelectionService: Profile {profileId} authenticated successfully");
            return true;
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ProfileSelectionService", $"Error authenticating profile {profileId}", ex);
            return false;
        }
    }

    /// <summary>
    /// Set the current active profile
    /// </summary>
    public async Task<bool> SetCurrentProfileAsync(int profileId)
    {
        try
        {
            DiagnosticLogger.LogStartupStep($"ProfileSelectionService: Setting current profile to {profileId}");
            
            var profile = await _context.UserProfiles.FindAsync(profileId);
            if (profile == null || profile.Status != ProfileStatus.Active)
            {
                DiagnosticLogger.LogStartupStep($"ProfileSelectionService: Cannot set current profile - profile {profileId} not found or inactive");
                return false;
            }

            _currentProfileId = profileId;

            // Update application settings to remember this profile
            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new Settings();
                _context.Settings.Add(settings);
            }

            if (settings.RememberLastProfile)
            {
                settings.DefaultProfileId = profileId;
                await _context.SaveChangesAsync();
            }

            DiagnosticLogger.LogStartupStep($"ProfileSelectionService: Current profile set to {profileId} ({profile.Name})");
            return true;
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ProfileSelectionService", $"Error setting current profile {profileId}", ex);
            return false;
        }
    }

    /// <summary>
    /// Get the current active profile
    /// </summary>
    public static int? GetCurrentProfileId()
    {
        return _currentProfileId;
    }

    /// <summary>
    /// Get the current active profile details
    /// </summary>
    public async Task<UserProfile?> GetCurrentProfileAsync()
    {
        try
        {
            if (_currentProfileId == null)
            {
                return null;
            }

            return await _context.UserProfiles.FindAsync(_currentProfileId.Value);
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ProfileSelectionService", "Error getting current profile", ex);
            return null;
        }
    }

    /// <summary>
    /// Check if there are any profiles available for selection
    /// </summary>
    public async Task<bool> HasAvailableProfilesAsync()
    {
        try
        {
            var count = await _context.UserProfiles
                .Where(p => !p.IsDefault && p.Status == ProfileStatus.Active)
                .CountAsync();

            return count > 0;
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ProfileSelectionService", "Error checking for available profiles", ex);
            return false;
        }
    }

    /// <summary>
    /// Get the default profile if no other profiles are available
    /// </summary>
    public async Task<UserProfile?> GetDefaultProfileAsync()
    {
        try
        {
            return await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.IsDefault);
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ProfileSelectionService", "Error getting default profile", ex);
            return null;
        }
    }

    /// <summary>
    /// Check if profile selection should be skipped (e.g., only default profile exists)
    /// </summary>
    public async Task<bool> ShouldSkipProfileSelectionAsync()
    {
        try
        {
            var hasProfiles = await HasAvailableProfilesAsync();
            if (!hasProfiles)
            {
                // No user profiles, check if we should use default
                var defaultProfile = await GetDefaultProfileAsync();
                if (defaultProfile != null)
                {
                    _currentProfileId = defaultProfile.Id;
                    DiagnosticLogger.LogStartupStep("ProfileSelectionService: Using default profile, skipping selection");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ProfileSelectionService", "Error checking if profile selection should be skipped", ex);
            return false;
        }
    }

    /// <summary>
    /// Get profile filtering settings for content filtering
    /// </summary>
    public async Task<UserProfile?> GetProfileForContentFilteringAsync()
    {
        try
        {
            if (_currentProfileId == null)
            {
                // Fallback to default profile
                return await GetDefaultProfileAsync();
            }

            return await _context.UserProfiles.FindAsync(_currentProfileId.Value);
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ProfileSelectionService", "Error getting profile for content filtering", ex);
            return await GetDefaultProfileAsync();
        }
    }

    /// <summary>
    /// Log profile switch event for monitoring
    /// </summary>
    public async Task LogProfileSwitchAsync(int profileId, string? reason = null)
    {
        try
        {
            var profile = await _context.UserProfiles.FindAsync(profileId);
            if (profile != null)
            {
                DiagnosticLogger.LogStartupStep($"Profile Switch: {profile.Name} (ID: {profileId})" + 
                                              (reason != null ? $" - Reason: {reason}" : ""));
                
                // Here you could add additional logging to database if needed
                // For example, create a ProfileSwitchLog table and record the event
            }
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("ProfileSelectionService", "Error logging profile switch", ex);
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            _disposed = true;
        }
    }
}
