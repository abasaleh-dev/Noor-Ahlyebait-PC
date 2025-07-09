using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoorAhlulBayt.Companion.Controls;

/// <summary>
/// Custom window base class with Islamic theme and borderless design
/// </summary>
public class IslamicWindow : Window
{
    static IslamicWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(IslamicWindow), 
            new FrameworkPropertyMetadata(typeof(IslamicWindow)));
    }

    public IslamicWindow()
    {
        // Load the Islamic window style
        var resourceDict = new ResourceDictionary();
        resourceDict.Source = new Uri("pack://application:,,,/NoorAhlulBayt.Companion;component/Styles/IslamicWindowStyle.xaml");
        Resources.MergedDictionaries.Add(resourceDict);
        
        // Apply the Islamic window style
        Style = (Style)FindResource("IslamicWindowStyle");
        
        // Set default properties
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        
        // Handle window events
        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Ensure the window is properly sized and positioned
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }

        // Wire up event handlers for window controls
        WireUpEventHandlers();
    }

    private void WireUpEventHandlers()
    {
        if (Template != null)
        {
            // Title bar drag handling
            if (Template.FindName("TitleBar", this) is Border titleBar)
            {
                titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
            }

            // Window control buttons
            if (Template.FindName("MinimizeButton", this) is System.Windows.Controls.Button minimizeButton)
            {
                minimizeButton.Click += (s, e) => MinimizeButton_Click(s, e);
            }

            if (Template.FindName("MaximizeRestoreButton", this) is System.Windows.Controls.Button maxRestoreButton)
            {
                maxRestoreButton.Click += (s, e) => MaximizeRestoreButton_Click(s, e);
            }

            if (Template.FindName("CloseButton", this) is System.Windows.Controls.Button closeButton)
            {
                closeButton.Click += (s, e) => CloseButton_Click(s, e);
            }
        }
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        // Update maximize/restore button content
        if (Template?.FindName("MaximizeRestoreButton", this) is System.Windows.Controls.Button maxRestoreButton)
        {
            maxRestoreButton.Content = WindowState == WindowState.Maximized ? "🗗" : "🗖";
        }
    }

    /// <summary>
    /// Handle title bar mouse down for window dragging
    /// </summary>
    public void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click to maximize/restore
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        else
        {
            // Single click to drag
            DragMove();
        }
    }

    /// <summary>
    /// Handle minimize button click
    /// </summary>
    public void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Handle maximize/restore button click
    /// </summary>
    public void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    /// <summary>
    /// Handle close button click
    /// </summary>
    public void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Override to wire up event handlers when template is applied
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        WireUpEventHandlers();
    }

    /// <summary>
    /// Override to handle custom window behavior
    /// </summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        
        // Enable window resizing for borderless window
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            EnableWindowResizing(hwnd);
        }
    }

    /// <summary>
    /// Enable window resizing for borderless windows
    /// </summary>
    private void EnableWindowResizing(IntPtr hwnd)
    {
        try
        {
            var source = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
            source?.AddHook(WndProc);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enabling window resizing: {ex.Message}");
        }
    }

    /// <summary>
    /// Window procedure for handling resize messages
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTBOTTOM = 15;
        const int HTBOTTOMRIGHT = 17;
        const int HTBOTTOMLEFT = 16;

        if (msg == WM_NCHITTEST)
        {
            var point = PointFromScreen(new System.Windows.Point(
                (int)lParam & 0xFFFF,
                ((int)lParam >> 16) & 0xFFFF));

            var resizeMargin = 8;

            // Check for resize areas
            if (point.X <= resizeMargin && point.Y <= resizeMargin)
            {
                handled = true;
                return new IntPtr(HTTOPLEFT);
            }
            else if (point.X >= ActualWidth - resizeMargin && point.Y <= resizeMargin)
            {
                handled = true;
                return new IntPtr(HTTOPRIGHT);
            }
            else if (point.X <= resizeMargin && point.Y >= ActualHeight - resizeMargin)
            {
                handled = true;
                return new IntPtr(HTBOTTOMLEFT);
            }
            else if (point.X >= ActualWidth - resizeMargin && point.Y >= ActualHeight - resizeMargin)
            {
                handled = true;
                return new IntPtr(HTBOTTOMRIGHT);
            }
            else if (point.X <= resizeMargin)
            {
                handled = true;
                return new IntPtr(HTLEFT);
            }
            else if (point.X >= ActualWidth - resizeMargin)
            {
                handled = true;
                return new IntPtr(HTRIGHT);
            }
            else if (point.Y <= resizeMargin)
            {
                handled = true;
                return new IntPtr(HTTOP);
            }
            else if (point.Y >= ActualHeight - resizeMargin)
            {
                handled = true;
                return new IntPtr(HTBOTTOM);
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Show dialog with Islamic theme
    /// </summary>
    public new bool? ShowDialog()
    {
        // Ensure proper owner relationship
        if (Owner == null && System.Windows.Application.Current.MainWindow != this)
        {
            Owner = System.Windows.Application.Current.MainWindow;
        }

        return base.ShowDialog();
    }

    /// <summary>
    /// Show success message with Islamic theme
    /// </summary>
    public void ShowSuccessMessage(string title, string message)
    {
        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Show error message with Islamic theme
    /// </summary>
    public void ShowErrorMessage(string title, string message)
    {
        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// Show confirmation dialog with Islamic theme
    /// </summary>
    public bool ShowConfirmationDialog(string title, string message)
    {
        var result = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }
}
