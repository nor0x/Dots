﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Akavache;
using CliWrap;
using CliWrap.Buffered;
using Dots.Data;
using Dots.Helpers;
using Dots.Models;
#if MACOS
using Security;
#endif

namespace Dots.Services;

public class DotnetService
{
    List<Sdk> _sdks;
    List<InstalledSdk> _installedSdks = new();
    ReleaseIndex[] _releaseIndex;
    Dictionary<string, Release[]> _releases = new();

    public DotnetService()
    {
    }

    public async Task<List<Sdk>> GetSdks(bool force = false)
    {
        var result = new List<Sdk>();
        var index = await GetReleaseIndex(force);
        var releaseInfos = new List<Release>();
        await GetInstalledSdks(force);
        foreach (var item in index)
        {
            var infos = await GetReleaseInfos(item.ChannelVersion, force);
            releaseInfos.AddRange(infos);
        }
        foreach (var release in releaseInfos)
        {
            var sdk = new Sdk()
            {
                Data = release,
                ColorHex = ColorHelper.GenerateHexColor(release.Sdk.Version.First().ToString()),
                Path = _installedSdks.FirstOrDefault(x => x.Version == release.Sdk.Version)?.Path ?? string.Empty,
                VersionDisplay = release.Sdk.Version,
            };
            result.Add(sdk);

            if (release.Sdks is not null)
            {
                foreach (var subSdk in release.Sdks)
                {
                    var sub = new Sdk()
                    {
                        Data = release,
                        ColorHex = ColorHelper.GenerateHexColor(release.Sdk.Version.First().ToString()),
                        Path = _installedSdks.FirstOrDefault(x => x.Version == subSdk.Version)?.Path ?? string.Empty,
                        VersionDisplay = subSdk.Version,
                    };

                    if (result.FirstOrDefault(s => s.VersionDisplay == subSdk.VersionDisplay) is null)
                    {
                        result.Add(sub);
                    }

                }
            }
        }

        foreach(var installed in _installedSdks)
        {
            if(result.FirstOrDefault(x => x.VersionDisplay == installed.Version) is null)
            {
                result.Add(
                    new Sdk() 
                    {
                        Data = null,
                        VersionDisplay = installed.Version, 
                        Path = installed.Path ,
                        ColorHex = ColorHelper.GenerateHexColor(installed.Version.First().ToString()),
                    }
                );
            }
        }

        return result.OrderByDescending(x => x.VersionDisplay).ToList();
    }

    async Task<ReleaseIndex[]> GetReleaseIndex(bool force = false)
    {
        if (!force && _releaseIndex is not null)
        {
            return _releaseIndex;
        }
        if (_releaseIndex is null && await BlobCache.UserAccount.ContainsKey(Constants.ReleaseIndexKey) && !force)
        {
            var json = await File.ReadAllTextAsync(Constants.ReleaseIndexPath);
            var deserialized = JsonSerializer.Deserialize<ReleaseIndexInfo>(json, ReleaseSerializerOptions.Options);
            _releaseIndex = deserialized.ReleasesIndex;
            return _releaseIndex;
        }

        using var client = new HttpClient();
        var response = await client.GetStringAsync(Constants.ReleaseIndexUrl);
        var releaseIndex = JsonSerializer.Deserialize<ReleaseIndexInfo>(response, ReleaseSerializerOptions.Options);
        _releaseIndex = releaseIndex.ReleasesIndex;
        if (!Directory.Exists(Constants.AppDataPath))
        {
            Directory.CreateDirectory(Constants.AppDataPath);
        }


        await File.WriteAllTextAsync(Constants.ReleaseIndexPath, response);
        BlobCache.UserAccount.InsertObject(Constants.ReleaseIndexKey, Constants.ReleaseIndexPath);
        return _releaseIndex;
    }

