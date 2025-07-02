using System.Configuration;
using System.Data;
using System.Windows;
using NoorAhlulBayt.Common.Services;

namespace NoorAhlulBayt.Browser;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Initialize logging system first
            DiagnosticLogger.Initialize("NoorAhlulBayt.Browser");
            DiagnosticLogger.CreateDebugConsole();

            DiagnosticLogger.LogStartupStep("Application starting", $"Arguments: {string.Join(" ", e.Args)}");
            DiagnosticLogger.LogStartupStep("CLR Version", Environment.Version.ToString());
            DiagnosticLogger.LogStartupStep("OS Version", Environment.OSVersion.ToString());
            DiagnosticLogger.LogStartupStep("Machine Name", Environment.MachineName);
            DiagnosticLogger.LogStartupStep("User Name", Environment.UserName);
            DiagnosticLogger.LogStartupStep("Is 64-bit Process", Environment.Is64BitProcess.ToString());
            DiagnosticLogger.LogStartupStep("Is 64-bit OS", Environment.Is64BitOperatingSystem.ToString());

            // Set up global exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            DiagnosticLogger.LogStartupStep("Exception handlers registered");

            base.OnStartup(e);

            DiagnosticLogger.LogStartupStep("Base startup completed");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogFatal("App", "Critical error during application startup", ex);
            MessageBox.Show($"Critical error during application startup:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                          "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        DiagnosticLogger.LogFatal("App", "Unhandled domain exception", exception);

        if (e.IsTerminating)
        {
            DiagnosticLogger.LogFatal("App", "Application is terminating due to unhandled exception");
            DiagnosticLogger.Shutdown();
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        DiagnosticLogger.LogError("App", "Unhandled dispatcher exception", e.Exception);

        // Try to handle the exception gracefully
        MessageBox.Show($"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will continue running, but some features may not work correctly.",
                      "Error", MessageBoxButton.OK, MessageBoxImage.Warning);

        e.Handled = true; // Prevent application crash
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DiagnosticLogger.LogStartupStep("Application exiting", $"Exit code: {e.ApplicationExitCode}");
        DiagnosticLogger.Shutdown();
        base.OnExit(e);
    }
}

