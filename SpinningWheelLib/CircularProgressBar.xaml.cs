using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SpinningWheelLib
{
    public partial class CircularProgressBar : UserControl
    {
        public static readonly DependencyProperty ProgressProperty = 
            DependencyProperty.Register(nameof(Progress), typeof(double), typeof(CircularProgressBar), 
                new PropertyMetadata(0.0, OnProgressChanged));

        public static readonly DependencyProperty SizeProperty = 
            DependencyProperty.Register(nameof(Size), typeof(double), typeof(CircularProgressBar), 
                new PropertyMetadata(100.0, OnSizeChanged));

        public static readonly DependencyProperty ThicknessProperty = 
            DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(CircularProgressBar), 
                new PropertyMetadata(4.0));

        public static readonly DependencyProperty ProgressColorProperty = 
            DependencyProperty.Register(nameof(ProgressColor), typeof(Brush), typeof(CircularProgressBar), 
                new PropertyMetadata(Brushes.Green));

        public static readonly DependencyProperty ProgressLeftColorProperty = 
            DependencyProperty.Register(nameof(ProgressLeftColor), typeof(Brush), typeof(CircularProgressBar), 
                new PropertyMetadata(Brushes.LightGreen));

        public static readonly DependencyProperty TextColorProperty = 
            DependencyProperty.Register(nameof(TextColor), typeof(Brush), typeof(CircularProgressBar), 
                new PropertyMetadata(Brushes.DarkGreen));

        public CircularProgressBar()
        {
            InitializeComponent();
            SizeChanged += (s, e) => UpdateProgressBar();
        }

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
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

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgressBar)d;
            control.UpdateProgressBar();
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgressBar)d;
            control.Width = control.Size;
            control.Height = control.Size;
            control.UpdateProgressBar();
        }

      private const int TotalMarks = 120;
private readonly List<Line> progressLines = new();

private void UpdateProgressBar()
{
     var center = Size / 2;
    var radius = (Size / 2) - (Thickness * 2); // Adjusted radius calculation
    var outerRadius = radius + (Thickness * 2); // Scale outer radius relative to thickness

    // Center the background circle with proper sizing
    BackgroundArc.Data = new EllipseGeometry(new Point(center, center), radius, radius);

    // Initialize progress marks if needed
    if (progressLines.Count == 0)
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

    // Calculate visible marks
    int visibleMarks = (int)(TotalMarks * (Progress / 100.0));

    // Update each line
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

        // Animate opacity
        var animation = new DoubleAnimation
        {
            To = i < visibleMarks ? 1 : 0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase()
        };
        line.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    ProgressText.Text = $"{(int)Progress}%";
}






    }
}
