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
    Microsoft.UI.Xaml.Controls.ProgressRing _spinner;
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
                _button = btn;
                _button.Padding = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 0);
                _spinner = new Microsoft.UI.Xaml.Controls.ProgressRing();
                _spinner.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                _spinner.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                _spinner.Height = btn.Height - 3;
                _spinner.Width = btn.Height - 3;
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
            _button.Content = _spinner;
#endif

        }
        else
        {
#if MACCATALYST
            _spinner.StopAnimating();
#endif
#if WINDOWS
            _button.Content = _initialText;
#endif
        }
    }
}

