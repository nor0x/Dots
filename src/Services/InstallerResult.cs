namespace Dots.Services;

public enum InstallerOutcome
{
	Success,
	SuccessRebootRequired,
	AlreadyInstalled,
	NotInstalled,
	ElevationDeclined,
	AccessDenied,
	Cancelled,
	AnotherInstallationInProgress,
	/// <summary>
	/// The SDK is installed, but not by something Dots can safely drive - a Visual Studio MSI,
	/// or an install with no registered uninstaller at all.
	/// </summary>
	NotUninstallable,
	Failed,
}

/// <param name="Message">Already phrased for the status bar - callers display it verbatim.</param>
public readonly record struct InstallerResult(InstallerOutcome Outcome, int ExitCode, string Message, string? LogPath = null)
{
	/// <summary>
	/// AlreadyInstalled counts as success: the SDK the user asked for is on the machine, which is
	/// what they wanted. Reporting it as a failure is what left the row stuck on "Install".
	/// </summary>
	public bool IsSuccess => Outcome is InstallerOutcome.Success
		or InstallerOutcome.SuccessRebootRequired
		or InstallerOutcome.AlreadyInstalled;

	/// <summary>The user chose to stop - worth showing, but not worth an error banner.</summary>
	public bool IsUserAbort => Outcome is InstallerOutcome.ElevationDeclined or InstallerOutcome.Cancelled;
}

public static class InstallerExitCodes
{
	// Process.ExitCode is signed, so an HRESULT arrives negative and can't be written as a bare literal.
	const int AccessDenied = unchecked((int)0x80070005);
	const int ErrorCancelled = unchecked((int)0x800704C7);
	const int UserExit = unchecked((int)0x80070642);

	/// <summary>
	/// Maps a WiX burn / MSI exit code onto an outcome. Burn reports plenty of non-zero codes that
	/// are not failures - treating only 0 as success is what made a reboot-required uninstall look
	/// like an error and left the progress bar stuck.
	/// </summary>
	public static InstallerResult Interpret(int exitCode, bool uninstalling, string? logPath = null) => exitCode switch
	{
		0 => new(InstallerOutcome.Success, exitCode, uninstalling ? "Uninstalled" : "Installed", logPath),
		1641 => new(InstallerOutcome.SuccessRebootRequired, exitCode,
			uninstalling ? "Uninstalled - Windows is restarting" : "Installed - Windows is restarting", logPath),
		3010 => new(InstallerOutcome.SuccessRebootRequired, exitCode,
			uninstalling ? "Uninstalled - restart Windows to finish" : "Installed - restart Windows to finish", logPath),
		1602 or ErrorCancelled or UserExit => new(InstallerOutcome.ElevationDeclined, exitCode,
			"Cancelled - administrator approval is required", logPath),
		1603 => new(InstallerOutcome.Failed, exitCode, "The installer failed - see the log for details", logPath),
		1605 or 1614 => new(InstallerOutcome.NotInstalled, exitCode, "This installer did not install this SDK", logPath),
		1618 => new(InstallerOutcome.AnotherInstallationInProgress, exitCode,
			"Another installation is running - try again in a moment", logPath),
		1638 => new(InstallerOutcome.AlreadyInstalled, exitCode, "Another version of this SDK is already installed", logPath),
		AccessDenied => new(InstallerOutcome.AccessDenied, exitCode, "Access denied - administrator rights are required", logPath),
		_ => new(InstallerOutcome.Failed, exitCode, $"Failed (exit code {exitCode}, 0x{exitCode:X8})", logPath),
	};

	/// <summary>Turns an exception into a short line a user can act on, rather than a type name.</summary>
	public static string Summarize(Exception ex) => ex switch
	{
		System.Net.Http.HttpRequestException => "Network error - check your connection",
		TaskCanceledException => "Timed out",
		UnauthorizedAccessException => "Access denied",
		System.IO.IOException io => $"Disk error - {io.Message}",
		_ => ex.Message,
	};
}
