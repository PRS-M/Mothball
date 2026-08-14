using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IItemDetailsQueryHandler
{
    Task<ItemDetailsResult?> GetDetailsAsync(string itemId);
}
