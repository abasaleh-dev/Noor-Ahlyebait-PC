using System.IO;
using System.Media;
using System.Diagnostics;
using NoorAhlulBayt.Common.Models;

namespace NoorAhlulBayt.Common.Services;

/// <summary>
/// Service for handling Azan audio playback with support for both WAV and MP3 files
/// </summary>
public class AzanService : IDisposable
{
    private SoundPlayer? _soundPlayer;
    private Process? _mediaProcess;
    private string _azanFolderPath;
    private bool _disposed = false;

    public AzanService()
    {
        // Try multiple possible locations for the Azan folder
        var possiblePaths = new[]
        {
            // 1. Application directory
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Azan"),

            // 2. Companion project folder (for development)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "NoorAhlulBayt.Companion", "Azan"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "NoorAhlulBayt.Companion", "Azan"),

            // 3. Solution root (for development)
            Path.Combine(Directory.GetCurrentDirectory(), "NoorAhlulBayt.Companion", "Azan"),

            // 4. AppData fallback
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoorAhlulBayt", "Companion", "Azan")
        };

        // Find the first existing path
        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            Console.WriteLine($"Checking Azan path: {fullPath}");

            if (Directory.Exists(fullPath))
            {
                _azanFolderPath = fullPath;
                Console.WriteLine($"✅ Found Azan folder: {_azanFolderPath}");
                break;
            }
        }

        // If no existing folder found, use the first option and create it
        if (string.IsNullOrEmpty(_azanFolderPath))
        {
            _azanFolderPath = Path.GetFullPath(possiblePaths[0]);
            Console.WriteLine($"Creating new Azan folder: {_azanFolderPath}");
            Directory.CreateDirectory(_azanFolderPath);
        }

        Console.WriteLine($"AzanService initialized with path: {_azanFolderPath}");

        // List available files for debugging
        if (Directory.Exists(_azanFolderPath))
        {
            var files = Directory.GetFiles(_azanFolderPath, "*.*")
                .Where(f => f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Console.WriteLine($"Available Azan files ({files.Length}):");
            foreach (var file in files)
            {
                Console.WriteLine($"  🎵 {Path.GetFileName(file)}");
            }
        }
    }

