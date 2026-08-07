using System.Globalization;
using Dots.Services;
using Xunit;

namespace Dots.Tests;

public class CacheServiceTests
{
	[Theory]
	[InlineData(0, "0 B")]
	[InlineData(512, "512 B")]
	[InlineData(1024, "1 KB")]
	[InlineData(1536, "1.5 KB")]
	[InlineData(350L * 1024 * 1024, "350 MB")]
	public void SizesAreFormattedForHumans(long bytes, string expected)
	{
		// the separator follows the user's locale by design, so the test pins one rather than
		// asserting whatever the build machine happens to use
		var previous = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			Assert.Equal(expected, CacheService.FormatSize(bytes));
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Fact]
	public void StatsSeparateInstallersFromMetadata()
	{
		var directory = Path.Combine(Path.GetTempPath(), "dots-cache-test-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var previous = Constants.AppDataPath;
		try
		{
			Constants.AppDataPath = directory;

			File.WriteAllBytes(Path.Combine(directory, "dotnet-sdk-8.0.423-win-x64.exe"), new byte[100]);
			File.WriteAllBytes(Path.Combine(directory, "dotnet-sdk-9.0.100-osx-arm64.pkg"), new byte[200]);
			File.WriteAllBytes(Path.Combine(directory, "dotnet-sdk-9.0.100-linux-x64.tar.gz"), new byte[300]);
			// a cancelled or interrupted transfer still counts as a downloaded installer
			File.WriteAllBytes(Path.Combine(directory, "dotnet-sdk-10.0.100-win-x64.exe.partial"), new byte[400]);
			File.WriteAllText(Path.Combine(directory, "release-8.0.json"), "{}");
			File.WriteAllText(Path.Combine(directory, "uninstall-8-0-423.sh"), "#!/bin/sh");

			var stats = new CacheService().GetStats();

			Assert.Equal(4, stats.Installers.Files);
			Assert.Equal(1000, stats.Installers.Bytes);
			Assert.Equal(1, stats.Metadata.Files);
			Assert.Equal(1, stats.Other.Files);
		}
		finally
		{
			Constants.AppDataPath = previous;
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public void ClearInstallersLeavesMetadataAlone()
	{
		var directory = Path.Combine(Path.GetTempPath(), "dots-cache-test-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var previous = Constants.AppDataPath;
		try
		{
			Constants.AppDataPath = directory;

			var installer = Path.Combine(directory, "dotnet-sdk-8.0.423-win-x64.exe");
			var metadata = Path.Combine(directory, "release-8.0.json");
			File.WriteAllBytes(installer, new byte[100]);
			File.WriteAllText(metadata, "{}");

			var freed = new CacheService().ClearInstallers();

			Assert.Equal(100, freed);
			Assert.False(File.Exists(installer));
			Assert.True(File.Exists(metadata));
		}
		finally
		{
			Constants.AppDataPath = previous;
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public void StatsOnAMissingFolderAreEmpty()
	{
		var previous = Constants.AppDataPath;
		try
		{
			Constants.AppDataPath = Path.Combine(Path.GetTempPath(), "dots-does-not-exist-" + Guid.NewGuid().ToString("N"));
			var stats = new CacheService().GetStats();
			Assert.True(stats.Installers.IsEmpty);
			Assert.Equal(0, stats.TotalBytes);
		}
		finally
		{
			Constants.AppDataPath = previous;
		}
	}
}
