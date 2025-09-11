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

    [ObservableProperty]
    private string? validationMessage;

    public AddItemViewModel(IInventoryDomainRepository repo, Infrastructure.INavigationService nav)
    {
        this.repo = repo;
        this.nav = nav;
    }

    private bool CanAdd() => !string.IsNullOrWhiteSpace(Name);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by MVVM Toolkit source generator")]
    partial void OnNameChanged(string value)
    {
        AddCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        var trimmed = Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            ValidationMessage = "Name is required.";
            return;
        }

        await RunCommandAsync(async () =>
        {
            var item = new Item
            {
                Name = trimmed,
                Description = Description?.Trim() ?? string.Empty
            };

            await repo.InsertItemAsync(item);
            ValidationMessage = null;
            await nav.GoBackAsync();
        });
    }
}
