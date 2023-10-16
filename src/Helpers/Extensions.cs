using Akavache;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using HyperText.Avalonia.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var tmp)) return false;
        return tmp.Scheme == Uri.UriSchemeHttp || tmp.Scheme == Uri.UriSchemeHttps;
    }

    public static void OpenUrl(this string url)
    {
        if (!IsValidUrl(url)) throw new InvalidUrlException("invalid url: " + url);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var proc = new Process { StartInfo = { UseShellExecute = true, FileName = url } };
            proc.Start();

            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("x-www-browser", url);
            return;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) throw new InvalidUrlException("invalid url: " + url);
        Process.Start("open", url);
        return;
    }

    public static void OpenFilePath(this string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var proc = new Process { StartInfo = { UseShellExecute = true, FileName = $"explorer", Arguments = path } };
            proc.Start();

            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", path);
            return;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) throw new InvalidUrlException("invalid path: " + path);
        Process.Start("open", path);
        return;
    }
}