using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Dots.Helpers;
using Dots.Services;

#if MACOS
using Foundation;
#endif

namespace Dots
{
	public partial class AboutWindow : Window
    {
        bool _imageFlipped;
        bool _canFlipBack;
        IImage _front;
        IImage _back;
        bool _readyToRestart;
        public AboutWindow()
        {
            InitializeComponent();

            // when installed by Velopack the package version is authoritative - it reflects what was
            // actually swapped in, which the assembly/bundle metadata does not after a delta update
            var packageVersion = UpdateService.Shared.CurrentVersion;
            if (packageVersion is not null)
            {
                VersionRun.Text = packageVersion;
            }
            else
            {
#if WINDOWS
                var version = Assembly.GetEntryAssembly().GetName().Version;
                VersionRun.Text = version.ToString(3);
#endif

#if MACOS
                var v = NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleVersion")?.ToString();
                VersionRun.Text = v;
#endif

#if LINUX
                // no bundle metadata to read from - the assembly version is stamped by housekeeping.sh
                var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version;
                VersionRun.Text = assemblyVersion?.ToString(3);
#endif
            }

            // nothing to update when this build wasn't installed by Velopack (dotnet run, old portable builds)
            UpdateButton.IsVisible = UpdateService.Shared.IsSupported;

			CreditsTextBlock.Text = $"©️ {DateTime.Now.Year} Joachim Leonfellner";
        }

        private void OpenSourceButton_Clicked(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            Constants.GithubUrl.OpenUrl();
        }

        private void SupportButton_Clicked(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            Constants.SupportURl.OpenUrl();
        }

        private async void UpdateButton_Clicked(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (_readyToRestart)
            {
                UpdateService.Shared.ApplyAndRestart();
                return;
            }

            UpdateButton.IsEnabled = false;
            UpdateStatusTextBlock.Text = "checking...";

            var newVersion = await UpdateService.Shared.CheckAsync();
            if (newVersion is null)
            {
                UpdateStatusTextBlock.Text = UpdateService.Shared.LastError is null
                    ? "you're up to date"
                    : $"check failed - {UpdateService.Shared.LastError}";
                UpdateButton.IsEnabled = true;
                return;
            }

            UpdateStatusTextBlock.Text = $"downloading {newVersion}...";
            // Velopack reports progress from a background thread
            var downloaded = await UpdateService.Shared.DownloadAsync(p =>
                Dispatcher.UIThread.Post(() => UpdateStatusTextBlock.Text = $"downloading {newVersion} - {p}%"));

            if (!downloaded)
            {
                UpdateStatusTextBlock.Text = $"download failed - {UpdateService.Shared.LastError}";
                UpdateButton.IsEnabled = true;
                return;
            }

            _readyToRestart = true;
            UpdateStatusTextBlock.Text = $"{newVersion} is ready";
            UpdateButton.Content = "Restart Now";
            UpdateButton.IsEnabled = true;
        }
    }
}
