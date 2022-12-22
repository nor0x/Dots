using Dots.Helpers;
using Dots.Models;
using Microsoft.UI.Input;
using System;
using System.Diagnostics;

namespace Dots;

public partial class MainPage : ContentPage
{
    MainViewModel _vm => BindingContext as MainViewModel;
    public MainPage(MainViewModel vm)
    {
        BindingContext = vm;
        InitializeComponent();
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CheckSdks();
    }

    private async void SdkCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var newSelection = e.CurrentSelection.FirstOrDefault() != e.PreviousSelection.FirstOrDefault();
        var open = DetailsPanel.Width == 0;
        if (open)
        {
            await DetailsPanel.WidthTo(300);
            await CollapseDetailsButton.FadeTo(100);

        }
        else
        {
            if (newSelection)
            {
                await DetailsPanel.WidthTo(0);
                await CollapseDetailsButton.FadeTo(0);
            }
        }
        if (e.CurrentSelection.FirstOrDefault() is Sdk sdk)
        {
            _vm.SelectedSdk = sdk;
        }
    }

    private void ReleaseNotes_PointerExited(object sender, Microsoft.Maui.Controls.PointerEventArgs e)
    {
        if (sender is Label label)
        {
#if WINDOWS
            if (label.Handler.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock textBlock)
            {
                textBlock.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
            }
#endif
        }
    }

    private void ReleaseNotes_PointerEntered(object sender, Microsoft.Maui.Controls.PointerEventArgs e)
    {
        if (sender is Label label)
        {
#if WINDOWS
            if(label.Handler.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock textBlock)
            {
                textBlock.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Hand));
            }
#endif
        }
    }

    private async void CollapseDetails_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        if(DetailsPanel.Width > 0)
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
}

