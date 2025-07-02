using System.Windows;
using System.Windows.Input;
using NoorAhlulBayt.Common.Models;
using NoorAhlulBayt.Common.Services;
using NoorAhlulBayt.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace NoorAhlulBayt.Browser;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ApplicationDbContext _context;
    private UserProfile _currentProfile;
    private Settings _appSettings;
    private bool _hasExistingPin;

    public SettingsWindow(ApplicationDbContext context, UserProfile currentProfile, Settings appSettings)
    {
        InitializeComponent();
        _context = context;
        _currentProfile = currentProfile;
        _appSettings = appSettings;
        _hasExistingPin = !string.IsNullOrEmpty(_currentProfile.EncryptedPin);
        
        LoadSettings();
        PopulateCountryComboBox();
    }

    private void LoadSettings()
    {
        // Load PIN settings
        RequirePinForSettingsCheckBox.IsChecked = _currentProfile.RequirePinForSettings;
        
        // Load content filtering settings
        EnableProfanityFilterCheckBox.IsChecked = _currentProfile.EnableProfanityFilter;
        EnableNsfwFilterCheckBox.IsChecked = _currentProfile.EnableNsfwFilter;
        EnableAdBlockerCheckBox.IsChecked = _currentProfile.EnableAdBlocker;
        EnableSafeSearchCheckBox.IsChecked = _currentProfile.EnableSafeSearch;
        
        // Load time management settings
        DailyTimeLimitTextBox.Text = _currentProfile.DailyTimeLimitMinutes.ToString();

        if (_currentProfile.AllowedStartTime.HasValue)
        {
            AllowedStartTimeTextBox.Text = _currentProfile.AllowedStartTime.Value.ToString(@"hh\:mm");
        }

        if (_currentProfile.AllowedEndTime.HasValue)
        {
            AllowedEndTimeTextBox.Text = _currentProfile.AllowedEndTime.Value.ToString(@"hh\:mm");
        }
        
        // Load prayer time settings
        EnableAzanBlockingCheckBox.IsChecked = _currentProfile.EnableAzanBlocking;
        CityTextBox.Text = _currentProfile.City ?? "";
        CountryComboBox.Text = _currentProfile.Country ?? "US";
        AzanDurationTextBox.Text = _currentProfile.AzanBlockingDurationMinutes.ToString();
    }

    private void PopulateCountryComboBox()
    {
        var countries = new[]
        {
            "US", "CA", "GB", "AU", "DE", "FR", "IT", "ES", "NL", "BE", "CH", "AT", "SE", "NO", "DK", "FI",
            "SA", "AE", "QA", "KW", "BH", "OM", "JO", "LB", "SY", "IQ", "IR", "AF", "PK", "BD", "IN",
            "MY", "ID", "SG", "TH", "PH", "VN", "KR", "JP", "CN", "HK", "TW", "TR", "EG", "MA", "TN",
            "DZ", "LY", "SD", "ET", "KE", "TZ", "UG", "ZA", "NG", "GH", "SN", "ML", "BF", "NE", "TD"
        };
        
        foreach (var country in countries)
        {
            CountryComboBox.Items.Add(country);
        }
    }

    private void SetPin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var currentPin = CurrentPinBox.Password;
            var newPin = NewPinBox.Password;
            var confirmPin = ConfirmPinBox.Password;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(newPin))
            {
                MessageBox.Show("Please enter a new PIN.", "Validation Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPin.Length < 4)
            {
                MessageBox.Show("PIN must be at least 4 characters long.", "Validation Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPin != confirmPin)
            {
                MessageBox.Show("New PIN and confirmation do not match.", "Validation Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // If there's an existing PIN, verify current PIN
            if (_hasExistingPin)
            {
                if (string.IsNullOrWhiteSpace(currentPin))
                {
                    MessageBox.Show("Please enter your current PIN.", "Validation Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!CryptographyService.VerifyPin(currentPin, _currentProfile.EncryptedPin!))
                {
                    MessageBox.Show("Current PIN is incorrect.", "Authentication Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Encrypt and save new PIN
            _currentProfile.EncryptedPin = CryptographyService.EncryptPin(newPin);
            _hasExistingPin = true;

            // Clear PIN fields
            CurrentPinBox.Clear();
            NewPinBox.Clear();
            ConfirmPinBox.Clear();

            MessageBox.Show("PIN has been set successfully.", "Success", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error setting PIN: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate time limit input
            if (!int.TryParse(DailyTimeLimitTextBox.Text, out int timeLimit) || timeLimit < 0)
            {
                MessageBox.Show("Please enter a valid daily time limit (0 or positive number).", 
                              "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate Azan duration input
            if (!int.TryParse(AzanDurationTextBox.Text, out int azanDuration) || azanDuration < 1)
            {
                MessageBox.Show("Please enter a valid Azan blocking duration (1 or more minutes).", 
                              "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update profile settings
            _currentProfile.RequirePinForSettings = RequirePinForSettingsCheckBox.IsChecked ?? true;
            
            // Content filtering
            _currentProfile.EnableProfanityFilter = EnableProfanityFilterCheckBox.IsChecked ?? true;
            _currentProfile.EnableNsfwFilter = EnableNsfwFilterCheckBox.IsChecked ?? true;
            _currentProfile.EnableAdBlocker = EnableAdBlockerCheckBox.IsChecked ?? true;
            _currentProfile.EnableSafeSearch = EnableSafeSearchCheckBox.IsChecked ?? true;
            
            // Time management
            _currentProfile.DailyTimeLimitMinutes = timeLimit;

            // Parse start time
            if (!string.IsNullOrWhiteSpace(AllowedStartTimeTextBox.Text))
            {
                if (TimeSpan.TryParse(AllowedStartTimeTextBox.Text, out TimeSpan startTime))
                {
                    _currentProfile.AllowedStartTime = startTime;
                }
                else
                {
                    MessageBox.Show("Please enter a valid start time in HH:MM format.",
                                  "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                _currentProfile.AllowedStartTime = null;
            }

            // Parse end time
            if (!string.IsNullOrWhiteSpace(AllowedEndTimeTextBox.Text))
            {
                if (TimeSpan.TryParse(AllowedEndTimeTextBox.Text, out TimeSpan endTime))
                {
                    _currentProfile.AllowedEndTime = endTime;
                }
                else
                {
                    MessageBox.Show("Please enter a valid end time in HH:MM format.",
                                  "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                _currentProfile.AllowedEndTime = null;
            }
            
            // Prayer time settings
            _currentProfile.EnableAzanBlocking = EnableAzanBlockingCheckBox.IsChecked ?? true;
            _currentProfile.City = string.IsNullOrWhiteSpace(CityTextBox.Text) ? null : CityTextBox.Text.Trim();
            _currentProfile.Country = string.IsNullOrWhiteSpace(CountryComboBox.Text) ? "US" : CountryComboBox.Text.Trim();
            _currentProfile.AzanBlockingDurationMinutes = azanDuration;
            
            // Update timestamp
            _currentProfile.UpdatedAt = DateTime.UtcNow;

            // Save to database
            _context.UserProfiles.Update(_currentProfile);
            await _context.SaveChangesAsync();

            MessageBox.Show("Settings saved successfully!", "Success", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
            
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click to maximize/restore (disabled for settings window)
            return;
        }
        else
        {
            // Single click to drag
            this.DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
