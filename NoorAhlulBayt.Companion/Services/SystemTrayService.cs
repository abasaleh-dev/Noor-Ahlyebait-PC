using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace NoorAhlulBayt.Companion.Services;

/// <summary>
/// Service for managing system tray functionality
/// </summary>
public class SystemTrayService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private readonly BrowserMonitoringService _browserMonitor;
    private bool _disposed = false;

    public SystemTrayService(BrowserMonitoringService browserMonitor)
    {
        _browserMonitor = browserMonitor;
        InitializeSystemTray();
    }

    /// <summary>
    /// Initialize the system tray icon and context menu
    /// </summary>
    private void InitializeSystemTray()
    {
        try
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateIcon(),
                Text = "Noor-e-AhlulBayt Companion",
                Visible = true
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            
            // Browser status item
            var statusItem = new ToolStripMenuItem("Browser Status: Checking...")
            {
                Enabled = false
            };
            contextMenu.Items.Add(statusItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Settings item
            var settingsItem = new ToolStripMenuItem("Settings")
            {
                Image = SystemIcons.Application.ToBitmap()
            };
            settingsItem.Click += (s, e) => ShowSettings();
            contextMenu.Items.Add(settingsItem);
            
            // Show browser item
            var showBrowserItem = new ToolStripMenuItem("Show Browser")
            {
                Image = SystemIcons.Information.ToBitmap()
            };
            showBrowserItem.Click += (s, e) => ShowBrowser();
            contextMenu.Items.Add(showBrowserItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Exit item
            var exitItem = new ToolStripMenuItem("Exit")
            {
                Image = SystemIcons.Error.ToBitmap()
            };
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowSettings();

            // Subscribe to browser monitoring events
            _browserMonitor.BrowserStatusChanged += OnBrowserStatusChanged;
            
            // Update initial status
            UpdateBrowserStatus(statusItem);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error initializing system tray: {ex.Message}", 
                "Companion Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Create the system tray icon
    /// </summary>
    private Icon CreateIcon()
    {
        try
        {
            // Create a simple green icon for Islamic theme
            using var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Fill with Islamic green
            using var brush = new SolidBrush(Color.FromArgb(46, 125, 50));
            graphics.FillEllipse(brush, 2, 2, 12, 12);
            
            // Add gold border
            using var pen = new Pen(Color.FromArgb(255, 215, 0), 1);
            graphics.DrawEllipse(pen, 2, 2, 12, 12);
            
            return Icon.FromHandle(bitmap.GetHicon());
        }
        catch
        {
            // Fallback to system icon
            return SystemIcons.Application;
        }
    }

    /// <summary>
    /// Handle browser status changes
    /// </summary>
    private void OnBrowserStatusChanged(object? sender, BrowserStatusEventArgs e)
    {
        if (_notifyIcon?.ContextMenuStrip?.Items[0] is ToolStripMenuItem statusItem)
        {
            UpdateBrowserStatus(statusItem);
        }

        // Show notification for important status changes
        if (e.IsBlocked)
        {
            ShowNotification("Browser Blocked", 
                "The Islamic browser has been blocked due to time limits or restrictions.",
                ToolTipIcon.Warning);
        }
        else if (e.IsRunning && !e.WasRunning)
        {
            ShowNotification("Browser Started", 
                "The Islamic browser is now running and being monitored.",
                ToolTipIcon.Info);
        }
    }

    /// <summary>
    /// Update browser status in context menu
    /// </summary>
    private void UpdateBrowserStatus(ToolStripMenuItem statusItem)
    {
        var status = _browserMonitor.GetCurrentStatus();
        statusItem.Text = $"Browser Status: {(status.IsRunning ? "Running" : "Not Running")}";
        
        if (status.IsBlocked)
        {
            statusItem.Text += " (Blocked)";
        }
    }

    /// <summary>
    /// Show a system tray notification
    /// </summary>
    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        try
        {
            _notifyIcon?.ShowBalloonTip(5000, title, message, icon);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error showing notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Show the settings window
    /// </summary>
    private void ShowSettings()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                }
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error showing settings: {ex.Message}", 
                "Companion Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Show or launch the browser
    /// </summary>
    private void ShowBrowser()
    {
        try
        {
            _browserMonitor.LaunchBrowser();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error launching browser: {ex.Message}", 
                "Companion Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Exit the application
    /// </summary>
    private void ExitApplication()
    {
        try
        {
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during application exit: {ex.Message}");
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Hide the system tray icon
    /// </summary>
    public void Hide()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
        }
    }

    /// <summary>
    /// Show the system tray icon
    /// </summary>
    public void Show()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = true;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _notifyIcon?.Dispose();
            _disposed = true;
        }
    }
}
