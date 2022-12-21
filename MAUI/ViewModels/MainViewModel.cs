
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Dots.Models;
using Dots.Services;
using System.Reactive;
using Dots.Data;
using Dots.Helpers;

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
        ObservableRangeCollection<Sdk> _sdks;

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

            var filteredCollection = _baseSdks.Where(s => 
            s.Data.Sdk.Version.ToLowerInvariant().Contains(query.ToLowerInvariant()) || 
            s.Path.ToLowerInvariant().Contains(query.ToLowerInvariant())).ToList();

            foreach(var s in _baseSdks)
            {
                if(!filteredCollection.Contains(s))
                {
                    Sdks.Remove(s);
                }
                else if(!Sdks.Contains(s))
                {
                    Sdks.Add(s);
                }
            }
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

        bool _showOnline;
        bool _showInstalled;

        [RelayCommand]
        void ToggleOnline()
        {
            if (!_showOnline)
            {
                Sdks.RemoveRange(_baseSdks.Where(s => !s.Installed).ToList());
            }
            else
            {
                Sdks.AddRange(_baseSdks.Where(s => !s.Installed));
            }
            _showOnline = !_showOnline;
        }
        
        [RelayCommand]
        void ToggleInstalled()
        {
            //filter observable collection
            if(!_showInstalled)
            {
                Sdks.RemoveRange(_baseSdks.Where(s => s.Installed).ToList());
            }
            else
            {
                Sdks.AddRange(_baseSdks.Where(s => s.Installed));
            }
            _showInstalled = !_showInstalled;
        }



        public async Task CheckSdks()
        {
            var sdkList = await _dotnet.GetSdks();
            Sdks = new ObservableRangeCollection<Sdk>(sdkList);
            _baseSdks = sdkList;
            LastUpdated = " " + DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
        }
    }
}
