using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Inventory.Withdrawal;
using CoreApp.Domain.Entities.InventoryAggregate;

namespace MothballMobile.UI.Features.Items.ItemDetails;

public sealed class ItemInventoryWithdrawalCoordinator
{
    private readonly IItemInventoryCommandService inventoryCommands;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;

    public ItemInventoryWithdrawalCoordinator(
        IItemInventoryCommandService inventoryCommands,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions)
    {
        this.inventoryCommands = inventoryCommands;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
    }

    public async Task<ItemInventoryWithdrawalExecutionResult?> ExecuteAsync(
        InventorySnapshot inventory,
        int requestedTotal,
        Guid? preferredContainerId)
    {
        var session = new ItemInventoryAdjustmentSession(inventory, requestedTotal, preferredContainerId);

        while (true)
        {
            switch (session.State)
            {
                case ItemInventoryAdjustmentState.WithdrawAssigned:
                    var selectedContainer = session.PreferredAllocation
                        ?? await popup.SelectOptionAsync(
                            popupDefinitions.WithdrawalContainerPicker(session.RemainingAllocations));
                    if (selectedContainer is null)
                    {
                        session.Cancel();
                        continue;
                    }

                    var assignedWithdrawal = await popup.PickNumberAsync(
                        popupDefinitions.WithdrawFromContainer(
                            selectedContainer,
                            session.CarriedWithdrawal,
                            session.SuggestedAssignedWithdrawal));
                    if (assignedWithdrawal is null)
                    {
                        session.Cancel();
                        continue;
                    }

                    try
                    {
                        session.WithdrawAssigned(selectedContainer.ContainerId, assignedWithdrawal.Value);
                    }
                    catch (InvalidOperationException)
                    {
                        await popup.ShowAlertAsync(
                            popupDefinitions.WithdrawalCarryTooSmall(session.CarriedWithdrawal));
                    }
                    break;

                case ItemInventoryAdjustmentState.ConfirmUnassignedWithdrawal:
                    if (await popup.ConfirmAsync(
                        popupDefinitions.ConfirmUnassignedWithdrawal(session.UnassignedQuantity)))
                    {
                        session.AcceptUnassignedWithdrawal();
                    }
                    else
                    {
                        session.DeclineUnassignedWithdrawal();
                    }
                    break;

                case ItemInventoryAdjustmentState.WithdrawUnassigned:
                    var unassignedWithdrawal = await popup.PickNumberAsync(
                        popupDefinitions.WithdrawUnassignedQuantity(session.UnassignedQuantity));
                    session.WithdrawUnassigned(unassignedWithdrawal ?? 0);
                    break;

                case ItemInventoryAdjustmentState.ReadyToCommit:
                    var plan = session.BuildPlan();
                    var update = await inventoryCommands.ApplyWithdrawalAsync(inventory.Item.ItemId, plan);
                    return new ItemInventoryWithdrawalExecutionResult(plan, update);

                case ItemInventoryAdjustmentState.Cancelled:
                    return null;

                default:
                    throw new InvalidOperationException($"Unsupported adjustment state {session.State}.");
            }
        }
    }
}

public sealed record ItemInventoryWithdrawalExecutionResult(
    ItemInventoryWithdrawalPlan Plan,
    ItemInventoryUpdateResult Update);