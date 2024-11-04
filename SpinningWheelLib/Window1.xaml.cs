using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace SpinningWheelLib
{
    public partial class Window1 : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _stopwatch;
        private readonly double _estimatedDuration;
        private const double BaseHeight = 464;
        private const double ExpandedHeight = 684;

        public Window1(double estimatedDurationInSeconds = 30)
        {
            InitializeComponent();
            _estimatedDuration = estimatedDurationInSeconds;
            _stopwatch = new Stopwatch();
        
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _timer.Tick += Timer_Tick;

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

            StartProgress();
        
            SizeToContent = SizeToContent.Height;
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

            var paragraph = new Paragraph();
            var run = new Run(text)
            {
                Foreground = new SolidColorBrush(color)
            };
            paragraph.Inlines.Add(run);
            consoleOutput.Document.Blocks.Add(paragraph);
            consoleOutput.ScrollToEnd();
        }

        public void SetProgress(double value)
        {
            Dispatcher.Invoke(() => progressBar.Progress = value);
        }

        public new void Close()
        {
            _timer.Stop();
            _stopwatch.Stop();
            base.Close();
        }
    }
}
