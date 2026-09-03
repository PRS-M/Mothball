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
                    await WithdrawAssignedAsync(session);
                    break;

                case ItemInventoryAdjustmentState.ConfirmUnassignedWithdrawal:
                    await ConfirmUnassignedWithdrawalAsync(session);
                    break;

                case ItemInventoryAdjustmentState.WithdrawUnassigned:
                    await WithdrawUnassignedAsync(session);
                    break;

                case ItemInventoryAdjustmentState.ReadyToCommit:
                    return await CommitAsync(inventory.Item.ItemId, session);

                case ItemInventoryAdjustmentState.Cancelled:
                    return null;

                default:
                    throw new InvalidOperationException($"Unsupported adjustment state {session.State}.");
            }
        }
    }

    private async Task WithdrawAssignedAsync(ItemInventoryAdjustmentSession session)
    {
        var selectedContainer = session.PreferredAllocation
            ?? await popup.SelectOptionAsync(
                popupDefinitions.WithdrawalContainerPicker(session.GetRemainingAllocations()));
        if (selectedContainer is null)
        {
            session.Cancel();
            return;
        }

        var assignedWithdrawal = await popup.PickNumberAsync(
            popupDefinitions.WithdrawFromContainer(
                selectedContainer,
                session.CarriedWithdrawal,
                session.SuggestedAssignedWithdrawal));
        if (assignedWithdrawal is null)
        {
            session.Cancel();
            return;
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
    }

    private async Task ConfirmUnassignedWithdrawalAsync(ItemInventoryAdjustmentSession session)
    {
        if (await popup.ConfirmAsync(
            popupDefinitions.ConfirmUnassignedWithdrawal(session.UnassignedQuantity)))
        {
            session.AcceptUnassignedWithdrawal();
            return;
        }

        session.DeclineUnassignedWithdrawal();
    }

    private async Task WithdrawUnassignedAsync(ItemInventoryAdjustmentSession session)
    {
        var unassignedWithdrawal = await popup.PickNumberAsync(
            popupDefinitions.WithdrawUnassignedQuantity(session.UnassignedQuantity));
        session.WithdrawUnassigned(unassignedWithdrawal ?? 0);
    }

    private async Task<ItemInventoryWithdrawalExecutionResult> CommitAsync(
        Guid itemId,
        ItemInventoryAdjustmentSession session)
    {
        var plan = session.BuildPlan();
        var update = await inventoryCommands.ApplyWithdrawalAsync(itemId, plan);
        return new ItemInventoryWithdrawalExecutionResult(plan, update);
    }
}

public sealed record ItemInventoryWithdrawalExecutionResult(
    ItemInventoryWithdrawalPlan Plan,
    ItemInventoryUpdateResult Update);