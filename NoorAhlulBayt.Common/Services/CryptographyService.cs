using System.Security.Cryptography;
using System.Text;

namespace NoorAhlulBayt.Common.Services;

public class CryptographyService
{
    private const string ENTROPY_STRING = "NoorAhlulBayt-Islamic-Browser-2024";

    /// <summary>
    /// Encrypts a PIN using Windows DPAPI (Data Protection API)
    /// </summary>
    /// <param name="pin">The PIN to encrypt</param>
    /// <returns>Base64 encoded encrypted PIN</returns>
    public static string EncryptPin(string pin)
    {
        if (string.IsNullOrEmpty(pin))
            throw new ArgumentException("PIN cannot be null or empty", nameof(pin));

        try
        {
            // Convert PIN to bytes
            byte[] pinBytes = Encoding.UTF8.GetBytes(pin);

            // Create entropy for additional security
            byte[] entropy = Encoding.UTF8.GetBytes(ENTROPY_STRING);

            // Encrypt using DPAPI
            byte[] encryptedBytes = ProtectedData.Protect(
                pinBytes,
                entropy,
                DataProtectionScope.CurrentUser
            );

            // Return as Base64 string
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to encrypt PIN", ex);
        }
    }

    /// <summary>
    /// Decrypts a PIN using Windows DPAPI
    /// </summary>
    /// <param name="encryptedPin">Base64 encoded encrypted PIN</param>
    /// <returns>Decrypted PIN</returns>
    public static string DecryptPin(string encryptedPin)
    {
        if (string.IsNullOrEmpty(encryptedPin))
            throw new ArgumentException("Encrypted PIN cannot be null or empty", nameof(encryptedPin));

        try
        {
            // Convert from Base64
            byte[] encryptedBytes = Convert.FromBase64String(encryptedPin);

            // Create entropy
            byte[] entropy = Encoding.UTF8.GetBytes(ENTROPY_STRING);

            // Decrypt using DPAPI
            byte[] decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                entropy,
                DataProtectionScope.CurrentUser
            );

            // Return as string
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decrypt PIN", ex);
        }
    }

    /// <summary>
    /// Verifies if a provided PIN matches the encrypted PIN
    /// </summary>
    /// <param name="providedPin">PIN provided by user</param>
    /// <param name="encryptedPin">Stored encrypted PIN</param>
    /// <returns>True if PINs match</returns>
    public static bool VerifyPin(string providedPin, string encryptedPin)
    {
        if (string.IsNullOrEmpty(providedPin) || string.IsNullOrEmpty(encryptedPin))
            return false;

        try
        {
            string decryptedPin = DecryptPin(encryptedPin);
            return providedPin.Equals(decryptedPin, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a secure hash for additional validation
    /// </summary>
    /// <param name="input">Input string to hash</param>
    /// <returns>SHA256 hash as hex string</returns>
    public static string GenerateHash(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input cannot be null or empty", nameof(input));

        using var sha256 = SHA256.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = sha256.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}