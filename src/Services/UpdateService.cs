using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Dots.Services;

/// <summary>
/// Wraps Velopack's <see cref="UpdateManager"/> against the GitHub releases of this repo.
/// Shared by <see cref="MainWindow"/> and <see cref="AboutWindow"/> so a download started from one
/// is visible to the other.
/// </summary>
public class UpdateService
{
	public static readonly UpdateService Shared = new();

	readonly UpdateManager _manager;
	UpdateInfo? _pending;

	public UpdateService()
	{
		var localFeed = Environment.GetEnvironmentVariable("DOTS_UPDATE_FEED");
		_manager = string.IsNullOrEmpty(localFeed)
			? new UpdateManager(new GithubSource(Constants.GithubUrl, null, false))
			: new UpdateManager(localFeed);
	}

	/// <summary>
	/// False when running from `dotnet run` or any build that wasn't installed by Velopack.
	/// Every caller has to no-op on false - the manager throws otherwise.
	/// </summary>
	public bool IsSupported => _manager.IsInstalled;

	/// <summary>
	/// The installed package version, or null when not installed by Velopack.
	/// </summary>
	public string? CurrentVersion => _manager.CurrentVersion?.ToString();

	/// <summary>
	/// The version found by the last <see cref="CheckAsync"/>, or null if there is nothing to install.
	/// </summary>
	public string? PendingVersion => _pending?.TargetFullRelease?.Version?.ToString();

	public bool HasPendingUpdate => _pending is not null;

	/// <summary>
	/// Asks GitHub for a newer release. Returns the new version, or null if up to date,
	/// not installed by Velopack, or the check failed.
	/// </summary>
	public async Task<string?> CheckAsync()
	{
		if (!IsSupported)
		{
			return null;
		}

		try
		{
			_pending = await _manager.CheckForUpdatesAsync();
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			_pending = null;
			return null;
		}

		LastError = null;
		return PendingVersion;
	}

	/// <summary>
	/// Downloads the update found by <see cref="CheckAsync"/>. <paramref name="progress"/> reports 0-100.
	/// Returns false when there is nothing to download or the download failed.
	/// </summary>
	public async Task<bool> DownloadAsync(Action<int>? progress = null)
	{
		if (!IsSupported || _pending is null)
		{
			return false;
		}

		try
		{
			await _manager.DownloadUpdatesAsync(_pending, progress);
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			return false;
		}

		LastError = null;
		return true;
	}

	/// <summary>
	/// Swaps in the downloaded update and restarts the app. Does not return on success.
	/// </summary>
	public void ApplyAndRestart()
	{
		if (!IsSupported || _pending is null)
		{
			return;
		}

		_manager.ApplyUpdatesAndRestart(_pending);
	}

	/// <summary>
	/// Message of the last failed operation - there is no dialog infrastructure in this app,
	/// so callers surface this through the status bar.
	/// </summary>
	public string? LastError { get; private set; }
}
