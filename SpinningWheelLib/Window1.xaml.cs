using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using H.NotifyIcon;

namespace SpinningWheelLib
{
    public class HeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double workAreaBottom = (double)value;
            return workAreaBottom - 200;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class WidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double workAreaRight = (double)value;
            return workAreaRight - 350;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class ConsoleOutputRedirector : TextWriter
    {
        private readonly Window1 _window;

        public ConsoleOutputRedirector(Window1 window)
        {
            _window = window;
        }

        public override void Write(string? value)
        {
            if (value != null)
                _window.WriteToConsole(value, Colors.White);
        }

        public override void WriteLine(string? value)
        {
            if (value != null)
                _window.WriteToConsole(value + Environment.NewLine, Colors.White);
        }

        public override Encoding Encoding => Encoding.UTF8;
    }

    public class MultiWriter : TextWriter
    {
        private readonly TextWriter[] writers;

        public MultiWriter(TextWriter[] writers)
        {
            this.writers = writers;
        }

        public override void Write(string? value)
        {
            foreach (var writer in writers)
                writer.Write(value);
        }

        public override void WriteLine(string? value)
        {
            foreach (var writer in writers)
                writer.WriteLine(value);
        }

        public override Encoding Encoding => Encoding.UTF8;
    }

    public partial class Window1 : Window
    {
        private bool IsWindows11OrGreater => Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch;
        private readonly double _estimatedDuration;
        private bool _isMessageBox;
private bool _positionSet;
        private const double BaseHeight = 464;
        private const double ExpandedHeight = 684;
        private const double MessageBoxMinHeight = 200;
        private const double MessageBoxMinWidth = 200;
        private const double ProgressWindowWidth = 1146;
        private const double ProgressWindowPadding = 20;
        public bool DialogResult { get; private set; }

        private readonly TextWriter _originalConsole;
        private readonly StreamWriter _fileWriter;
        private readonly string _tempLogFile;

        public delegate IEnumerable<string> ListItemsProvider();
        public delegate void ButtonClickHandler(object sender, RoutedEventArgs e);
        private DispatcherTimer _autoCloseTimer;
        private const uint HWND_TOP = 0;
private const uint SWP_SHOWWINDOW = 0x0040;
private const uint SWP_NOZORDER = 0x0004;

[DllImport("user32.dll")]
private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

private bool _needsCentering;
private double _desiredWidth;
private double _desiredHeight;

        public Window1(double estimatedDurationInSeconds = 30, bool isMessageBox = false,
    string customLabel = "Loading...", double? customWidth = null, double? customHeight = null,
    string messageTitle = "", string messageIcon = "!", List<string> listItems = null,
    string footerText = "", ListItemsProvider listItemsProvider = null,
    RoutedEventHandler okHandler = null, RoutedEventHandler cancelHandler = null,
    bool hideButtons = false, int? autoCloseSeconds = null)
        {
            Console.WriteLine($"Constructor called - isMessageBox: {isMessageBox}");
            InitializeComponent();

            _isMessageBox = isMessageBox;
            _estimatedDuration = estimatedDurationInSeconds;
            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += Timer_Tick;

            if (isMessageBox)
            {
                Console.WriteLine("Configuring as MessageBox");
                WindowStyle = WindowStyle.SingleBorderWindow;
                AllowsTransparency = false;
                ConfigureAsMessageBox(customLabel, messageTitle, messageIcon, listItems, footerText,
                    customWidth, customHeight, listItemsProvider, okHandler, cancelHandler, hideButtons);
            }
            else
            {
                Console.WriteLine("Configuring as Progress Window");
                ConfigureAsProgressWindow();
                ProgressText.Text = customLabel;
            }

            if (autoCloseSeconds.HasValue)
            {
                ConfigureAutoClose(autoCloseSeconds.Value);
            }
        }


        private void CenterWindowOnScreen()
        {
            // Get the working area of the primary screen
            var workArea = SystemParameters.WorkArea;

            // Calculate center position
            Left = (workArea.Width - ActualWidth) / 2;
            Top = (workArea.Height - ActualHeight) / 2;
        }

        private void SetWindowPosition()
{
    if (_isMessageBox || _positionSet)
    {
        Console.WriteLine("SetWindowPosition skipped - MessageBox mode or position already set");
        return;
    }

    Console.WriteLine("Setting ProgressWindow position");
    var workArea = SystemParameters.WorkArea;
    Left = workArea.Right - Width;
    Top = workArea.Bottom - Height;
    Console.WriteLine($"ProgressWindow position set to: Left={Left}, Top={Top}");
}

        private void ConfigureAsProgressWindow()
        {
            Width = 355;
            Height = 200;
            ProgressContent.Visibility = Visibility.Visible;
            MessageBoxContent.Visibility = Visibility.Collapsed;

            var showProgressTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            showProgressTimer.Tick += (s, e) =>
            {
                progressBar.Visibility = Visibility.Visible;
                showProgressTimer.Stop();
            };
            showProgressTimer.Start();

            StartProgress();
        }

        private void ConfigureAutoClose(int seconds)
        {
            _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer.Stop();
                Close();
            };
            _autoCloseTimer.Start();
        }

