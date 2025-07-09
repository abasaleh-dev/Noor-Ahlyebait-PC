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
    }

    private void LoadSettings()
    {
        // Load PIN settings only - all other settings are managed in Companion app
        RequirePinForSettingsCheckBox.IsChecked = _currentProfile.RequirePinForSettings;
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
            // Update only PIN settings - all other settings are managed in Companion app
            _currentProfile.RequirePinForSettings = RequirePinForSettingsCheckBox.IsChecked ?? true;

            // Update timestamp
            _currentProfile.UpdatedAt = DateTime.UtcNow;

            // Save to database
            _context.UserProfiles.Update(_currentProfile);
            await _context.SaveChangesAsync();

            MessageBox.Show("PIN settings saved successfully!", "Success",
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
