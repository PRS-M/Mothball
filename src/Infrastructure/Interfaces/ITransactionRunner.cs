namespace Infrastructure.Interfaces;

using Infrastructure.Services.DatabaseModels;

public interface ITransactionRunner
{
    Task RunAsync(Action<ITransactionalDeleteScope> action);
}

public interface ITransactionalDeleteScope
{
    void DeleteImage(Guid imageId, Guid ownerId);
    void DeleteImagesByOwner(Guid ownerId);
    void DeleteRelationsByContainer(Guid containerId);
    void DeleteRelationsByItem(Guid itemId);
    void UpdateContainer(DbContainer container);
    void UpdateItem(DbItem item);
    void DeleteContainer(Guid containerId);
    void DeleteItem(Guid itemId);
}
