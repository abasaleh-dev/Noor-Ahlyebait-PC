using System.Windows;

namespace NoorAhlulBayt.Companion;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
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

    protected override void OnExit(ExitEventArgs e)
    {
        Console.WriteLine("Noor-e-AhlulBayt Companion shutting down");
        base.OnExit(e);
    }
}

