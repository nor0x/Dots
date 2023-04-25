#if WINDOWS
using Microsoft.UI.Input;
#endif
#if MACCATALYST
using AppKit;
using UIKit;
#endif
using Dots.Helpers;
using Dots.Models;
using System;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Dots;

public partial class MainPage : ContentPage
{
    MainViewModel _vm => BindingContext as MainViewModel;
    public MainPage(MainViewModel vm)
    {
        BindingContext = vm;
        InitializeComponent();
        AnimationView.Source = new HtmlWebViewSource()
        {
            Html = $$"""<body style=background:#605d64;overflow:hidden><video autoplay loop muted style=width:180px;height:180px><source src="https://github.com/nor0x/Dots/raw/main/Assets/dotsanimation1.mp4" "type=" video/mp4">"""
        };
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        var lastChecked = Preferences.Get(Constants.LastCheckedKey, DateTime.MinValue);

        bool force = false;
        if (DateTime.Now.Subtract(lastChecked).TotalDays > 15)
        {
            force = true;
            Preferences.Set(Constants.LastCheckedKey, DateTime.Now);
        }
        await _vm.CheckSdks(force);

    }

    private async void CollapseDetails_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        if (DetailsPanel.Width > 0)
        {
            await DetailsPanel.WidthTo(0);
        }
        await CollapseDetailsButton.FadeTo(0);
    }

    private void Logo_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        if (Application.Current.Windows.Count() == 1)
        {
            var secondWindow = new Window
            {
                Page = new AboutPage(),
                MaximumHeight = 420,
                MaximumWidth = 320,
                MinimumHeight = 420,
                MinimumWidth = 320,
            };

            Application.Current.OpenWindow(secondWindow);
        }
        else
        {
            App.Current.CloseWindow(Application.Current.Windows.FirstOrDefault(w => w.Page is AboutPage));
        }
    }

    private async void Item_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        if (sender is Grid gr && gr.BindingContext is Sdk s)
        {
            var border = (VisualElement)gr.Children.First();
            await border.ScaleTo(0.80, 150, Easing.BounceIn);
            await border.ScaleTo(1.0, 150, Easing.BounceOut);

            bool newSelection = s != _vm.SelectedSdk;
            _vm.SelectedSdk = s;

            var open = DetailsPanel.Width == 0;
            if (open)
            {
                await DetailsPanel.WidthTo(300);
                await CollapseDetailsButton.FadeTo(100);

            }
            else
            {
                if (!newSelection)
                {
                    await DetailsPanel.WidthTo(0);
                    await CollapseDetailsButton.FadeTo(0);
                }
            }
        }
    }
}

