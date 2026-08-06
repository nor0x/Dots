using Akavache;
using Akavache.Core;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Dots
{
	public partial class App : Application
	{
		AboutWindow _aboutWindow = new AboutWindow();
		bool _aboutWindowOpen = false;
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			CacheDatabase.Initialize<SystemJsonSerializer>(configure: b =>
				b.WithApplicationName("Dots")
				.WithSqliteProvider()
				.WithSqliteDefaults(), "Dots");


			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				desktop.MainWindow = new MainWindow();
			}


			base.OnFrameworkInitializationCompleted();
		}

		private void AboutMenu_Clicked(object? sender, System.EventArgs e)
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
	}
}
