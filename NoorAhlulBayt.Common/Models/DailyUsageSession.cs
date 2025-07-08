using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoorAhlulBayt.Common.Models;

public class DailyUsageSession
{
    [Key]
    public int Id { get; set; }

    // Foreign Key
    public int UserProfileId { get; set; }

    // Session tracking
    public DateTime Date { get; set; } = DateTime.Today; // Date only (no time)
    public DateTime SessionStart { get; set; } = DateTime.Now;
    public DateTime? SessionEnd { get; set; }
    public int DurationMinutes { get; set; } = 0; // Total minutes for this session
    
    // Session state
    public bool IsActive { get; set; } = true; // Whether this session is currently active
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey(nameof(UserProfileId))]
    public virtual UserProfile UserProfile { get; set; } = null!;

    /// <summary>
    /// Calculate the current session duration in minutes
    /// </summary>
    public int GetCurrentDurationMinutes()
    {
        var endTime = SessionEnd ?? DateTime.Now;
        return (int)(endTime - SessionStart).TotalMinutes;
    }

    /// <summary>
    /// End the current session and update duration
    /// </summary>
    public void EndSession()
    {
        if (IsActive)
        {
            SessionEnd = DateTime.Now;
            DurationMinutes = GetCurrentDurationMinutes();
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
