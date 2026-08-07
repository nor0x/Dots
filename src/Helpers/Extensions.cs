using Akavache;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using HyperText.Avalonia.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

    public static void OpenUrl(this Uri url)
    {
        OpenUrl(url.ToString());
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


    /// <summary>
    /// How long the transfer may go without receiving a single byte before it is treated as dead.
    /// This replaces HttpClient.Timeout, which is a deadline on the whole download and so punished
    /// slow connections rather than broken ones.
    /// </summary>
    public static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(60);

    const int DownloadBufferSize = 1 << 20;

    /// <summary>
    /// Streams <paramref name="url"/> into <paramref name="destinationPath"/>, resuming from whatever
    /// is already there. The destination stream is owned here, so cancellation just unwinds - the
    /// partial file is deliberately left on disk for the next attempt to continue from.
    /// </summary>
    /// <param name="progress">
    /// Reports a 0..1 fraction, or a negative value when the server sends no Content-Length, meaning
    /// "length unknown - show an indeterminate bar".
    /// </param>
    public static async Task DownloadToFileAsync(this HttpClient client, Uri url, string destinationPath,
        IProgress<(float progress, string task)>? progress = null, CancellationToken cancellationToken = default)
    {
        var resumeFrom = File.Exists(destinationPath) ? new System.IO.FileInfo(destinationPath).Length : 0;

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (resumeFrom > 0)
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(resumeFrom, null);
        }

        // The stall watchdog re-arms on every chunk, so a slow-but-alive transfer runs as long as it
        // needs while a dead socket still fails in StallTimeout.
        using var stallSource = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stallSource.Token);
        stallSource.CancelAfter(StallTimeout);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token);

        // The transfer finished but the process died before the file was verified and renamed:
        // resuming from EOF asks for a range past the end. Nothing left to fetch - let the caller's
        // checksum decide whether what's on disk is good.
        if (response.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable && resumeFrom > 0)
        {
            return;
        }

        response.EnsureSuccessStatusCode();

        // A server that ignores the Range header replies 200 with the whole file - start over.
        var appending = response.StatusCode == System.Net.HttpStatusCode.PartialContent && resumeFrom > 0;
        if (!appending)
        {
            resumeFrom = 0;
        }

        var contentLength = response.Content.Headers.ContentLength;
        var totalLength = contentLength.HasValue ? contentLength.Value + resumeFrom : (long?)null;

        await using var destination = new FileStream(destinationPath,
            appending ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None,
            DownloadBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var download = await response.Content.ReadAsStreamAsync(linked.Token);

        var buffer = new byte[DownloadBufferSize];
        var totalRead = resumeFrom;
        var lastReportedPercent = -1;
        var lastReport = Stopwatch.StartNew();

        int read;
        while ((read = await download.ReadAsync(buffer, linked.Token).ConfigureAwait(false)) != 0)
        {
            stallSource.CancelAfter(StallTimeout);
            await destination.WriteAsync(buffer.AsMemory(0, read), linked.Token).ConfigureAwait(false);
            totalRead += read;

            if (progress is null)
            {
                continue;
            }

            // ~350 reports instead of ~4400: each one re-formats the status bar on the UI thread
            if (totalLength is > 0)
            {
                var percent = (int)(totalRead * 100 / totalLength.Value);
                if (percent != lastReportedPercent)
                {
                    lastReportedPercent = percent;
                    progress.Report(((float)totalRead / totalLength.Value, "Downloading"));
                }
            }
            else if (lastReport.ElapsedMilliseconds >= 250)
            {
                lastReport.Restart();
                progress.Report((-1f, $"Downloading {totalRead / 1_048_576:N0} MB"));
            }
        }

        // surface a caller-requested cancel as cancellation, not as a stall
        cancellationToken.ThrowIfCancellationRequested();
    }
}
