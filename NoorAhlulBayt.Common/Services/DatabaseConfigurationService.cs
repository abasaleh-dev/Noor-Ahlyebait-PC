using Microsoft.EntityFrameworkCore;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;

namespace NoorAhlulBayt.Common.Services;

/// <summary>
/// Centralized database configuration service for both Companion and Browser applications
/// </summary>
public static class DatabaseConfigurationService
{
    private static readonly string SharedDatabasePath = GetSharedDatabasePath();

    /// <summary>
    /// Get the shared database path used by both Companion and Browser applications
    /// </summary>
    public static string GetSharedDatabasePath()
    {
        // Use a shared location that both applications can access
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "NoorAhlulBayt");
        Directory.CreateDirectory(appFolder);
        
        return Path.Combine(appFolder, "noor_family_browser.db");
    }

    /// <summary>
    /// Create a configured ApplicationDbContext with the shared database
    /// </summary>
    public static ApplicationDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite($"Data Source={SharedDatabasePath}");
        
        var context = new ApplicationDbContext(optionsBuilder.Options);
        
        // Ensure database is created
        context.Database.EnsureCreated();
        
        return context;
    }

    /// <summary>
    /// Create DbContextOptions for dependency injection
    /// </summary>
    public static DbContextOptions<ApplicationDbContext> CreateDbContextOptions()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite($"Data Source={SharedDatabasePath}");
        return optionsBuilder.Options;
    }

    /// <summary>
    /// Check if the shared database exists and is accessible
    /// </summary>
    public static bool IsDatabaseAccessible()
    {
        try
        {
            using var context = CreateDbContext();
            // Try to execute a simple query
            context.Database.CanConnect();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Initialize the database with default data if needed
    /// </summary>
    public static async Task<bool> InitializeDatabaseAsync()
    {
        try
        {
            using var context = CreateDbContext();
            
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            // Check if default profile exists
            var defaultProfile = await context.UserProfiles
                .FirstOrDefaultAsync(p => p.IsDefault);
            
            if (defaultProfile == null)
            {
                // Create default profile if it doesn't exist
                defaultProfile = new UserProfile
                {
                    Name = "Default",
                    IsDefault = true,
                    FilteringLevel = FilteringLevel.Child,
                    EnableProfanityFilter = true,
                    EnableNsfwFilter = true,
                    EnableAdBlocker = true,
                    EnableSafeSearch = true,
                    EnableAzanBlocking = true,
                    AzanBlockingDurationMinutes = 10,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                context.UserProfiles.Add(defaultProfile);
                await context.SaveChangesAsync();
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get database file information for diagnostics
    /// </summary>
    public static DatabaseInfo GetDatabaseInfo()
    {
        var dbPath = SharedDatabasePath;
        var fileInfo = new FileInfo(dbPath);
        
        return new DatabaseInfo
        {
            Path = dbPath,
            Exists = fileInfo.Exists,
            Size = fileInfo.Exists ? fileInfo.Length : 0,
            LastModified = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.MinValue,
            IsAccessible = IsDatabaseAccessible()
        };
    }

    /// <summary>
    /// Backup the database to a specified location
    /// </summary>
    public static async Task<bool> BackupDatabaseAsync(string backupPath)
    {
        try
        {
            var sourcePath = SharedDatabasePath;
            if (!File.Exists(sourcePath))
            {
                return false;
            }

            // Ensure backup directory exists
            var backupDir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // Copy the database file
            await Task.Run(() => File.Copy(sourcePath, backupPath, true));
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database backup error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Restore database from a backup
    /// </summary>
    public static async Task<bool> RestoreDatabaseAsync(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                return false;
            }

            var targetPath = SharedDatabasePath;
            
            // Copy the backup file to the database location
            await Task.Run(() => File.Copy(backupPath, targetPath, true));
            
            // Verify the restored database is accessible
            return IsDatabaseAccessible();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database restore error: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// Database information for diagnostics
/// </summary>
public class DatabaseInfo
{
    public string Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsAccessible { get; set; }
}