    async Task<Release[]> GetReleaseInfos(string channel, bool force = false)
    {
        if (!force && _releases is not null && _releases.ContainsKey(channel))
        {
            return _releases[channel];
        }
        if (await BlobCache.UserAccount.ContainsKey(Constants.ReleaseBaseKey + channel) && !force)
        {
            var cachedFile = Path.Combine(Constants.AppDataPath, $"release-{channel}.json");
            var json = await File.ReadAllTextAsync(cachedFile);
            var deserialized = JsonSerializer.Deserialize<ReleaseInfo>(json, ReleaseSerializerOptions.Options);

            _releases.Add(channel, deserialized.Releases);
            return _releases[channel];
        }

        using var client = new HttpClient();
        var url = Constants.ReleaseInfoUrl + channel + Constants.ReleaseInfoUrlEnd;
        var response = await client.GetStringAsync(url);
        var releases = JsonSerializer.Deserialize<ReleaseInfo>(response, ReleaseSerializerOptions.Options);
        var path = Path.Combine(Constants.AppDataPath, $"release-{channel}.json");
        await File.WriteAllTextAsync(path, response);
        await BlobCache.UserAccount.InsertObject(Constants.ReleaseBaseKey + channel, path);
        return releases.Releases;
    }


    async ValueTask<List<InstalledSdk>> GetInstalledSdks(bool force = false)
    {
        try
        {
            if (!_installedSdks.IsNullOrEmpty() && !force)
            {
                return _installedSdks;
            }
            if (await BlobCache.UserAccount.ContainsKey(Constants.InstalledSdksKey) && !force)
            {
                var sdks = await BlobCache.UserAccount.GetObject<string>(Constants.InstalledSdksKey);
                _installedSdks = JsonSerializer.Deserialize<List<InstalledSdk>>(sdks);
                return _installedSdks;
            }

            List<InstalledSdk> result = new();
            var cmdresult = await Cli.Wrap(Constants.DotnetCommand)
                .WithArguments(Constants.ListSdksCommand)
                .ExecuteBufferedAsync(Encoding.UTF8);

            if (!string.IsNullOrEmpty(cmdresult.StandardOutput))
            {
                if (cmdresult.StandardOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries) is string[] sdks)
                {
                    foreach (var s in sdks)
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
            await BlobCache.UserAccount.InsertObject(Constants.InstalledSdksKey, JsonSerializer.Serialize(result));
            return _installedSdks;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            //Analytics.TrackEvent("GetInstalledSdks", new Dictionary<string, string>() { { "Error", ex.Message } });
            return null;
        }

    }

    public async ValueTask<string> Download(Sdk sdk, bool toDesktop = false)
    {
        try
        {
            Rid rid = GetRid();
            var extension = GetExtension();
            if (sdk.Data.Sdk.Files.Where(f => f.Rid == rid).FirstOrDefault(r => r.Name.Contains(extension)) is Data.FileInfo info)
            {
                if (!Directory.Exists(Constants.AppDataPath))
                {
                    Directory.CreateDirectory(Constants.AppDataPath);
                }
                var sdkFile = info.Url.ToString().Split("/").LastOrDefault();
                var path = Path.Combine(Constants.AppDataPath, sdkFile);
                if (File.Exists(path))
                {
                    if (toDesktop)
                    {
                        //if file exists on desktop, return desktop path otherwise copy and return desktop path
                        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        var filename = Path.Combine(desktop, sdkFile);
                        if (File.Exists(Path.Combine(desktop, filename)))
                        {
                            return desktop;
                        }
                        else
                        {
                            await File.WriteAllBytesAsync(Path.Combine(desktop, sdkFile), await File.ReadAllBytesAsync(path));
                            return desktop;
                        }
                    }
                    return path;
                }

                var progress = new ProgressTask();
                progress.Title = $"Downloading {sdk.Data.Sdk.Version}";
                progress.Url = info.Url.ToString();
                progress.CancellationTokenSource = new CancellationTokenSource();

                var p = new Progress<float>();
                p.ProgressChanged += (s, e) =>
                {
                    progress.Value = e;
                };
                progress.Progress = p;


                sdk.ProgressTask = progress;

                // Use the provided extension method
                using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                using var client = new HttpClient();
                await client.DownloadDataAsync(info.Url.ToString(), file, p);

                if (toDesktop)
                {
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var filename = Path.Combine(desktop, sdkFile);
                    await File.WriteAllBytesAsync(Path.Combine(desktop, sdkFile), await File.ReadAllBytesAsync(path));
                    path = desktop;
                }

                return path;
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            //Analytics.TrackEvent("Download SDK", new Dictionary<string, string>() { { "Error", ex.Message }, { "SDK Version", sdk.Data.Sdk.Version } });
            return null;
        }
    }

    public async ValueTask<bool> Install(string exe)
    {
        try
        {
#if WINDOWS
            var result = await Cli.Wrap(exe).WithArguments(" /install /quiet /qn /norestart").WithValidation(CommandResultValidation.None).ExecuteAsync();
            return result.ExitCode == 0;
#endif
#if MACOS

            return RunAsRoot("/usr/sbin/installer", new[] { "-pkg", exe, "-target", "/", null });
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            //Analytics.TrackEvent("Install SDK", new Dictionary<string, string>() { { "Error", ex.Message }, { "Exe", exe } });
            return false;
        }
        return false;
    }

    public async Task<string> GetInstallationPath(Sdk sdk)
    {
        var installed = await GetInstalledSdks(true);
        return installed.FirstOrDefault(x => x.Version == sdk.Data.Sdk.Version)?.Path ?? string.Empty;
    }

    public async Task OpenFolder(Sdk sdk)
    {
        try
        {
            string path = Path.Combine(sdk.Path, sdk.Data.Sdk.Version);
            path.OpenFilePath();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            //Analytics.TrackEvent("OpenFolder", new Dictionary<string, string>() { { "Error", ex.Message }, { "Path", sdk.Path } });
        }
    }

    public async Task OpenFolder(string path)
    {
        try
        {
            await Cli.Wrap(Constants.ExplorerCommand).WithArguments(path).WithValidation(CommandResultValidation.None).ExecuteAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            //Analytics.TrackEvent("OpenFolder", new Dictionary<string, string>() { { "Error", ex.Message }, { "Path", sdk.Path } });
        }
    }


    public async Task<bool> Uninstall(Sdk sdk, string setupPath = "")
    {
        try
        {
#if WINDOWS
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Constants.UninstallerPath);
            var filename = GetSetupName(sdk);

            string[] files = Directory.GetFiles(path, filename, SearchOption.AllDirectories);

            if (!files.IsNullOrEmpty())
            {
                var result = await Cli.Wrap(files.First()).WithArguments(" /uninstall /quiet /qn /norestart").WithValidation(CommandResultValidation.None).ExecuteAsync();
                return result.ExitCode == 0;
            }
            else
            {

                var setupInLocalDirectory = Path.Combine(Constants.AppDataPath, filename);
                if (!string.IsNullOrEmpty(setupPath))
                {
                    var result = await Cli.Wrap(setupPath).WithArguments(" /uninstall /quiet /qn /norestart").WithValidation(CommandResultValidation.None).ExecuteAsync();
                    return result.ExitCode == 0;
                }
                else if (File.Exists(setupInLocalDirectory))
                {
                    var result = await Cli.Wrap(setupInLocalDirectory).WithArguments(" /uninstall /quiet /qn /norestart").WithValidation(CommandResultValidation.None).ExecuteAsync();
                    return result.ExitCode == 0;
                }
                else
                {
                    var exe = await Download(sdk);
                    if (!string.IsNullOrEmpty(exe))
                    {
                        return await Uninstall(sdk, exe);
                    }
                }
            }
            return false;
#endif
#if MACOS

            if (!Directory.Exists(Constants.AppDataPath))
            {
                Directory.CreateDirectory(Constants.AppDataPath);
            }
            //write Constants.UninstallScriptFile to file
            var script = Constants.UninstallScriptFile.Replace("XXXXX", sdk.Data.Sdk.Version);
            script = script.Replace("XXXXX", sdk.Data.Sdk.Version);
            var filename = "uninstall-" + sdk.Data.Sdk.Version.Replace(".", "-") + ".sh";
            var path = Path.Combine(Constants.AppDataPath, filename);
            await File.WriteAllTextAsync(path, script);
            return RunAsRoot("/bin/sh", new[] { path, null });
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            //Analytics.TrackEvent("Uninstall SDK", new Dictionary<string, string>() { { "Error", ex.Message }, { "SDK Version", sdk.Data.Sdk.Version } });
        }
        return false;
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                os = "macos";
            }

            return $"dotnet-sdk-{sdk.Data.Sdk.Version}-{os}-{arch}{env}.exe";

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            //Analytics.TrackEvent("GetSetupName", new Dictionary<string, string>() { { "Error", ex.Message }, { "SDK Version", sdk.Data.Sdk.Version } });
            return null;
        }
    }

