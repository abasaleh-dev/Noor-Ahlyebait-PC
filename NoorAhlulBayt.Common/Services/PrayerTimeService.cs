using System.Text.Json;
using NoorAhlulBayt.Common.Models;
using NoorAhlulBayt.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace NoorAhlulBayt.Common.Services;

public class PrayerTimeService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _context;
    private readonly SettingsService? _settingsService;
    private readonly AzanService? _azanService;
    private const string ALADHAN_API_BASE = "https://api.aladhan.com/v1";

    private DailyPrayerTimes? _todaysPrayerTimes;
    private DateTime _lastCalculationDate;

    public event EventHandler<PrayerTimeEventArgs>? PrayerTimeReached;
    public event EventHandler<DailyPrayerTimes>? PrayerTimesUpdated;

    public PrayerTimeService(HttpClient httpClient, ApplicationDbContext context, SettingsService? settingsService = null, AzanService? azanService = null)
    {
        _httpClient = httpClient;
        _context = context;
        _settingsService = settingsService;
        _azanService = azanService;
        _lastCalculationDate = DateTime.MinValue;
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

    /// <summary>
    /// Get prayer times for today using settings service
    /// </summary>
    public async Task<DailyPrayerTimes?> GetTodaysPrayerTimesAsync()
    {
        if (_settingsService == null) return null;

        var today = DateTime.Today;

        if (_todaysPrayerTimes == null || _lastCalculationDate != today)
        {
            var settings = _settingsService.GetSettings();
            var location = settings.PrayerTimes.Location;

            if (!string.IsNullOrEmpty(location.City) && !string.IsNullOrEmpty(location.Country))
            {
                var prayerTime = await FetchPrayerTimesAsync(location.City, location.Country, today, (int)settings.PrayerTimes.CalculationMethod);

                if (prayerTime != null)
                {
                    _todaysPrayerTimes = ConvertToDaily(prayerTime, settings.PrayerTimes.Adjustments);
                    _lastCalculationDate = today;
                    PrayerTimesUpdated?.Invoke(this, _todaysPrayerTimes);
                }
            }
        }

        return _todaysPrayerTimes;
    }

    /// <summary>
    /// Check if it's currently prayer time
    /// </summary>
    public async Task<(bool IsPrayerTime, string? PrayerName)> CheckCurrentPrayerTimeAsync()
    {
        var prayerTimes = await GetTodaysPrayerTimesAsync();
        if (prayerTimes == null) return (false, null);

        var now = DateTime.Now.TimeOfDay;

        // Check each prayer time (within 1 minute tolerance)
        var prayers = prayerTimes.GetAllPrayerTimes().Where(p => p.Key != "Sunrise");

        foreach (var prayer in prayers)
        {
            var timeDiff = Math.Abs((prayer.Value - now).TotalMinutes);
            if (timeDiff <= 1) // Within 1 minute of prayer time
            {
                return (true, prayer.Key);
            }
        }

        return (false, null);
    }

    /// <summary>
    /// Get next prayer information
    /// </summary>
    public async Task<(string PrayerName, TimeSpan Time, TimeSpan TimeUntil)?> GetNextPrayerAsync()
    {
        var prayerTimes = await GetTodaysPrayerTimesAsync();
        if (prayerTimes == null) return null;

        var nextPrayer = prayerTimes.GetNextPrayer();

        if (nextPrayer.HasValue)
        {
            var now = DateTime.Now.TimeOfDay;
            var timeUntil = nextPrayer.Value.Time - now;

            if (timeUntil < TimeSpan.Zero)
                timeUntil = timeUntil.Add(TimeSpan.FromDays(1));

            return (nextPrayer.Value.PrayerName, nextPrayer.Value.Time, timeUntil);
        }

        return null;
    }

    /// <summary>
    /// Start monitoring for prayer times
    /// </summary>
    public void StartPrayerTimeMonitoring()
    {
        var timer = new System.Timers.Timer(60000); // Check every minute
        timer.Elapsed += async (sender, e) => await CheckForPrayerTimeAsync();
        timer.Start();
    }

    private async Task CheckForPrayerTimeAsync()
    {
        if (_settingsService == null) return;

        var (isPrayerTime, prayerName) = await CheckCurrentPrayerTimeAsync();

        if (isPrayerTime && !string.IsNullOrEmpty(prayerName))
        {
            var settings = _settingsService.GetSettings();
            if (settings.PrayerTimes.EnablePrayerNotifications)
            {
                // Play Azan if enabled and service is available
                if (_azanService != null && settings.PrayerTimes.Azan.PlayAzan)
                {
                    _ = Task.Run(async () => await _azanService.PlayAzanAsync(settings.PrayerTimes.Azan, prayerName));
                }

                PrayerTimeReached?.Invoke(this, new PrayerTimeEventArgs
                {
                    PrayerName = prayerName,
                    PrayerTime = DateTime.Now,
                    Settings = settings.PrayerTimes
                });
            }
        }
    }

    private DailyPrayerTimes ConvertToDaily(PrayerTime prayerTime, PrayerAdjustments adjustments)
    {
        var daily = new DailyPrayerTimes
        {
            Date = prayerTime.Date,
            Fajr = prayerTime.Fajr.Add(TimeSpan.FromMinutes(adjustments.Fajr)),
            Sunrise = prayerTime.Sunrise.Add(TimeSpan.FromMinutes(adjustments.Sunrise)),
            Dhuhr = prayerTime.Dhuhr.Add(TimeSpan.FromMinutes(adjustments.Dhuhr)),
            Asr = prayerTime.Asr.Add(TimeSpan.FromMinutes(adjustments.Asr)),
            Maghrib = prayerTime.Maghrib.Add(TimeSpan.FromMinutes(adjustments.Maghrib)),
            Isha = prayerTime.Isha.Add(TimeSpan.FromMinutes(adjustments.Isha))
        };

        return daily;
    }

    /// <summary>
    /// Auto-detect location using IP geolocation
    /// </summary>
    public async Task<LocationSettings?> AutoDetectLocationAsync()
    {
        try
        {
            // In a real implementation, you would use a geolocation service
            // For now, return a default location (Mecca)
            return new LocationSettings
            {
                City = "Mecca",
                Country = "Saudi Arabia",
                Latitude = 21.4225,
                Longitude = 39.8262,
                TimeZone = "Asia/Riyadh",
                AutoDetectLocation = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error auto-detecting location: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Validate prayer time settings
    /// </summary>
    public bool ValidatePrayerTimeSettings(PrayerTimeSettings settings)
    {
        if (settings.Location.Latitude < -90 || settings.Location.Latitude > 90)
            return false;

        if (settings.Location.Longitude < -180 || settings.Location.Longitude > 180)
            return false;

        if (string.IsNullOrWhiteSpace(settings.Location.City))
            return false;

        return true;
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

/// <summary>
/// Event args for prayer time notifications
/// </summary>
public class PrayerTimeEventArgs : EventArgs
{
    public string PrayerName { get; set; } = "";
    public DateTime PrayerTime { get; set; }
    public PrayerTimeSettings Settings { get; set; } = new();
}