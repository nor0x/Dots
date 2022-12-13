using System;
using System.Collections.Generic;
using System.Diagnostics;
using CliWrap;
using CliWrap.Buffered;
using Dots.Helpers;
using Dots.Models;

namespace Dots.Services;

public class DotnetService
{
    public async Task<List<SDK>> CheckInstalledSdks()
    {

        List<SDK> result = new();
        // Calling `ExecuteBufferedAsync()` instead of `ExecuteAsync()`
        // implicitly configures pipes that write to in-memory buffers.
        var cmdresult = await Cli.Wrap("dotnet")
            .WithArguments("--list-sdks")
            .ExecuteBufferedAsync();

        if(!string.IsNullOrEmpty(cmdresult.StandardOutput))
        {
            if(cmdresult.StandardOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries) is string[] sdks)
            {
                foreach(var s in sdks)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        var lineSplit = s.Split("[", StringSplitOptions.RemoveEmptyEntries);
                        var versionString = lineSplit[0].Trim();
                        var path = lineSplit[1].TrimEnd(']');
                        Version version = new Version();
                        string appendix = string.Empty;
                        if (versionString.Contains("-"))
                        {
                            var parts = versionString.Split("-", StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2)
                            {
                                version = new Version(parts[0]);
                                appendix = "-" + parts[1];
                            }
                        }
                        else
                        {
                            version = new Version(versionString);
                        }
                        result.Add(new SDK
                        {
                            Version = version,
                            Appendix = appendix,
                            Path = path,
                            Color = Color.FromRgba(ColorHelper.GenerateHexColor(version + appendix))
                        });
                    }
                }
            }
        }
        return result.OrderByDescending(x => x.Version).ToList();
    }
}