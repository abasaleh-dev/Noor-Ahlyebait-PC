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
    private readonly string _azanFolderPath;
    private bool _disposed = false;

    public AzanService()
    {
        // Set up Azan folder path
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var companionDataPath = Path.Combine(appDataPath, "NoorAhlulBayt", "Companion");
        _azanFolderPath = Path.Combine(companionDataPath, "Azan");

        // Ensure directory exists
        Directory.CreateDirectory(_azanFolderPath);
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
            // Remove extension if provided
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            
            // Try different file name patterns and extensions
            var possibleFiles = new[]
            {
                Path.Combine(_azanFolderPath, $"{baseName}.mp3"),
                Path.Combine(_azanFolderPath, $"{baseName}.wav"),
                Path.Combine(_azanFolderPath, $"{fileName}.mp3"),
                Path.Combine(_azanFolderPath, $"{fileName}.wav"),
                Path.Combine(_azanFolderPath, $"azan_{baseName}.mp3"),
                Path.Combine(_azanFolderPath, $"azan_{baseName}.wav"),
                Path.Combine(_azanFolderPath, $"adhan_{baseName}.mp3"),
                Path.Combine(_azanFolderPath, $"adhan_{baseName}.wav")
            };

            return possibleFiles.FirstOrDefault(File.Exists);
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
