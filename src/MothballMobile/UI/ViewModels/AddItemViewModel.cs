using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;

namespace MothballMobile.UI.ViewModels;

public partial class AddItemViewModel : BaseViewModel
{
    private readonly IInventoryDomainRepository repo;
    private readonly Infrastructure.INavigationService nav;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string quantity = "1"; // reserved for future relation quantity use

    public AddItemViewModel(IInventoryDomainRepository repo, Infrastructure.INavigationService nav)
    {
        this.repo = repo;
        this.nav = nav;
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var trimmed = Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;

        await RunCommandAsync(async () =>
        {
            var item = new Item
            {
                Name = trimmed,
                Description = Description?.Trim() ?? string.Empty
            };

            await repo.InsertItemAsync(item);
            await nav.GoBackAsync();
        });
    }
}
