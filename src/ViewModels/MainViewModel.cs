using System.Collections.ObjectModel;
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
	public MainViewModel(DotnetService dotnet, ErrorPopupHelper errorHelper, UpdateService updateService)
	{
		_dotnet = dotnet;
		_errorHelper = errorHelper;
		_updateService = updateService;
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
	List<Sdk> _baseSdks;
	List<Sdk> _filteredSelection;


	[ObservableProperty]
	bool _selectionEnabled;

	[ObservableProperty]
	bool _isBusy;

	[ObservableProperty]
	Sdk? _selectedSdk;

	[ObservableProperty]
	ObservableView<Sdk>? _sdks;

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

	[RelayCommand(AllowConcurrentExecutions = true)]
	async Task DoCleanup()
	{
		int current = 0;
		var toCleanup = Sdks?.View?.ToList() ?? new List<Sdk>();

		foreach (var sdk in toCleanup)
		{
			CurrentStatusText = $"Uninstalling {sdk.VersionDisplay} - Cleanup {current + 1} | {toCleanup.Count}";
			CurrentStatusIcon = LucideIcons.Trash2;
			sdk.IsInstalling = true;
			var result = await _dotnet.Uninstall(sdk, status: new Progress<(float progress, string task)>(p =>
			{
				sdk.Progress = p.progress;
				CurrentStatusText = $"Cleanup {sdk.VersionDisplay} - {p.task} {p.progress:P0}";
				CurrentStatusIcon = LucideIcons.Trash2;
				if (p.progress == 1)
				{
					ResetStatusInfo().SafeFireAndForget();
				}
			}));
			if (result)
			{
				sdk.Path = string.Empty;
			}
			sdk.IsInstalling = false;
			current++;
		}

		await CheckSdks(false);
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

	[RelayCommand(AllowConcurrentExecutions = true)]
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
		sdk?.ProgressTask?.CancellationTokenSource?.Cancel();
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
				var path = await _dotnet.Download(sdk, true, status: new Progress<(float progress, string task)>(p =>
				{
					sdk.Progress = p.progress;
					CurrentStatusText = $"{sdk.VersionDisplay} - {p.task} {p.progress:P0}";
					CurrentStatusIcon = LucideIcons.Download;
					if (p.progress == 1)
					{
						CurrentStatusText = "Downloaded to Desktop - opening...";
						CurrentStatusIcon = LucideIcons.Folder;
						ResetStatusInfo().SafeFireAndForget();
					}
				}));
				if (!string.IsNullOrEmpty(path))
				{
					await _dotnet.OpenFolder(path);
				}
			}
			sdk.IsDownloading = false;

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
				var result = await _dotnet.Uninstall(sdk, status: new Progress<(float progress, string task)>(p =>
				{
					sdk.Progress = p.progress;
					CurrentStatusText = $"{sdk.VersionDisplay} - {sdk.StatusMessage} - {p.task} {p.progress:P0}";
					CurrentStatusIcon = LucideIcons.Trash2;
					if (p.progress == 1)
					{
						ResetStatusInfo().SafeFireAndForget();
					}
				}));
				if (result)
				{
					sdk.Path = string.Empty;
				}
			}
			else
			{
				sdk.StatusMessage = Constants.DownloadingText;
				CurrentStatusText = $"{sdk.VersionDisplay} - {sdk.StatusMessage}";
				var path = await _dotnet.Download(sdk, status: new Progress<(float progress, string task)>(p =>
				{
					sdk.Progress = p.progress;
					CurrentStatusText = $"{sdk.VersionDisplay} - {sdk.StatusMessage} - {p.task} {p.progress:P0}";
					CurrentStatusIcon = LucideIcons.Download;
					if (p.progress == 1)
					{
						ResetStatusInfo().SafeFireAndForget();
					}
				}));
				if (!string.IsNullOrEmpty(path))
				{
					sdk.StatusMessage = Constants.InstallingText;
					var result = await _dotnet.Install(path, status: new Progress<(float progress, string task)>(p =>
					{
						sdk.Progress = p.progress;
						CurrentStatusText = $"{sdk.VersionDisplay} - {sdk.StatusMessage} - {p.task} {p.progress:P0}";
						CurrentStatusIcon = LucideIcons.HardDriveDownload;
						if (p.progress == 1)
						{
							ResetStatusInfo().SafeFireAndForget();
						}
					}));
					if (result)
					{
						sdk.Path = await _dotnet.GetInstallationPath(sdk);
					}
					else
					{
						//show popup and prompt to manually install
					}
				}
			}
			sdk.IsInstalling = false;
		}

		catch (Exception ex)
		{
			sdk.IsInstalling = false;
			await _errorHelper.ShowPopup(ex);
		}
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
	void OpenSettings()
	{ }

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
