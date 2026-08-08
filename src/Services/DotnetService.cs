using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using Akavache;
using AsyncAwaitBestPractices;
using CliWrap;
using CliWrap.Buffered;
using Dots.Data;
using Dots.Helpers;
using Dots.Models;
#if MACOS
using Security;
#endif
#if LINUX
using System.Formats.Tar;
using System.IO.Compression;
#endif

namespace Dots.Services;

public class DotnetService
{
	List<InstalledSdk> _installedSdks = new();
	ReleaseIndex[] _releaseIndex;
	Dictionary<string, Release[]> _releases = new();

	/// <summary>
	/// One client for the whole app. The infinite timeout is the important part: HttpClient.Timeout
	/// covers reading the response body too, even with ResponseHeadersRead, so the 100s default
	/// aborted every SDK download slower than ~28 Mbit/s. <see cref="Extensions.StallTimeout"/>
	/// replaces it with a stall watchdog, which is the semantics we actually wanted.
	/// </summary>
	static readonly HttpClient Http = new(new SocketsHttpHandler
	{
		PooledConnectionLifetime = TimeSpan.FromMinutes(5),
		AutomaticDecompression = DecompressionMethods.All,
	})
	{
		Timeout = Timeout.InfiniteTimeSpan,
	};

	public DotnetService()
	{
	}

	static string GetMajorVersion(string version) =>
		version?.Split('.').FirstOrDefault() ?? string.Empty;

	public async Task<List<Sdk>> GetSdks(bool force = false)
	{
		var result = new List<Sdk>();
		var index = await GetReleaseIndex(force);
		var releaseInfos = new List<Release>();
		await GetInstalledSdks(force);

		var parallelOptions = new ParallelOptions();
		parallelOptions.MaxDegreeOfParallelism = 10;

		await Parallel.ForEachAsync(index, parallelOptions, async (item, token) =>
		{
			var infos = await GetReleaseInfos(item.ChannelVersion, force);
			infos.ToList().ForEach(r => r.SupportPhase = item.SupportPhase);
			infos.ToList().ForEach(r => r.ReleaseType = item.ReleaseType);
			releaseInfos.AddRange(infos);
		});

		foreach (var release in releaseInfos)
		{
			if (release?.Sdk is null)
			{
				continue;
			}
			var sdk = new Sdk()
			{
				Data = release,
				ColorHex = ColorHelper.GenerateHexColor(GetMajorVersion(release.Sdk.Version)),
				Path = _installedSdks.FirstOrDefault(x => x.Version == release.Sdk.Version)?.Path ?? string.Empty,
				VersionDisplay = release.Sdk.Version,
				SdkData = release.Sdk,
			};

			sdk.Data.ReleaseType = release.ReleaseType;
			sdk.Data.SupportPhase = release.SupportPhase;

			result.Add(sdk);

			if (release.Sdks is not null)
			{
				foreach (var subSdk in release.Sdks)
				{
					var sub = new Sdk()
					{
						Data = release,
						ColorHex = ColorHelper.GenerateHexColor(GetMajorVersion(subSdk.Version)),
						Path = _installedSdks.FirstOrDefault(x => x.Version == subSdk.Version)?.Path ?? string.Empty,
						VersionDisplay = subSdk.Version,
						SdkData = subSdk,
					};

					if (result.FirstOrDefault(s => s.VersionDisplay == subSdk.VersionDisplay) is null)
					{
						result.Add(sub);
					}
				}
			}
		}

		foreach (var installed in _installedSdks)
		{
			if (result.FirstOrDefault(x => x.VersionDisplay == installed.Version) is null)
			{
				result.Add(
					new Sdk()
					{
						Data = null,
						VersionDisplay = installed.Version,
						Path = installed.Path,
						ColorHex = ColorHelper.GenerateHexColor(GetMajorVersion(installed.Version)),
					}
				);
			}
		}

		result = result.GroupBy(x => x.Group)
			.SelectMany(x => x.OrderByDescending(y => y.Data?.ReleaseDate))
			.OrderByDescending(x => x.Group)
			.ToList();

		AnnotateOwnership(result);

		return result;
	}

