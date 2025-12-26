using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.ViewModels;

public partial class AddItemViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IInventoryDomainRepository repo;
    private readonly Infrastructure.INavigationService nav;

    [ObservableProperty]
    private string containerId = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string quantity = "1"; // reserved for future relation quantity use

    [ObservableProperty]
    private string? validationMessage;

    public AddItemViewModel(IInventoryDomainRepository repo, Infrastructure.INavigationService nav)
    {
        this.repo = repo;
        this.nav = nav;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null) return;
        if (query.TryGetValue(NavigationParams.ContainerId, out var value) && value is string id)
        {
            ContainerId = id;
        }
    }

    private bool CanAdd() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        var trimmed = Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            ValidationMessage = "Name is required.";
            return;
        }

        if (!int.TryParse(Quantity?.Trim(), out var parsedQuantity) || parsedQuantity <= 0)
        {
            ValidationMessage = "Quantity must be a positive number.";
            return;
        }

        await RunCommandAsync(async () =>
        {
            try
            {
                var item = new Item
                {
                    Name = trimmed,
                    Description = Description?.Trim() ?? string.Empty
                };

                await repo.InsertItemAsync(item);

                if (Guid.TryParse(ContainerId, out var cid) && cid != Guid.Empty)
                {
                    await repo.InsertItemContainerRelation(item.ItemId, cid, parsedQuantity);
                }

                ValidationMessage = null;
                await nav.GoBackAsync();
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Failed to save item: {ex.Message}";
            }
        });
    }
}
