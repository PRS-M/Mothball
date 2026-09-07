using CoreApp.Domain.Entities.TagAggregate;
using CoreApp.Domain.ValueObjects;

namespace Mothball.Tests.Unit.Core.Entities;

[TestFixture]
public class TagTests
{
    [Test]
    public void Constructor_CreatesTagWithNormalizedName()
    {
        var tagId = Guid.NewGuid();

        var tag = new Tag(tagId, "#Winter");

        Assert.That(tag.TagId, Is.EqualTo(tagId));
        Assert.That(tag.Name.Value, Is.EqualTo("Winter"));
        Assert.That(tag.Name.NormalizedValue, Is.EqualTo("WINTER"));
    }

    [Test]
    public void Rename_ReplacesName()
    {
        var tag = new Tag(Guid.NewGuid(), "winter");

        tag.Rename(new TagName("summer"));

        Assert.That(tag.Name.Value, Is.EqualTo("summer"));
    }

    [Test]
    public void Constructor_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new Tag(Guid.Empty, "winter"));
    }
}
