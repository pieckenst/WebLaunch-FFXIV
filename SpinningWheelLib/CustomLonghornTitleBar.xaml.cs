using System.Runtime.InteropServices;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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

        #region DWM Imports
        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern bool DwmIsCompositionEnabled();

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

        private bool IsDwmCompositionEnabled()
        {
            try
            {
                return DwmIsCompositionEnabled();
            }
            catch
            {
                return false;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        private const int WM_DWMCOMPOSITIONCHANGED = 0x031E;
        #endregion

        public static readonly DependencyProperty UseAeroThemeProperty =
            DependencyProperty.Register("UseAeroTheme", typeof(bool), typeof(CustomLonghornTitleBar),
                new PropertyMetadata(false, OnUseAeroThemeChanged));

        public bool UseAeroTheme
        {
            get { return (bool)GetValue(UseAeroThemeProperty); }
            set { SetValue(UseAeroThemeProperty, value); }
        }

        private static void OnUseAeroThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
                GlassOverlay.Effect = null;
                Background = Brushes.Transparent;
            }
            else if (UseAeroTheme)
            {
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    parentWindow.AllowsTransparency = true;
                    parentWindow.WindowStyle = WindowStyle.None;

                    if (IsDwmCompositionEnabled())
                    {
                        var mainWindowPtr = new WindowInteropHelper(parentWindow).Handle;
                        if (mainWindowPtr != IntPtr.Zero)
                        {
                            var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                            DwmExtendFrameIntoClientArea(mainWindowPtr, ref margins);
                        }
                    }
                }

                glassArea.Background = Brushes.Transparent;
                GlassOverlay.Background = (LinearGradientBrush)FindResource("AeroGlassEffect");
                Background = Brushes.Transparent;
            }
            else
            {
                glassArea.Background = Background;
                GlassOverlay.Effect = null;
            }
        }



        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DWMCOMPOSITIONCHANGED)
            {
                UpdateBackground();
                handled = true;
            }
            return IntPtr.Zero;
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

            Loaded += (s, e) =>
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                    if (source != null)
                    {
                        source.AddHook(WndProc);
                    }
                }
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
