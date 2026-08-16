namespace MothballMobile.UI.Controls;

public partial class ListLoadingView : ContentView
{
	public ListLoadingView()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty IsLoadingProperty =
		BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(ListLoadingView), false);

	public static readonly BindableProperty LoadingTextProperty =
		BindableProperty.Create(nameof(LoadingText), typeof(string), typeof(ListLoadingView), string.Empty);

	public bool IsLoading
	{
		get => (bool)GetValue(IsLoadingProperty);
		set => SetValue(IsLoadingProperty, value);
	}

	public string LoadingText
	{
		get => (string)GetValue(LoadingTextProperty);
		set => SetValue(LoadingTextProperty, value);
	}
}