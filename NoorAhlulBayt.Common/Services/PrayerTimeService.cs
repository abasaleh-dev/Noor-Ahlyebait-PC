using System.Text.Json;
using NoorAhlulBayt.Common.Models;
using NoorAhlulBayt.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace NoorAhlulBayt.Common.Services;

public class PrayerTimeService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _context;
    private const string ALADHAN_API_BASE = "https://api.aladhan.com/v1";

    public PrayerTimeService(HttpClient httpClient, ApplicationDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
    }

    /// <summary>
    /// Fetches prayer times for a specific city and date from Aladhan API
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="country">Country name</param>
    /// <param name="date">Date for prayer times</param>
    /// <param name="method">Calculation method (default: 2)</param>
    /// <returns>PrayerTime object or null if failed</returns>
    public async Task<PrayerTime?> FetchPrayerTimesAsync(string city, string country, DateTime date, int method = 2)
    {
        try
        {
            // Check if we already have this data in database
            var existingPrayerTime = await _context.PrayerTimes
                .FirstOrDefaultAsync(pt => pt.City == city &&
                                         pt.Country == country &&
                                         pt.Date.Date == date.Date);

            if (existingPrayerTime != null)
                return existingPrayerTime;

            // Format date for API (DD-MM-YYYY)
            string dateString = date.ToString("dd-MM-yyyy");

            // Build API URL
            string url = $"{ALADHAN_API_BASE}/timingsByCity/{dateString}?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&method={method}";

            // Make API request
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<AladhanApiResponse>(jsonContent);

            if (apiResponse?.Code == 200 && apiResponse.Data?.Timings != null)
            {
                var prayerTime = new PrayerTime
                {
                    Date = date.Date,
                    City = city,
                    Country = country,
                    CalculationMethod = method,
                    Fajr = ParseTimeString(apiResponse.Data.Timings.Fajr),
                    Sunrise = ParseTimeString(apiResponse.Data.Timings.Sunrise),
                    Dhuhr = ParseTimeString(apiResponse.Data.Timings.Dhuhr),
                    Asr = ParseTimeString(apiResponse.Data.Timings.Asr),
                    Maghrib = ParseTimeString(apiResponse.Data.Timings.Maghrib),
                    Isha = ParseTimeString(apiResponse.Data.Timings.Isha)
                };

                // Save to database
                _context.PrayerTimes.Add(prayerTime);
                await _context.SaveChangesAsync();

                return prayerTime;
            }
        }
        catch (Exception ex)
        {
            // Log error (in a real app, use proper logging)
            Console.WriteLine($"Error fetching prayer times: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Gets prayer times for today for a specific city
    /// </summary>
    public async Task<PrayerTime?> GetTodaysPrayerTimesAsync(string city, string country, int method = 2)
    {
        return await FetchPrayerTimesAsync(city, country, DateTime.Today, method);
    }

    /// <summary>
    /// Checks if current time is within Azan period
    /// </summary>
    public async Task<bool> IsCurrentlyAzanTimeAsync(string city, string country, int azanDurationMinutes = 10)
    {
        var prayerTimes = await GetTodaysPrayerTimesAsync(city, country);
        if (prayerTimes == null) return false;

        var currentTime = DateTime.Now.TimeOfDay;
        return prayerTimes.IsAzanTime(currentTime, azanDurationMinutes);
    }

    /// <summary>
    /// Gets the next prayer time for today
    /// </summary>
    public async Task<(string PrayerName, TimeSpan Time)?> GetNextPrayerAsync(string city, string country)
    {
        var prayerTimes = await GetTodaysPrayerTimesAsync(city, country);
        if (prayerTimes == null) return null;

        var currentTime = DateTime.Now.TimeOfDay;
        return prayerTimes.GetNextPrayer(currentTime);
    }

    /// <summary>
    /// Parses time string from API (format: "HH:MM (TIMEZONE)")
    /// </summary>
    private TimeSpan ParseTimeString(string timeString)
    {
        if (string.IsNullOrEmpty(timeString)) return TimeSpan.Zero;

        // Remove timezone info if present
        var timePart = timeString.Split(' ')[0];

        if (TimeSpan.TryParse(timePart, out var time))
            return time;

        return TimeSpan.Zero;
    }
}

// DTOs for Aladhan API
public class AladhanApiResponse
{
    public int Code { get; set; }
    public string Status { get; set; } = string.Empty;
    public AladhanData? Data { get; set; }
}

public class AladhanData
{
    public AladhanTimings? Timings { get; set; }
}

public class AladhanTimings
{
    public string Fajr { get; set; } = string.Empty;
    public string Sunrise { get; set; } = string.Empty;
    public string Dhuhr { get; set; } = string.Empty;
    public string Asr { get; set; } = string.Empty;
    public string Maghrib { get; set; } = string.Empty;
    public string Isha { get; set; } = string.Empty;
}