namespace MothballMobile.UI.Controls;

public partial class EmptyStateView : ContentView
{
	public EmptyStateView()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(EmptyStateView), string.Empty);

	public static readonly BindableProperty SubtitleProperty =
		BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(EmptyStateView), string.Empty);

	public static readonly BindableProperty SubtitleFontSizeProperty =
		BindableProperty.Create(nameof(SubtitleFontSize), typeof(double), typeof(EmptyStateView), 12d);

	public static readonly BindableProperty EmptyPaddingProperty =
		BindableProperty.Create(nameof(EmptyPadding), typeof(Thickness), typeof(EmptyStateView), new Thickness(40));

	public static readonly BindableProperty SpacingProperty =
		BindableProperty.Create(nameof(Spacing), typeof(double), typeof(EmptyStateView), 10d);

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

	public double SubtitleFontSize
	{
		get => (double)GetValue(SubtitleFontSizeProperty);
		set => SetValue(SubtitleFontSizeProperty, value);
	}

	public Thickness EmptyPadding
	{
		get => (Thickness)GetValue(EmptyPaddingProperty);
		set => SetValue(EmptyPaddingProperty, value);
	}

	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}
}
