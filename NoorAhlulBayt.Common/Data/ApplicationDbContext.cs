using Microsoft.EntityFrameworkCore;
using NoorAhlulBayt.Common.Models;

namespace NoorAhlulBayt.Common.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Settings> Settings { get; set; }
    public DbSet<Bookmark> Bookmarks { get; set; }
    public DbSet<BrowsingHistory> BrowsingHistory { get; set; }
    public DbSet<PrayerTime> PrayerTimes { get; set; }
    public DbSet<DailyUsageSession> DailyUsageSessions { get; set; }
    public DbSet<BrowserAttemptLog> BrowserAttemptLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UserProfile Configuration
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsDefault);

            // One-to-many relationships
            entity.HasMany(e => e.Bookmarks)
                  .WithOne(e => e.UserProfile)
                  .HasForeignKey(e => e.UserProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.BrowsingHistory)
                  .WithOne(e => e.UserProfile)
                  .HasForeignKey(e => e.UserProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.DailyUsageSessions)
                  .WithOne(e => e.UserProfile)
                  .HasForeignKey(e => e.UserProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Settings Configuration
        modelBuilder.Entity<Settings>(entity =>
        {
            // Only one settings record should exist
            entity.HasData(new Settings { Id = 1 });
        });

        // Bookmark Configuration
        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasIndex(e => new { e.UserProfileId, e.Url });
            entity.HasIndex(e => e.FolderPath);
        });

        // BrowsingHistory Configuration
        modelBuilder.Entity<BrowsingHistory>(entity =>
        {
            entity.HasIndex(e => new { e.UserProfileId, e.VisitedAt });
            entity.HasIndex(e => e.Url);
        });

        // PrayerTime Configuration
        modelBuilder.Entity<PrayerTime>(entity =>
        {
            entity.HasIndex(e => new { e.City, e.Country, e.Date }).IsUnique();
        });

        // DailyUsageSession Configuration
        modelBuilder.Entity<DailyUsageSession>(entity =>
        {
            entity.HasIndex(e => new { e.UserProfileId, e.Date });
            entity.HasIndex(e => e.IsActive);
        });

        // BrowserAttemptLog Configuration
        modelBuilder.Entity<BrowserAttemptLog>(entity =>
        {
            entity.HasIndex(e => e.AttemptTime);
            entity.HasIndex(e => e.BrowserName);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.UserProfileId);
        });

        // Seed default data
        SeedDefaultData(modelBuilder);
    }

    private void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Create default user profile
        modelBuilder.Entity<UserProfile>().HasData(new UserProfile
        {
            Id = 1,
            Name = "Default",
            IsDefault = true,
            EnableProfanityFilter = true,
            EnableNsfwFilter = true,
            EnableAdBlocker = true,
            EnableSafeSearch = true,
            EnableAzanBlocking = true,
            AzanBlockingDurationMinutes = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }
}