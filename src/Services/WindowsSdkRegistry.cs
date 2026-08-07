#if WINDOWS
// The whole file only compiles into the Windows build, but the TFM is plain net10.0 so the platform
// analyzer can't see that. Annotating the type instead just moves the warning to every call site.
#pragma warning disable CA1416
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace Dots.Services;

public enum SdkOwnership
{
	/// <summary>A standalone WiX burn bundle whose cached installer is on disk - Dots can uninstall it.</summary>
	StandaloneBundle,
	/// <summary>Registered as a bundle, but its cached installer is gone from the Package Cache.</summary>
	BundleCacheMissing,
	/// <summary>Installed by the Visual Studio installer as an MSI - only VS can safely remove it.</summary>
	VisualStudio,
	/// <summary>Registered, but not as a bundle. Ripping the MSI out could break whatever owns it.</summary>
	UnmanagedMsi,
	/// <summary>Nothing in Add/Remove Programs matches this version.</summary>
	NotFound,
}

/// <param name="Executable">The installer to run, when <see cref="Ownership"/> is <see cref="SdkOwnership.StandaloneBundle"/>.</param>
/// <param name="Message">Already phrased for the user - explains why Dots can't act, when it can't.</param>
public sealed record SdkUninstallPlan(
	SdkOwnership Ownership,
	string? Executable,
	IReadOnlyList<string>? Arguments,
	string? DisplayName,
	string Message);

public sealed record BundleEntry(
	string SubKeyName,
	string DisplayName,
	string? BundleCachePath,
	string? QuietUninstallString,
	string? SdkVersion,
	string? Architecture,
	bool FromVisualStudio);

/// <summary>
/// Resolves an installed .NET SDK to the installer that can uninstall it, by reading the
/// Add/Remove Programs registry rather than guessing a filename and scanning the Package Cache.
///
/// The registry is authoritative in a way the old filename glob never was: an entry without a
/// BundleCachePath is by construction not a standalone bundle, which is exactly the Visual Studio
/// case. So the key we match on and the "can Dots act on this?" test are the same field.
/// </summary>
public static class WindowsSdkRegistry
{
	const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
	const string DisplayNamePrefix = "Microsoft .NET SDK ";

