using System;
using AppKit;

namespace Dots.Controls;

public class HyperLinkLabel : Label
{
    public HyperLinkLabel()
    {
        pointerGesture = new PointerGestureRecognizer();
        pointerGesture.PointerEntered += PointerEntered;
        pointerGesture.PointerExited += PointerExited;
        GestureRecognizers.Add(pointerGesture);
    }

    ~HyperLinkLabel()
    {
        pointerGesture.PointerEntered -= PointerEntered;
        pointerGesture.PointerExited -= PointerExited;
    }
    PointerGestureRecognizer pointerGesture;

    void PointerEntered(object sender, Microsoft.Maui.Controls.PointerEventArgs e)
    {
        if (sender is Label label)
        {
#if WINDOWS
        if (label.Handler.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock textBlock)
        {
            textBlock.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Hand));
        }
#endif

#if MACCATALYST
            NSCursor.PointingHandCursor.Set();
#endif
        }
    }

    void PointerExited(object sender, Microsoft.Maui.Controls.PointerEventArgs e)
    {
        if (sender is Label label)
        {
#if WINDOWS
        if (label.Handler.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock textBlock)
        {
            textBlock.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        }
#endif
#if MACCATALYST
            NSCursor.ArrowCursor.Set();
#endif
        }
    }
}