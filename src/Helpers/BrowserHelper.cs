using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dots.Helpers;

public static class BrowserHelper
{
    private static void ShellExec(string cmd, bool waitForExit = true)
    {
        var escapedArgs = Regex.Replace(cmd, "(?=[`~!#&*()|;'<>])", "\\")
            .Replace("\"", "\\\\\\\"");

        using (var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        ))
        {
            if (waitForExit)
            {
                process.WaitForExit();
            }
        }
    }

    public static void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // If no associated application/json MimeType is found xdg-open opens retrun error
            // but it tries to open it anyway using the console editor (nano, vim, other..)
            ShellExec($"xdg-open {url}", waitForExit: false);
        }
        else
        {
            using Process process = Process.Start(new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? url : "open",
                Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"{url}" : "",
                CreateNoWindow = true,
                UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            });
        }
    }

    public static void OpenBrowser(Uri uri)
    {
        OpenBrowser(uri.ToString());
    }
}
