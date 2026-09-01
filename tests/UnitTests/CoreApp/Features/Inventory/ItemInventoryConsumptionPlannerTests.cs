using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.Inventory;

namespace Mothball.Tests.Unit.Core.Features.Inventory;

[TestFixture]
public sealed class ItemInventoryConsumptionPlannerTests
{
    private static readonly Guid BoxId = Guid.NewGuid();
    private static readonly Guid DrawerId = Guid.NewGuid();

    [Test]
    public void ContainerConsumption_DecreasesTotalAndSelectedAllocationOnly()
    {
        var plan = ItemInventoryConsumptionPlanner.Plan(
            Snapshot(total: 10, (BoxId, "Box", 4), (DrawerId, "Drawer", 2)),
            ItemInventoryConsumptionSource.FromContainer(BoxId),
            quantity: 3);

        Assert.Multiple(() =>
        {
            Assert.That(plan.TotalQuantity, Is.EqualTo(7));
            Assert.That(plan.AssignedQuantity, Is.EqualTo(3));
            Assert.That(plan.UnassignedQuantity, Is.EqualTo(4));
            Assert.That(plan.Allocations.Single(a => a.ContainerId == BoxId).Quantity, Is.EqualTo(1));
            Assert.That(plan.Allocations.Single(a => a.ContainerId == DrawerId).Quantity, Is.EqualTo(2));
        });
    }

    [Test]
    public void ContainerConsumption_ExhaustingAllocationRemovesIt()
    {
        var plan = ItemInventoryConsumptionPlanner.Plan(
            Snapshot(total: 5, (BoxId, "Box", 2)),
            ItemInventoryConsumptionSource.FromContainer(BoxId),
            quantity: 2);

        Assert.That(plan.Allocations, Is.Empty);
        Assert.That(plan.UnassignedQuantity, Is.EqualTo(3));
    }

    [Test]
    public void UnassignedConsumption_LeavesAllocationsUnchanged()
    {
        var plan = ItemInventoryConsumptionPlanner.Plan(
            Snapshot(total: 8, (BoxId, "Box", 3)),
            ItemInventoryConsumptionSource.FromUnassigned(),
            quantity: 4);

        Assert.Multiple(() =>
        {
            Assert.That(plan.TotalQuantity, Is.EqualTo(4));
            Assert.That(plan.AssignedQuantity, Is.EqualTo(3));
            Assert.That(plan.UnassignedQuantity, Is.EqualTo(1));
            Assert.That(plan.Allocations.Single().Quantity, Is.EqualTo(3));
        });
    }

    [Test]
    public void FinalUnitConsumption_ProducesDeletionPlan()
    {
        var plan = ItemInventoryConsumptionPlanner.Plan(
            Snapshot(total: 1),
            ItemInventoryConsumptionSource.FromUnassigned(),
            quantity: 1);

        Assert.That(plan.DeleteItem, Is.True);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void NonPositiveConsumption_IsRejected(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemInventoryConsumptionPlanner.Plan(
            Snapshot(total: 2),
            ItemInventoryConsumptionSource.FromUnassigned(),
            quantity));
    }

    [Test]
    public void ConsumptionAboveSelectedSourceCapacity_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => ItemInventoryConsumptionPlanner.Plan(
            Snapshot(total: 5, (BoxId, "Box", 2)),
            ItemInventoryConsumptionSource.FromContainer(BoxId),
            quantity: 3));
    }

    private static InventorySnapshot Snapshot(
        int total,
        params (Guid Id, string Name, int Quantity)[] allocations)
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var mapped = allocations
            .Select(a => new ItemContainerAllocation(a.Id, a.Name, a.Quantity))
            .ToList();
        return new InventorySnapshot(item, total, mapped.Sum(a => a.Quantity), mapped);
    }
}
