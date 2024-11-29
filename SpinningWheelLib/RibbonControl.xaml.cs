using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SpinningWheelLib.Controls
{
    public partial class RibbonControl : UserControl
    {
        public static readonly DependencyProperty IsFoldedProperty =
            DependencyProperty.Register("IsFolded", typeof(bool), typeof(RibbonControl),
                new PropertyMetadata(true, OnIsFoldedChanged));

        public static readonly DependencyProperty QuickAccessToolbarContentProperty =
            DependencyProperty.Register("QuickAccessToolbarContent", typeof(object), typeof(RibbonControl));

        public static readonly DependencyProperty MenuContentProperty =
            DependencyProperty.Register("MenuContent", typeof(object), typeof(RibbonControl));

        public static readonly DependencyProperty SelectedTabIndexProperty =
            DependencyProperty.Register("SelectedTabIndex", typeof(int), typeof(RibbonControl),
                new PropertyMetadata(0, OnSelectedTabIndexChanged));

        public bool IsFolded
        {
            get => (bool)GetValue(IsFoldedProperty);
            set => SetValue(IsFoldedProperty, value);
        }

        public object QuickAccessToolbarContent
        {
            get => GetValue(QuickAccessToolbarContentProperty);
            set => SetValue(QuickAccessToolbarContentProperty, value);
        }

        public object MenuContent
        {
            get => GetValue(MenuContentProperty);
            set => SetValue(MenuContentProperty, value);
        }

        public int SelectedTabIndex
        {
            get => (int)GetValue(SelectedTabIndexProperty);
            set => SetValue(SelectedTabIndexProperty, value);
        }

        public RibbonControl()
        {
            InitializeComponent();
            PART_TabControl.SelectionChanged += OnTabSelectionChanged;
        }

        private static void OnIsFoldedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = (RibbonControl)d;
            ribbon.UpdateRibbonHeight();
        }

        private static void OnSelectedTabIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = (RibbonControl)d;
            ribbon.PART_TabControl.SelectedIndex = (int)e.NewValue;
        }

        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsFolded)
            {
                UpdateRibbonHeight();
            }
        }

        private void UpdateRibbonHeight()
        {
            var parentTitleBar = this.GetParentOfType<CustomLonghornTitleBar>();
            if (parentTitleBar != null)
            {
                var targetHeight = IsFolded ? 110 : 180;
                var animation = new DoubleAnimation
                {
                    To = targetHeight,
                    Duration = TimeSpan.FromMilliseconds(167),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                parentTitleBar.BeginAnimation(HeightProperty, animation);
            }
        }

        private void OnCollapseButtonClick(object sender, RoutedEventArgs e)
        {
            IsFolded = !IsFolded;
        }

        public void AddTab(TabItem tab)
        {
            PART_TabControl.Items.Add(tab);
        }

        public void RemoveTab(TabItem tab)
        {
            PART_TabControl.Items.Remove(tab);
        }

        public void ClearTabs()
        {
            PART_TabControl.Items.Clear();
        }
    }

    public static class ControlExtensions
    {
        public static T GetParentOfType<T>(this DependencyObject element) where T : DependencyObject
        {
            if (element == null) return null;
            
            var parent = VisualTreeHelper.GetParent(element);
            
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            
            return parent as T;
        }
    }
}
