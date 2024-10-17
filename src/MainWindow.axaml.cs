using Akavache;
using Avalonia.Controls;
using Avalonia.Media;
using Dots.Helpers;
using Dots.Models;
using Dots.Services;
using System.Reactive.Linq;

namespace Dots
{
    public partial class MainWindow : Window
    {
        MainViewModel _vm = new MainViewModel(new DotnetService(), new ErrorPopupHelper());
        AboutWindow _aboutWindow = new AboutWindow();
        bool _aboutWindowOpen = false;

        public MainWindow()
        {
            this.DataContext = _vm;
            InitializeComponent();
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            var paneWidth = (int)(this.Width * 0.25);
            paneWidth = paneWidth < 200 ? 200 : paneWidth;
            MainSplitView.OpenPaneLength = Math.Min(paneWidth, 500);
        }

        protected override async void OnInitialized()
        {
            base.OnInitialized();
            if (!await BlobCache.UserAccount.ContainsKey(Constants.LastCheckedKey))
            {
                await BlobCache.UserAccount.InsertObject(Constants.LastCheckedKey, DateTime.Now);
            }
            var lastChecked = await BlobCache.UserAccount.GetObject<DateTime>(Constants.LastCheckedKey);

            bool force = false;
            if (DateTime.Now.Subtract(lastChecked).TotalDays > 10)
            {
                force = true;
                await BlobCache.UserAccount.InsertObject(Constants.LastCheckedKey, DateTime.Now);
            }
            await _vm.CheckSdks(force);
        }

        private void Logo_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (_aboutWindowOpen)
            {
                _aboutWindow.Close();
                _aboutWindowOpen = false;
            }
            else
            {
                _aboutWindow = new AboutWindow();
                _aboutWindow.Show();
                _aboutWindowOpen = true;
            }
        }

        private void ToggleDetails_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            var paneWidth = (int)(this.Width * 0.25);
            paneWidth = paneWidth < 200 ? 200 : paneWidth;
            MainSplitView.OpenPaneLength = Math.Min(paneWidth, 500);
            MainSplitView.IsPaneOpen = !MainSplitView.IsPaneOpen;
            ToggleDetailsButton.Content = MainSplitView.IsPaneOpen ? LucideIcons.ChevronRight : LucideIcons.ChevronLeft;
            if (!MainSplitView.IsPaneOpen)
            {
                _vm.SetSelectedSdk(null);
                SdkList.SelectedItem = null;
                SdkList.Selection = null;
            }
        }

        private void TextBox_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
        {
            _vm.FilterSdksCommand.Execute(MainSearchBar.Text);
        }

        private void SdkItem_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (e.Source is Control control && (control.Parent is Button || control.Parent.Parent is Button))
            {
                //workaround for button click inside ListBox item
                return;
            }
            var paneWidth = (int)(this.Width * 0.25);
            paneWidth = paneWidth < 200 ? 200 : paneWidth;
            MainSplitView.OpenPaneLength = Math.Min(paneWidth, 500);
            var unselect = _vm.SetSelectedSdk((Sdk)((Avalonia.Controls.Grid)sender).DataContext);
            if (unselect)
            {
                SdkList.SelectedItem = null;
                SdkList.Selection = null;
                MainSplitView.IsPaneOpen = false;
            }
            else
            {
                MainSplitView.IsPaneOpen = true;
            }
            ToggleDetailsButton.Content = MainSplitView.IsPaneOpen ? LucideIcons.ChevronRight : LucideIcons.ChevronLeft;

        }

        private void PathTextBlock_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            var path = ((TextBlock)sender).Text;
            path.OpenFilePath();
        }

        private void Filter_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            FilterButton.Flyout.Hide();
        }
    }
}
