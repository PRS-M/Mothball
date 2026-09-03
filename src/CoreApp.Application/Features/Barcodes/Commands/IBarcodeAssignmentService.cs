using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;

namespace CoreApp.Application.Features.Barcodes.Commands;

/// <summary>
/// Assigns optional globally unique barcodes to inventory containers and items.
/// </summary>
public interface IBarcodeAssignmentService
{
    /// <summary>
    /// Assigns, replaces, or clears a container barcode.
    /// </summary>
    /// <param name="container">The container whose barcode is updated.</param>
    /// <param name="barcode">The barcode to assign, or <see langword="null"/> to clear it.</param>
    Task UpdateContainerAsync(Container container, Barcode? barcode);

    /// <summary>
    /// Assigns, replaces, or clears an item barcode.
    /// </summary>
    /// <param name="item">The item whose barcode is updated.</param>
    /// <param name="barcode">The barcode to assign, or <see langword="null"/> to clear it.</param>
    Task UpdateItemAsync(Item item, Barcode? barcode);
}