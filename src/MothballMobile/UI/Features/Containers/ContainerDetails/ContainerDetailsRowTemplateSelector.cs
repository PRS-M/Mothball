namespace MothballMobile.UI.Features.Containers.ContainerDetails;

public sealed class ContainerDetailsRowTemplateSelector : DataTemplateSelector
{
    public DataTemplate HeaderTemplate { get; set; } = null!;
    public DataTemplate ItemTemplate { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => item is ContainerDetailsViewModel ? HeaderTemplate : ItemTemplate;
}
