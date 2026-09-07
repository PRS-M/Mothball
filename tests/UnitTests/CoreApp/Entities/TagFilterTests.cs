using CoreApp.Application.Contracts.Tags;

namespace Mothball.Tests.Unit.Core.Entities;

[TestFixture]
public class TagFilterTests
{
    [Test]
    public void NormalizedNames_RemovesDuplicatesAndHashtags()
    {
        var filter = new TagFilter(
            TagTargetType.Item,
            ["#Winter", "winter", " garage "]);

        Assert.That(filter.NormalizedNames, Is.EqualTo(new[] { "WINTER", "GARAGE" }));
    }
}
