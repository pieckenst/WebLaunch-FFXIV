using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SpinningWheelLib.Controls
{
    public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    
    public RelayCommand(Action<object> execute)
    {
        _execute = execute;
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object parameter) => true;

    public void Execute(object parameter)
    {
        _execute(parameter);
    }
}

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

        public ICommand ToggleCollapseCommand { get; }


        public RibbonControl()
        {
            try
            {
                Console.WriteLine("Initializing RibbonControl...");
                InitializeComponent();
                PART_TabControl.SelectionChanged += OnTabSelectionChanged;
                
                Console.WriteLine("RibbonControl initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing RibbonControl: {ex.Message}");
            }
        }

        private static void OnIsFoldedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                Console.WriteLine("IsFolded property changed.");
                var ribbon = (RibbonControl)d;
                ribbon.UpdateRibbonHeight();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnIsFoldedChanged: {ex.Message}");
            }
        }

        private static void OnSelectedTabIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                Console.WriteLine("SelectedTabIndex property changed.");
                var ribbon = (RibbonControl)d;
                ribbon.PART_TabControl.SelectedIndex = (int)e.NewValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnSelectedTabIndexChanged: {ex.Message}");
            }
        }

        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Console.WriteLine("Tab selection changed.");
                if (!IsFolded)
                {
                    UpdateRibbonHeight();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnTabSelectionChanged: {ex.Message}");
            }
        }

        private void UpdateRibbonHeight()
        {
            try
            {
                Console.WriteLine("Updating ribbon height...");
                var parentTitleBar = this.GetParentOfType<CustomLonghornTitleBar>();
                if (parentTitleBar != null)
                {
                    var targetHeight = IsFolded ? 74 : 180;
                    Console.WriteLine($"Target height set to {targetHeight}.");

                    // Update parent window layout
                    var window = Window.GetWindow(parentTitleBar);
                    if (window != null)
                    {
                        window.InvalidateVisual();
                        window.UpdateLayout();
                    }

                    // Animate height change
                    var animation = new DoubleAnimation
                    {
                        To = targetHeight,
                        Duration = TimeSpan.FromMilliseconds(167),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    parentTitleBar.Height = targetHeight;
                    parentTitleBar.BeginAnimation(HeightProperty, animation);
                    parentTitleBar.UpdateLayout();
                    Console.WriteLine("Ribbon height updated successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateRibbonHeight: {ex.Message}");
            }
        }

        private void OnCollapseButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Collapse button clicked.");
                IsFolded = !IsFolded;
                Console.WriteLine($"IsFolded set to {IsFolded}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnCollapseButtonClick: {ex.Message}");
            }
        }

        public void AddTab(TabItem tab)
        {
            try
            {
                Console.WriteLine("Adding tab...");
                PART_TabControl.Items.Add(tab);
                Console.WriteLine("Tab added successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddTab: {ex.Message}");
            }
        }

        public void RemoveTab(TabItem tab)
        {
            try
            {
                Console.WriteLine("Removing tab...");
                PART_TabControl.Items.Remove(tab);
                Console.WriteLine("Tab removed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveTab: {ex.Message}");
            }
        }

        public void ClearTabs()
        {
            try
            {
                Console.WriteLine("Clearing all tabs...");
                PART_TabControl.Items.Clear();
                Console.WriteLine("All tabs cleared successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ClearTabs: {ex.Message}");
            }
        }
    }

    public static class ControlExtensions
    {
        public static T GetParentOfType<T>(this DependencyObject element) where T : DependencyObject
        {
            try
            {
                if (element == null) return null;

                var parent = VisualTreeHelper.GetParent(element);

                while (parent != null && !(parent is T))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }

                return parent as T;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetParentOfType: {ex.Message}");
                return null;
            }
        }
    }
}
