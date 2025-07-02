using System.ComponentModel.DataAnnotations;

namespace NoorAhlulBayt.Common.Models;

public class PrayerTime
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    // Prayer Times (stored as TimeSpan for the day)
    public TimeSpan Fajr { get; set; }
    public TimeSpan Sunrise { get; set; }
    public TimeSpan Dhuhr { get; set; }
    public TimeSpan Asr { get; set; }
    public TimeSpan Maghrib { get; set; }
    public TimeSpan Isha { get; set; }

    // Calculation method used (Aladhan API method number)
    public int CalculationMethod { get; set; } = 2;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Helper method to get next prayer time
    public (string PrayerName, TimeSpan Time)? GetNextPrayer(TimeSpan currentTime)
    {
        var prayers = new[]
        {
            ("Fajr", Fajr),
            ("Sunrise", Sunrise),
            ("Dhuhr", Dhuhr),
            ("Asr", Asr),
            ("Maghrib", Maghrib),
            ("Isha", Isha)
        };

        return prayers.FirstOrDefault(p => p.Item2 > currentTime);
    }

    // Helper method to check if current time is within Azan period
    public bool IsAzanTime(TimeSpan currentTime, int azanDurationMinutes = 10)
    {
        var prayers = new[] { Fajr, Dhuhr, Asr, Maghrib, Isha };

        foreach (var prayerTime in prayers)
        {
            var azanStart = prayerTime;
            var azanEnd = prayerTime.Add(TimeSpan.FromMinutes(azanDurationMinutes));

            if (currentTime >= azanStart && currentTime <= azanEnd)
                return true;
        }

        return false;
    }
}