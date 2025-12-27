namespace MothballMobile.UI.Controls;

public partial class PageHeader : ContentView
{
	public PageHeader()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(PageHeader), string.Empty);

	public static readonly BindableProperty SubtitleProperty =
		BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(PageHeader), string.Empty, propertyChanged: (_, __, ___) => { });

	public static readonly BindableProperty TitleFontSizeProperty =
		BindableProperty.Create(nameof(TitleFontSize), typeof(double), typeof(PageHeader), 24d);

	public static readonly BindableProperty SubtitleFontSizeProperty =
		BindableProperty.Create(nameof(SubtitleFontSize), typeof(double), typeof(PageHeader), 14d);

	public static readonly BindableProperty SpacingProperty =
		BindableProperty.Create(nameof(Spacing), typeof(double), typeof(PageHeader), 6d);

	public static readonly BindableProperty HeaderMarginProperty =
		BindableProperty.Create(nameof(HeaderMargin), typeof(Thickness), typeof(PageHeader), new Thickness(0, 0, 0, 10));

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public string Subtitle
	{
		get => (string)GetValue(SubtitleProperty);
		set => SetValue(SubtitleProperty, value);
	}

	public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

	public double TitleFontSize
	{
		get => (double)GetValue(TitleFontSizeProperty);
		set => SetValue(TitleFontSizeProperty, value);
	}

	public double SubtitleFontSize
	{
		get => (double)GetValue(SubtitleFontSizeProperty);
		set => SetValue(SubtitleFontSizeProperty, value);
	}

	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	public Thickness HeaderMargin
	{
		get => (Thickness)GetValue(HeaderMarginProperty);
		set => SetValue(HeaderMarginProperty, value);
	}
}
