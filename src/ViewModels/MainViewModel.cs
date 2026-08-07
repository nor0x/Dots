using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.Input;
using Dots.Data;
using Dots.Helpers;
using Dots.Models;
using Dots.Services;
using ObservableView;
using ObservableView.Searching.Operators;


namespace Dots.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
	public MainViewModel(DotnetService dotnet, ErrorPopupHelper errorHelper, UpdateService updateService, CacheService cacheService)
	{
		_dotnet = dotnet;
		_errorHelper = errorHelper;
		_updateService = updateService;
		_cacheService = cacheService;
		_errorHelper.ErrorRaised += (message, detail) =>
		{
			ErrorMessage = message;
			ErrorDetail = detail;
			HasError = true;
		};
		_progressTasks = new ObservableCollection<ProgressTask>();
		SelectedFilterIcon = LucideIcons.ListFilter;
		CurrentStatusIcon = LucideIcons.Info;
		_filteredSelection = new();
		_baseSdks = new();
		_currentStatusIcon = LucideIcons.Info;
		_currentStatusText = "Loading SDKs...";
		_lastUpdated = "";
	}

	string _query = "";
	bool _isLoading = false;

	DotnetService _dotnet;
	ErrorPopupHelper _errorHelper;
	UpdateService _updateService;
	CacheService _cacheService;
	List<Sdk> _baseSdks;
	List<Sdk> _filteredSelection;

	[ObservableProperty]
	bool _hasError;

	[ObservableProperty]
	string _errorMessage = "";

	[ObservableProperty]
	string? _errorDetail;

	[ObservableProperty]
	string _cacheSizeDisplay = "";

	[ObservableProperty]
	string _cacheInstallersDisplay = "";

	[ObservableProperty]
	string _cacheMetadataDisplay = "";

	public string CachePath => Constants.AppDataPath;


	[ObservableProperty]
	bool _selectionEnabled;

	[ObservableProperty]
	bool _isBusy;

	[ObservableProperty]
	Sdk? _selectedSdk;

	[ObservableProperty]
	ObservableView<Sdk>? _sdks;

	[ObservableProperty]
	ObservableCollection<SdkGroup> _sdkGroups = new();

	[ObservableProperty]
	string _lastUpdated;

	[ObservableProperty]
	bool _showDetails = false;

	[ObservableProperty]
	ObservableCollection<ProgressTask> _progressTasks;

	[ObservableProperty]
	string _selectedFilterIcon;

	[ObservableProperty]
	string _currentStatusIcon;

	[ObservableProperty]
	string _currentStatusText;	

	[ObservableProperty]
	bool _emptyData;

	[ObservableProperty]
	bool _updateAvailable;

	[ObservableProperty]
	string _availableVersion = "";

	bool _showOnline = true;
	bool _showInstalled = true;

	// SDKs that are installed but not part of the release index have no Data,
	// and VersionDisplay is not guaranteed to be long enough to slice
	static string VersionGroup(Sdk sdk)
	{
		var version = sdk?.VersionDisplay ?? string.Empty;
		return version.Length >= 3 ? version.Substring(0, 3) : version;
	}

	static DateTimeOffset ReleaseDate(Sdk sdk) => sdk?.Data?.ReleaseDate ?? DateTimeOffset.MinValue;

	// same major version Sdk.Group exposes, but tolerant: an installed SDK whose version does not
	// start with a number has no group and simply stays out of the rail
	static int? MajorVersion(Sdk sdk) =>
		int.TryParse(sdk?.VersionDisplay?.Split('.').FirstOrDefault(), out var major) ? major : null;

	partial void OnSdksChanged(ObservableView<Sdk>? oldValue, ObservableView<Sdk>? newValue)
	{
		if (oldValue is not null)
		{
			oldValue.PropertyChanged -= SdksPropertyChanged;
		}

		if (newValue is not null)
		{
			newValue.PropertyChanged += SdksPropertyChanged;
		}

		RefreshSdkGroups();
	}

	// ObservableView raises this for View on every search, filter and refresh - the same signal the
	// list binding rides on, so the rail can never show a group the list no longer has
	void SdksPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ObservableView<Sdk>.View))
		{
			RefreshSdkGroups();
		}
	}

	/// <summary>
	/// Rebuilds the jump rail from what is currently in view, keeping the list's own order. The
	/// first row of each group is kept as the scroll target.
	/// </summary>
	void RefreshSdkGroups()
	{
		var view = Sdks?.View;
		if (view is null)
		{
			SdkGroups = new ObservableCollection<SdkGroup>();
			return;
		}

		var groups = view
			.Where(s => s is not null)
			.Select(s => (Sdk: s, Major: MajorVersion(s)))
			.Where(x => x.Major is not null)
			.GroupBy(x => x.Major!.Value)
			.Select(g => new SdkGroup(g.Key, g.First().Sdk));

		SdkGroups = new ObservableCollection<SdkGroup>(groups);
	}

	public bool SetSelectedSdk(Sdk sdk)
	{
		var showDetails = true;
		if (sdk is null)
		{
			showDetails = false;
		}
		else if (SelectedSdk is null)
		{
			showDetails = true;
		}
		else if (sdk is not null && sdk.VersionDisplay == SelectedSdk.VersionDisplay)
		{
			showDetails = !ShowDetails;
		}
		ShowDetails = showDetails;
		EmptyData = sdk?.Data is null;

		if (sdk?.VersionDisplay == SelectedSdk?.VersionDisplay)
		{
			SelectedSdk = null;
			return true;
		}
		else
		{
			SelectedSdk = sdk;
			return false;
		}
	}


	[RelayCommand]
	async Task DownloadScript()
	{
		try
		{
			using var client = new HttpClient();
			var response = await client.GetAsync(Constants.InstallerScript);
			var content = await response.Content.ReadAsStringAsync();
			//save file to disk
			var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}


			var filename = Path.Combine(folder, Constants.InstallerScriptFileName);
			await File.WriteAllTextAsync(filename, content);
			Debug.WriteLine("done - " + filename);

		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}
	}

	[RelayCommand]
	async Task DoCleanup()
	{
		int current = 0;
		var toCleanup = Sdks?.View?.ToList() ?? new List<Sdk>();

		foreach (var sdk in toCleanup)
		{
			CurrentStatusText = $"Uninstalling {sdk.VersionDisplay} - Cleanup {current + 1} | {toCleanup.Count}";
			CurrentStatusIcon = LucideIcons.Trash2;
			sdk.IsInstalling = true;
			var result = await _dotnet.Uninstall(sdk, status: Track(sdk, LucideIcons.Trash2));
			if (result.IsSuccess)
			{
				sdk.Path = string.Empty;
			}
			sdk.IsInstalling = false;
			current++;

			// each uninstall raises its own UAC prompt; once the user declines one, firing the
			// rest of the batch at them is just noise
			if (result.Outcome is InstallerOutcome.ElevationDeclined or InstallerOutcome.Cancelled)
			{
				ShowResult(sdk, result);
				break;
			}

			if (!result.IsSuccess)
			{
				ShowResult(sdk, result);
			}
		}

		await CheckSdks(false);
		RefreshCacheStats();
	}

	public async Task<bool> FilterCleanupSdks()
	{
		await CheckSdks(true);
		if (Sdks is null)
		{
			CurrentStatusText = $"no SDKs to cleanup";
			CurrentStatusIcon = LucideIcons.TriangleAlert;
			ResetStatusInfo().SafeFireAndForget();
			return false;
		}

		var source = Sdks.Source.Where(s => s is not null).ToList();
		var toCleanup = source.Where(s => s.Installed && s.Data?.SupportPhase is SupportPhase.Eol).ToList();
		var installed = source.Where(s => s.Installed).GroupBy(VersionGroup).Where(g => g.Count() >= 1).SelectMany(g => g).ToList();
		var latests = source.Where(s => s.Data is not null && !s.Data.Preview).GroupBy(VersionGroup).Select(g => g.OrderByDescending(ReleaseDate).First()).ToList();

		var installedGrouped = installed.GroupBy(VersionGroup).ToList();

		var addToCleanup = new List<Sdk>();
		foreach (var sdk in installed)
		{
			if (latests.Contains(sdk))
			{
				continue;
			}
			else
			{
				//add from the same major version but skip the latest
				var group = installedGrouped.FirstOrDefault(g => g.Key == VersionGroup(sdk));
				if (group is not null)
				{
					var ordered = group.OrderByDescending(ReleaseDate).ToList();
					for (int i = 1; i < ordered.Count; i++)
					{
						addToCleanup.Add(ordered[i]);
					}
				}
			}
		}
		toCleanup.AddRange(addToCleanup);
		toCleanup.AddRange(installed.Where(s => s.Data?.SupportPhase is SupportPhase.Eol).ToList());
		toCleanup = toCleanup.Distinct().ToList();

		if (toCleanup.Count() == 0)
		{
			CurrentStatusText = $"no SDKs to cleanup";
			CurrentStatusIcon = LucideIcons.TriangleAlert;
			ResetStatusInfo().SafeFireAndForget();
			return false;
		}

		_filteredSelection = toCleanup;
		Sdks.Search(" ");
		Sdks.Search(".");
		return true;
	}

	[RelayCommand]
	async Task DoUpdate()
	{
		int current = 0;
		var toInstall = Sdks?.View?.ToList() ?? new List<Sdk>();
		foreach (var sdk in toInstall)
		{
			CurrentStatusText = $"Installing {sdk.VersionDisplay} - Update {current + 1} | {toInstall.Count()}";
			CurrentStatusIcon = LucideIcons.CircleFadingArrowUp;
			await InstallOrUninstall(sdk);
		}

		await CheckSdks(false);
	}

	public async Task<bool> FilterUpdateSdks()
	{
		await CheckSdks(true);
		if (Sdks is null)
		{
			CurrentStatusText = $"everything is up to date - no SDKs to update";
			CurrentStatusIcon = LucideIcons.TriangleAlert;
			ResetStatusInfo().SafeFireAndForget();
			return false;
		}

		var source = Sdks.Source.Where(s => s is not null).ToList();
		var latests = source.GroupBy(VersionGroup).Select(g => g.OrderByDescending(ReleaseDate).First()).ToList();
		var installed = source.Where(s => s.Installed).GroupBy(VersionGroup).Where(g => g.Count() >= 1).SelectMany(g => g).ToList();
		var toInstall = latests.Except(installed).ToList().Where(s => s.Data?.SupportPhase is SupportPhase.Active || s.Data?.SupportPhase is SupportPhase.Preview || s.Data?.SupportPhase is SupportPhase.Maintenance);
		toInstall = toInstall.Distinct().ToList();

		if (toInstall.Count() == 0)
		{
			CurrentStatusText = $"everything is up to date - no SDKs to update";
			CurrentStatusIcon = LucideIcons.TriangleAlert;
			ResetStatusInfo().SafeFireAndForget();
			return false;
		}

		_filteredSelection = toInstall.ToList();
		Sdks.Search(" ");
		Sdks.Search(".");
		return true;
	}

	public async Task<bool> ResetSelectionFilter()
	{
		await CheckSdks(false);
		_filteredSelection = new();
		return false;
	}

	[RelayCommand]
	void CancelTask(Sdk sdk)
	{
		//no ProgressTask until a download or uninstall has actually started
		if (sdk?.ProgressTask?.CanCancel != true)
		{
			return;
		}

		// immediate feedback - the click otherwise looks ignored until the exception unwinds
		CurrentStatusIcon = LucideIcons.CircleX;
		CurrentStatusText = $"{sdk.VersionDisplay} - Cancelling...";
		sdk.ProgressTask.CancellationTokenSource?.Cancel();
	}


	[RelayCommand(AllowConcurrentExecutions = true)]
	async Task ListSdks()
	{
		LastUpdated = " " + DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
		await CheckSdks(true);
	}

	[RelayCommand]
	void FilterSdks(string query)
	{
		_query = query ?? "";
		//the search bar is live while the initial load is still running
		Sdks?.Search(_query);
	}

	[RelayCommand]
	void ToggleSelection()
	{ }

	[RelayCommand]
	void ApplyFilter(string f)
	{
		if (!int.TryParse(f, out var filter))
		{
			return;
		}
		//0 all
		//1 online
		//2 installed
		if (filter == 0)
		{
			_showOnline = true;
			_showInstalled = true;
			SelectedFilterIcon = LucideIcons.ListFilter;
		}
		else if (filter == 1)
		{
			_showInstalled = false;
			_showOnline = true;
			SelectedFilterIcon = LucideIcons.Cloudy;
		}
		else if (filter == 2)
		{
			_showOnline = false;
			_showInstalled = true;
			SelectedFilterIcon = LucideIcons.HardDrive;
		}
		if (Sdks is null)
		{
			return;
		}

		Sdks.Search(" ");
		Sdks.Search(_query);

		if (!Sdks.View.Contains(SelectedSdk))
		{
			SelectedSdk = null;
		}
	}

	[RelayCommand(AllowConcurrentExecutions = true)]
	async Task OpenOrDownload(Sdk sdk)
	{
		if (sdk is null)
		{
			return;
		}

		try
		{
			sdk.IsDownloading = true;
			if (sdk.Installed)
			{
				sdk.StatusMessage = Constants.OpeningText;
				//locally installed SDKs that are not in the release index have no Data
				CurrentStatusText = $"Opening {Path.Combine(sdk.Path, sdk.VersionDisplay ?? "")}";
				CurrentStatusIcon = LucideIcons.Folder;
				ResetStatusInfo().SafeFireAndForget();
				await _dotnet.OpenFolder(sdk);
			}
			else
			{
				sdk.StatusMessage = Constants.DownloadingText;
				// Download returns the file path; the folder is what we want to reveal
				var path = await _dotnet.Download(sdk, true, status: Track(sdk, LucideIcons.Download));
				if (!string.IsNullOrEmpty(path))
				{
					CurrentStatusText = "Downloaded to Desktop - opening...";
					CurrentStatusIcon = LucideIcons.Folder;
					var folder = Path.GetDirectoryName(path);
					if (!string.IsNullOrEmpty(folder))
					{
						await _dotnet.OpenFolder(folder);
					}
				}
				ResetStatusInfo().SafeFireAndForget();
			}
			sdk.IsDownloading = false;
			RefreshCacheStats();

		}
		catch (Exception ex)
		{
			sdk.IsDownloading = false;
			await _errorHelper.ShowPopup(ex);
		}
	}

	[RelayCommand(AllowConcurrentExecutions = true)]
	async Task InstallOrUninstall(Sdk sdk)
	{
		if (sdk is null)
		{
			return;
		}

		try
		{
			sdk.IsInstalling = true;
			if (sdk.Installed)
			{
				sdk.StatusMessage = Constants.UninstallingText;
				CurrentStatusText = $"{sdk.VersionDisplay} - {sdk.StatusMessage}";
				var result = await _dotnet.Uninstall(sdk, status: Track(sdk, LucideIcons.Trash2));
				ShowResult(sdk, result);
				if (result.IsSuccess)
				{
					sdk.Path = string.Empty;
				}
			}
			else
			{
				sdk.StatusMessage = Constants.DownloadingText;
				CurrentStatusText = $"{sdk.VersionDisplay} - {sdk.StatusMessage}";
				var path = await _dotnet.Download(sdk, status: Track(sdk, LucideIcons.Download));
				if (string.IsNullOrEmpty(path))
				{
					// Download already reported why through the status callback; leave it on screen.
					ResetStatusInfo().SafeFireAndForget();
				}
				else
				{
					sdk.StatusMessage = Constants.InstallingText;
					var result = await _dotnet.Install(path, sdk, status: Track(sdk, LucideIcons.HardDriveDownload));
					ShowResult(sdk, result);
					if (result.IsSuccess)
					{
						sdk.Path = await _dotnet.GetInstallationPath(sdk);
					}
				}
			}
			sdk.IsInstalling = false;
			RefreshCacheStats();
		}

		catch (Exception ex)
		{
			sdk.IsInstalling = false;
			await _errorHelper.ShowPopup(ex);
		}
	}

	/// <summary>
	/// Mirrors an operation's progress into the status bar. Deliberately does not reset the status
	/// on progress == 1 - the reset is driven by <see cref="ShowResult"/> off the awaited result, so
	/// it also happens on the paths that fail before ever reaching 1.
	/// </summary>
	IProgress<(float progress, string task)> Track(Sdk sdk, string icon) =>
		new Progress<(float progress, string task)>(p =>
		{
			sdk.Progress = p.progress;
			CurrentStatusIcon = icon;
			CurrentStatusText = p.progress < 0
				? $"{sdk.VersionDisplay} - {p.task}"
				: $"{sdk.VersionDisplay} - {p.task} {p.progress:P0}";
		});

	/// <summary>
	/// Puts the outcome on screen and makes sure the status bar always settles - a failure used to
	/// leave it frozen mid-progress because only a progress value of exactly 1 triggered the reset.
	/// </summary>
	void ShowResult(Sdk sdk, InstallerResult result)
	{
		CurrentStatusIcon = result.Outcome switch
		{
			InstallerOutcome.Success => LucideIcons.CircleCheck,
			InstallerOutcome.SuccessRebootRequired => LucideIcons.RotateCcw,
			InstallerOutcome.AlreadyInstalled => LucideIcons.Info,
			InstallerOutcome.ElevationDeclined or InstallerOutcome.AccessDenied => LucideIcons.ShieldAlert,
			InstallerOutcome.Cancelled => LucideIcons.Info,
			_ => LucideIcons.TriangleAlert,
		};
		CurrentStatusText = $"{sdk.VersionDisplay} - {result.Message}";

		if (result.IsSuccess || result.IsUserAbort)
		{
			ResetStatusInfo().SafeFireAndForget();
			return;
		}

		// anything else is a real failure and gets the banner, which stays until dismissed
		_errorHelper.ShowError($"{sdk.VersionDisplay} - {result.Message}",
			result.LogPath is null ? null : $"Installer log: {result.LogPath}");
		ResetStatusInfo().SafeFireAndForget();
	}

	async ValueTask ResetStatusInfo(bool delay = true)
	{
		if (delay)
		{
			await Task.Delay(1800);
		}

		//Sdks is replaced by CheckSdks and can be gone by the time the delay elapsed
		var source = Sdks?.Source;
		if (source is null)
		{
			CurrentStatusText = "Loading SDKs...";
			CurrentStatusIcon = LucideIcons.Info;
			return;
		}

		CurrentStatusText = $"{source.Count()} SDKs found - {source.Count(s => s.Installed)} installed";
		CurrentStatusIcon = LucideIcons.Info;
	}

	[RelayCommand]
	async Task CheckForAppUpdate()
	{
		var version = await _updateService.CheckAsync();
		AvailableVersion = version is null ? "" : $"Dots {version} is available";
		UpdateAvailable = version is not null;
	}

	[RelayCommand]
	async Task InstallAppUpdate()
	{
		if (!_updateService.HasPendingUpdate)
		{
			return;
		}

		IsBusy = true;
		CurrentStatusIcon = LucideIcons.Download;
		CurrentStatusText = $"Downloading Dots {_updateService.PendingVersion}...";

		var downloaded = await _updateService.DownloadAsync(p =>
		{
			CurrentStatusText = $"Downloading Dots {_updateService.PendingVersion} - {p}%";
		});

		IsBusy = false;

		if (!downloaded)
		{
			CurrentStatusIcon = LucideIcons.TriangleAlert;
			CurrentStatusText = $"update failed - {_updateService.LastError}";
			return;
		}

		CurrentStatusIcon = LucideIcons.CircleFadingArrowUp;
		CurrentStatusText = "restarting to finish the update...";
		_updateService.ApplyAndRestart();
	}

	[RelayCommand]
	void ToggleMultiSelection()
	{

	}

	[RelayCommand]
	void DismissError()
	{
		HasError = false;
		ErrorMessage = "";
		ErrorDetail = null;
	}

	/// <summary>Recomputes the numbers shown in the Settings window. Cheap enough to call eagerly.</summary>
	public void RefreshCacheStats()
	{
		try
		{
			var stats = _cacheService.GetStats();
			CacheInstallersDisplay = stats.Installers.Display;
			CacheMetadataDisplay = stats.Metadata.Display;
			CacheSizeDisplay = CacheService.FormatSize(stats.TotalBytes);
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}
	}

	[RelayCommand]
	async Task ClearInstallerCache()
	{
		var freed = await Task.Run(_cacheService.ClearInstallers);
		RefreshCacheStats();
		CurrentStatusIcon = LucideIcons.Trash2;
		CurrentStatusText = freed > 0
			? $"Freed {CacheService.FormatSize(freed)} of downloaded installers"
			: "No downloaded installers to remove";
		ResetStatusInfo().SafeFireAndForget();
	}

	[RelayCommand]
	async Task ClearMetadataCache()
	{
		var freed = await _cacheService.ClearMetadata();
		RefreshCacheStats();
		CurrentStatusIcon = LucideIcons.Trash2;
		CurrentStatusText = $"Cleared {CacheService.FormatSize(freed)} of release metadata";
		// the metadata is gone, so the list has to come from the network again
		await CheckSdks(true);
	}

	[RelayCommand]
	async Task OpenCacheFolder()
	{
		if (!Directory.Exists(Constants.AppDataPath))
		{
			Directory.CreateDirectory(Constants.AppDataPath);
		}
		await _dotnet.OpenFolder(Constants.AppDataPath);
	}

	void Sdks_FilterHandler(object sender, ObservableView.Filtering.FilterEventArgs<Sdk> e)
	{
		if (e?.Item is null)
		{
			return;
		}

		if (_showOnline && _showInstalled)
		{
			e.IsAllowed = true;
		}
		else if (_showOnline && !_showInstalled)
		{
			e.IsAllowed = !e.Item.Installed;
		}
		else if (!_showOnline && _showInstalled)
		{
			e.IsAllowed = e.Item.Installed;
		}
		else
		{
			e.IsAllowed = false;
		}

		if(_filteredSelection.Count > 0)
		{
			e.IsAllowed = _filteredSelection.FirstOrDefault(s => s.VersionDisplay == e.Item.VersionDisplay) is not null;
		}
	}

	public async Task CheckSdks(bool force = false)
	{
		try
		{
			if (_isLoading) return;
			_isLoading = true;
			if (Sdks is not null) Sdks.FilterHandler -= Sdks_FilterHandler;
			IsBusy = true;
			var sdkList = await _dotnet.GetSdks(force) ?? new List<Sdk>();
			sdkList = sdkList.Where(s => s is not null).DistinctBy(s => s.VersionDisplay).ToList();
			Sdks = new ObservableView<Sdk>(sdkList);
			Sdks.SearchSpecification.Add(x => x.VersionDisplay, BinaryOperator.Contains);
			Sdks.SearchSpecification.Add(x => x.Path, BinaryOperator.Contains);
			Sdks.FilterHandler += Sdks_FilterHandler;

			_baseSdks = sdkList;
			LastUpdated = " " + DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
			ResetStatusInfo(false).SafeFireAndForget();
		}
		catch (Exception ex)
		{
			await _errorHelper.ShowPopup(ex);
		}
		finally
		{
			//without this a failed load leaves _isLoading stuck and Sdks null forever
			IsBusy = false;
			_isLoading = false;
		}
	}
}