#if MACOS
    bool RunAsRoot(string exe, string[] args)
    {
        try
        {
            var parameters = new AuthorizationParameters
            {
                Prompt = "",
                PathToSystemPrivilegeTool = ""
            };

            var flags = AuthorizationFlags.ExtendRights |
                AuthorizationFlags.InteractionAllowed |
                AuthorizationFlags.PreAuthorize;

            using var auth = Authorization.Create(parameters, null, flags);
            int result = auth.ExecuteWithPrivileges(
                exe,
                AuthorizationFlags.Defaults,
                args);
            if (result == 0) return true;
            if (Enum.TryParse(result.ToString(), out AuthorizationStatus authStatus))
            {
                if (authStatus == AuthorizationStatus.Canceled)
                {
                    return false;
                }
                else if (authStatus == AuthorizationStatus.ToolExecuteFailure)
                {
                    // Reaches here. -60031
                    // https://developer.apple.com/documentation/security/1540004-authorization_services_result_co/errauthorizationtoolexecutefailure
                    throw new InvalidOperationException($"Could not get authorization. {authStatus}");
                }
                else
                {
                    throw new InvalidOperationException($"Could not get authorization. {authStatus}");
                }
            }
            return false;

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            //Analytics.TrackEvent("RunAsRoot", new Dictionary<string, string>() { { "Error", ex.Message }, { "Executable", exe }, { "Args", string.Join("", args) } });
            return false;
        }
    }

