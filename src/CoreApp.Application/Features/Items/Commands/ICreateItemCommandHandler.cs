using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;

namespace CoreApp.Application.Features.Items.Commands;

/// <summary>
/// Defines the command that creates an item.
/// </summary>
public interface ICreateItemCommandHandler
{
    /// <summary>
    /// Creates an item with optional container allocation and photo data.
    /// </summary>
    /// <param name="name">The value used by the operation.</param>
    /// <param name="description">The value used by the operation.</param>
    /// <param name="containerId">The identifier used by the operation.</param>
    /// <param name="quantity">The quantity used by the operation.</param>
    /// <param name="photoBytes">The value used by the operation.</param>
    /// <param name="barcode">The optional globally unique barcode assigned to the item.</param>
    Task<Item> CreateAsync(string name, string description, Guid? containerId = null, int quantity = 1, byte[]? photoBytes = null, Barcode? barcode = null);
}
