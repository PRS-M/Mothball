using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Features.Items.Commands;

public interface IUpdateItemDescriptionCommandHandler
{
    Task UpdateAsync(Item item, string description);
}
