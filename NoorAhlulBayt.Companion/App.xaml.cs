using System.Windows;

namespace NoorAhlulBayt.Companion;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private static Mutex? _mutex = null;
    private const string MutexName = "NoorAhlulBaytCompanionApp_SingleInstance";
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Check for single instance
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            System.Windows.MessageBox.Show(
                "Noor-e-AhlulBayt Companion is already running.\n\nOnly one instance can run at a time to prevent data conflicts.",
                "Application Already Running",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Try to bring the existing instance to foreground
            BringExistingInstanceToForeground();

            Shutdown();
            return;
        }

        try
        {
            Console.WriteLine("Noor-e-AhlulBayt Companion starting (single instance confirmed)");

            // Create and show main window (it will hide itself to tray)
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;

            // Don't show the window initially - it will hide to tray
            // mainWindow.Show();

            Console.WriteLine("Noor-e-AhlulBayt Companion started successfully");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error starting companion app: {ex.Message}",
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void BringExistingInstanceToForeground()
    {
        try
        {
            // Find the existing window and bring it to foreground
            var processes = System.Diagnostics.Process.GetProcessesByName("NoorAhlulBayt.Companion");
            foreach (var process in processes)
            {
                if (process.Id != System.Diagnostics.Process.GetCurrentProcess().Id)
                {
                    // Bring the window to foreground
                    ShowWindow(process.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(process.MainWindowHandle);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error bringing existing instance to foreground: {ex.Message}");
        }
    }

    // Windows API imports for bringing window to foreground
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    protected override void OnExit(ExitEventArgs e)
    {
        Console.WriteLine("Noor-e-AhlulBayt Companion shutting down");

        // Release the mutex
        try
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error releasing mutex: {ex.Message}");
        }

        base.OnExit(e);
    }
}

