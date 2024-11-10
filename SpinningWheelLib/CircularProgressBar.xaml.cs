using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SpinningWheelLib
{
    public partial class CircularProgressBar : UserControl
    {
        private const int TotalMarks = 120;
        private readonly List<Line> progressLines = new();
        private bool isMarquee;
        private DispatcherTimer marqueeTimer;

        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(CircularProgressBar),
                new PropertyMetadata(0.0, OnProgressChanged));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(CircularProgressBar),
                new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(double), typeof(CircularProgressBar),
                new PropertyMetadata(100.0));

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register("Thickness", typeof(double), typeof(CircularProgressBar),
                new PropertyMetadata(4.0));

        public static readonly DependencyProperty ProgressColorProperty =
            DependencyProperty.Register("ProgressColor", typeof(Brush), typeof(CircularProgressBar),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(144, 238, 144)))); // LightGreen

        public static readonly DependencyProperty ProgressLeftColorProperty =
            DependencyProperty.Register("ProgressLeftColor", typeof(Brush), typeof(CircularProgressBar),
                new PropertyMetadata(Brushes.White));

        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register("TextColor", typeof(Brush), typeof(CircularProgressBar),
                new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty IsMarqueeProperty =
            DependencyProperty.Register("IsMarquee", typeof(bool), typeof(CircularProgressBar),
                new PropertyMetadata(false, OnIsMarqueeChanged));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public Brush ProgressColor
        {
            get => (Brush)GetValue(ProgressColorProperty);
            set => SetValue(ProgressColorProperty, value);
        }

        public Brush ProgressLeftColor
        {
            get => (Brush)GetValue(ProgressLeftColorProperty);
            set => SetValue(ProgressLeftColorProperty, value);
        }

        public Brush TextColor
        {
            get => (Brush)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public bool IsMarquee
        {
            get => (bool)GetValue(IsMarqueeProperty);
            set => SetValue(IsMarqueeProperty, value);
        }

        public CircularProgressBar()
        {
            InitializeComponent();
            InitializeMarqueeTimer();
            UpdateProgressBar();
        }

        private void InitializeMarqueeTimer()
        {
            marqueeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            marqueeTimer.Tick += MarqueeTimer_Tick;
        }

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgressBar)d;
            control.UpdateProgressBar();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgressBar)d;
            control.Progress = (double)e.NewValue;
            control.UpdateProgressBar();
        }

        private static void OnIsMarqueeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgressBar)d;
            control.isMarquee = (bool)e.NewValue;

            if (control.isMarquee)
                control.marqueeTimer.Start();
            else
                control.marqueeTimer.Stop();
        }

        private void MarqueeTimer_Tick(object sender, EventArgs e)
        {
            UpdateMarqueeAnimation();
        }

        private void UpdateMarqueeAnimation()
        {
            if (!isMarquee) return;

            var blocks = new List<object>();
            int totalBlocks = (int)(ActualWidth / 10);
            int visibleBlocks = Math.Min(totalBlocks / 4, 5);

            for (int i = 0; i < visibleBlocks; i++)
            {
                blocks.Add(new object());
            }

            ProgressBlocks.ItemsSource = blocks;

            var marqueeAnim = new DoubleAnimation
            {
                From = -100,
                To = ActualWidth,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };

            ProgressIndicator.BeginAnimation(MarginProperty, marqueeAnim);
        }

        private void UpdateProgressBar()
        {
            if (isMarquee) return;

            var center = Size / 2;
            var radius = (Size / 2) - (Thickness * 2);

            BackgroundArc.Data = new EllipseGeometry(new Point(center, center), radius, radius);

            if (progressLines.Count == 0)
            {
                InitializeProgressMarks();
            }

            int visibleMarks = (int)(TotalMarks * (Progress / 100.0));
            UpdateProgressMarks(visibleMarks);

            // Update XP Style Progress Bar
            int totalBlocks = (int)(ActualWidth / 10);
            int visibleBlocks = (int)(totalBlocks * (Progress / 100.0));
            var blocks = new List<object>();

            for (int i = 0; i < visibleBlocks; i++)
            {
                blocks.Add(new object());
            }

            ProgressBlocks.ItemsSource = blocks;
        }

        private void InitializeProgressMarks()
        {
            ProgressCanvas.Width = Size;
            ProgressCanvas.Height = Size;

            for (int i = 0; i < TotalMarks; i++)
            {
                var line = new Line
                {
                    Stroke = ProgressColor,
                    StrokeThickness = 2,
                    Opacity = 0
                };

                progressLines.Add(line);
                ProgressCanvas.Children.Add(line);
            }
        }

        private void UpdateProgressMarks(int visibleMarks)
        {
            var center = Size / 2;
            var radius = (Size / 2) - (Thickness * 2);
            var outerRadius = radius + (Thickness * 2);

            for (int i = 0; i < TotalMarks; i++)
            {
                double angle = (i * 360.0 / TotalMarks) * Math.PI / 180;
                var startPoint = new Point(
                    center + radius * Math.Cos(angle),
                    center + radius * Math.Sin(angle));
                var endPoint = new Point(
                    center + outerRadius * Math.Cos(angle),
                    center + outerRadius * Math.Sin(angle));

                var line = progressLines[i];
                line.X1 = startPoint.X;
                line.Y1 = startPoint.Y;
                line.X2 = endPoint.X;
                line.Y2 = endPoint.Y;

                var animation = new DoubleAnimation
                {
                    To = i < visibleMarks ? 1 : 0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase()
                };
                line.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }
    }
}