	/// <summary>
	/// Marks installed SDKs that Dots cannot remove, so the UI can say so up front instead of the
	/// user finding out only after clicking Uninstall.
	/// </summary>
	public void AnnotateOwnership(IEnumerable<Sdk> sdks)
	{
		foreach (var sdk in sdks.Where(s => s is not null && s.Installed))
		{
			try
			{
				AnnotateOwnership(sdk);
			}
			catch (Exception ex)
			{
				// a version we can't classify just stays unmarked - the uninstall path reports it
				Debug.WriteLine(ex);
			}
		}
	}

	public void AnnotateOwnership(Sdk sdk)
	{
		sdk.ExternalOwner = null;
		sdk.UninstallBlockedReason = null;

		if (!sdk.Installed)
		{
			return;
		}

		var version = sdk.SdkData?.Version ?? sdk.VersionDisplay;
		if (string.IsNullOrEmpty(version))
		{
			return;
		}

#if WINDOWS
		var plan = WindowsSdkRegistry.Resolve(version);
		if (plan.Ownership == SdkOwnership.StandaloneBundle)
		{
			return;
		}

		sdk.ExternalOwner = plan.Ownership switch
		{
			SdkOwnership.VisualStudio => "Visual Studio",
			SdkOwnership.UnmanagedMsi => "Windows Installer",
			SdkOwnership.BundleCacheMissing => "Installer missing",
			_ => null,
		};
		sdk.UninstallBlockedReason = plan.Message;
#endif
#if LINUX
		// an SDK outside $HOME belongs to the distro's package manager
		if (GetWritableDotnetRoot(sdk) is null)
		{
			sdk.ExternalOwner = "System";
			sdk.UninstallBlockedReason = "Installed system-wide - remove it with your package manager";
		}
#endif
	}

	async Task<ReleaseIndex[]> GetReleaseIndex(bool force = false)
	{
		if (!force && _releaseIndex is not null)
		{
			return _releaseIndex;
		}
		if (_releaseIndex is null && await CacheDatabase.UserAccount.ContainsKey(Constants.ReleaseIndexKey) && !force
			&& File.Exists(Constants.ReleaseIndexPath))
		{
			var json = await File.ReadAllTextAsync(Constants.ReleaseIndexPath);
			var deserialized = JsonSerializer.Deserialize<ReleaseIndexInfo>(json, ReleaseSerializerOptions.Options);
			_releaseIndex = deserialized.ReleasesIndex;
			return _releaseIndex;
		}

		var response = await Http.GetStringAsync(Constants.ReleaseIndexUrl);
		var releaseIndex = JsonSerializer.Deserialize<ReleaseIndexInfo>(response, ReleaseSerializerOptions.Options);
		_releaseIndex = releaseIndex.ReleasesIndex;
		if (!Directory.Exists(Constants.AppDataPath))
		{
			Directory.CreateDirectory(Constants.AppDataPath);
		}


		await File.WriteAllTextAsync(Constants.ReleaseIndexPath, response);
		CacheDatabase.UserAccount.InsertObject(Constants.ReleaseIndexKey, Constants.ReleaseIndexPath);
		return _releaseIndex;
	}

	async Task<Release[]> GetReleaseInfos(string channel, bool force = false)
	{
		if (!force && _releases is not null && _releases.ContainsKey(channel))
		{
			return _releases[channel];
		}
		var cachedFile = Path.Combine(Constants.AppDataPath, $"release-{channel}.json");
		// the Akavache key can outlive the file it points at - a manual cache purge, or a user
		// clearing LocalAppData - so the file has to be checked, not just the key
		if (!force && await CacheDatabase.UserAccount.ContainsKey(Constants.ReleaseBaseKey + channel) && File.Exists(cachedFile))
		{
			var json = await File.ReadAllTextAsync(cachedFile);
			var deserialized = JsonSerializer.Deserialize<ReleaseInfo>(json, ReleaseSerializerOptions.Options);

			_releases[channel] = deserialized.Releases;
			return _releases[channel];
		}

		var url = Constants.ReleaseInfoUrl + channel + Constants.ReleaseInfoUrlEnd;
		var response = await Http.GetStringAsync(url);
		var releases = JsonSerializer.Deserialize<ReleaseInfo>(response, ReleaseSerializerOptions.Options);
		if (!Directory.Exists(Constants.AppDataPath))
		{
			Directory.CreateDirectory(Constants.AppDataPath);
		}
		await File.WriteAllTextAsync(cachedFile, response);
		await CacheDatabase.UserAccount.InsertObject(Constants.ReleaseBaseKey + channel, cachedFile);
		// the old code returned without populating _releases, so every channel re-parsed on each call
		_releases[channel] = releases.Releases;
		return _releases[channel];
	}


