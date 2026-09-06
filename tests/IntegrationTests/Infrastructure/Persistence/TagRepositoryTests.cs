using CoreApp.Application.Contracts.Tags;
using CoreApp.Domain.ValueObjects;
using Infrastructure.Services.Database;
using Infrastructure.Services.Repositories;

namespace Mothball.Tests.Integration.Infrastructure.Persistence;

[TestFixture]
public class TagRepositoryTests
{
    private string dbPath = null!;
    private MothballDatabase database = null!;

    [SetUp]
    public async Task SetUp()
    {
        dbPath = Path.Combine(Path.GetTempPath(), $"mothball-tags-{Guid.NewGuid():N}.db");
        database = new MothballDatabase(dbPath);
        await database.InitializeAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await database.DisposeAsync();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    [Test]
    public async Task GetOrCreateAsync_ReusesNormalizedTag()
    {
        var repository = new TagRepository(database);

        var first = await repository.GetOrCreateAsync(new TagName("#Winter"));
        var second = await repository.GetOrCreateAsync(new TagName("winter"));

        Assert.That(second.TagId, Is.EqualTo(first.TagId));
    }

    [Test]
    public async Task AssignAndRemoveAsync_ManagesItemAssignmentIdempotently()
    {
        var repository = new TagRepository(database);
        var tag = await repository.GetOrCreateAsync(new TagName("winter"));
        var itemId = Guid.NewGuid();

        await repository.AssignAsync(tag.TagId, TagTargetType.Item, itemId);
        await repository.AssignAsync(tag.TagId, TagTargetType.Item, itemId);

        var assigned = await repository.GetForTargetAsync(TagTargetType.Item, itemId);
        Assert.That(assigned.Select(item => item.TagId), Is.EqualTo(new[] { tag.TagId }));

        await repository.RemoveAsync(tag.TagId, TagTargetType.Item, itemId);

        Assert.That(await repository.GetForTargetAsync(TagTargetType.Item, itemId), Is.Empty);
    }
}
