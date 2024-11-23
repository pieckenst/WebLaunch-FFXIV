﻿using System.Windows;
using System.Windows.Controls;

namespace SpinningWheelLib.Controls
{
    public partial class CustomLonghornTitleBar : UserControl
    {
        public static readonly DependencyProperty TitleBarContentProperty =
            DependencyProperty.Register("TitleBarContent", typeof(object), typeof(CustomLonghornTitleBar), new PropertyMetadata(null));

        public static readonly DependencyProperty MenuBarContentProperty =
            DependencyProperty.Register("MenuBarContent", typeof(object), typeof(CustomLonghornTitleBar), new PropertyMetadata(null));

        public static readonly DependencyProperty BreadcrumbContentProperty =
            DependencyProperty.Register("BreadcrumbContent", typeof(object), typeof(CustomLonghornTitleBar), new PropertyMetadata(null));

        public static readonly DependencyProperty AdditionalContentProperty =
            DependencyProperty.Register("AdditionalContent", typeof(object), typeof(CustomLonghornTitleBar), new PropertyMetadata(null));

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
        if (window != null) {
            window.WindowState = window.WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
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
    }
}