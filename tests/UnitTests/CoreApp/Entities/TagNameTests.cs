using CoreApp.Domain.ValueObjects;

namespace Mothball.Tests.Unit.Core.Entities;

[TestFixture]
public class TagNameTests
{
    [TestCase("winter", "winter", "WINTER")]
    [TestCase("  #Winter  ", "Winter", "WINTER")]
    [TestCase("garage shelf", "garage shelf", "GARAGE SHELF")]
    public void Constructor_NormalizesDisplayAndLookupValues(
        string input,
        string expectedValue,
        string expectedNormalizedValue)
    {
        var tag = new TagName(input);

        Assert.That(tag.Value, Is.EqualTo(expectedValue));
        Assert.That(tag.NormalizedValue, Is.EqualTo(expectedNormalizedValue));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("#")]
    public void Constructor_RejectsEmptyNames(string input)
    {
        Assert.Throws<ArgumentException>(() => new TagName(input));
    }
}
