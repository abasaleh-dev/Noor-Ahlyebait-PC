using System.Configuration;
using System.Data;
using System.Windows;
using NoorAhlulBayt.Common.Services;
using NoorAhlulBayt.Browser.Services;

namespace NoorAhlulBayt.Browser;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Initialize logging system first
            DiagnosticLogger.Initialize("NoorAhlulBayt.Browser");
            DiagnosticLogger.CreateDebugConsole();

            // Hide debug console by default - can be shown via View menu
            DiagnosticLogger.HideDebugConsole();

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

            // Check if we should skip profile selection
            await HandleStartupFlowAsync(e);

            DiagnosticLogger.LogStartupStep("Startup flow completed");
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

    /// <summary>
    /// Handle the startup flow - decide whether to show profile selection or go directly to main window
    /// </summary>
    private async Task HandleStartupFlowAsync(StartupEventArgs e)
    {
        try
        {
            DiagnosticLogger.LogStartupStep("Checking startup flow requirements");

            using var profileService = new ProfileSelectionService();

            // Check if we should skip profile selection
            bool shouldSkip = await profileService.ShouldSkipProfileSelectionAsync();

            if (shouldSkip)
            {
                DiagnosticLogger.LogStartupStep("Skipping profile selection - launching main browser directly");

                // Launch main browser window directly
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                DiagnosticLogger.LogStartupStep("Showing profile selection window");

                // Show profile selection window
                var profileWindow = new ProfileSelectionWindow();
                MainWindow = profileWindow;
                profileWindow.Show();
            }
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("App", "Error in startup flow", ex);

            // Fallback to main window if there's an error
            DiagnosticLogger.LogStartupStep("Startup flow error - falling back to main window");
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}

