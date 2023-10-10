using Akavache;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dots.Helpers;

public static class Extensions
{
    public static async Task HeightTo(this Control view, double height, uint duration = 250, Easing easing = null)
    {
        var animation = new Animation()
        {
            Duration = TimeSpan.FromMilliseconds(duration),
            Easing = easing ?? new CubicEaseInOut(),
            IterationCount = new IterationCount(1),
            FillMode = FillMode.Forward,
            PlaybackDirection = PlaybackDirection.Normal,
            Children =
            {
                new KeyFrame()
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter()
                        {
                            Property = Control.HeightProperty,
                            Value = height
                        }
                    }
                }
            }
        };
        await animation.RunAsync(view);

    }

    public static async Task WidthTo(this Control view, double width, uint duration = 250, Easing easing = null)
    {
        var animation = new Animation()
        {
            Duration = TimeSpan.FromMilliseconds(duration),
            Easing = easing ?? new CubicEaseInOut(),
            IterationCount = new IterationCount(1),
            FillMode = FillMode.Forward,
            PlaybackDirection = PlaybackDirection.Normal,
            Children =
            {
                new KeyFrame()
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter()
                        {
                            Property = Control.WidthProperty,
                            Value = width
                        }
                    }
                }
            }
        };
        await animation.RunAsync(view);
    }

    public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
    {
        return collection is null || !collection.Any();
    }

    public static Task<bool> ContainsKey(this IBlobCache This, string key)
    {
        var tcs = new TaskCompletionSource<bool>();
        This.Get(key).Subscribe(
             x => tcs.SetResult(true),
             ex => tcs.SetResult(false));

        return tcs.Task;
    }
}