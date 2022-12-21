using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Dots.Data;
using Dots.Helpers;
using Dots.Models;

namespace Dots.Services;

public class DotnetService
{   
    List<Sdk> _sdks;
    List<InstalledSdk> _installedSdks = new();

    public async Task<List<Sdk>> GetSdks()
    {
        var result = new List<Sdk>();
        var index = await GetReleaseIndex();
        var releaseInfos = new List<Release>();
        var installed = GetInstalledSdks();
        foreach(var item in index)
        {
            var infos = await GetReleaseInfos(item.ChannelVersion);
            releaseInfos.AddRange(infos);
        }
        foreach(var release in releaseInfos)
        {
            var sdk = new Sdk()
            {
                Data = release.Sdk,
                ColorHex = ColorHelper.GenerateHexColor(release.Sdk.VersionDisplay ?? release.Sdk.Version),
                Path = _installedSdks.FirstOrDefault(x => x.Version == release?.Sdk?.VersionDisplay)?.Path ?? string.Empty
            };
            result.Add(sdk);
        }
        return result;
    }

    async Task<List<ReleaseIndex>> GetReleaseIndex()
    {
        using var client = new HttpClient();
        var response = await client.GetStringAsync(Constants.ReleaseIndexUrl);
        var releaseIndex = JsonSerializer.Deserialize<ReleaseIndexInfo>(response, ReleaseSerializerOptions.Options);
        return releaseIndex.ReleasesIndex.ToList();
    }

    async Task<List<Release>> GetReleaseInfos(string channel)
    {
        using var client = new HttpClient();
        var url = Constants.ReleaseInfoUrl + channel + Constants.ReleaseInfoUrlEnd;
        var response = await client.GetStringAsync(url);
        var releases = JsonSerializer.Deserialize<ReleaseInfo>(response, ReleaseSerializerOptions.Options);
        return releases.Releases.ToList();
    }


    public async ValueTask<List<InstalledSdk>> GetInstalledSdks(bool force = false)
    {
        if (!_installedSdks.IsNullOrEmpty() && !force)
        {
            return _installedSdks;
        }
        if (Preferences.ContainsKey(Constants.InstalledSdkSKey) && !force)
        {
            var sdks = Preferences.Get(Constants.InstalledSdkSKey, "");
            _installedSdks = JsonSerializer.Deserialize<List<InstalledSdk>>(sdks);
            return _installedSdks;
        }

        List<InstalledSdk> result = new();
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
                        result.Add(new InstalledSdk() { Version = versionString, Path = path });
                    }
                }
            }
        }
        _installedSdks = result;
        Preferences.Set(Constants.InstalledSdkSKey, JsonSerializer.Serialize(_installedSdks));
        return _installedSdks;
    }

    public async Task OpenFolder(Sdk sdk)
    {
        try
        {
            string path = Path.Combine(sdk.Path, sdk.Data.VersionDisplay);
            await Cli.Wrap("explorer").WithArguments(path).WithValidation(CommandResultValidation.None).ExecuteAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    
    public async Task<bool> Uninstall(Sdk sdk)
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Constants.UninstallerPath);
            var filename = GetSetupName(sdk);

            Debug.WriteLine(path);

            string[] files = Directory.GetFiles(path, filename, SearchOption.AllDirectories);

            if(!files.IsNullOrEmpty())
            {
                var result = await Cli.Wrap(files.First()).WithArguments(" /uninstall /quiet").WithValidation(CommandResultValidation.None).ExecuteAsync();
                return result.ExitCode == 0;
            }
            return false;
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    string GetSetupName(Sdk sdk)
    {
        try
        {
            var env = Environment.Is64BitOperatingSystem ? "64" : "32";
            var arch = "x";
            if (RuntimeInformation.OSArchitecture == Architecture.Arm ||
               RuntimeInformation.OSArchitecture == Architecture.Arm64 ||
               RuntimeInformation.OSArchitecture == Architecture.Armv6)
            {
                arch = "arm";
            }
            var os = "win";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                os = "macos";
            }

            return $"dotnet-sdk-{sdk.Data.VersionDisplay}-{os}-{arch}{env}.exe";

        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }
}