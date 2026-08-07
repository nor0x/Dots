#if WINDOWS
using System.Diagnostics;
using Dots.Services;
using Xunit;
using Xunit.Abstractions;

namespace Dots.Tests;

public class WindowsSdkRegistryTests
{
	readonly ITestOutputHelper _output;

	public WindowsSdkRegistryTests(ITestOutputHelper output) => _output = output;

	[Fact]
	public void EnumerateOnlyReturnsRealSdkBundles()
	{
		var entries = WindowsSdkRegistry.Enumerate(force: true);

		foreach (var entry in entries)
		{
			_output.WriteLine($"{entry.DisplayName} | ver={entry.SdkVersion} | arch={entry.Architecture} | vs={entry.FromVisualStudio} | cache={entry.BundleCachePath}");
			// the workload manifests ("Microsoft.NET.Sdk.Android.Manifest-10.0.100") must not match
			Assert.StartsWith("Microsoft .NET SDK ", entry.DisplayName, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public void UnknownVersionResolvesToNotFound()
	{
		var plan = WindowsSdkRegistry.Resolve("0.0.0-not-a-real-sdk");
		Assert.Equal(SdkOwnership.NotFound, plan.Ownership);
		Assert.Null(plan.Executable);
		Assert.NotEmpty(plan.Message);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void BlankVersionResolvesToNotFound(string? version)
	{
		Assert.Equal(SdkOwnership.NotFound, WindowsSdkRegistry.Resolve(version).Ownership);
	}

	[Fact]
	public void OnlyStandaloneBundlesCarryAnExecutable()
	{
		foreach (var entry in WindowsSdkRegistry.Enumerate())
		{
			if (entry.SdkVersion is null)
			{
				continue;
			}

			var plan = WindowsSdkRegistry.Resolve(entry.SdkVersion);
			if (plan.Ownership == SdkOwnership.StandaloneBundle)
			{
				Assert.NotNull(plan.Executable);
				Assert.True(File.Exists(plan.Executable));
				Assert.NotNull(plan.Arguments);
				// /norestart is what stops a quiet bundle rebooting the machine unannounced
				Assert.Contains("/norestart", plan.Arguments!);
				Assert.Contains("/uninstall", plan.Arguments!);
			}
			else
			{
				Assert.Null(plan.Executable);
				Assert.NotEmpty(plan.Message);
			}
		}
	}

	/// <summary>
	/// Read-only end-to-end check against the machine's real SDKs: every version `dotnet --list-sdks`
	/// reports must resolve to a plan with an actionable message, and nothing may trigger a download.
	/// </summary>
	[Fact]
	public void EveryInstalledSdkResolvesToAPlan()
	{
		var listed = ListInstalledSdkVersions();
		Assert.NotEmpty(listed);

		foreach (var version in listed)
		{
			var plan = WindowsSdkRegistry.Resolve(version);
			_output.WriteLine($"{version,-40} -> {plan.Ownership,-20} {plan.Message}");
			Assert.NotEmpty(plan.Message);
		}
	}

	/// <summary>
	/// The UI marking is driven entirely by AnnotateOwnership, so it has to agree with Resolve:
	/// anything Dots can't uninstall must come back flagged, and nothing else may be.
	/// </summary>
	[Fact]
	public void AnnotationMatchesWhatUninstallWouldDo()
	{
		var service = new DotnetService();

		foreach (var version in ListInstalledSdkVersions())
		{
			var sdk = new Dots.Models.Sdk { VersionDisplay = version, Path = @"C:\Program Files\dotnet\sdk" };
			service.AnnotateOwnership(sdk);

			var expectedBlocked = WindowsSdkRegistry.Resolve(version).Ownership != SdkOwnership.StandaloneBundle;
			_output.WriteLine($"{version,-40} blocked={sdk.IsExternallyManaged,-6} label={sdk.ExternalOwnerLabel}");

			Assert.Equal(expectedBlocked, sdk.IsExternallyManaged);
			Assert.Equal(!expectedBlocked, sdk.CanInstallOrUninstall);
			if (expectedBlocked)
			{
				Assert.False(string.IsNullOrWhiteSpace(sdk.UninstallBlockedReason));
				Assert.False(string.IsNullOrWhiteSpace(sdk.ExternalOwnerLabel));
			}
		}
	}

	[Fact]
	public void AnAvailableButNotInstalledSdkIsNeverFlagged()
	{
		// nothing blocks installing, so the button must stay live for a version we don't have
		var sdk = new Dots.Models.Sdk { VersionDisplay = "10.0.302", Path = "" };
		new DotnetService().AnnotateOwnership(sdk);

		Assert.False(sdk.IsExternallyManaged);
		Assert.True(sdk.CanInstallOrUninstall);
	}

	static List<string> ListInstalledSdkVersions()
	{
		var psi = new ProcessStartInfo("dotnet", "--list-sdks")
		{
			RedirectStandardOutput = true,
			UseShellExecute = false,
		};
		using var process = Process.Start(psi)!;
		var output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();

		return output
			.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
			.Select(line => line.Split('[', StringSplitOptions.RemoveEmptyEntries))
			.Where(parts => parts.Length >= 2)
			.Select(parts => parts[0].Trim())
			.ToList();
	}
}
#endif