	// ...\Package Cache\{guid}\dotnet-sdk-10.0.100-preview.7.25380.108-win-x64.exe
	// The filename is the only source that carries the exact version string `dotnet --list-sdks`
	// prints, prerelease suffix and all. BundleVersion is a packed 4-part number (8.4.2326.32602
	// for SDK 8.0.423) and structurally cannot represent a prerelease.
	static readonly Regex CachedInstallerName = new(
		@"^dotnet-sdk-(?<ver>.+)-win-(?<arch>x64|x86|arm64|arm)\.exe$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	static readonly Regex DisplayNamePattern = new(
		@"^Microsoft \.NET SDK (?<ver>\S+) \((?<arch>x64|x86|arm64|arm)\)(?<vs> from Visual Studio)?$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	static IReadOnlyList<BundleEntry>? _cache;
	static readonly Lock _gate = new();

	public static void Invalidate()
	{
		lock (_gate)
		{
			_cache = null;
		}
	}

	public static IReadOnlyList<BundleEntry> Enumerate(bool force = false)
	{
		lock (_gate)
		{
			if (!force && _cache is not null)
			{
				return _cache;
			}

			var entries = new Dictionary<string, BundleEntry>(StringComparer.OrdinalIgnoreCase);

			// The 32-bit view is not just a WOW6432Node mirror - it is where a win-x86 SDK
			// registers on an x64 OS, so it has to be swept even though most entries are dupes.
			foreach (var (hive, view) in new[]
			{
				(RegistryHive.LocalMachine, RegistryView.Registry64),
				(RegistryHive.LocalMachine, RegistryView.Registry32),
				(RegistryHive.CurrentUser, RegistryView.Default),
			})
			{
				try
				{
					using var baseKey = RegistryKey.OpenBaseKey(hive, view);
					using var uninstall = baseKey.OpenSubKey(UninstallKey);
					if (uninstall is null)
					{
						continue;
					}

					foreach (var name in uninstall.GetSubKeyNames())
					{
						// A handful of ARP keys deny read to a non-elevated user. One throw must
						// not abort the sweep - that is the bug the old Directory.GetFiles had.
						try
						{
							using var key = uninstall.OpenSubKey(name);
							if (key?.GetValue("DisplayName") is not string displayName ||
								!displayName.StartsWith(DisplayNamePrefix, StringComparison.OrdinalIgnoreCase))
							{
								continue;
							}

							var entry = Read(name, displayName, key);
							// The bundle GUID is identical in both views; first read wins.
							entries.TryAdd(entry.SubKeyName, entry);
						}
						catch (Exception ex)
						{
							Debug.WriteLine(ex);
						}
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}

			_cache = entries.Values.ToList();
			return _cache;
		}
	}

	static BundleEntry Read(string subKeyName, string displayName, RegistryKey key)
	{
		var cachePath = key.GetValue("BundleCachePath") as string;
		var quiet = key.GetValue("QuietUninstallString") as string;

		string? version = null;
		string? architecture = null;

		// Primary: the cached installer's filename.
		if (!string.IsNullOrEmpty(cachePath))
		{
			var match = CachedInstallerName.Match(Path.GetFileName(cachePath));
			if (match.Success)
			{
				version = match.Groups["ver"].Value;
				architecture = match.Groups["arch"].Value;
			}
		}

		// Fallback, for classification only: entries with no cache path still need to be found so
		// we can tell the user *why* Dots can't remove them.
		var displayMatch = DisplayNamePattern.Match(displayName);
		version ??= displayMatch.Success ? displayMatch.Groups["ver"].Value : null;
		architecture ??= displayMatch.Success ? displayMatch.Groups["arch"].Value : null;

		var fromVisualStudio = displayMatch.Groups["vs"].Success ||
			displayName.EndsWith("from Visual Studio", StringComparison.OrdinalIgnoreCase);

		return new BundleEntry(subKeyName, displayName, cachePath, quiet, version, architecture, fromVisualStudio);
	}

	public static SdkUninstallPlan Resolve(string? sdkVersion, string? logPath = null)
	{
		if (string.IsNullOrWhiteSpace(sdkVersion))
		{
			return NotFound(sdkVersion);
		}

		var candidates = Enumerate()
			.Where(e => string.Equals(e.SdkVersion, sdkVersion, StringComparison.OrdinalIgnoreCase))
			.ToList();

		if (candidates.Count == 0)
		{
			return NotFound(sdkVersion);
		}

		if (candidates.Count > 1)
		{
			// Same version, different architectures. Prefer the one matching the OS; if that is
			// still ambiguous, say so rather than uninstalling the wrong one.
			var preferred = candidates.Where(e =>
				string.Equals(e.Architecture, ExpectedArchitecture(), StringComparison.OrdinalIgnoreCase)).ToList();
			if (preferred.Count != 1)
			{
				return new SdkUninstallPlan(SdkOwnership.NotFound, null, null, null,
					$"Several {sdkVersion} installs match ({string.Join(", ", candidates.Select(c => c.Architecture ?? "?"))}) - remove it from Settings > Apps");
			}
			candidates = preferred;
		}

		var entry = candidates[0];

		if (!string.IsNullOrEmpty(entry.BundleCachePath) && File.Exists(entry.BundleCachePath))
		{
			return new SdkUninstallPlan(SdkOwnership.StandaloneBundle, entry.BundleCachePath,
				BuildArguments(logPath), entry.DisplayName, "Uninstalling");
		}

		// A QuietUninstallString without a cached exe is rare, but it is still a bundle we can drive.
		if (!string.IsNullOrEmpty(entry.BundleCachePath) || !string.IsNullOrEmpty(entry.QuietUninstallString))
		{
			var executable = FirstToken(entry.QuietUninstallString);
			if (executable is not null && File.Exists(executable))
			{
				return new SdkUninstallPlan(SdkOwnership.StandaloneBundle, executable,
					BuildArguments(logPath), entry.DisplayName, "Uninstalling");
			}

			return new SdkUninstallPlan(SdkOwnership.BundleCacheMissing, null, null, entry.DisplayName,
				$"The installer for {sdkVersion} is missing from the Windows package cache");
		}

		if (entry.FromVisualStudio)
		{
			return new SdkUninstallPlan(SdkOwnership.VisualStudio, null, null, entry.DisplayName,
				"Installed by Visual Studio - remove it in the Visual Studio Installer");
		}

		// Deliberately not falling back to UninstallString (msiexec /X): ripping an MSI out from
		// under whatever installed it is the exact failure this class exists to prevent.
		return new SdkUninstallPlan(SdkOwnership.UnmanagedMsi, null, null, entry.DisplayName,
			"Installed as an MSI that Dots can't safely remove - use Settings > Apps");
	}

	static SdkUninstallPlan NotFound(string? sdkVersion) => new(SdkOwnership.NotFound, null, null, null,
		$"No uninstaller is registered for {sdkVersion ?? "this SDK"} - it may have been installed by Visual Studio or extracted manually");

	/// <summary>
	/// Built as a list so CliWrap quotes each argument. /norestart is not optional: QuietUninstallString
	/// omits it, so running that string verbatim lets a quiet bundle reboot the machine unannounced.
	/// </summary>
	static IReadOnlyList<string> BuildArguments(string? logPath)
	{
		var args = new List<string> { "/uninstall", "/quiet", "/norestart" };
		if (!string.IsNullOrEmpty(logPath))
		{
			args.Add("/log");
			args.Add(logPath);
		}
		return args;
	}

	/// <summary>Extracts the executable from a `"C:\path\to.exe" /uninstall /quiet` command line.</summary>
	static string? FirstToken(string? commandLine)
	{
		if (string.IsNullOrWhiteSpace(commandLine))
		{
			return null;
		}

		commandLine = commandLine.Trim();
		if (commandLine[0] != '"')
		{
			var space = commandLine.IndexOf(' ');
			return space < 0 ? commandLine : commandLine[..space];
		}

		var closing = commandLine.IndexOf('"', 1);
		return closing < 0 ? null : commandLine[1..closing];
	}

	static string ExpectedArchitecture() => RuntimeInformation.OSArchitecture switch
	{
		Architecture.Arm64 => "arm64",
		Architecture.Arm or Architecture.Armv6 => "arm",
		Architecture.X86 => "x86",
		_ => "x64",
	};
}
#endif
