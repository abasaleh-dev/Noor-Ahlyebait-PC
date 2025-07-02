using System.Windows;
using System.Windows.Input;
using NoorAhlulBayt.Common.Services;

namespace NoorAhlulBayt.Browser;

/// <summary>
/// Interaction logic for PinVerificationDialog.xaml
/// </summary>
public partial class PinVerificationDialog : Window
{
    private readonly string _encryptedPin;
    private readonly string _purpose;
    private int _attemptCount = 0;
    private const int MaxAttempts = 3;

    public bool IsVerified { get; private set; } = false;

    public PinVerificationDialog(string encryptedPin, string purpose = "access this feature")
    {
        InitializeComponent();
        _encryptedPin = encryptedPin;
        _purpose = purpose;
        
        MessageTextBlock.Text = $"Please enter your PIN to {_purpose}:";
        PinPasswordBox.Focus();
    }

    private void Verify_Click(object sender, RoutedEventArgs e)
    {
        VerifyPin();
    }

    private void PinPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            VerifyPin();
        }
    }

    private void VerifyPin()
    {
        try
        {
            var enteredPin = PinPasswordBox.Password;
            
            if (string.IsNullOrWhiteSpace(enteredPin))
            {
                ShowError("Please enter your PIN.");
                return;
            }

            if (CryptographyService.VerifyPin(enteredPin, _encryptedPin))
            {
                IsVerified = true;
                DialogResult = true;
                Close();
            }
            else
            {
                _attemptCount++;
                
                if (_attemptCount >= MaxAttempts)
                {
                    ShowError($"Maximum attempts ({MaxAttempts}) exceeded. Access denied.");
                    // Use a timer instead of await to avoid async issues
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        DialogResult = false;
                        Close();
                    };
                    timer.Start();
                    return;
                }
                else
                {
                    var remainingAttempts = MaxAttempts - _attemptCount;
                    ShowError($"Incorrect PIN. {remainingAttempts} attempt(s) remaining.");
                    PinPasswordBox.Clear();
                    PinPasswordBox.Focus();
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error verifying PIN: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Static method to show PIN verification dialog
    /// </summary>
    /// <param name="owner">Owner window</param>
    /// <param name="encryptedPin">Encrypted PIN to verify against</param>
    /// <param name="purpose">Purpose description for the dialog</param>
    /// <returns>True if PIN was verified successfully</returns>
    public static bool ShowDialog(Window owner, string encryptedPin, string purpose = "access this feature")
    {
        if (string.IsNullOrEmpty(encryptedPin))
        {
            // No PIN set, allow access
            return true;
        }

        var dialog = new PinVerificationDialog(encryptedPin, purpose)
        {
            Owner = owner
        };

        var result = dialog.ShowDialog();
        return result == true && dialog.IsVerified;
    }
}
