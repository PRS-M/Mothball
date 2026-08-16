using CoreApp.Contracts;

namespace CoreApp.Features.Items.Queries;

public interface IItemDetailsQueryHandler
{
    Task<ItemDetailsResult?> GetDetailsAsync(string itemId);
}
