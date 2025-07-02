using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoorAhlulBayt.Common.Models;

public class Bookmark
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? FolderPath { get; set; } // e.g., "Islamic/Quran", "News", etc.

    public string? FaviconBase64 { get; set; } // Base64 encoded favicon

    public int SortOrder { get; set; } = 0;

    // Foreign Key
    public int UserProfileId { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey(nameof(UserProfileId))]
    public virtual UserProfile UserProfile { get; set; } = null!;
}