
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Dots.Models;
using Dots.Services;
using System.Reactive;

namespace Dots.ViewModels
{
    public partial class MainViewModel : ObservableRecipient
    {
        public MainViewModel(DotnetService dotnet)
        {
            _dotnet = dotnet;
        }
        DotnetService _dotnet;
        List<SDK> _baseSdks;

        [ObservableProperty]
        bool _selectionEnabled;

        [ObservableProperty]
        List<SDK> _sdks;

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
            _baseSdks = await _dotnet.CheckInstalledSdks(true);
            Sdks = _baseSdks;
        }

        [RelayCommand]
        void FilterSdks(string query)
        {
            Sdks = _baseSdks.Where(s => s.VersionText.ToLower().Contains(query.ToLower())).ToList();
        }
        
        [RelayCommand]
        void ToggleSelection()
        {
            SelectionEnabled = !SelectionEnabled;
        }

        [RelayCommand]
        async Task Download(SDK sdk)
        {

        }

        [RelayCommand]
        async Task Install(SDK sdk)
        {

        }

        [RelayCommand]
        async Task Uninstall(SDK sdk)
        {

        }



        [RelayCommand]
        async Task OpenPath(SDK sdk)
        {
            await _dotnet.OpenFolder(sdk);
        }

        public async Task CheckSdks()
        {
            Sdks = await _dotnet.CheckInstalledSdks();
        }
    }
}
