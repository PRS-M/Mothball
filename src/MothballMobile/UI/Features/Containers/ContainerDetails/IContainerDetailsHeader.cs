namespace MothballMobile.UI.Features.Containers.ContainerDetails;

/// <summary>Container-level counts the item coordinator refreshes after a quantity change.</summary>
public interface IContainerDetailsHeader
{
    int ItemTypesCount { get; set; }
    int TotalItemCount { get; set; }
}
