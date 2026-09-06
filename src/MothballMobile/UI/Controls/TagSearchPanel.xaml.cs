using System.Collections.ObjectModel;
using System.Windows.Input;
using CoreApp.Application.Contracts.Tags;

namespace MothballMobile.UI.Controls;

/// <summary>
/// Displays free-text search together with removable, exact tag filters and suggestions.
/// </summary>
public partial class TagSearchPanel : ContentView
{
    public TagSearchPanel() => InitializeComponent();

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(TagSearchPanel), string.Empty);

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TagSearchPanel), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty SearchCommandProperty =
        BindableProperty.Create(nameof(SearchCommand), typeof(ICommand), typeof(TagSearchPanel));

    public static readonly BindableProperty SelectedTagsProperty =
        BindableProperty.Create(nameof(SelectedTags), typeof(ObservableCollection<TagDescriptor>), typeof(TagSearchPanel));

    public static readonly BindableProperty SuggestedTagsProperty =
        BindableProperty.Create(nameof(SuggestedTags), typeof(ObservableCollection<TagDescriptor>), typeof(TagSearchPanel));

    public static readonly BindableProperty IsTagSuggestionsVisibleProperty =
        BindableProperty.Create(nameof(IsTagSuggestionsVisible), typeof(bool), typeof(TagSearchPanel), false);

    public static readonly BindableProperty AddTagCommandProperty =
        BindableProperty.Create(nameof(AddTagCommand), typeof(ICommand), typeof(TagSearchPanel));

    public static readonly BindableProperty RemoveTagCommandProperty =
        BindableProperty.Create(nameof(RemoveTagCommand), typeof(ICommand), typeof(TagSearchPanel));

    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public ICommand? SearchCommand { get => (ICommand?)GetValue(SearchCommandProperty); set => SetValue(SearchCommandProperty, value); }
    public ObservableCollection<TagDescriptor>? SelectedTags { get => (ObservableCollection<TagDescriptor>?)GetValue(SelectedTagsProperty); set => SetValue(SelectedTagsProperty, value); }
    public ObservableCollection<TagDescriptor>? SuggestedTags { get => (ObservableCollection<TagDescriptor>?)GetValue(SuggestedTagsProperty); set => SetValue(SuggestedTagsProperty, value); }
    public bool IsTagSuggestionsVisible { get => (bool)GetValue(IsTagSuggestionsVisibleProperty); set => SetValue(IsTagSuggestionsVisibleProperty, value); }
    public ICommand? AddTagCommand { get => (ICommand?)GetValue(AddTagCommandProperty); set => SetValue(AddTagCommandProperty, value); }
    public ICommand? RemoveTagCommand { get => (ICommand?)GetValue(RemoveTagCommandProperty); set => SetValue(RemoveTagCommandProperty, value); }
}