	async ValueTask<List<InstalledSdk>> GetInstalledSdks(bool force = false)
	{
		try
		{
			if (!_installedSdks.IsNullOrEmpty() && !force)
			{
				return _installedSdks;
			}
			if (await CacheDatabase.UserAccount.ContainsKey(Constants.InstalledSdksKey) && !force)
			{
				var sdks = await CacheDatabase.UserAccount.GetObject<string>(Constants.InstalledSdksKey);
				_installedSdks = JsonSerializer.Deserialize<List<InstalledSdk>>(sdks);
				return _installedSdks;
			}

			List<InstalledSdk> result = new();
#if LINUX
			foreach (var host in GetDotnetHosts())
			{
				try
				{
					var hostResult = await Cli.Wrap(host)
						.WithArguments(Constants.ListSdksCommand)
						.ExecuteBufferedAsync(Encoding.UTF8);
					AddInstalledSdks(hostResult.StandardOutput, result);
				}
				catch (Exception ex)
				{
					// a host that isn't there yet (no ~/.dotnet) or no dotnet on PATH at all is expected
					Debug.WriteLine(ex);
				}
			}
#else
			var cmdresult = await Cli.Wrap(Constants.DotnetCommand)
				.WithArguments(Constants.ListSdksCommand)
				.ExecuteBufferedAsync(Encoding.UTF8);
			AddInstalledSdks(cmdresult.StandardOutput, result);
#endif
			_installedSdks = result;
			await CacheDatabase.UserAccount.InsertObject(Constants.InstalledSdksKey, JsonSerializer.Serialize(result));
			return _installedSdks;
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
			//Analytics.TrackEvent("GetInstalledSdks", new Dictionary<string, string>() { { "Error", ex.Message } });
			return null;
		}

	}

	/// <summary>
	/// Parses `dotnet --list-sdks` output ("9.0.100 [/path/to/sdk]" per line) into <paramref name="result"/>,
	/// skipping versions already collected from another host.
	/// </summary>
	static void AddInstalledSdks(string listSdksOutput, List<InstalledSdk> result)
	{
		if (string.IsNullOrEmpty(listSdksOutput))
		{
			return;
		}

		foreach (var line in listSdksOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
		{
			var lineSplit = line.Split("[", StringSplitOptions.RemoveEmptyEntries);
			if (lineSplit.Length < 2)
			{
				continue;
			}

			var version = lineSplit[0].Trim();
			if (result.Any(x => x.Version == version))
			{
				continue;
			}

			result.Add(new InstalledSdk() { Version = version, Path = lineSplit[1].TrimEnd(']') });
		}
	}

#if LINUX
	/// <summary>
	/// Every dotnet host worth asking for installed SDKs. .NET 7 dropped multi-level lookup, so a host
	/// only reports what lives beside it - a user with a distro-packaged dotnet would lose those SDKs
	/// from the list the moment Dots creates ~/.dotnet.
	/// </summary>
	static IEnumerable<string> GetDotnetHosts()
	{
		yield return Constants.DotnetCommand;
		if (Constants.DotnetCommand != "dotnet")
		{
			yield return "dotnet";
		}
	}

	/// <summary>
	/// The dotnet root that owns <paramref name="sdk"/> (the parent of the `sdk` directory reported by
	/// `--list-sdks`), or null when it lives outside the user's home and would need root to modify.
	/// </summary>
	static string? GetWritableDotnetRoot(Sdk sdk)
	{
		var root = string.IsNullOrEmpty(sdk.Path)
			? Constants.DotnetRoot
			: Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(sdk.Path));

		if (string.IsNullOrEmpty(root))
		{
			return null;
		}

		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var full = Path.GetFullPath(root);
		return full.StartsWith(Path.GetFullPath(home) + Path.DirectorySeparatorChar, StringComparison.Ordinal)
			? full
			: null;
	}
#endif

