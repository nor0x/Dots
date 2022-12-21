
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Dots.Models;
using Dots.Services;
using System.Reactive;
using Dots.Data;

namespace Dots.ViewModels
{
    public partial class MainViewModel : ObservableRecipient
    {
        public MainViewModel(DotnetService dotnet)
        {
            _dotnet = dotnet;
        }
        DotnetService _dotnet;
        List<Sdk> _baseSdks;

        [ObservableProperty]
        bool _selectionEnabled;

        [ObservableProperty]
        List<Sdk> _sdks;

        [ObservableProperty]
        string _lastUpdated;

        [RelayCommand]
        async Task DownloadScript()
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(Constants.InstallerScript);
                var content = await response.Content.ReadAsStringAsync();
                //save file to disk
                var folder = FileSystem.Current.AppDataDirectory;
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            

                var filename = Path.Combine(folder, "dotnet-install.ps1");
                await File.WriteAllTextAsync(filename, content);
                Debug.WriteLine("done - " + filename);

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [RelayCommand]
        async Task ListRuntimes()
        {
            LastUpdated = " " + DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
        }

        [RelayCommand]
        void FilterSdks(string query)
        {
            Sdks = _baseSdks.Where(s => s.Data.VersionDisplay.ToLower().Contains(query.ToLower())).ToList();
        }
        
        [RelayCommand]
        async Task ToggleSelection()
        {
            SelectionEnabled = !SelectionEnabled;
            await _dotnet.GetSdks();
        }

        [RelayCommand]
        async Task Download(Sdk sdk)
        {

        }

        [RelayCommand]
        async Task Install(Sdk sdk)
        {

        }

        [RelayCommand]
        async Task Uninstall(Sdk sdk)
        {
            var result = await _dotnet.Uninstall(sdk);
            if (result)
            {
                sdk.Path = string.Empty;
            }
        }



        [RelayCommand]
        async Task OpenPath(Sdk sdk)
        {
            await _dotnet.OpenFolder(sdk);
        }

        public async Task CheckSdks()
        {
            Sdks = await _dotnet.GetSdks();
            _baseSdks = Sdks;
            LastUpdated = " " + DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
        }
    }
}
