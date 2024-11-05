using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
private const double MessageBoxHeight = 200;
private const double MessageBoxWidth = 400;
private const double ProgressWindowWidth = 1166;
    public bool DialogResult { get; private set; }

    private readonly TextWriter _originalConsole;
    private readonly StreamWriter _fileWriter;
    private readonly string _tempLogFile;

    public Window1(double estimatedDurationInSeconds = 30, bool isMessageBox = false, string customLabel = "Loading...", double? customWidth = null, double? customHeight = null)
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
        double width = customWidth ?? MessageBoxWidth;
        double height = customHeight ?? MessageBoxHeight;
        ConfigureAsMessageBox(customLabel, width, height);
    }
    else
    {
        ConfigureAsProgressWindow();
        MainLabel.Content = customLabel;
    }

    SizeToContent = SizeToContent.Height;
}


private void ConfigureAsMessageBox(string message, double width, double height)
{
    Title = "Message";
    Width = width;
    Height = height;
    if (MainLabel != null)
    {
        MainLabel.FontSize = 14;
        MainLabel.Content = message;
        MainLabel.Height = Double.NaN;
        MainLabel.Width = Double.NaN;
    }
    if (progressBar != null)
    {
        progressBar.Visibility = Visibility.Collapsed;
    }
    if (ConsoleExpander != null)
    {
        ConsoleExpander.Visibility = Visibility.Collapsed;
    }
    if (MessageBoxButtons != null)
    {
        MessageBoxButtons.Visibility = Visibility.Visible;
    }
    Background = new SolidColorBrush(Color.FromRgb(236, 233, 216));
    ResizeMode = ResizeMode.NoResize;
    WindowStyle = WindowStyle.SingleBorderWindow;
}

    // All existing methods remain fully implemented
    private void ConfigureAsProgressWindow()
{
    Width = ProgressWindowWidth;
    Height = BaseHeight;
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
            Foreground = new SolidColorBrush(Colors.Black) // Force black text color
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
    }}

}
