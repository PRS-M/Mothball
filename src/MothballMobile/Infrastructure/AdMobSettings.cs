using System.Text.Json;

namespace MothballMobile.Infrastructure;

public sealed record AdMobSettings(string AppOpenAdUnitId, string BannerAdUnitId)
{
	public static AdMobSettings Load()
	{
#if DEBUG
		return CreateTestSettings();
#elif IOS || ANDROID
		return LoadReleaseSettings();
#else
		return new AdMobSettings(string.Empty, string.Empty);
#endif
	}

	private static AdMobSettings LoadReleaseSettings()
	{
		try
		{
			using var stream = FileSystem.Current
				.OpenAppPackageFileAsync("appsettings.Release.json")
				.GetAwaiter()
				.GetResult();
			var configuration = JsonSerializer.Deserialize<AdMobConfigurationFile>(stream)
				?? throw new InvalidOperationException("The AdMob configuration file is empty.");

			return configuration.AdMob?.Validate()
				?? throw new InvalidOperationException("The AdMob section is missing.");
		}
		catch (Exception exception) when (exception is FileNotFoundException or JsonException or InvalidOperationException)
		{
			throw new InvalidOperationException(
				"Release builds require Properties/appsettings.Release.json. Copy the committed example and provide production AdMob ad-unit IDs.",
				exception);
		}
	}

	private static AdMobSettings CreateTestSettings()
	{
#if IOS
		return new AdMobSettings(
			"ca-app-pub-3940256099942544/5575463023",
			"ca-app-pub-3940256099942544/2934735716");
#elif ANDROID
		return new AdMobSettings(
			"ca-app-pub-3940256099942544/9257395921",
			"ca-app-pub-3940256099942544/6300978111");
#else
		return new AdMobSettings(string.Empty, string.Empty);
#endif
	}

	private AdMobSettings Validate()
	{
		if (string.IsNullOrWhiteSpace(AppOpenAdUnitId) || string.IsNullOrWhiteSpace(BannerAdUnitId))
		{
			throw new InvalidOperationException("Both app-open and banner ad-unit IDs are required.");
		}

		return this;
	}

	private sealed class AdMobConfigurationFile
	{
		public AdMobSettings? AdMob { get; init; }
	}
}