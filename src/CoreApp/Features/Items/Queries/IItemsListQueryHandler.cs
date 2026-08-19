using CoreApp.Entities.InventoryAggregate;
using CoreApp.Contracts;
using CoreApp.Specifications;

namespace CoreApp.Features.Items.Queries;

/// <summary>
/// Defines queries for listing items.
/// </summary>
public interface IItemsListQueryHandler
{
    /// <summary>
    /// Queries inventory snapshots that match the supplied filter and optional search criteria.
    /// </summary>
    /// <param name="filter">The category of items to include.</param>
    /// <param name="searchTerm">Optional text used to filter items.</param>
    /// <param name="pageNumber">The optional zero-based page number.</param>
    /// <param name="pageSize">The optional number of items per page.</param>
    Task<List<InventorySnapshot>> QueryAsync(
        ItemQueryFilter filter,
        string? searchTerm = null,
        int? pageNumber = null,
        int? pageSize = null);
}
