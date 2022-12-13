
using System;
using CommunityToolkit.Mvvm.Input;

namespace Dots.ViewModels
{
    public partial class MainViewModel : ObservableRecipient
    {
        public MainViewModel()
        {
            int j = 0;
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
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.AppName);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            

                var filename = Path.Combine(folder, "dotnet-install.ps1");
                await File.WriteAllTextAsync(filename, content);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
