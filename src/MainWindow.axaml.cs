using Akavache;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Dots.Helpers;
using Dots.Models;
using Dots.Services;
using System.Reactive.Linq;

namespace Dots
{
	public partial class MainWindow : Window
	{
		MainViewModel _vm = new MainViewModel(new DotnetService(), new ErrorPopupHelper(), UpdateService.Shared, new CacheService());
		AboutWindow _aboutWindow = new AboutWindow();
		bool _aboutWindowOpen = false;
		SettingsWindow? _settingsWindow;
		bool _settingsWindowOpen = false;
		int _minPaneWidth = 260;
		double _paneRatio = 0.3;

		public MainWindow()
		{
			this.DataContext = _vm;
			InitializeComponent();
		}

		protected override void OnSizeChanged(SizeChangedEventArgs e)
		{
			SetPaneWidth();
		}

		protected override async void OnInitialized()
		{
			base.OnInitialized();
			if (!await CacheDatabase.UserAccount.ContainsKey(Constants.LastCheckedKey))
			{
				await CacheDatabase.UserAccount.InsertObject(Constants.LastCheckedKey, DateTime.Now);
			}
			var lastChecked = await CacheDatabase.UserAccount.GetObject<DateTime>(Constants.LastCheckedKey);

			bool force = false;
			if (DateTime.Now.Subtract(lastChecked).TotalDays > 10)
			{
				force = true;
				await CacheDatabase.UserAccount.InsertObject(Constants.LastCheckedKey, DateTime.Now);
			}
			await _vm.CheckSdks(force);
			await CheckForAppUpdate();
		}

		async Task CheckForAppUpdate()
		{
			if (!UpdateService.Shared.IsSupported)
			{
				return;
			}

			if (!await CacheDatabase.UserAccount.ContainsKey(Constants.LastUpdateCheckKey))
			{
				await CacheDatabase.UserAccount.InsertObject(Constants.LastUpdateCheckKey, DateTime.MinValue);
			}
			var lastUpdateCheck = await CacheDatabase.UserAccount.GetObject<DateTime>(Constants.LastUpdateCheckKey);

			if (DateTime.Now.Subtract(lastUpdateCheck).TotalDays < 1)
			{
				return;
			}

			await CacheDatabase.UserAccount.InsertObject(Constants.LastUpdateCheckKey, DateTime.Now);
			await _vm.CheckForAppUpdateCommand.ExecuteAsync(null);
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

		private void SettingsButton_Clicked(object? sender, Avalonia.Input.TappedEventArgs e)
		{
			if (_settingsWindowOpen)
			{
				_settingsWindow?.Close();
				return;
			}

			// numbers are read on open rather than polled, so they are fresh without a timer
			_vm.RefreshCacheStats();
			_settingsWindow = new SettingsWindow { DataContext = _vm };
			_settingsWindow.Closed += (_, _) => _settingsWindowOpen = false;
			_settingsWindowOpen = true;
			_settingsWindow.Show(this);
		}

		private void ToggleDetails_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
		{
			SetPaneWidth();
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

			SetPaneWidth();
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

		private void GroupDot_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
		{
			if ((sender as Control)?.DataContext is not SdkGroup group)
			{
				return;
			}

			// the rail is rebuilt from Sdks.View, but ObservableView hands out a fresh collection on
			// every read, so the index has to come from the ListBox's own items
			var index = SdkList.Items.IndexOf(group.First);
			if (index < 0)
			{
				return;
			}

			// realizes the container - with virtualization there is nothing to measure until then
			SdkList.ScrollIntoView(index);

			// ScrollIntoView only guarantees visibility, so a group reached from above lands at the
			// bottom edge. Runs after the scroll's layout pass and lifts the row to the top instead.
			Dispatcher.UIThread.Post(() =>
			{
				var scroll = SdkList.FindDescendantOfType<ScrollViewer>();
				var container = SdkList.ContainerFromIndex(index);
				var offset = container?.TranslatePoint(default, scroll!);
				if (scroll is null || offset is null)
				{
					return;
				}

				var max = Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height);
				scroll.Offset = scroll.Offset.WithY(Math.Clamp(scroll.Offset.Y + offset.Value.Y, 0, max));
			}, DispatcherPriority.Background);
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

		private async void UpdateAndCleanupButton_Click(object? sender, Avalonia.Input.TappedEventArgs e)
		{
			var filterTask = _vm.FilterUpdateSdks;
			if (SelectionInfoContainer.Height != 0)
			{
				if (SelectionInfoText.Text == Constants.UpdateText)
				{
					SelectionInfoContainer.HeightTo(0);
					filterTask = _vm.ResetSelectionFilter;
				}
			}
			SelectionInfoButton.Command = _vm.DoUpdateCommand;
			SelectionInfoButton.Content = Constants.UpdateButtonText;
			SelectionInfoText.Text = Constants.UpdateText;
			SelectionInfoButton.Background = Constants.UpdateBrush;

			var hasSelection = await filterTask();
			if (!hasSelection)
			{
				await SelectionInfoContainer.HeightTo(0);
			}
			else
			{
				SelectionInfoContainer.HeightTo(105);
			}
		}

		private async void CleanupButton_Click(object? sender, Avalonia.Input.TappedEventArgs e)
		{
			var filterTask = _vm.FilterCleanupSdks;
			if (SelectionInfoContainer.Height != 0)
			{
				if (SelectionInfoText.Text == Constants.CleanupText)
				{
					SelectionInfoContainer.HeightTo(0);
					filterTask = _vm.ResetSelectionFilter;
				}
			}
			SelectionInfoButton.Command = _vm.DoCleanupCommand;
			SelectionInfoButton.Content = Constants.CleanupButtonText;
			SelectionInfoText.Text = Constants.CleanupText;
			SelectionInfoButton.Background = Constants.CleanupBrush;

			var hasSelection = await filterTask();
			if(!hasSelection)
			{
				await SelectionInfoContainer.HeightTo(0);
			}
			else
			{
				SelectionInfoContainer.HeightTo(105);
			}

		}

		void SetPaneWidth()
		{
			var paneWidth = (int)(this.Width * _paneRatio);
			paneWidth = paneWidth < _minPaneWidth ? _minPaneWidth : paneWidth;
			MainSplitView.OpenPaneLength = Math.Min(paneWidth, 500);
		}

		private async void CloseSelectionInfoButton_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
		{
			await _vm.ResetSelectionFilter();
			await SelectionInfoContainer.HeightTo(0);
			_vm.Sdks?.Search(" ");
			_vm.Sdks?.Search(".");
		}
	}
}
