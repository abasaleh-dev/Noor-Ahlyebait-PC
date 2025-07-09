using Serilog;
using Serilog.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NoorAhlulBayt.Common.Services;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

public static class DiagnosticLogger
{
    private static Logger? _logger;
    private static readonly object _lock = new object();
    private static bool _isInitialized = false;
    private static bool _consoleWindowCreated = false;

    public static void Initialize(string applicationName = "NoorAhlulBayt")
    {
        lock (_lock)
        {
            if (_isInitialized) return;

            try
            {
                // Create logs directory if it doesn't exist
                var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(logsDirectory);

                // Create log file path with timestamp
                var logFileName = $"{applicationName}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                var logFilePath = Path.Combine(logsDirectory, logFileName);

                // Configure Serilog
                _logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        logFilePath,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext} ({ThreadId}): {Message:lj}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7)
                    .Enrich.WithThreadId()
                    .CreateLogger();

                _isInitialized = true;

                // Log initialization success
                LogInfo("DiagnosticLogger", "Logging system initialized successfully");
                LogInfo("DiagnosticLogger", $"Log file: {logFilePath}");
                LogInfo("DiagnosticLogger", $"Application: {applicationName}");
                LogInfo("DiagnosticLogger", $"Process ID: {Process.GetCurrentProcess().Id}");
                LogInfo("DiagnosticLogger", $"Thread ID: {Environment.CurrentManagedThreadId}");
                LogInfo("DiagnosticLogger", $"Working Directory: {Environment.CurrentDirectory}");
                LogInfo("DiagnosticLogger", $"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
            }
            catch (Exception ex)
            {
                // Fallback to console if file logging fails
                Console.WriteLine($"[ERROR] Failed to initialize logging: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                
                try
                {
                    _logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console()
                        .CreateLogger();
                    _isInitialized = true;
                }
                catch
                {
                    // If even console logging fails, we'll use Console.WriteLine as fallback
                }
            }
        }
    }

    public static void CreateDebugConsole()
    {
        if (_consoleWindowCreated) return;

        try
        {
            // Allocate a console for this GUI application
            if (AllocConsole())
            {
                // Redirect console output
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                
                // Set console title
                SetConsoleTitle("Noor-e-AhlulBayt Browser - Debug Console");
                
                _consoleWindowCreated = true;
                LogInfo("DiagnosticLogger", "Debug console window created successfully");
            }
        }
        catch (Exception ex)
        {
            LogError("DiagnosticLogger", $"Failed to create debug console: {ex.Message}", ex);
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool SetConsoleTitle(string lpConsoleTitle);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    /// <summary>
    /// Check if the debug console is currently visible
    /// </summary>
    public static bool IsConsoleVisible()
    {
        if (!_consoleWindowCreated) return false;

        try
        {
            IntPtr consoleWindow = GetConsoleWindow();
            return consoleWindow != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Show the debug console window
    /// </summary>
    public static void ShowDebugConsole()
    {
        try
        {
            if (!_consoleWindowCreated)
            {
                CreateDebugConsole();
            }
            else
            {
                IntPtr consoleWindow = GetConsoleWindow();
                if (consoleWindow != IntPtr.Zero)
                {
                    ShowWindow(consoleWindow, SW_SHOW);
                }
            }
        }
        catch (Exception ex)
        {
            LogError("DiagnosticLogger", $"Failed to show debug console: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Hide the debug console window
    /// </summary>
    public static void HideDebugConsole()
    {
        try
        {
            IntPtr consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_HIDE);
            }
        }
        catch (Exception ex)
        {
            LogError("DiagnosticLogger", $"Failed to hide debug console: {ex.Message}", ex);
        }
    }

    public static void LogDebug(string source, string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log(LogLevel.Debug, source, message, null, memberName, filePath, lineNumber);
    }

    public static void LogInfo(string source, string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log(LogLevel.Info, source, message, null, memberName, filePath, lineNumber);
    }

    public static void LogWarning(string source, string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log(LogLevel.Warning, source, message, null, memberName, filePath, lineNumber);
    }

    public static void LogError(string source, string message, Exception? exception = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log(LogLevel.Error, source, message, exception, memberName, filePath, lineNumber);
    }

    public static void LogFatal(string source, string message, Exception? exception = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log(LogLevel.Fatal, source, message, exception, memberName, filePath, lineNumber);
    }

    private static void Log(LogLevel level, string source, string message, Exception? exception,
        string memberName, string filePath, int lineNumber)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var sourceContext = $"{source}.{memberName}({fileName}:{lineNumber})";
        var logMessage = $"{message}";

        try
        {
            if (_logger != null)
            {
                switch (level)
                {
                    case LogLevel.Debug:
                        _logger.ForContext("SourceContext", sourceContext).Debug(logMessage, exception);
                        break;
                    case LogLevel.Info:
                        _logger.ForContext("SourceContext", sourceContext).Information(logMessage, exception);
                        break;
                    case LogLevel.Warning:
                        _logger.ForContext("SourceContext", sourceContext).Warning(logMessage, exception);
                        break;
                    case LogLevel.Error:
                        _logger.ForContext("SourceContext", sourceContext).Error(exception, logMessage);
                        break;
                    case LogLevel.Fatal:
                        _logger.ForContext("SourceContext", sourceContext).Fatal(exception, logMessage);
                        break;
                }
            }
            else
            {
                // Fallback to console
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var levelStr = level.ToString().ToUpper().PadRight(5);
                Console.WriteLine($"[{timestamp} {levelStr}] {sourceContext}: {logMessage}");
                if (exception != null)
                {
                    Console.WriteLine($"Exception: {exception}");
                }
            }
        }
        catch (Exception ex)
        {
            // Last resort fallback
            Console.WriteLine($"[LOGGING ERROR] {DateTime.Now:HH:mm:ss.fff} Failed to log message: {ex.Message}");
            Console.WriteLine($"[ORIGINAL MESSAGE] {level} {sourceContext}: {message}");
            if (exception != null)
            {
                Console.WriteLine($"[ORIGINAL EXCEPTION] {exception}");
            }
        }
    }

    public static void LogStartupStep(string step, string details = "")
    {
        var message = string.IsNullOrEmpty(details) ? $"STARTUP: {step}" : $"STARTUP: {step} - {details}";
        LogInfo("Startup", message);
    }

    public static void LogWebView2Event(string eventName, string details = "")
    {
        var message = string.IsNullOrEmpty(details) ? $"WEBVIEW2: {eventName}" : $"WEBVIEW2: {eventName} - {details}";
        LogInfo("WebView2", message);
    }

    public static void LogWindowEvent(string eventName, string details = "")
    {
        var message = string.IsNullOrEmpty(details) ? $"WINDOW: {eventName}" : $"WINDOW: {eventName} - {details}";
        LogInfo("Window", message);
    }

    public static void Shutdown()
    {
        lock (_lock)
        {
            if (_logger != null)
            {
                LogInfo("DiagnosticLogger", "Shutting down logging system");
                _logger.Dispose();
                _logger = null;
                _isInitialized = false;
            }
        }
    }
}
