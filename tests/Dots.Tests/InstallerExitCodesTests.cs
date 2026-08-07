using Dots.Services;
using Xunit;

namespace Dots.Tests;

public class InstallerExitCodesTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(3010)] // reboot required
	[InlineData(1641)] // reboot initiated
	[InlineData(1638)] // another version already installed
	public void SucceedingCodesAreSuccesses(int exitCode)
	{
		Assert.True(InstallerExitCodes.Interpret(exitCode, uninstalling: false).IsSuccess);
		Assert.True(InstallerExitCodes.Interpret(exitCode, uninstalling: true).IsSuccess);
	}

	[Theory]
	[InlineData(1603)] // fatal error during installation
	[InlineData(1605)] // not installed
	[InlineData(1618)] // another installation in progress
	[InlineData(-1)]
	public void FailingCodesAreNotSuccesses(int exitCode)
	{
		Assert.False(InstallerExitCodes.Interpret(exitCode, uninstalling: false).IsSuccess);
	}

	[Theory]
	[InlineData(1602)]
	[InlineData(unchecked((int)0x800704C7))]
	[InlineData(unchecked((int)0x80070642))]
	public void DeclinedElevationIsAUserAbort(int exitCode)
	{
		var result = InstallerExitCodes.Interpret(exitCode, uninstalling: true);
		Assert.Equal(InstallerOutcome.ElevationDeclined, result.Outcome);
		Assert.True(result.IsUserAbort);
		Assert.False(result.IsSuccess);
	}

	[Fact]
	public void AccessDeniedHresultIsRecognised()
	{
		// Process.ExitCode is signed, so this HRESULT arrives as -2147024891
		var result = InstallerExitCodes.Interpret(unchecked((int)0x80070005), uninstalling: false);
		Assert.Equal(InstallerOutcome.AccessDenied, result.Outcome);
	}

	[Fact]
	public void RebootRequiredIsSuccessButDistinguishable()
	{
		var result = InstallerExitCodes.Interpret(3010, uninstalling: true);
		Assert.Equal(InstallerOutcome.SuccessRebootRequired, result.Outcome);
		Assert.True(result.IsSuccess);
		Assert.Contains("restart", result.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void MessageWordingFollowsTheVerb()
	{
		Assert.Equal("Installed", InstallerExitCodes.Interpret(0, uninstalling: false).Message);
		Assert.Equal("Uninstalled", InstallerExitCodes.Interpret(0, uninstalling: true).Message);
	}

	[Fact]
	public void UnknownCodeCarriesTheRawValue()
	{
		var result = InstallerExitCodes.Interpret(1234, uninstalling: false);
		Assert.Equal(InstallerOutcome.Failed, result.Outcome);
		Assert.Contains("1234", result.Message);
	}

	[Fact]
	public void LogPathFlowsThrough()
	{
		var result = InstallerExitCodes.Interpret(1603, uninstalling: false, @"C:\logs\install.log");
		Assert.Equal(@"C:\logs\install.log", result.LogPath);
	}
}
