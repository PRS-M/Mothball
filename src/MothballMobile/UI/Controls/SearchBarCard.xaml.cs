using System.Windows.Input;

namespace MothballMobile.UI.Controls;

public partial class SearchBarCard : ContentView
{
	public SearchBarCard()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty PlaceholderProperty =
		BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(SearchBarCard), string.Empty);

	public static readonly BindableProperty TextProperty =
		BindableProperty.Create(nameof(Text), typeof(string), typeof(SearchBarCard), string.Empty, defaultBindingMode: BindingMode.TwoWay);

	public static readonly BindableProperty SearchCommandProperty =
		BindableProperty.Create(nameof(SearchCommand), typeof(ICommand), typeof(SearchBarCard), null);

    /// <summary>Identifies the <see cref="HasSearchFocus"/> bindable property.</summary>
    public static readonly BindableProperty HasSearchFocusProperty =
        BindableProperty.Create(nameof(HasSearchFocus), typeof(bool), typeof(SearchBarCard), false);

	public string Placeholder
	{
		get => (string)GetValue(PlaceholderProperty);
		set => SetValue(PlaceholderProperty, value);
	}

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public ICommand? SearchCommand
	{
		get => (ICommand?)GetValue(SearchCommandProperty);
		set => SetValue(SearchCommandProperty, value);
	}

    /// <summary>Gets a value indicating whether the native search field currently has focus.</summary>
    public bool HasSearchFocus
    {
        get => (bool)GetValue(HasSearchFocusProperty);
        private set => SetValue(HasSearchFocusProperty, value);
    }

    private void OnSearchBarFocused(object? sender, FocusEventArgs e) => HasSearchFocus = true;

    private void OnSearchBarUnfocused(object? sender, FocusEventArgs e) => HasSearchFocus = false;
}
