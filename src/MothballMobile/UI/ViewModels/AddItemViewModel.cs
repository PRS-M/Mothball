using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;

namespace MothballMobile.UI.ViewModels;

public partial class AddItemViewModel : ObservableObject
{
    private readonly IInventoryDomainRepository _repo;
    private readonly Infrastructure.INavigationService _nav;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string quantity = "1"; // reserved for future relation quantity use

    public AddItemViewModel(IInventoryDomainRepository repo, Infrastructure.INavigationService nav)
    {
        _repo = repo;
        _nav = nav;
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var trimmed = Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;

        var item = new Item
        {
            Name = trimmed,
            Description = Description?.Trim() ?? string.Empty
        };

        await _repo.InsertItemAsync(item);
        await _nav.GoBackAsync();
    }
}