        private void StartProgress()
        {
            _stopwatch.Start();
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            double progress = (_stopwatch.ElapsedMilliseconds / (_estimatedDuration * 1000)) * 100;
            progress = Math.Min(progress, 100);
            progressBar.Value = progress;
            ProgressPercentage.Text = $"{(int)progress}%";
        }

        public void WriteToConsole(string text, Color color)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => WriteToConsole(text, color));
                return;
            }

            if (consoleOutput?.Document == null) return;

            var paragraph = new Paragraph();
            var run = new Run(text)
            {
                Foreground = new SolidColorBrush(Colors.Black)
            };
            paragraph.Inlines.Add(run);
            consoleOutput.Document.Blocks.Add(paragraph);
            consoleOutput.ScrollToEnd();
        }

        private void ConfigureAsMessageBox(string message, string title, string icon, List<string> listItems,
    string footerText, double? customWidth, double? customHeight, ListItemsProvider listItemsProvider,
    RoutedEventHandler okHandler = null, RoutedEventHandler cancelHandler = null, bool hideButtons = false)
{
    Console.WriteLine($"[ConfigureAsMessageBox] Start - {DateTime.Now}");
    LogCallStack();

    Title = title;
    Width = customWidth ?? MessageBoxMinWidth;
    Height = customHeight ?? MessageBoxMinHeight;
    MinWidth = MessageBoxMinWidth;
    MinHeight = MessageBoxMinHeight;
    WindowStyle = WindowStyle.SingleBorderWindow;
    AllowsTransparency = false;
    Background = new SolidColorBrush(Color.FromRgb(236, 233, 216));
    ResizeMode = ResizeMode.NoResize;

    // Configure tray icon
    TrayIcon.ToolTipText = "Window Controls";
    
    TrayIcon.ContextMenu = new ContextMenu();
    var centerMenuItem = new MenuItem { Header = "Center Window" };
    centerMenuItem.Click += CenterWindow_Click;
    TrayIcon.ContextMenu.Items.Add(centerMenuItem);

    // Configure window content
    ProgressContent.Visibility = Visibility.Collapsed;
    MessageBoxContent.Visibility = Visibility.Visible;
    MessageIcon.Text = icon;
    MessageTitle.Text = title;
    MessageText.Text = message;

    ConfigureMessageBoxButtons(hideButtons, okHandler, cancelHandler);
    ConfigureMessageBoxList(listItems, listItemsProvider);
    MessageFooter.Text = footerText;

    ForceWindowToCenter();
}

private void ForceWindowToCenter()
{
    Console.WriteLine("Forcing window to center position");
    
    // First position at 0,0
    Left = 0;
    Top = 0;
    UpdateLayout();
    
    var source = PresentationSource.FromVisual(this);
    var dpiScale = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
    
    var workArea = SystemParameters.WorkArea;
    double scaledWidth = Width * dpiScale;
    double scaledHeight = Height * dpiScale;
    
    double x = (workArea.Width - scaledWidth) / 2;
    double y = (workArea.Height - scaledHeight) / 2;
    
    var helper = new WindowInteropHelper(this);
    SetWindowPos(helper.Handle, IntPtr.Zero, (int)x, (int)y, 
        (int)scaledWidth, (int)scaledHeight, 
        SWP_NOZORDER | SWP_SHOWWINDOW);
    
    Console.WriteLine($"Window positioned at: X={x}, Y={y}");
}

private void CenterWindow_Click(object sender, RoutedEventArgs e)
{
    ForceWindowToCenter();
}

