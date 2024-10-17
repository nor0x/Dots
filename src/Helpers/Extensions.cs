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


    //credits https://gist.github.com/dalexsoto/9fd3c5bdbe9f61a717d47c5843384d11
    public static async Task DownloadDataAsync(this HttpClient client, string requestUrl, Stream destination, IProgress<(float progress, string task)>? progress = null, CancellationToken cancellationToken = default(CancellationToken))
    {
        using (var response = await client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead))
        {
            var contentLength = response.Content.Headers.ContentLength;
            using (var download = await response.Content.ReadAsStreamAsync())
            {
                // no progress... no contentLength... very sad
                if (progress is null || !contentLength.HasValue)
                {
                    await download.CopyToAsync(destination);
                    return;
                }
                // Such progress and contentLength much reporting Wow!
                var progressWrapper = new Progress<(float progress, string task)>(p => progress.Report((GetProgress(p.progress, contentLength.Value), "Downloading")));
				await download.CopyToAsync(destination, 81920, progressWrapper, cancellationToken);
            }
        }

        float GetProgress(float totalBytes, float currentBytes) => (totalBytes / currentBytes);
    }

    static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<(float progress, string task)>? progress = null, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new InvalidOperationException($"'{nameof(source)}' is not readable.");
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new InvalidOperationException($"'{nameof(destination)}' is not writable.");

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report((totalBytesRead, "Downloading"));
		}
    }

	public static async Task WriteAllBytesAsync(this byte[] bytes, string path, IProgress<(float progress, string task)> progress, CancellationToken cancellationToken)
	{
		const int bufferSize = 81920;
		var totalBytes = bytes.Length;
		var bytesWritten = 0;

		using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
		{
			int index = 0;

			while (index < totalBytes)
			{
				cancellationToken.ThrowIfCancellationRequested();

				int bytesToWrite = Math.Min(bufferSize, totalBytes - index);

				await stream.WriteAsync(bytes, index, bytesToWrite, cancellationToken);

				index += bytesToWrite;
				bytesWritten += bytesToWrite;

				var p = bytesWritten / totalBytes;

				progress?.Report((p, "Writing to Disk"));
			}
		}
	}
}