	/// <summary>
	/// Downloads the installer for <paramref name="sdk"/> and returns its full path, or null if the
	/// download failed. Always a file path - never a directory - so callers can hand it straight to
	/// <see cref="Install"/>.
	/// </summary>
	/// <param name="toDesktop">
	/// Write to the Desktop instead of the app's data folder. The file goes there directly; there is
	/// deliberately no second copy in LocalAppData.
	/// </param>
	public async ValueTask<string?> Download(Sdk sdk, bool toDesktop = false, IProgress<(float progress, string task)>? status = null)
	{
		var version = sdk.SdkData?.Version ?? sdk.VersionDisplay;
		try
		{
			Rid rid = GetRid();
			var extension = GetExtension();
			if (sdk.SdkData?.Files?.Where(f => f.Rid == rid).FirstOrDefault(r => r.Name.Contains(extension)) is not Data.FileInfo info)
			{
				status?.Report((1f, "No download is published for this platform"));
				return null;
			}

			var targetDirectory = toDesktop
				? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
				: Constants.AppDataPath;
			if (!Directory.Exists(targetDirectory))
			{
				Directory.CreateDirectory(targetDirectory);
			}

			var finalPath = Path.Combine(targetDirectory, info.FileName);
			var partialPath = finalPath + Constants.PartialSuffix;

			var progress = new ProgressTask
			{
				Title = $"Downloading {version}",
				Url = info.Url.ToString(),
				CancellationTokenSource = new CancellationTokenSource(),
				CanCancel = true,
			};

			var p = new Progress<(float progress, string task)>();
			p.ProgressChanged += (s, e) =>
			{
				// a negative fraction means "we don't know the total" - keep the bar animating
				progress.IsIndeterminate = e.progress < 0;
				progress.Value = e.progress < 0 ? null : e.progress * 100;
				progress.Task = e.task;
				status?.Report(e);
			};
			progress.Progress = p;
			sdk.ProgressTask = progress;

			// Progress<T>.Report is protected; the interface is what exposes it
			IProgress<(float progress, string task)> report = p;
			var token = progress.CancellationTokenSource.Token;

			// An already-present file is only usable if it hashes correctly. Existence alone was the
			// old check, which happily handed a truncated installer to the installer.
			if (File.Exists(finalPath))
			{
				report.Report((0.95f, "Verifying"));
				if (await VerifyHashAsync(finalPath, info.Hash, token))
				{
					report.Report((1f, "Already downloaded"));
					return finalPath;
				}

				Debug.WriteLine($"cached {finalPath} failed verification - re-downloading");
				File.Delete(finalPath);
			}

			// A copy kept from an earlier install is worth reusing rather than pulling a few hundred
			// megabytes down again just to put the same bytes on the Desktop.
			if (toDesktop)
			{
				var cached = Path.Combine(Constants.AppDataPath, info.FileName);
				if (File.Exists(cached))
				{
					report.Report((0.95f, "Verifying"));
					if (await VerifyHashAsync(cached, info.Hash, token))
					{
						report.Report((0.97f, "Copying to Desktop"));
						File.Copy(cached, finalPath, overwrite: true);
						report.Report((1f, "Copied to Desktop"));
						return finalPath;
					}
				}
			}

			// One retry: a resumed download that fails the hash has a bad prefix, so the second
			// attempt starts from scratch rather than resuming onto the same corruption.
			for (var attempt = 0; attempt < 2; attempt++)
			{
				await Http.DownloadToFileAsync(info.Url, partialPath, p, token);

				report.Report((0.99f, "Verifying"));
				if (await VerifyHashAsync(partialPath, info.Hash, token))
				{
					File.Move(partialPath, finalPath, overwrite: true);
					report.Report((1f, "Downloaded"));
					return finalPath;
				}

				Debug.WriteLine($"{partialPath} failed verification (attempt {attempt + 1})");
				SafeDelete(partialPath);
			}

			report.Report((1f, "Download failed - the file did not match its checksum"));
			return null;
		}
		catch (OperationCanceledException)
		{
			// the .partial is left in place on purpose - the next attempt resumes from it
			sdk.ProgressTask?.Progress?.Report((1f, "Cancelled"));
			return null;
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
			sdk.ProgressTask?.Progress?.Report((1f, InstallerExitCodes.Summarize(ex)));
			return null;
		}
	}