    /// <summary>
    /// Play Azan audio file with specified settings
    /// </summary>
    /// <param name="azanSettings">Azan playback settings</param>
    /// <param name="prayerName">Name of the prayer (for logging)</param>
    /// <returns>True if playback started successfully</returns>
    public async Task<bool> PlayAzanAsync(AzanSettings azanSettings, string prayerName = "")
    {
        try
        {
            if (!azanSettings.PlayAzan)
            {
                Console.WriteLine("Azan playback is disabled in settings");
                return false;
            }

            // Check if this prayer should play Azan
            if (!azanSettings.PlayForAllPrayers && 
                azanSettings.PrayerSpecificAzan.ContainsKey(prayerName) &&
                !azanSettings.PrayerSpecificAzan[prayerName])
            {
                Console.WriteLine($"Azan playback disabled for {prayerName} prayer");
                return false;
            }

            var azanFile = FindAzanFile(azanSettings.AzanAudioFile);
            if (string.IsNullOrEmpty(azanFile))
            {
                Console.WriteLine($"Azan audio file not found: {azanSettings.AzanAudioFile}");
                return false;
            }

            // Stop any currently playing audio
            await StopAzanAsync();

            // Determine file type and play accordingly
            var fileExtension = Path.GetExtension(azanFile).ToLower();

            if (fileExtension == ".wav")
            {
                // Use SoundPlayer for WAV files
                _soundPlayer = new SoundPlayer(azanFile);
                _soundPlayer.Play();
                Console.WriteLine($"Playing WAV Azan for {prayerName}: {Path.GetFileName(azanFile)}");
            }
            else if (fileExtension == ".mp3")
            {
                // Use Windows Media Player for MP3 files via command line
                await PlayMp3FileAsync(azanFile, azanSettings.Volume);
                Console.WriteLine($"Playing MP3 Azan for {prayerName}: {Path.GetFileName(azanFile)}");
            }
            else
            {
                Console.WriteLine($"Unsupported audio format: {fileExtension}");
                return false;
            }

            Console.WriteLine($"Started playing Azan for {prayerName}: {Path.GetFileName(azanFile)} at {azanSettings.Volume}% volume");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing Azan: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stop currently playing Azan
    /// </summary>
    public async Task StopAzanAsync()
    {
        try
        {
            // Stop SoundPlayer if playing
            if (_soundPlayer != null)
            {
                _soundPlayer.Stop();
                _soundPlayer.Dispose();
                _soundPlayer = null;
            }

            // Stop media process if running
            if (_mediaProcess != null && !_mediaProcess.HasExited)
            {
                _mediaProcess.Kill();
                _mediaProcess.Dispose();
                _mediaProcess = null;
            }

            Console.WriteLine("Azan playback stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping Azan: {ex.Message}");
        }
    }

    /// <summary>
    /// Test Azan playback with specified file and volume
    /// </summary>
    /// <param name="azanFileName">Name of the Azan file to test</param>
    /// <param name="volume">Volume level (0-100)</param>
    /// <param name="durationSeconds">Duration to play in seconds (0 = full file)</param>
    public async Task<bool> TestAzanAsync(string azanFileName, int volume = 70, int durationSeconds = 30)
    {
        try
        {
            var azanFile = FindAzanFile(azanFileName);
            if (string.IsNullOrEmpty(azanFile))
            {
                Console.WriteLine($"Test Azan file not found: {azanFileName}");
                return false;
            }

            // Stop any currently playing audio
            await StopAzanAsync();

            // Determine file type and play accordingly
            var fileExtension = Path.GetExtension(azanFile).ToLower();

            if (fileExtension == ".wav")
            {
                // Use SoundPlayer for WAV files
                _soundPlayer = new SoundPlayer(azanFile);
                _soundPlayer.Play();
            }
            else if (fileExtension == ".mp3")
            {
                // Use Windows Media Player for MP3 files
                await PlayMp3FileAsync(azanFile, volume);
            }
            else
            {
                Console.WriteLine($"Unsupported audio format for testing: {fileExtension}");
                return false;
            }

            Console.WriteLine($"Testing Azan: {Path.GetFileName(azanFile)} at {volume}% volume");

            // Auto-stop after specified duration
            if (durationSeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(durationSeconds));
                await StopAzanAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing Azan: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Find Azan audio file by name, supporting both WAV and MP3 formats
    /// </summary>
    /// <param name="fileName">Base file name without extension</param>
    /// <returns>Full path to the audio file, or null if not found</returns>
    private string? FindAzanFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        try
        {
            Console.WriteLine($"Looking for Azan file: {fileName} in directory: {_azanFolderPath}");

            // Remove extension if provided
            var baseName = Path.GetFileNameWithoutExtension(fileName);

            // Try different file name patterns and extensions
            var possibleFiles = new[]
            {
                // Exact match
                Path.Combine(_azanFolderPath, $"{baseName}.mp3"),
                Path.Combine(_azanFolderPath, $"{baseName}.wav"),
                Path.Combine(_azanFolderPath, $"{fileName}.mp3"),
                Path.Combine(_azanFolderPath, $"{fileName}.wav"),

                // With prefixes
                Path.Combine(_azanFolderPath, $"azan_{baseName}.mp3"),
                Path.Combine(_azanFolderPath, $"azan_{baseName}.wav"),
                Path.Combine(_azanFolderPath, $"adhan_{baseName}.mp3"),
                Path.Combine(_azanFolderPath, $"adhan_{baseName}.wav"),

                // Alternative naming patterns
                Path.Combine(_azanFolderPath, $"{baseName.Replace("_", "-")}.mp3"),
                Path.Combine(_azanFolderPath, $"{baseName.Replace("_", "-")}.wav"),
                Path.Combine(_azanFolderPath, $"{baseName.Replace("-", "_")}.mp3"),
                Path.Combine(_azanFolderPath, $"{baseName.Replace("-", "_")}.wav")
            };

            // Log all files being checked
            foreach (var file in possibleFiles)
            {
                Console.WriteLine($"Checking: {file} - Exists: {File.Exists(file)}");
                if (File.Exists(file))
                {
                    Console.WriteLine($"Found Azan file: {file}");
                    return file;
                }
            }

            // If no exact match, try fuzzy matching
            if (Directory.Exists(_azanFolderPath))
            {
                var allFiles = Directory.GetFiles(_azanFolderPath, "*.wav")
                    .Concat(Directory.GetFiles(_azanFolderPath, "*.mp3"))
                    .ToArray();

                Console.WriteLine($"Available files in {_azanFolderPath}:");
                foreach (var file in allFiles)
                {
                    Console.WriteLine($"  - {Path.GetFileName(file)}");
                }

                // Try fuzzy matching
                var fuzzyMatch = allFiles.FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).ToLower().Contains(baseName.ToLower()) ||
                    baseName.ToLower().Contains(Path.GetFileNameWithoutExtension(f).ToLower()));

                if (fuzzyMatch != null)
                {
                    Console.WriteLine($"Found fuzzy match: {fuzzyMatch}");
                    return fuzzyMatch;
                }
            }

            Console.WriteLine($"No Azan file found for: {fileName}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding Azan file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get list of available Azan audio files
    /// </summary>
    /// <returns>List of available Azan file names (without extensions)</returns>
    public List<string> GetAvailableAzanFiles()
    {
        try
        {
            if (!Directory.Exists(_azanFolderPath))
                return new List<string>();

            var audioFiles = Directory.GetFiles(_azanFolderPath, "*.wav")
                .Concat(Directory.GetFiles(_azanFolderPath, "*.mp3"))
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            return audioFiles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting available Azan files: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Play MP3 file using Windows Media Player
    /// </summary>
    private async Task PlayMp3FileAsync(string filePath, int volume)
    {
        try
        {
            // Use Windows Media Player command line to play MP3
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"Add-Type -AssemblyName presentationCore; $mp = New-Object system.windows.media.mediaplayer; $mp.open('{filePath}'); $mp.Volume = {volume / 100.0}; $mp.Play(); Start-Sleep -Seconds 1\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _mediaProcess = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing MP3 file: {ex.Message}");
            // Fallback: try to open with default media player
            try
            {
                _mediaProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"Fallback MP3 playback also failed: {fallbackEx.Message}");
            }
        }
    }

    /// <summary>
    /// Check if Azan is currently playing
    /// </summary>
    public bool IsPlaying => _soundPlayer != null || (_mediaProcess != null && !_mediaProcess.HasExited);

    /// <summary>
    /// Get the Azan folder path
    /// </summary>
    public string AzanFolderPath => _azanFolderPath;

    /// <summary>
    /// Set a custom Azan folder path (for testing or manual configuration)
    /// </summary>
    public void SetAzanFolderPath(string path)
    {
        if (Directory.Exists(path))
        {
            _azanFolderPath = path;
            Console.WriteLine($"Azan folder path updated to: {_azanFolderPath}");
        }
        else
        {
            Console.WriteLine($"Warning: Azan folder path does not exist: {path}");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _soundPlayer?.Stop();
                _soundPlayer?.Dispose();
                _soundPlayer = null;

                if (_mediaProcess != null && !_mediaProcess.HasExited)
                {
                    _mediaProcess.Kill();
                }
                _mediaProcess?.Dispose();
                _mediaProcess = null;
            }
            _disposed = true;
        }
    }
}
