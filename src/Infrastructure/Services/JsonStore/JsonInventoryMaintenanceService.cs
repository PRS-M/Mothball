namespace Infrastructure.Services.JsonStore;

using CoreApp.Application.Abstractions.DomainEvents;
using CoreApp.Domain.Events;

public sealed class JsonInventoryMaintenanceService : IInventoryMaintenanceService
{
    private readonly JsonInventoryStore store;
    private readonly IFileHandler? files;
    private readonly IInventoryChangeTracker? inventoryChanges;
    private readonly IDomainEventDispatcher? domainEvents;

    public JsonInventoryMaintenanceService(
        JsonInventoryStore store,
        IInventoryChangeTracker? inventoryChanges = null,
        IFileHandler? files = null,
        IDomainEventDispatcher? domainEvents = null)
    {
        this.store = store;
        this.inventoryChanges = inventoryChanges;
        this.files = files;
        this.domainEvents = domainEvents;
    }

    public Task ReplaceAllPhotosWithSharedAssetsAsync(IProgress<MaintenanceProgress>? progress = null, CancellationToken cancellationToken = default)
        => store.ReplaceAllPhotosWithSharedAssetsAsync(
            files ?? throw new InvalidOperationException("A file handler is required for photo maintenance."),
            progress,
            cancellationToken);

    public async Task ResetAllDataAsync(IProgress<MaintenanceProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        await store.ResetAllDataAsync(
            files ?? throw new InvalidOperationException("A file handler is required for data reset."),
            progress,
            cancellationToken);
        if (domainEvents is not null)
        {
            await domainEvents.DispatchAsync([new InventoryReset()]).ConfigureAwait(false);
        }
        else
        {
            inventoryChanges?.MarkChanged();
        }
    }

    /// <inheritdoc />
    public Task<bool> TryRecoverAsync() => store.TryRecoverAsync();

    /// <inheritdoc />
    public Task<bool> TryRollbackLastCommitAsync() => store.TryRollbackLastCommitAsync();
}
