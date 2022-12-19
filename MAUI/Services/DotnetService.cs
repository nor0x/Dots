using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Dots.Helpers;
using Dots.Models;

namespace Dots.Services;

public class DotnetService
{   
    List<SDK> _sdks;

    public async ValueTask<List<SDK>> CheckInstalledSdks(bool force = false)
    {
        if (!_sdks.IsNullOrEmpty() && !force)
        {
            return _sdks;
        }
        if (Preferences.ContainsKey(Constants.InstalledSDKSKey) && !force)
        {
            var sdks = Preferences.Get(Constants.InstalledSDKSKey, "");
            _sdks = JsonSerializer.Deserialize<List<SDK>>(sdks);
            return _sdks;
        }

        List<SDK> result = new();
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
                            ColorHex = ColorHelper.GenerateHexColor(version + appendix),
                        });
                    }
                }
            }
        }
        _sdks = result.OrderByDescending(x => x.Version).ToList();
        Preferences.Set(Constants.InstalledSDKSKey, JsonSerializer.Serialize(_sdks));
        
        return _sdks;
    }

    public async Task OpenFolder(SDK sdk)
    {
        try
        {
            string path = Path.Combine(sdk.Path, sdk.VersionText);
            await Cli.Wrap("explorer").WithArguments(path).WithValidation(CommandResultValidation.None).ExecuteAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public async Task Uninstall(SDK sdk)
    {

    }
}