using System;
using Maui.BindableProperty.Generator.Core;
#if MACCATALYST
using UIKit;
using CoreGraphics;
#endif

namespace Dots.Controls;

public partial class BusyButton : Button
{
    [AutoBindable(OnChanged = nameof(UpdateIsBusy))]
    private readonly bool _isBusy;

#if WINDOWS
    Microsoft.UI.Xaml.Controls.Button _button;
    Microsoft.UI.Xaml.controls.Indicator _spinner;
#endif
#if MACCATALYST
    UIButton _button;
    UIActivityIndicatorView _spinner;
#endif

    string _initialText;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null)
        {
            _initialText = Text;
#if MACCATALYST
            if (Handler.PlatformView is UIButton btn)
            {
                //add activity indicator view to button
                _spinner = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.White);
                _spinner.Frame = new CGRect(0, 0, btn.Frame.Width, btn.Frame.Height);
                _spinner.HidesWhenStopped = true;
                btn.AddSubview(_spinner);

            }
#endif
#if WINDOWS
            if (Handler.PlatformView is Microsoft.UI.Xaml.Controls.Button btn)
            {
                //add winui indicator to button
                _spinner = new Microsoft.UI.Xaml.Controls.Indicator();
                _spinner.Width = btn.Width;
                _spinner.Height = btn.Height;
                _spinner.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                _spinner.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                _spinner.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                btn.Content = _spinner;
            }
#endif
        }
    }

    void UpdateIsBusy()
    {
        Text = IsBusy ? "" : _initialText;
        if (IsBusy)
        {
#if MACCATALYST
            _spinner.StartAnimating();
#endif
#if WINDOWS
            _spinner.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
#endif

        }
        else
        {
#if MACCATALYST
            _spinner.StopAnimating();
#endif
#if WINDOWS
            _spinner.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
#endif
        }
    }
}

