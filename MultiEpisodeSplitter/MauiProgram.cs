using CommunityToolkit.Maui;
using FFMpegCore;
using MultiEpisodeSplitter.Services;

namespace MultiEpisodeSplitter;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddSingleton<IMediaInformationService, MediaInformationService>();
        builder.Services.AddSingleton<IMediaSplitService, MediaSplitService>();

        GlobalFFOptions.Configure(options => options.BinaryFolder = @"C:\bin\ffmpeg");

        return builder.Build();
	}
}
