using System.Collections;
using CoreApp.Utilities;

namespace Mothball.Tests.Unit.Utilities;

[TestFixture]
public sealed class EnumValuesTests
{
    [Test]
    public void CreateReadOnly_ReturnsAllEnumValues_AsAnImmutableIList()
    {
        var values = EnumValues.CreateReadOnly<TestFilter>();

        Assert.That(values, Is.EqualTo(new[] { TestFilter.All, TestFilter.Unassigned, TestFilter.Assigned }));
        Assert.That(values, Is.InstanceOf<IList>());
        Assert.That(
            () => ((IList)values).Add(TestFilter.All),
            Throws.TypeOf<NotSupportedException>());
    }

    private enum TestFilter
    {
        All,
        Unassigned,
        Assigned,
    }
}