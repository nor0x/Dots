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
                        //5.0.408 [C:\Program Files\dotnet\sdk]
                        //6.0.403 [C:\Program Files\dotnet\sdk]
                        //7.0.100 - rc.1.22431.12[C:\Program Files\dotnet\sdk]
                        //7.0.100 [C:\Program Files\dotnet\sdk]
                        //parse and build SDKs
                        var sdk = new SDK();
                        var parts = s.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            if (Version.TryParse(parts[0], out var version))
                            {
                                sdk.Version = version;
                            }
                        }
                       


                        var version = s.Split(" [", StringSplitOptions.RemoveEmptyEntries)[0];
                        var path = s.Split(" [", StringSplitOptions.RemoveEmptyEntries)[1].Replace("]", "");
                        Debug.WriteLine(version);
                        Debug.WriteLine(version);
                        result.Add(new SDK() { Version = new(s.Split(" [")[0]), Path = s.Split(" [")[1] });
                    }
                }
            }
        }
        return result;
    }
}