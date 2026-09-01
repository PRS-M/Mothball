using CoreApp.Application.Utilities;

namespace Mothball.Tests.Unit.Application.Utilities;

[TestFixture]
public sealed class InventoryChangeTrackerTests
{
    [Test]
    public void MarkChanged_AdvancesRevision()
    {
        var tracker = new InventoryChangeTracker();

        tracker.MarkChanged();
        tracker.MarkChanged();

        Assert.That(tracker.Revision, Is.EqualTo(2));
    }
}