	/// <summary>
	/// Compares a file against the SHA512 published in the release metadata. Returns true when no
	/// hash is published - some older release entries have none, and refusing those would be worse
	/// than the status quo.
	/// </summary>
	public static async Task<bool> VerifyHashAsync(string path, string? expectedHex, CancellationToken token = default)
	{
		if (string.IsNullOrWhiteSpace(expectedHex))
		{
			return true;
		}

		await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
			1 << 20, FileOptions.Asynchronous | FileOptions.SequentialScan);
		var hash = await SHA512.HashDataAsync(stream, token);
		// releases.json ships lowercase hex; Convert.ToHexString is uppercase
		return Convert.ToHexString(hash).Equals(expectedHex, StringComparison.OrdinalIgnoreCase);
	}

	static void SafeDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}
	}

	/// <summary>
	/// Runs the installer at <paramref name="exe"/>. Always reports a terminal (1f, message) before
	/// returning - including on failure - so the caller's status display can always settle.
	/// </summary>
	public async ValueTask<InstallerResult> Install(string exe, Sdk? sdk = null, IProgress<(float progress, string task)>? status = null)
	{
		var result = new InstallerResult(InstallerOutcome.Failed, -1, "Install failed");
		try
		{
			result = await InstallCore(exe, sdk, status);
		}
		catch (OperationCanceledException)
		{
			result = new InstallerResult(InstallerOutcome.Cancelled, -1, "Cancelled");
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
			result = new InstallerResult(InstallerOutcome.Failed, -1, InstallerExitCodes.Summarize(ex));
		}
		finally
		{
			status?.Report((1f, result.Message));
		}
		return result;
	}

	async ValueTask<InstallerResult> InstallCore(string exe, Sdk? sdk, IProgress<(float progress, string task)>? status)
	{
#if WINDOWS
		var logPath = CreateLogPath("install", Path.GetFileNameWithoutExtension(exe));
		status?.Report((0.5f, "Installing"));
		// no cancelling past this point - see ProgressTask.CanCancel
		if (sdk?.ProgressTask is not null)
		{
			sdk.ProgressTask.CanCancel = false;
		}
		var result = await Cli.Wrap(exe)
			.WithArguments(new[] { "/install", "/quiet", "/norestart", "/log", logPath })
			.WithValidation(CommandResultValidation.None)
			.ExecuteAsync();
		WindowsSdkRegistry.Invalidate();
		return InstallerExitCodes.Interpret(result.ExitCode, uninstalling: false, logPath);
#endif
#if MACOS
		status?.Report((0.5f, "Installing"));
		if (sdk?.ProgressTask is not null)
		{
			sdk.ProgressTask.CanCancel = false;
		}
		var ok = RunAsRoot("/usr/sbin/installer", new[] { "-pkg", exe, "-target", "/", null });
		return ok
			? new InstallerResult(InstallerOutcome.Success, 0, "Installed")
			: new InstallerResult(InstallerOutcome.Failed, -1, "The installer failed");
#endif
#if LINUX
		// the Linux SDK ships as a tarball that is simply unpacked over a dotnet root - no installer,
		// no elevation. TarFile applies each entry's unix mode, so the extracted host comes out
		// executable without a chmod pass.
		status?.Report((0.1f, "Extracting"));
		if (sdk?.ProgressTask is not null)
		{
			sdk.ProgressTask.CanCancel = false;
		}
		Directory.CreateDirectory(Constants.DotnetRoot);
		await Task.Run(() =>
		{
			using var archive = File.OpenRead(exe);
			using var gzip = new GZipStream(archive, CompressionMode.Decompress);
			TarFile.ExtractToDirectory(gzip, Constants.DotnetRoot, overwriteFiles: true);
		});
		return new InstallerResult(InstallerOutcome.Success, 0, "Installed");
#endif
	}

