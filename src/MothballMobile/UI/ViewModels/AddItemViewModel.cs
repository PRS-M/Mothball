using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Services;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.ViewModels;

public partial class AddItemViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ImageService imageService;
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly IRetryService retryService;
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

    public AddItemViewModel(
        ImageService imageService,
        IInventoryCommandRepository inventoryCommands,
        IRetryService retryService,
        Infrastructure.INavigationService nav)
    {
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
        this.nav = nav ?? throw new ArgumentNullException(nameof(nav));
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

                await inventoryCommands.InsertItemAsync(item);
                await CapturePhotoWithOptionalRetryAsync(item);

                if (Guid.TryParse(ContainerId, out var cid) && cid != Guid.Empty)
                {
                    await inventoryCommands.InsertItemContainerRelation(item.ItemId, cid, parsedQuantity);
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

    private async Task CapturePhotoWithOptionalRetryAsync(Item item)
    {
        await retryService.RetryAsync(
            async () =>
            {
                var bytesLength = await imageService.CaptureItemPhotoAsync(item);
                return bytesLength > 0;
            },
            canceledTitle: "Photo capture canceled",
            canceledMessage: "Please try again or continue without a photo.",
            retryButton: "Retry",
            continueButton: "Continue",
            continueAlertTitle: "No photo",
            continueAlertMessage: "Continuing without a photo.");
    }
}
