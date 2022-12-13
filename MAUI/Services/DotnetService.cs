using System;
using System.Collections.Generic;
using System.Diagnostics;
using CliWrap;
using CliWrap.Buffered;
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
                        result.Add(new SDK() { Version = new(s.Split(" [")[0]), Path = s.Split(" [")[1] });
                    }
                }
            }
        }
        return result;
    }
}