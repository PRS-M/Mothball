namespace MothballMobile.UI.Controls;

public partial class TitleActionRow : ContentView
{
	public TitleActionRow()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(TitleActionRow), string.Empty);

	public static readonly BindableProperty ActionsProperty =
		BindableProperty.Create(
			nameof(Actions),
			typeof(View),
			typeof(TitleActionRow),
			defaultValue: null,
			propertyChanged: OnActionsChanged);

	public static readonly BindableProperty TitleFontSizeProperty =
		BindableProperty.Create(nameof(TitleFontSize), typeof(double), typeof(TitleActionRow), 18d);

	public static readonly BindableProperty TitleFontAttributesProperty =
		BindableProperty.Create(nameof(TitleFontAttributes), typeof(FontAttributes), typeof(TitleActionRow), FontAttributes.Bold);

	public static readonly BindableProperty ColumnSpacingProperty =
		BindableProperty.Create(nameof(ColumnSpacing), typeof(double), typeof(TitleActionRow), 12d);

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public View? Actions
	{
		get => (View?)GetValue(ActionsProperty);
		set => SetValue(ActionsProperty, value);
	}

	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();

		if (Actions is not null)
			Actions.BindingContext = BindingContext;
	}

	private static void OnActionsChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is not TitleActionRow row)
			return;

		if (row.ActionsPresenter is not null)
			row.ActionsPresenter.Content = row.Actions;

		if (row.Actions is not null)
			row.Actions.BindingContext = row.BindingContext;
	}

	public double TitleFontSize
	{
		get => (double)GetValue(TitleFontSizeProperty);
		set => SetValue(TitleFontSizeProperty, value);
	}

	public FontAttributes TitleFontAttributes
	{
		get => (FontAttributes)GetValue(TitleFontAttributesProperty);
		set => SetValue(TitleFontAttributesProperty, value);
	}

	public double ColumnSpacing
	{
		get => (double)GetValue(ColumnSpacingProperty);
		set => SetValue(ColumnSpacingProperty, value);
	}
}
