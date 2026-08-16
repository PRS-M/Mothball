using CoreApp.Entities.Shared;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;

namespace Infrastructure.Services.Repositories;

public class ImageRepository : IImageRepository
{
    private readonly IRepository<DbImage> photos;

    public ImageRepository(IRepository<DbImage> photos)
    {
        this.photos = photos;
    }

    public async Task InsertAsync(ImageItem imageItem, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(imageItem);
        await photos.InsertAsync(imageItem.ToDb(ownerId));
    }

    public async Task UpdateAsync(ImageItem image, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(image);
        await photos.UpdateAsync(image.ToDb(ownerId));
    }

    public async Task DeleteAsync(Guid imageId, Guid ownerId)
    {
        var existing = await photos
            .WhereAsync(p => p.ImageId == imageId && p.OwnerUniqueId == ownerId)
            .ConfigureAwait(false);

        foreach (var image in existing)
        {
            await photos.DeleteAsync(image).ConfigureAwait(false);
        }
    }
}
