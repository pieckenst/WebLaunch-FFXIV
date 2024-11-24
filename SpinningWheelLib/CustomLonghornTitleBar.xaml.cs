using System.Runtime.InteropServices;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Diagnostics;

namespace SpinningWheelLib.Controls
{
    public partial class CustomLonghornTitleBar : UserControl
    {
        #region Fields
        private double originalWidth;
        private double originalHeight;
        private double originalLeft;
        private double originalTop;
        private bool isMaximized;
        #endregion

        #region DWM Imports and Structures
        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern bool DwmIsCompositionEnabled();

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        private static extern int DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DWM_BLURBEHIND
        {
            public int dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;
        }

        private const int DWM_BB_ENABLE = 0x00000001;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_MICA_EFFECT = 1029;
        private const int WM_DWMCOMPOSITIONCHANGED = 0x031E;
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty TitleBarContentProperty =
            DependencyProperty.Register("TitleBarContent", typeof(object), typeof(CustomLonghornTitleBar));

        public static readonly DependencyProperty MenuBarContentProperty =
            DependencyProperty.Register("MenuBarContent", typeof(object), typeof(CustomLonghornTitleBar));

        public static readonly DependencyProperty BreadcrumbContentProperty =
            DependencyProperty.Register("BreadcrumbContent", typeof(object), typeof(CustomLonghornTitleBar));

        public static readonly DependencyProperty AdditionalContentProperty =
            DependencyProperty.Register("AdditionalContent", typeof(object), typeof(CustomLonghornTitleBar));

        public static readonly DependencyProperty UseSlateThemeProperty =
            DependencyProperty.Register("UseSlateTheme", typeof(bool), typeof(CustomLonghornTitleBar),
                new PropertyMetadata(true, OnUseSlateThemeChanged));

        public static readonly DependencyProperty UseAeroThemeProperty =
            DependencyProperty.Register("UseAeroTheme", typeof(bool), typeof(CustomLonghornTitleBar),
                new PropertyMetadata(false, OnUseAeroThemeChanged));
        #endregion

        #region Properties
        public bool UseSlateTheme
        {
            get => (bool)GetValue(UseSlateThemeProperty);
            set => SetValue(UseSlateThemeProperty, value);
        }

        public bool UseAeroTheme
        {
            get => (bool)GetValue(UseAeroThemeProperty);
            set => SetValue(UseAeroThemeProperty, value);
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
        #endregion

        public CustomLonghornTitleBar()
        {
            try
            {
                InitializeComponent();
                SetupWindowControls();
                SetupEventHandlers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initialization error: {ex.Message}");
            }
        }

        private void SetupWindowControls()
        {
            try
            {
                MinButton.Opacity = 0.8;
                MaxButton.Opacity = 0.8;
                CloseButton.Opacity = 0.8;

                MinButton.MouseEnter += (s, e) => AnimateWindowButton(MinButton, true);
                MinButton.MouseLeave += (s, e) => AnimateWindowButton(MinButton, false);

                MaxButton.MouseEnter += (s, e) => AnimateWindowButton(MaxButton, true);
                MaxButton.MouseLeave += (s, e) => AnimateWindowButton(MaxButton, false);

                CloseButton.MouseEnter += (s, e) => AnimateWindowButton(CloseButton, true);
                CloseButton.MouseLeave += (s, e) => AnimateWindowButton(CloseButton, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Window controls setup: {ex.Message}");
            }
        }

        private void AnimateWindowButton(Button button, bool isHovered)
        {
            try
            {
                var animation = new DoubleAnimation
                {
                    To = isHovered ? 1.0 : 0.8,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                button.BeginAnimation(UIElement.OpacityProperty, animation);

                // Add glow effect when hovered
                if (isHovered)
                {
                    button.Effect = new DropShadowEffect
                    {
                        Color = Colors.White,
                        BlurRadius = 15,
                        ShadowDepth = 0
                    };
                }
                else
                {
                    button.Effect = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Animate window button error: {ex.Message}");
            }
        }

        private void SetupEventHandlers()
        {
            try
            {
                MinButton.Click += (s, e) => {
                    try
                    {
                        AnimateMinimize();
                        var window = Window.GetWindow(this);
                        if (window != null) window.WindowState = WindowState.Minimized;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Minimize button click error: {ex.Message}");
                    }
                };

                MaxButton.Click += (s, e) => {
                    try
                    {
                        var window = Window.GetWindow(this);
                        if (window != null)
                        {
                            AnimateMaximize();
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
                                StoreWindowDimensions(window);
                                MaximizeWindow(window);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Maximize button click error: {ex.Message}");
                    }
                };

                CloseButton.Click += (s, e) => {
                    try
                    {
                        var window = Window.GetWindow(this);
                        AnimateClose(window);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Close button click error: {ex.Message}");
                    }
                };

                Loaded += OnLoaded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Setup event handlers error: {ex.Message}");
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    var source = PresentationSource.FromVisual(window) as HwndSource;
                    if (source != null)
                    {
                        source.AddHook(WndProc);
                    }
                    UpdateBackground();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loading error: {ex.Message}");
            }
        }

        private void SetupWindowAnimations()
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window == null) return;

                var storyboard = new Storyboard();

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeIn, window);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));

                storyboard.Children.Add(fadeIn);
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Setup window animations error: {ex.Message}");
            }
        }

