using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Dots.Models;
using Dots.Services;
using Dots.Data;
using Dots.Helpers;
using ObservableView;
using ObservableView.Searching.Operators;
using System.IO;
using System.Net.Http;
using AsyncAwaitBestPractices;
using Avalonia.Media;


namespace Dots.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
	public MainViewModel(DotnetService dotnet, ErrorPopupHelper errorHelper)
	{
		_dotnet = dotnet;
		_errorHelper = errorHelper;
		_progressTasks = new ObservableCollection<ProgressTask>();
		SelectedFilterIcon = LucideIcons.ListFilter;
		CurrentStatusIcon = LucideIcons.Info;
	}

	string _query = "";
	bool _isLoading = false;

	DotnetService _dotnet;
	ErrorPopupHelper _errorHelper;
	List<Sdk> _baseSdks;

	[ObservableProperty]
	bool _selectionEnabled;

	[ObservableProperty]
	bool _isBusy;

	[ObservableProperty]
	Sdk _selectedSdk;

	[ObservableProperty]
	ObservableView<Sdk> _sdks;

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

	bool _showOnline = true;
	bool _showInstalled = true;

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


			var filename = Path.Combine(folder, "dotnet-install.ps1");
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
		var toCleanup = Sdks.View.ToList();

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
	}

	public async Task FilterCleanupSdks()
	{
		await CheckSdks(true);
		var toCleanup = Sdks.Source.Where(s => s.Installed && s.Data.SupportPhase is SupportPhase.Eol).ToList();
		var installed = Sdks.Source.Where(s => s.Installed).GroupBy(s => s.VersionDisplay.Substring(0, 3)).Where(g => g.Count() >= 1).SelectMany(g => g).ToList();
		var latests = Sdks.Source.Where(s => !s.Data.Preview).GroupBy(s => s.VersionDisplay.Substring(0, 3)).Select(g => g.OrderByDescending(s => s.VersionDisplay).First()).ToList();

		var installedGrouped = installed.GroupBy(s => s.VersionDisplay.Substring(0, 3)).ToList();

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
				var group = installedGrouped.FirstOrDefault(g => g.Key == sdk.VersionDisplay.Substring(0, 3));
				if (group is not null)
				{
					var ordered = group.OrderByDescending(s => s.VersionDisplay).ToList();
					for (int i = 1; i < ordered.Count; i++)
					{
						addToCleanup.Add(ordered[i]);
					}
				}
			}
		}
		toCleanup.AddRange(addToCleanup);
		toCleanup.AddRange(installed.Where(s => s.Data.SupportPhase is SupportPhase.Eol).ToList());
		toCleanup = toCleanup.Distinct().ToList();

		foreach (var s in _baseSdks)
		{
			if (!toCleanup.Contains(s))
			{
				Sdks.View.Remove(s);
			}
			else if (!Sdks.View.Contains(s))
			{
				Sdks.View.Add(s);
			}
		}
	}

	[RelayCommand(AllowConcurrentExecutions = true)]
	async Task DoUpdate()
	{
		int current = 0;
		var toInstall = Sdks.View.ToList();
		foreach (var sdk in toInstall)
		{
			CurrentStatusText = $"Installing {sdk.VersionDisplay} - Update {current + 1} | {toInstall.Count()}";
			CurrentStatusIcon = LucideIcons.CircleFadingArrowUp;
			await InstallOrUninstall(sdk);
		}

		await FilterCleanupSdks();
		await DoCleanup();
	}

	public async Task FilterUpdateSdks()
	{
		await CheckSdks(true);
		var latests = Sdks.Source.GroupBy(s => s.VersionDisplay.Substring(0, 3)).Select(g => g.OrderByDescending(s => s.VersionDisplay).First()).ToList();
		var installed = Sdks.Source.Where(s => s.Installed).GroupBy(s => s.VersionDisplay.Substring(0, 3)).Where(g => g.Count() >= 1).SelectMany(g => g).ToList();
		var toInstall = latests.Except(installed).ToList().Where(s => s.Data.SupportPhase is SupportPhase.Active || s.Data.SupportPhase is SupportPhase.Preview || s.Data.SupportPhase is SupportPhase.Maintenance);
		toInstall = toInstall.Distinct().ToList();

		if (toInstall.Any())
		{
			foreach (var s in _baseSdks)
			{
				if (!toInstall.Contains(s))
				{
					Sdks.View.Remove(s);
				}
				else if (!Sdks.View.Contains(s))
				{
					Sdks.View.Add(s);
				}
			}
		}
		else
		{
			//handle empty 
		}
	}

	public async Task ResetSelectionFilter()
	{
		await CheckSdks(false);
	}

	[RelayCommand]
	void CancelTask(Sdk sdk)
	{
		sdk.ProgressTask.CancellationTokenSource.Cancel();
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
		_query = query;
		Sdks.Search(_query);

		var filteredCollection = _baseSdks.Where(s =>
		s.Data.Sdk.Version.ToLowerInvariant().Contains(query.ToLowerInvariant()) ||
		s.Path.ToLowerInvariant().Contains(query.ToLowerInvariant())).ToList();

		foreach (var s in _baseSdks)
		{
			if (!filteredCollection.Contains(s))
			{
				Sdks.View.Remove(s);
			}
			else if (!Sdks.View.Contains(s))
			{
				Sdks.View.Add(s);
			}
		}
	}

	[RelayCommand]
	void ToggleSelection()
	{ }

	[RelayCommand]
	void ApplyFilter(string f)
	{
		int filter = int.Parse(f);
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
		try
		{
			sdk.IsDownloading = true;
			if (sdk.Installed)
			{
				sdk.StatusMessage = Constants.OpeningText;
				CurrentStatusText = $"Opening {Path.Combine(sdk.Path, sdk.Data.Sdk.Version)}";
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
				await _dotnet.OpenFolder(path);
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
			await Task.Delay(1500);
		}
		CurrentStatusText = $"{Sdks.Source.Count()} SDKs found - {Sdks.Source.Count(s => s.Installed)} installed";
		CurrentStatusIcon = LucideIcons.Info;
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
	}

	public async Task CheckSdks(bool force = false)
	{
		try
		{
			if (_isLoading) return;
			_isLoading = true;
			if (Sdks is not null) Sdks.FilterHandler -= Sdks_FilterHandler;
			IsBusy = true;
			var sdkList = await _dotnet.GetSdks(force);
			sdkList = sdkList.DistinctBy(s => s.VersionDisplay).ToList();
			Sdks = new ObservableView<Sdk>(sdkList);
			Sdks.SearchSpecification.Add(x => x.VersionDisplay, BinaryOperator.Contains);
			Sdks.SearchSpecification.Add(x => x.Path, BinaryOperator.Contains);
			Sdks.FilterHandler += Sdks_FilterHandler;

			_baseSdks = sdkList;
			LastUpdated = " " + DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
			ResetStatusInfo(false).SafeFireAndForget();
			IsBusy = false;
			_isLoading = false;
		}
		catch (Exception ex)
		{
			await _errorHelper.ShowPopup(ex);
		}
	}
}
