using System;
using NoorAhlulBayt.Common.Services;

class TestLogging
{
    static void Main()
    {
        try
        {
            Console.WriteLine("Testing DiagnosticLogger...");
            
            // Initialize logging
            DiagnosticLogger.Initialize("TestApp");
            DiagnosticLogger.CreateDebugConsole();
            
            // Test different log levels
            DiagnosticLogger.LogInfo("Test", "This is an info message");
            DiagnosticLogger.LogDebug("Test", "This is a debug message");
            DiagnosticLogger.LogWarning("Test", "This is a warning message");
            DiagnosticLogger.LogError("Test", "This is an error message");
            
            // Test startup logging
            DiagnosticLogger.LogStartupStep("Test step", "Testing startup logging");
            DiagnosticLogger.LogWebView2Event("Test event", "Testing WebView2 logging");
            DiagnosticLogger.LogWindowEvent("Test window", "Testing window logging");
            
            Console.WriteLine("Logging test completed. Check the Logs directory for output files.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            
            DiagnosticLogger.Shutdown();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.ReadKey();
        }
    }
}
