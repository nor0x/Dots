using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Dots.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace Dots;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureLifecycleEvents(lifecycle => {
			#if WINDOWS
				lifecycle
					.AddWindows (windows => {
						_ = windows.OnWindowCreated (async (window) =>
							{
								window.ExtendsContentIntoTitleBar = true;
								await Task.Delay(100);
							});
					});
			#endif
			}).ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("lucide.ttf", "Lucide");
			});
		
#if DEBUG
		builder.Logging.AddDebug();
#endif
		builder.Services.AddSingleton<DotnetService>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<MainViewModel>();
		return builder.Build();
	}
}
