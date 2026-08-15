using CoreApp.Contracts;

namespace CoreApp.Services;

public enum ItemInventoryAdjustmentState
{
    WithdrawAssigned,
    ConfirmUnassignedWithdrawal,
    WithdrawUnassigned,
    ReadyToCommit,
    Cancelled,
}

public sealed class ItemInventoryAdjustmentSession
{
    private readonly ItemInventorySummary inventory;
    private readonly int requestedTotal;
    private readonly Guid? sourceContainerId;
    private readonly Dictionary<Guid, ItemContainerAllocation> remainingAllocations;
    private readonly List<ItemAllocationWithdrawal> assignedWithdrawals = [];
    private readonly List<int> unassignedWithdrawals = [];
    private readonly int requiredAssignedWithdrawal;
    private int assignedWithdrawn;
    private bool preferredAllocationUsed;
    private ItemInventoryWithdrawalPlan? plan;

    public ItemInventoryAdjustmentSession(
        ItemInventorySummary inventory,
        int requestedTotal,
        Guid? sourceContainerId = null)
    {
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        if (requestedTotal < 0 || requestedTotal >= inventory.TotalQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedTotal));
        }

        this.requestedTotal = requestedTotal;
        this.sourceContainerId = sourceContainerId;
        remainingAllocations = inventory.Allocations.ToDictionary(
            allocation => allocation.ContainerId,
            allocation => allocation);
        requiredAssignedWithdrawal = ItemInventoryWithdrawalPlanner.GetRequiredAssignedWithdrawal(
            inventory.TotalQuantity,
            requestedTotal,
            inventory.AssignedQuantity);

        AdvanceAssignedState();
    }

    public ItemInventoryAdjustmentState State { get; private set; }

    public IReadOnlyList<ItemContainerAllocation> RemainingAllocations
        => remainingAllocations.Values
            .Where(allocation => allocation.Quantity > 0)
            .OrderBy(allocation => allocation.ContainerName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public ItemContainerAllocation? PreferredAllocation
        => preferredAllocationUsed
            ? null
            : ItemInventoryWithdrawalPlanner.GetPreferredAllocation(RemainingAllocations, sourceContainerId);

    public int CarriedWithdrawal { get; private set; }

    public int SuggestedAssignedWithdrawal
        => Math.Max(CarriedWithdrawal, Math.Max(0, requiredAssignedWithdrawal - assignedWithdrawn));

    public int UnassignedQuantity => plan?.UnassignedQuantity ?? 0;

    public void WithdrawAssigned(Guid containerId, int quantity)
    {
        EnsureState(ItemInventoryAdjustmentState.WithdrawAssigned);
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (quantity == 0)
        {
            State = ItemInventoryAdjustmentState.Cancelled;
            return;
        }

        if (quantity < CarriedWithdrawal)
        {
            throw new InvalidOperationException(
                $"Withdrawal must be at least the carried amount of {CarriedWithdrawal}.");
        }

        if (!remainingAllocations.TryGetValue(containerId, out var allocation)
            || allocation.Quantity <= 0)
        {
            throw new ArgumentException("Selected container has no remaining allocation.", nameof(containerId));
        }

        int removed = Math.Min(allocation.Quantity, quantity);
        assignedWithdrawn += removed;
        CarriedWithdrawal = quantity - removed;
        remainingAllocations[containerId] = allocation with { Quantity = allocation.Quantity - removed };
        assignedWithdrawals.Add(new ItemAllocationWithdrawal(containerId, quantity));
        preferredAllocationUsed = true;

        AdvanceAssignedState();
    }

    public void AcceptUnassignedWithdrawal()
    {
        EnsureState(ItemInventoryAdjustmentState.ConfirmUnassignedWithdrawal);
        State = ItemInventoryAdjustmentState.WithdrawUnassigned;
    }

    public void DeclineUnassignedWithdrawal()
    {
        EnsureState(ItemInventoryAdjustmentState.ConfirmUnassignedWithdrawal);
        State = ItemInventoryAdjustmentState.ReadyToCommit;
    }

    public void WithdrawUnassigned(int quantity)
    {
        EnsureState(ItemInventoryAdjustmentState.WithdrawUnassigned);
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (quantity == 0)
        {
            State = ItemInventoryAdjustmentState.ReadyToCommit;
            return;
        }

        unassignedWithdrawals.Add(quantity);
        BuildCurrentPlan();
        State = plan!.DeleteItem || plan.UnassignedQuantity == 0
            ? ItemInventoryAdjustmentState.ReadyToCommit
            : ItemInventoryAdjustmentState.WithdrawUnassigned;
    }

    public void Cancel() => State = ItemInventoryAdjustmentState.Cancelled;

    public ItemInventoryWithdrawalPlan BuildPlan()
    {
        if (State != ItemInventoryAdjustmentState.ReadyToCommit || plan is null)
        {
            throw new InvalidOperationException("The adjustment session is not ready to commit.");
        }

        return plan;
    }

    private void AdvanceAssignedState()
    {
        if (CarriedWithdrawal > 0 || assignedWithdrawn < requiredAssignedWithdrawal)
        {
            State = ItemInventoryAdjustmentState.WithdrawAssigned;
            return;
        }

        BuildCurrentPlan();
        State = plan!.UnassignedQuantity > 0
            ? ItemInventoryAdjustmentState.ConfirmUnassignedWithdrawal
            : ItemInventoryAdjustmentState.ReadyToCommit;
    }

    private void BuildCurrentPlan()
    {
        plan = ItemInventoryWithdrawalPlanner.Plan(
            inventory.TotalQuantity,
            inventory.Allocations,
            assignedWithdrawals,
            unassignedWithdrawals,
            requestedTotal);
    }

    private void EnsureState(ItemInventoryAdjustmentState expected)
    {
        if (State != expected)
        {
            throw new InvalidOperationException($"Expected adjustment state {expected}, but was {State}.");
        }
    }
}
