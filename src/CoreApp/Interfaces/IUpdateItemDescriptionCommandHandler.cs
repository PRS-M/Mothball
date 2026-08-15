using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Interfaces;

public interface IUpdateItemDescriptionCommandHandler
{
    Task UpdateAsync(Item item, string description);
}
