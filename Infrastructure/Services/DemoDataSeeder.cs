using System;
using CoreApp.Interfaces;
using CoreApp.Utilities;
using Infrastructure.Interfaces;
using MothballMobile.Infrastructure.DatabaseModels;

namespace MothballMobile.Infrastructure;

/// <summary>
/// Development-only data seeder to populate the SQLite database with demo content.
/// Keeps seeding logic in infrastructure to avoid leaking persistence details into UI.
/// </summary>
public class DemoDataSeeder
{
    private readonly IRepository<DbContainer> _containers;
    private readonly IRepository<DbImage> _photos;
    private readonly IFileHandler _fileHandler;

    public DemoDataSeeder(IRepository<DbContainer> containers, IRepository<DbImage> photos, IFileHandler fileHandler)
    {
        _containers = containers;
        _photos = photos;
        _fileHandler = fileHandler;
    }

    /// <summary>
    /// Ensures at least <paramref name="minContainers"/> containers exist, optionally with one photo each.
    /// </summary>
    public async Task EnsureContainersAsync(int minContainers = 25, bool withPhotos = true)
    {
        await _containers.InitializeAsync();
        await _photos.InitializeAsync();

        var existing = await _containers.GetAllAsync();
        if (existing.Count >= minContainers) return;

        int toCreate = minContainers - existing.Count;

        for (int i = 0; i < toCreate; i++)
        {
            var id = Guid.NewGuid();
            var container = new DbContainer
            {
                ContainerId = id,
                Name = $"Container {existing.Count + i + 1}",
                Notes = $"Seeded notes for container {id.ToString()[..8]}"
            };

            await _containers.InsertAsync(container);

            if (withPhotos)
            {
                var img = new DbImage
                {
                    // ImageId auto-generated
                    OwnerUniqueId = id,
                    ImageData = null // keep on disk only; UI will fallback if file isn't present
                };

                await _photos.InsertAsync(img);
                await _fileHandler.CopyFileFromRawToAppDataAsync("container.png", img.FileName, Constants.PathToContainerPhotos);
            }
        }
    }
}
