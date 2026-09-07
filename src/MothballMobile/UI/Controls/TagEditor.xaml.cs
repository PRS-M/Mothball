using System.Collections.ObjectModel;
using System.Windows.Input;
using CoreApp.Application.Contracts.Tags;

namespace MothballMobile.UI.Controls;

/// <summary>Edits tag assignments for one item or container.</summary>
public partial class TagEditor : ContentView
{
    public TagEditor() => InitializeComponent();

    /// <summary>Identifies the <see cref="Tags"/> bindable property.</summary>
    public static readonly BindableProperty TagsProperty =
        BindableProperty.Create(nameof(Tags), typeof(ObservableCollection<TagDescriptor>), typeof(TagEditor));

    /// <summary>Identifies the <see cref="NewTagText"/> bindable property.</summary>
    public static readonly BindableProperty NewTagTextProperty =
        BindableProperty.Create(nameof(NewTagText), typeof(string), typeof(TagEditor), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Identifies the <see cref="AddCommand"/> bindable property.</summary>
    public static readonly BindableProperty AddCommandProperty =
        BindableProperty.Create(nameof(AddCommand), typeof(ICommand), typeof(TagEditor));

    /// <summary>Identifies the <see cref="RemoveCommand"/> bindable property.</summary>
    public static readonly BindableProperty RemoveCommandProperty =
        BindableProperty.Create(nameof(RemoveCommand), typeof(ICommand), typeof(TagEditor));

    /// <summary>Gets or sets the assigned tags.</summary>
    public ObservableCollection<TagDescriptor>? Tags { get => (ObservableCollection<TagDescriptor>?)GetValue(TagsProperty); set => SetValue(TagsProperty, value); }

    /// <summary>Gets or sets the pending tag name.</summary>
    public string NewTagText { get => (string)GetValue(NewTagTextProperty); set => SetValue(NewTagTextProperty, value); }

    /// <summary>Gets or sets the command that creates and assigns a tag.</summary>
    public ICommand? AddCommand { get => (ICommand?)GetValue(AddCommandProperty); set => SetValue(AddCommandProperty, value); }

    /// <summary>Gets or sets the command that removes a tag assignment.</summary>
    public ICommand? RemoveCommand { get => (ICommand?)GetValue(RemoveCommandProperty); set => SetValue(RemoveCommandProperty, value); }
}
