using System.Text.Json;
using NoorAhlulBayt.Common.Models;

namespace NoorAhlulBayt.Common.Services;

/// <summary>
/// Centralized settings management service
/// </summary>
public class SettingsService
{
    private readonly string _settingsFilePath;
    private ApplicationSettings _settings;
    private readonly object _lockObject = new();

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public SettingsService(string? customPath = null)
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "NoorAhlulBayt");
        Directory.CreateDirectory(appFolder);
        
        _settingsFilePath = customPath ?? Path.Combine(appFolder, "settings.json");
        _settings = LoadSettings();
    }

    /// <summary>
    /// Get current application settings
    /// </summary>
    public ApplicationSettings GetSettings()
    {
        lock (_lockObject)
        {
            return _settings;
        }
    }

    /// <summary>
    /// Update application settings
    /// </summary>
    public async Task<bool> UpdateSettingsAsync(ApplicationSettings settings)
    {
        try
        {
            lock (_lockObject)
            {
                _settings = settings;
            }

            await SaveSettingsAsync();
            OnSettingsChanged(new SettingsChangedEventArgs { Settings = settings });
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating settings: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update specific section of settings
    /// </summary>
    public async Task<bool> UpdateGeneralSettingsAsync(GeneralSettings generalSettings)
    {
        lock (_lockObject)
        {
            _settings.General = generalSettings;
        }
        await SaveSettingsAsync();
        OnSettingsChanged(new SettingsChangedEventArgs { Settings = _settings });
        return true;
    }

    public async Task<bool> UpdatePrayerTimeSettingsAsync(PrayerTimeSettings prayerSettings)
    {
        lock (_lockObject)
        {
            _settings.PrayerTimes = prayerSettings;
        }
        await SaveSettingsAsync();
        OnSettingsChanged(new SettingsChangedEventArgs { Settings = _settings });
        return true;
    }

    public async Task<bool> UpdateContentFilteringSettingsAsync(ContentFilteringSettings filteringSettings)
    {
        lock (_lockObject)
        {
            _settings.ContentFiltering = filteringSettings;
        }
        await SaveSettingsAsync();
        OnSettingsChanged(new SettingsChangedEventArgs { Settings = _settings });
        return true;
    }

    public async Task<bool> UpdateMonitoringSettingsAsync(MonitoringSettings monitoringSettings)
    {
        lock (_lockObject)
        {
            _settings.Monitoring = monitoringSettings;
        }
        await SaveSettingsAsync();
        OnSettingsChanged(new SettingsChangedEventArgs { Settings = _settings });
        return true;
    }

    public async Task<bool> UpdateThemeSettingsAsync(ThemeSettings themeSettings)
    {
        lock (_lockObject)
        {
            _settings.Theme = themeSettings;
        }
        await SaveSettingsAsync();
        OnSettingsChanged(new SettingsChangedEventArgs { Settings = _settings });
        return true;
    }

    /// <summary>
    /// Update profile-specific settings
    /// </summary>
    public async Task<bool> UpdateProfileSettingsAsync(int profileId, ProfileSpecificSettings profileSettings)
    {
        lock (_lockObject)
        {
            _settings.ProfileSettings[profileId] = profileSettings;
        }
        await SaveSettingsAsync();
        OnSettingsChanged(new SettingsChangedEventArgs { Settings = _settings });
        return true;
    }

    /// <summary>
    /// Get settings for a specific profile (with inheritance)
    /// </summary>
    public ApplicationSettings GetEffectiveSettingsForProfile(int profileId)
    {
        lock (_lockObject)
        {
            var effectiveSettings = JsonSerializer.Deserialize<ApplicationSettings>(
                JsonSerializer.Serialize(_settings))!;

            if (_settings.ProfileSettings.TryGetValue(profileId, out var profileSettings))
            {
                if (!profileSettings.InheritGlobalSettings)
                {
                    // Override with profile-specific settings
                    if (profileSettings.PrayerTimeOverrides != null)
                        effectiveSettings.PrayerTimes = profileSettings.PrayerTimeOverrides;
                    
                    if (profileSettings.ContentFilteringOverrides != null)
                        effectiveSettings.ContentFiltering = profileSettings.ContentFilteringOverrides;
                    
                    if (profileSettings.MonitoringOverrides != null)
                        effectiveSettings.Monitoring = profileSettings.MonitoringOverrides;
                }
            }

            return effectiveSettings;
        }
    }

    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    public async Task<bool> ResetToDefaultsAsync()
    {
        _settings = new ApplicationSettings();
        await SaveSettingsAsync();
        OnSettingsChanged(new SettingsChangedEventArgs { Settings = _settings });
        return true;
    }

    /// <summary>
    /// Export settings to file
    /// </summary>
    public async Task<bool> ExportSettingsAsync(string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting settings: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Import settings from file
    /// </summary>
    public async Task<bool> ImportSettingsAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var json = await File.ReadAllTextAsync(filePath);
            var importedSettings = JsonSerializer.Deserialize<ApplicationSettings>(json);
            
            if (importedSettings != null)
            {
                await UpdateSettingsAsync(importedSettings);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing settings: {ex.Message}");
            return false;
        }
    }

    private ApplicationSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<ApplicationSettings>(json);
                return settings ?? new ApplicationSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }

        return new ApplicationSettings();
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    protected virtual void OnSettingsChanged(SettingsChangedEventArgs e)
    {
        SettingsChanged?.Invoke(this, e);
    }
}

/// <summary>
/// Event args for settings changes
/// </summary>
public class SettingsChangedEventArgs : EventArgs
{
    public ApplicationSettings Settings { get; set; } = new();
    public string? ChangedSection { get; set; }
}
