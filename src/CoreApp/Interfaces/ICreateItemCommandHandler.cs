using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Interfaces;

public interface ICreateItemCommandHandler
{
    Task<Item> CreateAsync(string name, string description, Guid? containerId = null, int quantity = 1, byte[]? photoBytes = null);
}
