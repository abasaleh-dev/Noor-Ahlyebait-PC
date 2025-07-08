using System.Security.Cryptography;
using System.Text;
using NoorAhlulBayt.Common.Models;
using NoorAhlulBayt.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace NoorAhlulBayt.Common.Services;

/// <summary>
/// Service for managing master password authentication for the companion app
/// </summary>
public class MasterPasswordService
{
    private readonly ApplicationDbContext _context;

    public MasterPasswordService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Check if master password is set up
    /// </summary>
    public async Task<bool> IsMasterPasswordSetAsync()
    {
        try
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            return settings != null && 
                   !string.IsNullOrEmpty(settings.MasterPasswordHash) && 
                   !string.IsNullOrEmpty(settings.MasterPasswordSalt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking master password setup: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Set up master password for first time
    /// </summary>
    public async Task<bool> SetupMasterPasswordAsync(string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty");
            }

            if (password.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters long");
            }

            // Generate salt
            var salt = GenerateSalt();
            
            // Hash password with salt
            var hash = HashPassword(password, salt);

            // Get or create settings
            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new Settings();
                _context.Settings.Add(settings);
            }

            // Store encrypted hash and salt
            settings.MasterPasswordHash = CryptographyService.EncryptPin(hash);
            settings.MasterPasswordSalt = CryptographyService.EncryptPin(salt);
            settings.RequireMasterPassword = true;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting up master password: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verify master password
    /// </summary>
    public async Task<bool> VerifyMasterPasswordAsync(string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null || 
                string.IsNullOrEmpty(settings.MasterPasswordHash) || 
                string.IsNullOrEmpty(settings.MasterPasswordSalt))
            {
                return false;
            }

            // Decrypt stored hash and salt
            var storedHash = CryptographyService.DecryptPin(settings.MasterPasswordHash);
            var storedSalt = CryptographyService.DecryptPin(settings.MasterPasswordSalt);

            // Hash the provided password with stored salt
            var providedHash = HashPassword(password, storedSalt);

            // Compare hashes
            return storedHash == providedHash;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying master password: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Change master password
    /// </summary>
    public async Task<bool> ChangeMasterPasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            // Verify current password first
            if (!await VerifyMasterPasswordAsync(currentPassword))
            {
                return false;
            }

            // Set up new password
            return await SetupMasterPasswordAsync(newPassword);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error changing master password: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if master password is required
    /// </summary>
    public async Task<bool> IsMasterPasswordRequiredAsync()
    {
        try
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            return settings?.RequireMasterPassword ?? true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking master password requirement: {ex.Message}");
            return true; // Default to requiring password for security
        }
    }

    /// <summary>
    /// Generate a random salt for password hashing
    /// </summary>
    private string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

    /// <summary>
    /// Hash password with salt using PBKDF2
    /// </summary>
    private string HashPassword(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
        {
            var hashBytes = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
