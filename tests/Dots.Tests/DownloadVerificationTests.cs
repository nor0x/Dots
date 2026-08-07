using System.Security.Cryptography;
using Dots.Services;
using Xunit;

namespace Dots.Tests;

public class DownloadVerificationTests
{
	static string WriteTempFile(byte[] content)
	{
		var path = Path.Combine(Path.GetTempPath(), "dots-hash-test-" + Guid.NewGuid().ToString("N"));
		File.WriteAllBytes(path, content);
		return path;
	}

	[Fact]
	public async Task MatchingHashVerifies()
	{
		var content = "the quick brown fox"u8.ToArray();
		var path = WriteTempFile(content);
		try
		{
			// releases.json publishes lowercase hex, so that is what has to be accepted
			var expected = Convert.ToHexString(SHA512.HashData(content)).ToLowerInvariant();
			Assert.True(await DotnetService.VerifyHashAsync(path, expected));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task TruncatedFileFailsVerification()
	{
		var content = new byte[4096];
		Random.Shared.NextBytes(content);
		var expected = Convert.ToHexString(SHA512.HashData(content)).ToLowerInvariant();

		// what a killed or timed-out download leaves behind
		var path = WriteTempFile(content[..2048]);
		try
		{
			Assert.False(await DotnetService.VerifyHashAsync(path, expected));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task SingleFlippedByteFailsVerification()
	{
		var content = new byte[1024];
		Random.Shared.NextBytes(content);
		var expected = Convert.ToHexString(SHA512.HashData(content)).ToLowerInvariant();

		content[500] ^= 0xFF;
		var path = WriteTempFile(content);
		try
		{
			Assert.False(await DotnetService.VerifyHashAsync(path, expected));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task AbsentHashIsAccepted(string? hash)
	{
		// some older release entries publish no checksum; refusing those would be worse than the
		// status quo, so verification passes and the file is used
		var path = WriteTempFile([1, 2, 3]);
		try
		{
			Assert.True(await DotnetService.VerifyHashAsync(path, hash));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void ShortHashIsTruncatedAndLabelled()
	{
		var info = new Dots.Data.FileInfo { Hash = new string('a', 128) };
		Assert.StartsWith("SHA512 ", info.ShortHash);
		Assert.EndsWith("...", info.ShortHash);
		Assert.True(info.ShortHash.Length < 40);
	}

	[Fact]
	public void MissingHashSaysSo()
	{
		Assert.Equal("no checksum published", new Dots.Data.FileInfo { Hash = null! }.ShortHash);
	}
}