        private void SetupLonghornEffects()
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window == null) return;

                EnableBlurBehind(new WindowInteropHelper(window).Handle);
                SetupWindowAnimations();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Setup Longhorn effects error: {ex.Message}");
            }
        }

        private void EnableBlurBehind(IntPtr hwnd)
        {
            try
            {
                if (!IsCompositionEnabled()) return;

                var bb = new DWM_BLURBEHIND
                {
                    dwFlags = DWM_BB_ENABLE,
                    fEnable = true,
                    hRgnBlur = IntPtr.Zero
                };

                DwmEnableBlurBehindWindow(hwnd, ref bb);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Enable blur behind error: {ex.Message}");
            }
        }

        private void UpdateBackground()
        {
            try
            {
                if (UseSlateTheme)
                {
                    ApplySlateTheme();
                }
                else if (UseAeroTheme)
                {
                    ApplyAeroTheme();
                }
                else
                {
                    ApplyDefaultTheme();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update background error: {ex.Message}");
            }
        }

        private void ApplySlateTheme()
        {
            try
            {
                glassArea.Background = (LinearGradientBrush)FindResource("TaskbarBackground");
                GlassOverlay.Effect = null;
                Background = Brushes.Transparent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Apply Slate theme error: {ex.Message}");
            }
        }

        private void ApplyAeroTheme()
        {
            try
            {
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null && IsCompositionEnabled())
                {
                    ConfigureAeroWindow(parentWindow);
                    ApplyAeroEffects();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Apply Aero theme error: {ex.Message}");
            }
        }

        private void ConfigureAeroWindow(Window window)
        {
            try
            {

                window.WindowStyle = WindowStyle.None;
                var mainWindowPtr = new WindowInteropHelper(window).Handle;
                if (mainWindowPtr != IntPtr.Zero)
                {
                    var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                    DwmExtendFrameIntoClientArea(mainWindowPtr, ref margins);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Configure Aero window error: {ex.Message}");
            }
        }

        private void ApplyAeroEffects()
        {
            try
            {
                GlassOverlay.Effect = new BlurEffect
                {
                    Radius = 10,
                    RenderingBias = RenderingBias.Quality
                };

                GlassOverlay.Background = new LinearGradientBrush(
                    Color.FromArgb(128, 255, 255, 255),
                    Color.FromArgb(64, 255, 255, 255),
                    new Point(0, 0),
                    new Point(0, 1));

                glassArea.Background = new SolidColorBrush(Colors.Transparent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Apply Aero effects error: {ex.Message}");
            }
        }

        private void ApplyDefaultTheme()
        {
            try
            {
                glassArea.Background = Background;
                GlassOverlay.Effect = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Apply default theme error: {ex.Message}");
            }
        }

        private void AnimateMinimize()
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    var animation = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    window.BeginAnimation(UIElement.OpacityProperty, animation);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Animate minimize error: {ex.Message}");
            }
        }

        private void AnimateMaximize()
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    var scaleTransform = new ScaleTransform(1, 1);
                    window.RenderTransform = scaleTransform;

                    var animation = new DoubleAnimation
                    {
                        From = 1,
                        To = 1.02,
                        Duration = TimeSpan.FromMilliseconds(150),
                        AutoReverse = true,
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Animate maximize error: {ex.Message}");
            }
        }

        private void AnimateClose(Window window)
        {
            try
            {
                if (window != null)
                {
                    var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
                    fadeOut.Completed += (s2, e2) => window.Close();
                    window.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Animate close error: {ex.Message}");
            }
        }

        private void StoreWindowDimensions(Window window)
        {
            try
            {
                originalWidth = window.Width;
                originalHeight = window.Height;
                originalLeft = window.Left;
                originalTop = window.Top;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Store window dimensions error: {ex.Message}");
            }
        }

        private void MaximizeWindow(Window window)
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                window.Left = workArea.Left;
                window.Top = workArea.Top;
                window.Width = workArea.Width;
                window.Height = workArea.Height;
                isMaximized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Maximize window error: {ex.Message}");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if (msg == WM_DWMCOMPOSITIONCHANGED)
                {
                    UpdateBackground();
                    handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WndProc error: {ex.Message}");
            }
            return IntPtr.Zero;
        }

        private bool IsCompositionEnabled()
        {
            try
            {
                return DwmIsCompositionEnabled();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Is composition enabled error: {ex.Message}");
                return false;
            }
        }

        private static void OnUseSlateThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (d is CustomLonghornTitleBar control)
                {
                    control.UpdateBackground();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"On use Slate theme changed error: {ex.Message}");
            }
        }

        private static void OnUseAeroThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (d is CustomLonghornTitleBar control)
                {
                    control.UpdateBackground();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"On use Aero theme changed error: {ex.Message}");
            }
        }

        public void ShowFavoriteButton(bool show)
        {
            try
            {
                favButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Show favorite button error: {ex.Message}");
            }
        }

        public void EnableForwardButton(bool enable)
        {
            try
            {
                forwardButton.IsEnabled = enable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Enable forward button error: {ex.Message}");
            }
        }

        public event RoutedEventHandler FavoriteClick
        {
            add
            {
                try
                {
                    favButton.Click += value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Add favorite click event error: {ex.Message}");
                }
            }
            remove
            {
                try
                {
                    favButton.Click -= value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Remove favorite click event error: {ex.Message}");
                }
            }
        }

        public event RoutedEventHandler BackClick
        {
            add
            {
                try
                {
                    backButton.Click += value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Add back click event error: {ex.Message}");
                }
            }
            remove
            {
                try
                {
                    backButton.Click -= value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Remove back click event error: {ex.Message}");
                }
            }
        }

        public event RoutedEventHandler ForwardClick
        {
            add
            {
                try
                {
                    forwardButton.Click += value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Add forward click event error: {ex.Message}");
                }
            }
            remove
            {
                try
                {
                    forwardButton.Click -= value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Remove forward click event error: {ex.Message}");
                }
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.DragMove();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Remove forward click event error: {ex.Message}");
            }
        }
    }
}
