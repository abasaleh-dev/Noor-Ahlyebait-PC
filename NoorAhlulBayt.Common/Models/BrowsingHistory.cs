using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoorAhlulBayt.Common.Models;

public class BrowsingHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Url { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;

    public int VisitCount { get; set; } = 1;

    public bool IsIncognito { get; set; } = false;

    // Content filtering results
    public bool WasBlocked { get; set; } = false;
    public string? BlockReason { get; set; } // "Profanity", "NSFW", "Blacklist", etc.

    // Foreign Key
    public int UserProfileId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(UserProfileId))]
    public virtual UserProfile UserProfile { get; set; } = null!;
}