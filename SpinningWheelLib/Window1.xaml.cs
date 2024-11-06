using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;


namespace SpinningWheelLib
{
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
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch;
    private readonly double _estimatedDuration;
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
public Window1(double estimatedDurationInSeconds = 30, bool isMessageBox = false, 
        string customLabel = "Loading...", double? customWidth = null, double? customHeight = null,
        string messageTitle = "", string messageIcon = "!", List<string> listItems = null, 
        string footerText = "", ListItemsProvider listItemsProvider = null,
        RoutedEventHandler okHandler = null, RoutedEventHandler cancelHandler = null,
        bool hideButtons = false, int? autoCloseSeconds = null)
    {
        InitializeComponent();
        _estimatedDuration = estimatedDurationInSeconds;
        _stopwatch = new Stopwatch();

        if (!isMessageBox)
        {
            var multiWriter = new MultiWriter(new TextWriter[] 
            { 
                Console.Out,
                new ConsoleOutputRedirector(this)
            });
            Console.SetOut(multiWriter);
        }

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += Timer_Tick;

        if (isMessageBox)
        {
            ConfigureAsMessageBox(customLabel, messageTitle, messageIcon, listItems, footerText, 
                customWidth, customHeight, listItemsProvider, okHandler, cancelHandler, hideButtons);

            if (autoCloseSeconds.HasValue)
            {
                ConfigureAutoClose(autoCloseSeconds.Value);
            }
        }
        else
        {
            ConfigureAsProgressWindow();
            MainLabel.Content = customLabel;
        }
    }
private void ConfigureAutoClose(int seconds)
    {
        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(seconds)
        };
        _autoCloseTimer.Tick += (s, e) =>
        {
            _autoCloseTimer.Stop();
            Close();
        };
        _autoCloseTimer.Start();
    }

private void ConfigureAsMessageBox(string message, string title, string icon, List<string> listItems,
        string footerText, double? customWidth, double? customHeight, ListItemsProvider listItemsProvider,
        RoutedEventHandler okHandler = null, RoutedEventHandler cancelHandler = null, bool hideButtons = false)
    {
    Title = title;
    Width = customWidth ?? MessageBoxMinWidth;
    Height = customHeight ?? MessageBoxMinHeight;
    MinWidth = MessageBoxMinWidth;
    MinHeight = MessageBoxMinHeight;
    WindowStyle = WindowStyle.ThreeDBorderWindow;
    
    ProgressContent.Visibility = Visibility.Collapsed;
    MessageBoxContent.Visibility = Visibility.Visible;
    
    MessageIcon.Text = icon;
    MessageTitle.Text = title;
    MessageText.Text = message;

    if (hideButtons)
        {
            MessageBoxButtons.Visibility = Visibility.Collapsed;
        }
        else
        {
            if (okHandler != null)
            {
                var okButton = MessageBoxButtons.Children.OfType<Button>().First(b => b.Content.ToString() == "OK");
                okButton.Click -= OkButton_Click;
                okButton.Click += okHandler;
            }
            
            if (cancelHandler != null)
            {
                var cancelButton = MessageBoxButtons.Children.OfType<Button>().Last(b => b.Content.ToString() == "Cancel");
                cancelButton.Click -= CancelButton_Click;
                cancelButton.Click += cancelHandler;
            }
        }
    
    if (listItems != null && listItems.Count > 0)
    {
        MessageList.Visibility = Visibility.Visible;
        foreach (var item in listItems)
        {
            MessageList.Items.Add(item);
        }
    }
    else if (listItemsProvider != null)
    {
        MessageList.Visibility = Visibility.Visible;
        foreach (var item in listItemsProvider())
        {
            MessageList.Items.Add(item);
        }
    }
    
    MessageFooter.Text = footerText;
}

    private void ConfigureAsProgressWindow()
    {
        Width = ProgressWindowWidth + ProgressWindowPadding;
        Height = BaseHeight;
        MinWidth = ProgressWindowWidth + ProgressWindowPadding;
        MinHeight = BaseHeight;
        ProgressContent.Visibility = Visibility.Visible;
        MessageBoxContent.Visibility = Visibility.Collapsed;
        
        var showProgressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        showProgressTimer.Tick += (s, e) =>
        {
            progressBar.Visibility = Visibility.Visible;
            showProgressTimer.Stop();
        };
        showProgressTimer.Start();
        MessageBoxButtons.Visibility = Visibility.Collapsed;
        StartProgress();
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        Height = ExpandedHeight;
    }

    private void Expander_Collapsed(object sender, RoutedEventArgs e)
    {
        Height = BaseHeight;
    }

    private void StartProgress()
    {
        _stopwatch.Start();
        _timer.Start();
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        double progress = (_stopwatch.ElapsedMilliseconds / (_estimatedDuration * 1000)) * 100;
        progressBar.Progress = Math.Min(progress, 100);
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

    public void SetProgress(double value)
    {
        Dispatcher.Invoke(() => progressBar.Progress = value);
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
