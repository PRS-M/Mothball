using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Inventory;

namespace Mothball.Tests.Unit.Core.Entities;

[TestFixture]
public sealed class CanonicalInventoryTests
{
    private static readonly InventoryWorkspaceId Workspace = new(Guid.NewGuid());
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly InventoryPlacementId Unassigned = new(Guid.NewGuid());
    private static readonly InventoryPlacementId Container = new(Guid.NewGuid());
    private static readonly DateTimeOffset OccurredUtc = new(2026, 9, 4, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Receipt_IncreasesDestinationBalance()
    {
        var plan = InventoryMovementPlanner.PlanReceipt(Balance(Unassigned, 2), 3, "Initial receipt", OccurredUtc);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Movement.Type, Is.EqualTo(InventoryMovementType.Receipt));
            Assert.That(plan.Movement.DestinationPlacementId, Is.EqualTo(Unassigned));
            Assert.That(plan.ResultingBalances.Single().OnHandQuantity, Is.EqualTo(5));
        });
    }

    [Test]
    public void Transfer_DecreasesSourceAndIncreasesDestination()
    {
        var plan = InventoryMovementPlanner.PlanTransfer(Balance(Unassigned, 5), Balance(Container, 2), 3, "Place stock", OccurredUtc);

        Assert.That(plan.ResultingBalances.Select(balance => balance.OnHandQuantity), Is.EqualTo(new[] { 2, 5 }));
    }

    [Test]
    public void Withdrawal_RejectsQuantityAboveSource()
        => Assert.That(() => InventoryMovementPlanner.PlanWithdrawal(Balance(Container, 2), 3, "Consume", OccurredUtc),
            Throws.TypeOf<InvalidOperationException>());

    [Test]
    public void Adjustment_CanIncreaseOrDecreaseButNeverBelowZero()
    {
        var increase = InventoryMovementPlanner.PlanAdjustment(Balance(Unassigned, 2), 4, "Count correction", OccurredUtc);
        var decrease = InventoryMovementPlanner.PlanAdjustment(Balance(Unassigned, 2), -2, "Count correction", OccurredUtc);

        Assert.Multiple(() =>
        {
            Assert.That(increase.ResultingBalances.Single().OnHandQuantity, Is.EqualTo(6));
            Assert.That(decrease.ResultingBalances.Single().OnHandQuantity, Is.Zero);
            Assert.That(() => InventoryMovementPlanner.PlanAdjustment(Balance(Unassigned, 2), -3, "Count correction", OccurredUtc),
                Throws.TypeOf<InvalidOperationException>());
        });
    }

    [Test]
    public void Transfer_RejectsDifferentWorkspaceOrItem()
    {
        var otherWorkspace = new InventoryBalance(new InventoryWorkspaceId(Guid.NewGuid()), ItemId, Container, 0);

        Assert.That(() => InventoryMovementPlanner.PlanTransfer(Balance(Unassigned, 1), otherWorkspace, 1, "Move", OccurredUtc),
            Throws.TypeOf<InvalidOperationException>());
    }

    private static InventoryBalance Balance(InventoryPlacementId placement, int quantity)
        => new(Workspace, ItemId, placement, quantity);
}
