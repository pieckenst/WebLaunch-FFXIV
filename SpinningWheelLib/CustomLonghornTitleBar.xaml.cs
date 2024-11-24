using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpinningWheelLib.Controls
{
    public partial class CustomLonghornTitleBar : UserControl
    {

        private double originalWidth;
        private double originalHeight;
        private double originalLeft;
        private double originalTop;
        private bool isMaximized;

        public static readonly DependencyProperty TitleBarContentProperty =
            DependencyProperty.Register("TitleBarContent", typeof(object), typeof(CustomLonghornTitleBar), new PropertyMetadata(null));

        public static readonly DependencyProperty MenuBarContentProperty =
            DependencyProperty.Register("MenuBarContent", typeof(object), typeof(CustomLonghornTitleBar), new PropertyMetadata(null));

        public static readonly DependencyProperty BreadcrumbContentProperty =
            DependencyProperty.Register("BreadcrumbContent", typeof(object), typeof(CustomLonghornTitleBar), new PropertyMetadata(null));

        public static readonly DependencyProperty AdditionalContentProperty =
            DependencyProperty.Register("AdditionalContent", typeof(object), typeof(CustomLonghornTitleBar), new PropertyMetadata(null));

        public static readonly DependencyProperty UseSlateThemeProperty =
        DependencyProperty.Register("UseSlateTheme", typeof(bool), typeof(CustomLonghornTitleBar),
            new PropertyMetadata(true, OnUseSlateThemeChanged));



        public bool UseSlateTheme
        {
            get { return (bool)GetValue(UseSlateThemeProperty); }
            set { SetValue(UseSlateThemeProperty, value); }
        }

        private static void OnUseSlateThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CustomLonghornTitleBar;
            if (control != null)
            {
                control.UpdateBackground();
            }
        }

        private void UpdateBackground()
        {
            if (UseSlateTheme)
            {
                glassArea.Background = (LinearGradientBrush)FindResource("TaskbarBackground");
            }
            else
            {
                // Use Aero/Window background
                glassArea.Background = Background;
            }
        }

        public object TitleBarContent
        {
            get => GetValue(TitleBarContentProperty);
            set => SetValue(TitleBarContentProperty, value);
        }

        public object MenuBarContent
        {
            get => GetValue(MenuBarContentProperty);
            set => SetValue(MenuBarContentProperty, value);
        }

        public object BreadcrumbContent
        {
            get => GetValue(BreadcrumbContentProperty);
            set => SetValue(BreadcrumbContentProperty, value);
        }

        public object AdditionalContent
        {
            get => GetValue(AdditionalContentProperty);
            set => SetValue(AdditionalContentProperty, value);
        }

        public CustomLonghornTitleBar()
        {
            InitializeComponent();

            MinButton.Click += (s, e) => {
                var window = Window.GetWindow(this);
                if (window != null) window.WindowState = WindowState.Minimized;
            };

            MaxButton.Click += (s, e) => {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    if (isMaximized)
                    {
                        window.Width = originalWidth;
                        window.Height = originalHeight;
                        window.Left = originalLeft;
                        window.Top = originalTop;
                        isMaximized = false;
                    }
                    else
                    {
                        // Store original dimensions
                        originalWidth = window.Width;
                        originalHeight = window.Height;
                        originalLeft = window.Left;
                        originalTop = window.Top;

                        // Set maximized state using work area
                        var workArea = SystemParameters.WorkArea;
                        window.Left = workArea.Left;
                        window.Top = workArea.Top;
                        window.Width = workArea.Width;
                        window.Height = workArea.Height;
                        isMaximized = true;
                    }
                }
            };

            CloseButton.Click += (s, e) => {
                var window = Window.GetWindow(this);
                if (window != null) window.Close();
            };
        }


        public void ShowFavoriteButton(bool show)
        {
            favButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void EnableForwardButton(bool enable)
        {
            forwardButton.IsEnabled = enable;
        }

        public event RoutedEventHandler FavoriteClick
        {
            add { favButton.Click += value; }
            remove { favButton.Click -= value; }
        }

        public event RoutedEventHandler BackClick
        {
            add { backButton.Click += value; }
            remove { backButton.Click -= value; }
        }

        public event RoutedEventHandler ForwardClick
        {
            add { forwardButton.Click += value; }
            remove { forwardButton.Click -= value; }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.DragMove();
            }
        }
    }
}