#endif

    string GetExtension()
    {
        var ext = ".tar.gz";
        //if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) doesn't work on mac-catalyst
        if (RuntimeInformation.RuntimeIdentifier.Contains("mac"))
        {
            ext = ".pkg";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ext = ".exe";
        }
        return ext;
    }

    Rid GetRid()
    {
        try
        {
            //if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) doesn't work on mac-catalyst
            if (RuntimeInformation.RuntimeIdentifier.Contains("mac"))
            {
                return
                    (RuntimeInformation.OSArchitecture == Architecture.Arm ||
                    RuntimeInformation.OSArchitecture == Architecture.Arm64 ||
                    RuntimeInformation.OSArchitecture == Architecture.Armv6) ? Rid.OsxArm64 : Rid.OsxX64;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    return
                        (RuntimeInformation.OSArchitecture == Architecture.Arm ||
                        RuntimeInformation.OSArchitecture == Architecture.Arm64 ||
                        RuntimeInformation.OSArchitecture == Architecture.Armv6) ? Rid.WinArm64 : Rid.WinX64;
                }
                else
                {
                    return
                         (RuntimeInformation.OSArchitecture == Architecture.Arm ||
                         RuntimeInformation.OSArchitecture == Architecture.Arm64 ||
                         RuntimeInformation.OSArchitecture == Architecture.Armv6) ? Rid.WinArm : Rid.WinX86;
                }

            }
            return Rid.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            //Analytics.TrackEvent("GetRid", new Dictionary<string, string>() { { "Error", ex.Message } });
            return Rid.Empty;
        }
    }
}