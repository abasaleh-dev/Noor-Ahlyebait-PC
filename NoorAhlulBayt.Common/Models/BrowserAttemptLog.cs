using System.ComponentModel.DataAnnotations;

namespace NoorAhlulBayt.Common.Models;

/// <summary>
/// Model for logging browser attempt events
/// </summary>
public class BrowserAttemptLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string BrowserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ProcessPath { get; set; } = string.Empty;

    [Required]
    public DateTime AttemptTime { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // 'DETECTED', 'BLOCKED', 'ALLOWED', 'TERMINATED'

    // Foreign key to UserProfile (nullable for now)
    public int? UserProfileId { get; set; }
    public UserProfile? UserProfile { get; set; }

    // Additional metadata
    public string? AdditionalInfo { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