#if WINDOWS
	/// <summary>
	/// A per-operation burn log under the app data folder, so a failure has something concrete to
	/// point the user at instead of just an exit code.
	/// </summary>
	static string CreateLogPath(string verb, string name)
	{
		var directory = Path.Combine(Constants.AppDataPath, "logs");
		Directory.CreateDirectory(directory);
		return Path.Combine(directory, $"{verb}-{name}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
	}
#endif

	public async Task<string> GetInstallationPath(Sdk sdk)
	{
		var version = sdk.SdkData?.Version ?? sdk.VersionDisplay;
		// GetInstalledSdks returns null when `dotnet --list-sdks` fails
		var installed = await GetInstalledSdks(true) ?? new List<InstalledSdk>();
		return installed.FirstOrDefault(x => x.Version == version)?.Path ?? string.Empty;
	}

	public async Task OpenFolder(Sdk sdk)
	{
		try
		{
			//installed SDKs missing from the release index have no SdkData
			string path = Path.Combine(sdk.Path, sdk.SdkData?.Version ?? sdk.VersionDisplay ?? "");
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


	/// <summary>
	/// Removes an installed SDK. Always reports a terminal (1f, message) before returning, including
	/// on failure, so the caller's status display can always settle.
	/// </summary>
	/// <remarks>
	/// On Windows this never downloads anything. The installer is resolved from the Add/Remove
	/// Programs registry via <see cref="WindowsSdkRegistry"/>; an SDK Dots cannot drive (a Visual
	/// Studio MSI, say) is reported as such rather than being guessed at.
	/// </remarks>
	public async Task<InstallerResult> Uninstall(Sdk sdk, IProgress<(float progress, string task)>? status = null)
	{
		//installed SDKs missing from the release index have no SdkData
		var version = sdk.SdkData?.Version ?? sdk.VersionDisplay;

		var progress = new ProgressTask
		{
			Title = $"Uninstalling {version}",
			CancellationTokenSource = new CancellationTokenSource(),
			CanCancel = true,
		};

		var p = new Progress<(float progress, string task)>();
		p.ProgressChanged += (s, e) =>
		{
			progress.IsIndeterminate = e.progress < 0;
			progress.Value = e.progress < 0 ? null : e.progress * 100;
			progress.Task = e.task;
			status?.Report(e);
		};
		progress.Progress = p;
		sdk.ProgressTask = progress;

		// Progress<T>.Report is protected; the interface is what exposes it
		IProgress<(float progress, string task)> report = p;

		var result = new InstallerResult(InstallerOutcome.Failed, -1, "Uninstall failed");
		try
		{
			result = await UninstallCore(sdk, version, report);
		}
		catch (OperationCanceledException)
		{
			result = new InstallerResult(InstallerOutcome.Cancelled, -1, "Cancelled");
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
			result = new InstallerResult(InstallerOutcome.Failed, -1, InstallerExitCodes.Summarize(ex));
		}
		finally
		{
			// the one report that guarantees the caller can settle its status, whatever happened
			report.Report((1f, result.Message));
			// order matters: CanCancel gates CancelTask, so it has to be false before the source
			// it would have cancelled is disposed
			progress.CanCancel = false;
			progress.CancellationTokenSource?.Dispose();
		}

		if (result.IsSuccess)
		{
			GetInstalledSdks(true).SafeFireAndForget();
		}
		return result;
	}

	async Task<InstallerResult> UninstallCore(Sdk sdk, string? version, IProgress<(float progress, string task)> progress)
	{
		if (string.IsNullOrEmpty(version))
		{
			return new InstallerResult(InstallerOutcome.NotUninstallable, -1, "This SDK has no version to uninstall");
		}

#if WINDOWS
		progress.Report((0.2f, "Looking up installer"));
		var logPath = CreateLogPath("uninstall", version);
		var plan = WindowsSdkRegistry.Resolve(version, logPath);

		if (plan.Ownership != SdkOwnership.StandaloneBundle)
		{
			// Downloading the standalone installer here is what the old code did. It cannot work:
			// a bundle that never installed this SDK reports 1605 when asked to uninstall it.
			return new InstallerResult(InstallerOutcome.NotUninstallable, -1, plan.Message);
		}

		progress.Report((0.5f, "Uninstalling"));
		if (sdk.ProgressTask is not null)
		{
			sdk.ProgressTask.CanCancel = false;
		}
		var result = await Cli.Wrap(plan.Executable!)
			.WithArguments(plan.Arguments!)
			.WithValidation(CommandResultValidation.None)
			.ExecuteAsync();
		WindowsSdkRegistry.Invalidate();
		return InstallerExitCodes.Interpret(result.ExitCode, uninstalling: true, logPath);
#endif
#if MACOS
		if (!Directory.Exists(Constants.AppDataPath))
		{
			Directory.CreateDirectory(Constants.AppDataPath);
		}
		//write Constants.UninstallScriptFile to file
		var script = Constants.UninstallScriptFile.Replace("XXXXX", version);
		var filename = "uninstall-" + version.Replace(".", "-") + ".sh";
		var path = Path.Combine(Constants.AppDataPath, filename);
		progress.Report((0.5f, "Writing Uninstaller"));
		await File.WriteAllTextAsync(path, script);
		progress.Report((0.6f, "Uninstalling"));
		if (sdk.ProgressTask is not null)
		{
			sdk.ProgressTask.CanCancel = false;
		}
		return RunAsRoot("/bin/sh", new[] { path, null })
			? new InstallerResult(InstallerOutcome.Success, 0, "Uninstalled")
			: new InstallerResult(InstallerOutcome.Failed, -1, "The uninstaller failed");
#endif
#if LINUX
		var root = GetWritableDotnetRoot(sdk);
		if (root is null)
		{
			return new InstallerResult(InstallerOutcome.NotUninstallable, -1,
				"Installed system-wide - remove it with your package manager");
		}

		var sdkDirectory = Path.Combine(root, "sdk", version);
		if (!Directory.Exists(sdkDirectory))
		{
			return new InstallerResult(InstallerOutcome.NotInstalled, -1, "Nothing to remove");
		}

		progress.Report((0.5f, "Removing files"));
		if (sdk.ProgressTask is not null)
		{
			sdk.ProgressTask.CanCancel = false;
		}
		Directory.Delete(sdkDirectory, true);

		// the runtimes and the host resolver are shared by every SDK in this root, so they can only
		// go once the last one is gone. Leaving them behind costs disk space; removing them early
		// would break the SDKs that stay.
		var sdkRoot = Path.Combine(root, "sdk");
		if (!Directory.EnumerateDirectories(sdkRoot).Any())
		{
			var shared = new[]
			{
				Path.Combine(root, "shared", "Microsoft.NETCore.App"),
				Path.Combine(root, "shared", "Microsoft.AspNetCore.App"),
				Path.Combine(root, "host", "fxr"),
			};

			foreach (var directory in shared.Where(Directory.Exists))
			{
				Directory.Delete(directory, true);
			}
		}

		return new InstallerResult(InstallerOutcome.Success, 0, "Uninstalled");
#endif
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

            using var auth = Security.Authorization.Create(parameters, null, flags);
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
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				// alpine and friends need the musl builds - the glibc ones will not run there
				var musl = RuntimeInformation.RuntimeIdentifier.Contains("musl");
				if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
				{
					return musl ? Rid.LinuxMuslArm64 : Rid.LinuxArm64;
				}
				if (RuntimeInformation.OSArchitecture == Architecture.Arm ||
					RuntimeInformation.OSArchitecture == Architecture.Armv6)
				{
					return musl ? Rid.LinuxMuslArm : Rid.LinuxArm;
				}
				return musl ? Rid.LinuxMuslX64 : Rid.LinuxX64;
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
