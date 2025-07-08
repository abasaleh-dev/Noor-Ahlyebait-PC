using Microsoft.EntityFrameworkCore;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;
using System.Text.Json;
using System.Text;
using System.Net;

namespace NoorAhlulBayt.Common.Services;

public class BookmarkService
{
    private readonly ApplicationDbContext _context;

    public BookmarkService(ApplicationDbContext context)
    {
        _context = context;
    }

    #region CRUD Operations

    /// <summary>
    /// Add a new bookmark for the specified user profile
    /// </summary>
    public async Task<Bookmark> AddBookmarkAsync(int userProfileId, string title, string url, string? description = null, string? folderPath = null)
    {
        // Check for duplicate URL in the same profile
        var existingBookmark = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.UserProfileId == userProfileId && b.Url == url);

        if (existingBookmark != null)
        {
            throw new InvalidOperationException("A bookmark with this URL already exists.");
        }

        var bookmark = new Bookmark
        {
            UserProfileId = userProfileId,
            Title = title.Trim(),
            Url = url.Trim(),
            Description = description?.Trim(),
            FolderPath = folderPath?.Trim() ?? "Default",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SortOrder = await GetNextSortOrderAsync(userProfileId, folderPath)
        };

        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();

        return bookmark;
    }

    /// <summary>
    /// Update an existing bookmark
    /// </summary>
    public async Task<Bookmark> UpdateBookmarkAsync(int bookmarkId, string? title = null, string? url = null, 
        string? description = null, string? folderPath = null)
    {
        var bookmark = await _context.Bookmarks.FindAsync(bookmarkId);
        if (bookmark == null)
        {
            throw new ArgumentException("Bookmark not found.", nameof(bookmarkId));
        }

        if (!string.IsNullOrWhiteSpace(title))
            bookmark.Title = title.Trim();

        if (!string.IsNullOrWhiteSpace(url))
        {
            // Check for duplicate URL in the same profile (excluding current bookmark)
            var existingBookmark = await _context.Bookmarks
                .FirstOrDefaultAsync(b => b.UserProfileId == bookmark.UserProfileId && 
                                        b.Url == url.Trim() && b.Id != bookmarkId);

            if (existingBookmark != null)
            {
                throw new InvalidOperationException("A bookmark with this URL already exists.");
            }

            bookmark.Url = url.Trim();
        }

        if (description != null)
            bookmark.Description = description.Trim();

        if (!string.IsNullOrWhiteSpace(folderPath))
            bookmark.FolderPath = folderPath.Trim();

        bookmark.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return bookmark;
    }

    /// <summary>
    /// Delete a bookmark
    /// </summary>
    public async Task<bool> DeleteBookmarkAsync(int bookmarkId)
    {
        var bookmark = await _context.Bookmarks.FindAsync(bookmarkId);
        if (bookmark == null)
        {
            return false;
        }

        _context.Bookmarks.Remove(bookmark);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get a bookmark by ID
    /// </summary>
    public async Task<Bookmark?> GetBookmarkAsync(int bookmarkId)
    {
        return await _context.Bookmarks.FindAsync(bookmarkId);
    }

    #endregion

    #region Retrieval and Search

    /// <summary>
    /// Get all bookmarks for a user profile
    /// </summary>
    public async Task<List<Bookmark>> GetBookmarksAsync(int userProfileId)
    {
        return await _context.Bookmarks
            .Where(b => b.UserProfileId == userProfileId)
            .OrderBy(b => b.FolderPath)
            .ThenBy(b => b.SortOrder)
            .ThenBy(b => b.Title)
            .ToListAsync();
    }

    /// <summary>
    /// Get bookmarks in a specific folder
    /// </summary>
    public async Task<List<Bookmark>> GetBookmarksByFolderAsync(int userProfileId, string folderPath)
    {
        return await _context.Bookmarks
            .Where(b => b.UserProfileId == userProfileId && b.FolderPath == folderPath)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Title)
            .ToListAsync();
    }

    /// <summary>
    /// Search bookmarks by title, URL, or description
    /// </summary>
    public async Task<List<Bookmark>> SearchBookmarksAsync(int userProfileId, string searchTerm)
    {
        var term = searchTerm.ToLower().Trim();
        
        return await _context.Bookmarks
            .Where(b => b.UserProfileId == userProfileId &&
                       (b.Title.ToLower().Contains(term) ||
                        b.Url.ToLower().Contains(term) ||
                        (b.Description != null && b.Description.ToLower().Contains(term))))
            .OrderBy(b => b.Title)
            .ToListAsync();
    }

    /// <summary>
    /// Get all unique folder paths for a user profile
    /// </summary>
    public async Task<List<string>> GetFoldersAsync(int userProfileId)
    {
        return await _context.Bookmarks
            .Where(b => b.UserProfileId == userProfileId)
            .Select(b => b.FolderPath ?? "Default")
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync();
    }

    #endregion

    #region Folder Management

    /// <summary>
    /// Create a new folder (by adding a placeholder bookmark that gets removed later)
    /// </summary>
    public async Task<bool> CreateFolderAsync(int userProfileId, string folderPath)
    {
        var existingFolder = await _context.Bookmarks
            .AnyAsync(b => b.UserProfileId == userProfileId && b.FolderPath == folderPath);

        return !existingFolder; // Return true if folder doesn't exist (can be created)
    }

    /// <summary>
    /// Rename a folder by updating all bookmarks in that folder
    /// </summary>
    public async Task<bool> RenameFolderAsync(int userProfileId, string oldFolderPath, string newFolderPath)
    {
        var bookmarksInFolder = await _context.Bookmarks
            .Where(b => b.UserProfileId == userProfileId && b.FolderPath == oldFolderPath)
            .ToListAsync();

        if (!bookmarksInFolder.Any())
        {
            return false;
        }

        foreach (var bookmark in bookmarksInFolder)
        {
            bookmark.FolderPath = newFolderPath;
            bookmark.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Delete a folder and optionally move bookmarks to another folder
    /// </summary>
    public async Task<bool> DeleteFolderAsync(int userProfileId, string folderPath, string? moveToFolder = null)
    {
        var bookmarksInFolder = await _context.Bookmarks
            .Where(b => b.UserProfileId == userProfileId && b.FolderPath == folderPath)
            .ToListAsync();

        if (!bookmarksInFolder.Any())
        {
            return false;
        }

        if (moveToFolder != null)
        {
            // Move bookmarks to another folder
            foreach (var bookmark in bookmarksInFolder)
            {
                bookmark.FolderPath = moveToFolder;
                bookmark.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            // Delete all bookmarks in the folder
            _context.Bookmarks.RemoveRange(bookmarksInFolder);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Move bookmark to a different folder
    /// </summary>
    public async Task<bool> MoveBookmarkToFolderAsync(int bookmarkId, string newFolderPath)
    {
        var bookmark = await _context.Bookmarks.FindAsync(bookmarkId);
        if (bookmark == null)
        {
            return false;
        }

        bookmark.FolderPath = newFolderPath;
        bookmark.UpdatedAt = DateTime.UtcNow;
        bookmark.SortOrder = await GetNextSortOrderAsync(bookmark.UserProfileId, newFolderPath);

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get the next sort order for a bookmark in a specific folder
    /// </summary>
    private async Task<int> GetNextSortOrderAsync(int userProfileId, string? folderPath)
    {
        var maxSortOrder = await _context.Bookmarks
            .Where(b => b.UserProfileId == userProfileId && b.FolderPath == (folderPath ?? "Default"))
            .MaxAsync(b => (int?)b.SortOrder) ?? 0;

        return maxSortOrder + 1;
    }

    /// <summary>
    /// Update sort orders for bookmarks in a folder
    /// </summary>
    public async Task<bool> UpdateSortOrderAsync(int userProfileId, string folderPath, List<int> bookmarkIds)
    {
        var bookmarks = await _context.Bookmarks
            .Where(b => b.UserProfileId == userProfileId && b.FolderPath == folderPath)
            .ToListAsync();

        for (int i = 0; i < bookmarkIds.Count; i++)
        {
            var bookmark = bookmarks.FirstOrDefault(b => b.Id == bookmarkIds[i]);
            if (bookmark != null)
            {
                bookmark.SortOrder = i + 1;
                bookmark.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Import/Export

    /// <summary>
    /// Export bookmarks to JSON format
    /// </summary>
    public async Task<string> ExportBookmarksToJsonAsync(int userProfileId)
    {
        var bookmarks = await GetBookmarksAsync(userProfileId);

        var exportData = new
        {
            ExportDate = DateTime.UtcNow,
            UserProfileId = userProfileId,
            Bookmarks = bookmarks.Select(b => new
            {
                b.Title,
                b.Url,
                b.Description,
                b.FolderPath,
                b.CreatedAt,
                b.SortOrder
            })
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Export bookmarks to HTML format (Netscape Bookmark File Format)
    /// </summary>
    public async Task<string> ExportBookmarksToHtmlAsync(int userProfileId)
    {
        var bookmarks = await GetBookmarksAsync(userProfileId);
        var folders = bookmarks.GroupBy(b => b.FolderPath ?? "Default").OrderBy(g => g.Key);

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
        html.AppendLine("<!-- This is an automatically generated file.");
        html.AppendLine("     It will be read and overwritten.");
        html.AppendLine("     DO NOT EDIT! -->");
        html.AppendLine("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
        html.AppendLine("<TITLE>Bookmarks</TITLE>");
        html.AppendLine("<H1>Bookmarks Menu</H1>");
        html.AppendLine("<DL><p>");

        foreach (var folder in folders)
        {
            if (folder.Key != "Default")
            {
                html.AppendLine($"    <DT><H3>{WebUtility.HtmlEncode(folder.Key)}</H3>");
                html.AppendLine("    <DL><p>");
            }

            foreach (var bookmark in folder.OrderBy(b => b.SortOrder).ThenBy(b => b.Title))
            {
                var addDate = ((DateTimeOffset)bookmark.CreatedAt).ToUnixTimeSeconds();
                var indent = folder.Key != "Default" ? "        " : "    ";

                html.AppendLine($"{indent}<DT><A HREF=\"{WebUtility.HtmlEncode(bookmark.Url)}\" ADD_DATE=\"{addDate}\">{WebUtility.HtmlEncode(bookmark.Title)}</A>");

                if (!string.IsNullOrWhiteSpace(bookmark.Description))
                {
                    html.AppendLine($"{indent}<DD>{WebUtility.HtmlEncode(bookmark.Description)}");
                }
            }

            if (folder.Key != "Default")
            {
                html.AppendLine("    </DL><p>");
            }
        }

        html.AppendLine("</DL><p>");
        return html.ToString();
    }

    /// <summary>
    /// Import bookmarks from JSON format
    /// </summary>
    public async Task<ImportResult> ImportBookmarksFromJsonAsync(int userProfileId, string jsonContent)
    {
        var result = new ImportResult();

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.TryGetProperty("bookmarks", out var bookmarksElement))
            {
                foreach (var bookmarkElement in bookmarksElement.EnumerateArray())
                {
                    try
                    {
                        var title = bookmarkElement.GetProperty("title").GetString() ?? "Untitled";
                        var url = bookmarkElement.GetProperty("url").GetString() ?? "";
                        var description = bookmarkElement.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
                        var folderPath = bookmarkElement.TryGetProperty("folderPath", out var folderProp) ? folderProp.GetString() : "Imported";

                        if (string.IsNullOrWhiteSpace(url))
                        {
                            result.SkippedCount++;
                            continue;
                        }

                        // Check if bookmark already exists
                        var existingBookmark = await _context.Bookmarks
                            .FirstOrDefaultAsync(b => b.UserProfileId == userProfileId && b.Url == url);

                        if (existingBookmark != null)
                        {
                            result.DuplicateCount++;
                            continue;
                        }

                        await AddBookmarkAsync(userProfileId, title, url, description, folderPath);
                        result.ImportedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error importing bookmark: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorCount++;
            result.Errors.Add($"Error parsing JSON: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Import bookmarks from HTML format (Netscape Bookmark File Format)
    /// </summary>
    public async Task<ImportResult> ImportBookmarksFromHtmlAsync(int userProfileId, string htmlContent)
    {
        var result = new ImportResult();

        try
        {
            // Simple HTML parsing for bookmark files
            var lines = htmlContent.Split('\n');
            string currentFolder = "Imported";

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Check for folder
                if (trimmedLine.StartsWith("<DT><H3>") && trimmedLine.Contains("</H3>"))
                {
                    var start = trimmedLine.IndexOf(">") + 1;
                    var end = trimmedLine.IndexOf("</H3>");
                    if (start > 0 && end > start)
                    {
                        currentFolder = WebUtility.HtmlDecode(trimmedLine.Substring(start, end - start));
                    }
                }
                // Check for bookmark
                else if (trimmedLine.StartsWith("<DT><A HREF="))
                {
                    try
                    {
                        var hrefStart = trimmedLine.IndexOf("HREF=\"") + 6;
                        var hrefEnd = trimmedLine.IndexOf("\"", hrefStart);
                        var url = WebUtility.HtmlDecode(trimmedLine.Substring(hrefStart, hrefEnd - hrefStart));

                        var titleStart = trimmedLine.IndexOf(">", hrefEnd) + 1;
                        var titleEnd = trimmedLine.IndexOf("</A>");
                        var title = WebUtility.HtmlDecode(trimmedLine.Substring(titleStart, titleEnd - titleStart));

                        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(title))
                        {
                            result.SkippedCount++;
                            continue;
                        }

                        // Check if bookmark already exists
                        var existingBookmark = await _context.Bookmarks
                            .FirstOrDefaultAsync(b => b.UserProfileId == userProfileId && b.Url == url);

                        if (existingBookmark != null)
                        {
                            result.DuplicateCount++;
                            continue;
                        }

                        await AddBookmarkAsync(userProfileId, title, url, null, currentFolder);
                        result.ImportedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error importing bookmark from line: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorCount++;
            result.Errors.Add($"Error parsing HTML: {ex.Message}");
        }

        return result;
    }

    #endregion
}

/// <summary>
/// Result of bookmark import operation
/// </summary>
public class ImportResult
{
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new List<string>();

    public bool HasErrors => ErrorCount > 0;
    public int TotalProcessed => ImportedCount + DuplicateCount + SkippedCount + ErrorCount;
}
