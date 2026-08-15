using CoreApp.Contracts;
using CoreApp.Services;

namespace UnitTests;

[TestFixture]
public sealed class ItemInventoryAdjustmentSessionTests
{
    private static readonly Guid BoxId = Guid.NewGuid();
    private static readonly Guid DrawerId = Guid.NewGuid();

    [Test]
    public void SingleContainerDecrease_ReachesUnassignedDecisionAfterExactWithdrawal()
    {
        var session = CreateSession(total: 10, requestedTotal: 8, (BoxId, "Box", 10));

        Assert.That(session.State, Is.EqualTo(ItemInventoryAdjustmentState.WithdrawAssigned));
        Assert.That(session.PreferredAllocation?.ContainerId, Is.EqualTo(BoxId));
        Assert.That(session.SuggestedAssignedWithdrawal, Is.EqualTo(2));

        session.WithdrawAssigned(BoxId, 2);

        Assert.That(session.State, Is.EqualTo(ItemInventoryAdjustmentState.ReadyToCommit));
        Assert.That(session.BuildPlan().TotalQuantity, Is.EqualTo(8));
        Assert.That(session.BuildPlan().AssignedQuantity, Is.EqualTo(8));
    }

    [Test]
    public void Overdraw_EmptiesSelectedContainerAndCarriesRemainderToNextChoice()
    {
        var session = CreateSession(
            total: 10,
            requestedTotal: 5,
            (BoxId, "Box", 3),
            (DrawerId, "Drawer", 7));

        session.WithdrawAssigned(BoxId, 5);

        Assert.Multiple(() =>
        {
            Assert.That(session.State, Is.EqualTo(ItemInventoryAdjustmentState.WithdrawAssigned));
            Assert.That(session.CarriedWithdrawal, Is.EqualTo(2));
            Assert.That(session.RemainingAllocations.Select(a => a.ContainerId), Does.Not.Contain(BoxId));
            Assert.That(session.SuggestedAssignedWithdrawal, Is.EqualTo(2));
        });

        session.WithdrawAssigned(DrawerId, 2);

        Assert.That(session.State, Is.EqualTo(ItemInventoryAdjustmentState.ReadyToCommit));
        Assert.That(session.BuildPlan().AssignedQuantity, Is.EqualTo(5));
    }

    [Test]
    public void AdditionalAssignedWithdrawal_RequiresUnassignedDecision()
    {
        var session = CreateSession(
            total: 10,
            requestedTotal: 7,
            (BoxId, "Box", 5),
            (DrawerId, "Drawer", 5));

        session.WithdrawAssigned(BoxId, 4);

        Assert.That(session.State, Is.EqualTo(ItemInventoryAdjustmentState.ConfirmUnassignedWithdrawal));
        Assert.That(session.UnassignedQuantity, Is.EqualTo(1));

        session.DeclineUnassignedWithdrawal();

        Assert.That(session.State, Is.EqualTo(ItemInventoryAdjustmentState.ReadyToCommit));
        Assert.That(session.BuildPlan().UnassignedQuantity, Is.EqualTo(1));
    }

    [Test]
    public void AcceptedUnassignedWithdrawal_CanRepeatUntilZeroDeletesItem()
    {
        var session = CreateSession(total: 2, requestedTotal: 1);

        Assert.That(session.State, Is.EqualTo(ItemInventoryAdjustmentState.ConfirmUnassignedWithdrawal));
        session.AcceptUnassignedWithdrawal();
        session.WithdrawUnassigned(1);

        Assert.That(session.State, Is.EqualTo(ItemInventoryAdjustmentState.ReadyToCommit));
        Assert.That(session.BuildPlan().DeleteItem, Is.True);
    }

    [Test]
    public void ZeroWithdrawal_CancelsWithoutProducingPlan()
    {
        var session = CreateSession(total: 5, requestedTotal: 3, (BoxId, "Box", 5));

        session.WithdrawAssigned(BoxId, 0);

        Assert.That(session.State, Is.EqualTo(ItemInventoryAdjustmentState.Cancelled));
        Assert.Throws<InvalidOperationException>(() => session.BuildPlan());
    }

    private static ItemInventoryAdjustmentSession CreateSession(
        int total,
        int requestedTotal,
        params (Guid id, string name, int quantity)[] allocations)
    {
        var item = new CoreApp.Entities.ItemAggregate.Item(Guid.NewGuid(), "Widget", "", total);
        var allocationModels = allocations
            .Select(value => new ItemContainerAllocation(value.id, value.name, value.quantity))
            .ToList();
        var summary = new ItemInventorySummary(
            item,
            allocationModels.Sum(allocation => allocation.Quantity),
            allocationModels);

        return new ItemInventoryAdjustmentSession(summary, requestedTotal, allocations.FirstOrDefault().id);
    }
}