private void LogCallStack()
{
    var stackTrace = new StackTrace(true);
    Console.WriteLine("Full Call Stack:");
    foreach (var frame in stackTrace.GetFrames())
    {
        Console.WriteLine($"   at {frame.GetMethod().DeclaringType}.{frame.GetMethod().Name}");
    }
}



        private void ConfigureMessageBoxButtons(bool hideButtons, RoutedEventHandler okHandler, RoutedEventHandler cancelHandler)
        {
            try
            {
                Console.WriteLine("Configuring MessageBox buttons");
                if (MessageBoxButtons == null)
                {
                    Console.WriteLine("MessageBoxButtons control not found");
                    return;
                }

                if (hideButtons)
                {
                    MessageBoxButtons.Visibility = Visibility.Collapsed;
                    Console.WriteLine("Buttons hidden");
                    return;
                }

                var buttons = MessageBoxButtons.Children.OfType<Button>().ToList();
                var okButton = buttons.FirstOrDefault(b => b?.Content?.ToString() == "OK");
                var cancelButton = buttons.FirstOrDefault(b => b?.Content?.ToString() == "Cancel");

                if (okButton == null || cancelButton == null)
                {
                    Console.WriteLine("Required buttons not found");
                    return;
                }

                Console.WriteLine("Configuring button handlers");
                okButton.Click -= OkButton_Click;
                cancelButton.Click -= CancelButton_Click;

                okButton.Click += okHandler ?? OkButton_Click;
                cancelButton.Click += cancelHandler ?? CancelButton_Click;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConfigureMessageBoxButtons: {ex.Message}");
                throw;
            }
        }

        private void ConfigureMessageBoxList(List<string> listItems, ListItemsProvider listItemsProvider)
        {
            try
            {
                Console.WriteLine("Configuring MessageBox list");
                if (MessageList == null)
                {
                    Console.WriteLine("MessageList control not found");
                    return;
                }

                if (listItems?.Count > 0 || listItemsProvider != null)
                {
                    MessageList.Visibility = Visibility.Visible;
                    MessageList.Items.Clear();

                    if (listItems != null)
                    {
                        Console.WriteLine($"Adding {listItems.Count} list items");
                        foreach (var item in listItems)
                        {
                            if (item != null)
                                MessageList.Items.Add(item);
                        }
                    }
                    else if (listItemsProvider != null)
                    {
                        try
                        {
                            var items = listItemsProvider()?.ToList() ?? new List<string>();
                            Console.WriteLine($"Adding {items.Count} items from provider");
                            foreach (var item in items)
                            {
                                if (item != null)
                                    MessageList.Items.Add(item);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting items from provider: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No list items to configure");
                    MessageList.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConfigureMessageBoxList: {ex.Message}");
                throw;
            }
        }
        protected override void OnSourceInitialized(EventArgs e)
{
    base.OnSourceInitialized(e);
    Console.WriteLine($"Initial window position: Left={Left}, Top={Top}");

    if (_needsCentering)
    {
        var helper = new WindowInteropHelper(this);
        var source = PresentationSource.FromVisual(this);
        double dpiScale = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
        
        // Force initial position to 0,0
        Left = 0;
        Top = 0;
        UpdateLayout();
        Console.WriteLine($"Position after reset: Left={Left}, Top={Top}");
        
        var workArea = SystemParameters.WorkArea;
        double scaledWidth = ActualWidth * dpiScale;
        double scaledHeight = ActualHeight * dpiScale;
        
        // Calculate center position
        double centerX = (workArea.Width - scaledWidth) / 2;
        double centerY = (workArea.Height - scaledHeight) / 2;
        Console.WriteLine($"Calculated center: X={centerX}, Y={centerY}");
        
        // First position update
        SetWindowPos(helper.Handle, IntPtr.Zero, 
            (int)centerX, (int)centerY,
            (int)scaledWidth, (int)scaledHeight,
            SWP_NOZORDER | SWP_SHOWWINDOW);
        
        Console.WriteLine($"Position after first update: Left={Left}, Top={Top}");
        
        // If not at 0,0, force it
        if (Math.Abs(Left) > 0 || Math.Abs(Top) > 0)
        {
            Console.WriteLine("Position not at 0,0, forcing center position");
            SetWindowPos(helper.Handle, IntPtr.Zero, 0, 0,
                (int)scaledWidth, (int)scaledHeight,
                SWP_NOZORDER | SWP_SHOWWINDOW);
            
            // Final center calculation from 0,0
            centerX = workArea.Width / 2 - scaledWidth / 2;
            centerY = workArea.Height / 2 - scaledHeight / 2;
            
            SetWindowPos(helper.Handle, IntPtr.Zero, 
                (int)centerX, (int)centerY,
                (int)scaledWidth, (int)scaledHeight,
                SWP_NOZORDER | SWP_SHOWWINDOW);
        }
        
        Console.WriteLine($"Final position: Left={Left}, Top={Top}");
    }
    else if (!_isMessageBox)
    {
        SetWindowPosition();
    }
}
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea;
            var expandedHeight = Height + consoleOutput.Height + 20;
            var newTop = workArea.Bottom - expandedHeight;

            BeginAnimation(HeightProperty, new DoubleAnimation(expandedHeight, TimeSpan.FromSeconds(0.3)));
            BeginAnimation(TopProperty, new DoubleAnimation(newTop, TimeSpan.FromSeconds(0.3)));
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea;
            var originalHeight = 200;
            var originalTop = workArea.Bottom - originalHeight;

            BeginAnimation(HeightProperty, new DoubleAnimation(originalHeight, TimeSpan.FromSeconds(0.3)));
            BeginAnimation(TopProperty, new DoubleAnimation(originalTop, TimeSpan.FromSeconds(0.3)));
        }

        public void SetProgress(double value)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = value;
                ProgressPercentage.Text = $"{(int)value}%";
            });
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public new void Close()
        {
            if (_timer != null)
            {
                _timer.Stop();
            }

            if (_stopwatch != null)
            {
                _stopwatch.Stop();
            }

            if (_originalConsole != null)
            {
                Console.SetOut(_originalConsole);
            }

            if (_fileWriter != null)
            {
                _fileWriter.Flush();
                _fileWriter.Close();
                _fileWriter.Dispose();
            }

            _autoCloseTimer?.Stop();

            try
            {
                if (!string.IsNullOrEmpty(_tempLogFile) && File.Exists(_tempLogFile))
                {
                    File.Delete(_tempLogFile);
                }
            }
            catch (IOException)
            {
                // File will be cleaned up later
            }

            base.Close();
        }
    }
}
