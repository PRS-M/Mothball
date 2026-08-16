using CoreApp.Entities.Inventory;
using CoreApp.Contracts;

namespace Mothball.Tests.Unit.Core.Features.Inventory;

[TestFixture]
public sealed class ItemInventoryWithdrawalPlannerTests
{
    private static readonly Guid BoxId = Guid.NewGuid();
    private static readonly Guid DrawerId = Guid.NewGuid();

    [Test]
    public void GetPreferredAllocation_ReturnsSourceContainerWhenItHasStock()
    {
        var allocations = new[]
        {
            new ItemContainerAllocation(BoxId, "Box", 3),
            new ItemContainerAllocation(DrawerId, "Drawer", 4),
        };

        var preferred = ItemInventoryWithdrawalPlanner.GetPreferredAllocation(allocations, DrawerId);

        Assert.That(preferred, Is.EqualTo(allocations[1]));
    }

    [Test]
    public void GetPreferredAllocation_WhenSourceIsUnavailable_ReturnsNull()
    {
        var preferred = ItemInventoryWithdrawalPlanner.GetPreferredAllocation(
            [new ItemContainerAllocation(BoxId, "Box", 3)],
            DrawerId);

        Assert.That(preferred, Is.Null);
    }

    [Test]
    public void GetRequiredAssignedWithdrawal_UsesTotalReductionEvenWhenRequestedTotalExceedsAssigned()
    {
        var required = ItemInventoryWithdrawalPlanner.GetRequiredAssignedWithdrawal(
            currentTotal: 10,
            requestedTotal: 8,
            assignedQuantity: 5);

        Assert.That(required, Is.EqualTo(2));
    }

    [Test]
    public void GetRequiredAssignedWithdrawal_WhenReductionExceedsAssigned_WithdrawsAllAssigned()
    {
        var required = ItemInventoryWithdrawalPlanner.GetRequiredAssignedWithdrawal(
            currentTotal: 10,
            requestedTotal: 2,
            assignedQuantity: 3);

        Assert.That(required, Is.EqualTo(3));
    }

    [Test]
    public void Plan_CarriesSelectedWithdrawalAcrossContainersAndCapsAtAvailableAssignedStock()
    {
        var allocations = new[]
        {
            new ItemContainerAllocation(BoxId, "Box", 3),
            new ItemContainerAllocation(DrawerId, "Drawer", 4),
        };

        var plan = ItemInventoryWithdrawalPlanner.Plan(
            currentTotal: 10,
            allocations,
            assignedWithdrawals:
            [
                new ItemAllocationWithdrawal(BoxId, 5),
                new ItemAllocationWithdrawal(DrawerId, 2),
            ],
            unassignedWithdrawals: []);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Allocations.Single(a => a.ContainerId == BoxId).Quantity, Is.Zero);
            Assert.That(plan.Allocations.Single(a => a.ContainerId == DrawerId).Quantity, Is.EqualTo(2));
            Assert.That(plan.AssignedQuantity, Is.EqualTo(2));
            Assert.That(plan.TotalQuantity, Is.EqualTo(10));
            Assert.That(plan.UnassignedQuantity, Is.EqualTo(8));
        });
    }

    [Test]
    public void Plan_WhenCarryIsNotCompleted_RejectsIncompleteWorkflow()
    {
        var allocations = new[]
        {
            new ItemContainerAllocation(BoxId, "Box", 3),
            new ItemContainerAllocation(DrawerId, "Drawer", 4),
        };

        Assert.Throws<InvalidOperationException>(() => ItemInventoryWithdrawalPlanner.Plan(
            currentTotal: 10,
            allocations,
            assignedWithdrawals: [new ItemAllocationWithdrawal(BoxId, 5)],
            unassignedWithdrawals: []));
    }

    [Test]
    public void Plan_AdditionalAssignedWithdrawalCreatesUnassignedStockAtRequestedTarget()
    {
        var allocations = new[]
        {
            new ItemContainerAllocation(BoxId, "Box", 5),
            new ItemContainerAllocation(DrawerId, "Drawer", 5),
        };

        var plan = ItemInventoryWithdrawalPlanner.Plan(
            currentTotal: 10,
            allocations,
            assignedWithdrawals: [new ItemAllocationWithdrawal(BoxId, 4)],
            unassignedWithdrawals: [],
            requestedTotal: 7);

        Assert.Multiple(() =>
        {
            Assert.That(plan.TotalQuantity, Is.EqualTo(7));
            Assert.That(plan.AssignedQuantity, Is.EqualTo(6));
            Assert.That(plan.UnassignedQuantity, Is.EqualTo(1));
            Assert.That(plan.DeleteItem, Is.False);
        });
    }

    [Test]
    public void Plan_ZeroUnassignedWithdrawalStopsWithoutDeletingItem()
    {
        var plan = ItemInventoryWithdrawalPlanner.Plan(
            currentTotal: 7,
            allocations: [new ItemContainerAllocation(BoxId, "Box", 6)],
            assignedWithdrawals: [],
            unassignedWithdrawals: [0, 1]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.TotalQuantity, Is.EqualTo(7));
            Assert.That(plan.UnassignedQuantity, Is.EqualTo(1));
            Assert.That(plan.DeleteItem, Is.False);
        });
    }

    [Test]
    public void Plan_ExhaustingUnassignedStockMarksItemForDeletionInsteadOfPersistingZero()
    {
        var plan = ItemInventoryWithdrawalPlanner.Plan(
            currentTotal: 2,
            allocations: [],
            assignedWithdrawals: [],
            unassignedWithdrawals: [1, 1]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.TotalQuantity, Is.Zero);
            Assert.That(plan.UnassignedQuantity, Is.Zero);
            Assert.That(plan.DeleteItem, Is.True);
        });
    }
}
