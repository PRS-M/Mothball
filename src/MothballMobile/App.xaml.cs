namespace MothballMobile;

public partial class App : Application
{
	private readonly IApplicationSettings applicationSettings;
	private readonly ILocalizationService localization;
	private readonly AppStartupCoordinator startupCoordinator;

	public App(
		IApplicationSettings applicationSettings,
		ILocalizationService localization,
		AppStartupCoordinator startupCoordinator)
	{
		this.applicationSettings = applicationSettings;
		this.localization = localization;
		LocalizationManager.Configure(localization);
		localization.SetLanguage(applicationSettings.Language);
		InitializeComponent();
		UserAppTheme = applicationSettings.ThemeOverride;
		this.startupCoordinator = startupCoordinator;
		ThemePaletteApplier.Apply(Resources, applicationSettings.ThemePalette, UserAppTheme == AppTheme.Unspecified ? RequestedTheme : UserAppTheme);
		applicationSettings.ThemePaletteChanged += OnThemePaletteChanged;
		RequestedThemeChanged += OnRequestedThemeChanged;
	}

	private void OnThemePaletteChanged(object? sender, EventArgs args)
		=> ApplyThemePalette(UserAppTheme == AppTheme.Unspecified ? RequestedTheme : UserAppTheme);

	private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs args)
		=> ApplyThemePalette(args.RequestedTheme);

	private void ApplyThemePalette(AppTheme mode)
		=> ThemePaletteApplier.Apply(Resources, applicationSettings.ThemePalette, mode);

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(startupCoordinator.CreateStartupPage());
		_ = startupCoordinator.InitializeAsync(window);
		return window;
	}
}
