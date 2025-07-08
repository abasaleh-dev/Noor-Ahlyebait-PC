using Microsoft.EntityFrameworkCore;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;

namespace NoorAhlulBayt.Common.Services;

public class HistoryService
{
    private readonly ApplicationDbContext _context;

    public HistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    #region History Retrieval

    /// <summary>
    /// Get browsing history for a user profile with pagination
    /// </summary>
    public async Task<List<BrowsingHistory>> GetHistoryAsync(int userProfileId, int pageSize = 50, int pageNumber = 1, bool includeIncognito = false)
    {
        var query = _context.BrowsingHistory
            .Where(h => h.UserProfileId == userProfileId);

        if (!includeIncognito)
        {
            query = query.Where(h => !h.IsIncognito);
        }

        return await query
            .OrderByDescending(h => h.VisitedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Get history for a specific date range
    /// </summary>
    public async Task<List<BrowsingHistory>> GetHistoryByDateRangeAsync(int userProfileId, DateTime startDate, DateTime endDate, bool includeIncognito = false)
    {
        var query = _context.BrowsingHistory
            .Where(h => h.UserProfileId == userProfileId &&
                       h.VisitedAt >= startDate &&
                       h.VisitedAt <= endDate);

        if (!includeIncognito)
        {
            query = query.Where(h => !h.IsIncognito);
        }

        return await query
            .OrderByDescending(h => h.VisitedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get history for today
    /// </summary>
    public async Task<List<BrowsingHistory>> GetTodayHistoryAsync(int userProfileId, bool includeIncognito = false)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        return await GetHistoryByDateRangeAsync(userProfileId, today, tomorrow, includeIncognito);
    }

    /// <summary>
    /// Get history grouped by date
    /// </summary>
    public async Task<Dictionary<DateTime, List<BrowsingHistory>>> GetHistoryGroupedByDateAsync(int userProfileId, int days = 30, bool includeIncognito = false)
    {
        var startDate = DateTime.Today.AddDays(-days);
        var history = await GetHistoryByDateRangeAsync(userProfileId, startDate, DateTime.Now, includeIncognito);

        return history
            .GroupBy(h => h.VisitedAt.Date)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.VisitedAt).ToList());
    }

    /// <summary>
    /// Get most visited sites
    /// </summary>
    public async Task<List<BrowsingHistory>> GetMostVisitedAsync(int userProfileId, int count = 10, bool includeIncognito = false)
    {
        var query = _context.BrowsingHistory
            .Where(h => h.UserProfileId == userProfileId && !h.WasBlocked);

        if (!includeIncognito)
        {
            query = query.Where(h => !h.IsIncognito);
        }

        return await query
            .GroupBy(h => h.Url)
            .Select(g => new BrowsingHistory
            {
                Url = g.Key,
                Title = g.OrderByDescending(h => h.VisitedAt).First().Title,
                VisitCount = g.Sum(h => h.VisitCount),
                VisitedAt = g.Max(h => h.VisitedAt),
                UserProfileId = userProfileId
            })
            .OrderByDescending(h => h.VisitCount)
            .Take(count)
            .ToListAsync();
    }

    #endregion

    #region Search and Filter

    /// <summary>
    /// Search history by URL or title
    /// </summary>
    public async Task<List<BrowsingHistory>> SearchHistoryAsync(int userProfileId, string searchTerm, bool includeIncognito = false)
    {
        var term = searchTerm.ToLower().Trim();

        var query = _context.BrowsingHistory
            .Where(h => h.UserProfileId == userProfileId &&
                       (h.Title.ToLower().Contains(term) ||
                        h.Url.ToLower().Contains(term)));

        if (!includeIncognito)
        {
            query = query.Where(h => !h.IsIncognito);
        }

        return await query
            .OrderByDescending(h => h.VisitedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get history filtered by domain
    /// </summary>
    public async Task<List<BrowsingHistory>> GetHistoryByDomainAsync(int userProfileId, string domain, bool includeIncognito = false)
    {
        var query = _context.BrowsingHistory
            .Where(h => h.UserProfileId == userProfileId &&
                       h.Url.Contains(domain));

        if (!includeIncognito)
        {
            query = query.Where(h => !h.IsIncognito);
        }

        return await query
            .OrderByDescending(h => h.VisitedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get blocked content history (for admin review)
    /// </summary>
    public async Task<List<BrowsingHistory>> GetBlockedHistoryAsync(int userProfileId)
    {
        return await _context.BrowsingHistory
            .Where(h => h.UserProfileId == userProfileId && h.WasBlocked)
            .OrderByDescending(h => h.VisitedAt)
            .ToListAsync();
    }

    #endregion

    #region History Management

    /// <summary>
    /// Add or update history entry
    /// </summary>
    public async Task<BrowsingHistory> AddOrUpdateHistoryAsync(int userProfileId, string url, string title, bool isIncognito = false, bool wasBlocked = false, string? blockReason = null)
    {
        // Don't save blocked content to history unless specifically requested for admin review
        if (wasBlocked && string.IsNullOrEmpty(blockReason))
        {
            return new BrowsingHistory(); // Return empty entry
        }

        // Check if entry exists for today
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var existingEntry = await _context.BrowsingHistory
            .FirstOrDefaultAsync(h => h.UserProfileId == userProfileId &&
                                    h.Url == url &&
                                    h.VisitedAt >= today &&
                                    h.VisitedAt < tomorrow &&
                                    h.IsIncognito == isIncognito);

        if (existingEntry != null)
        {
            // Update existing entry
            existingEntry.VisitCount++;
            existingEntry.VisitedAt = DateTime.Now;
            existingEntry.Title = title; // Update title in case it changed
            
            if (wasBlocked)
            {
                existingEntry.WasBlocked = true;
                existingEntry.BlockReason = blockReason;
            }

            await _context.SaveChangesAsync();
            return existingEntry;
        }
        else
        {
            // Create new entry
            var historyEntry = new BrowsingHistory
            {
                UserProfileId = userProfileId,
                Url = url,
                Title = title,
                VisitedAt = DateTime.Now,
                VisitCount = 1,
                IsIncognito = isIncognito,
                WasBlocked = wasBlocked,
                BlockReason = blockReason
            };

            _context.BrowsingHistory.Add(historyEntry);
            await _context.SaveChangesAsync();
            return historyEntry;
        }
    }

    /// <summary>
    /// Delete a specific history entry
    /// </summary>
    public async Task<bool> DeleteHistoryEntryAsync(int historyId)
    {
        var entry = await _context.BrowsingHistory.FindAsync(historyId);
        if (entry == null)
        {
            return false;
        }

        _context.BrowsingHistory.Remove(entry);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Delete history entries by URL
    /// </summary>
    public async Task<int> DeleteHistoryByUrlAsync(int userProfileId, string url)
    {
        var entries = await _context.BrowsingHistory
            .Where(h => h.UserProfileId == userProfileId && h.Url == url)
            .ToListAsync();

        _context.BrowsingHistory.RemoveRange(entries);
        await _context.SaveChangesAsync();
        return entries.Count;
    }

    /// <summary>
    /// Clear history for a specific date range
    /// </summary>
    public async Task<int> ClearHistoryByDateRangeAsync(int userProfileId, DateTime startDate, DateTime endDate, bool includeIncognito = true)
    {
        var query = _context.BrowsingHistory
            .Where(h => h.UserProfileId == userProfileId &&
                       h.VisitedAt >= startDate &&
                       h.VisitedAt <= endDate);

        if (!includeIncognito)
        {
            query = query.Where(h => !h.IsIncognito);
        }

        var entries = await query.ToListAsync();
        _context.BrowsingHistory.RemoveRange(entries);
        await _context.SaveChangesAsync();
        return entries.Count;
    }

    /// <summary>
    /// Clear all history for a user profile
    /// </summary>
    public async Task<int> ClearAllHistoryAsync(int userProfileId, bool includeIncognito = true)
    {
        var query = _context.BrowsingHistory
            .Where(h => h.UserProfileId == userProfileId);

        if (!includeIncognito)
        {
            query = query.Where(h => !h.IsIncognito);
        }

        var entries = await query.ToListAsync();
        _context.BrowsingHistory.RemoveRange(entries);
        await _context.SaveChangesAsync();
        return entries.Count;
    }

    /// <summary>
    /// Clear history older than specified days
    /// </summary>
    public async Task<int> ClearOldHistoryAsync(int userProfileId, int olderThanDays, bool includeIncognito = true)
    {
        var cutoffDate = DateTime.Now.AddDays(-olderThanDays);
        return await ClearHistoryByDateRangeAsync(userProfileId, DateTime.MinValue, cutoffDate, includeIncognito);
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get history statistics for a user profile
    /// </summary>
    public async Task<HistoryStatistics> GetHistoryStatisticsAsync(int userProfileId, bool includeIncognito = false)
    {
        var query = _context.BrowsingHistory
            .Where(h => h.UserProfileId == userProfileId);

        if (!includeIncognito)
        {
            query = query.Where(h => !h.IsIncognito);
        }

        var totalEntries = await query.CountAsync();
        var totalVisits = await query.SumAsync(h => h.VisitCount);
        var uniqueUrls = await query.Select(h => h.Url).Distinct().CountAsync();
        var blockedEntries = await query.CountAsync(h => h.WasBlocked);

        var oldestEntry = await query.OrderBy(h => h.VisitedAt).FirstOrDefaultAsync();
        var newestEntry = await query.OrderByDescending(h => h.VisitedAt).FirstOrDefaultAsync();

        return new HistoryStatistics
        {
            TotalEntries = totalEntries,
            TotalVisits = totalVisits,
            UniqueUrls = uniqueUrls,
            BlockedEntries = blockedEntries,
            OldestEntry = oldestEntry?.VisitedAt,
            NewestEntry = newestEntry?.VisitedAt
        };
    }

    #endregion
}

/// <summary>
/// History statistics for a user profile
/// </summary>
public class HistoryStatistics
{
    public int TotalEntries { get; set; }
    public int TotalVisits { get; set; }
    public int UniqueUrls { get; set; }
    public int BlockedEntries { get; set; }
    public DateTime? OldestEntry { get; set; }
    public DateTime? NewestEntry { get; set; }
}
