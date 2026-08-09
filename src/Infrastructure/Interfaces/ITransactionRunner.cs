namespace Infrastructure.Interfaces;

public interface ITransactionRunner
{
    Task RunAsync(Action<ITransactionalDeleteScope> action);
}

public interface ITransactionalDeleteScope
{
    void DeleteImagesByOwner(Guid ownerId);
    void DeleteRelationsByContainer(Guid containerId);
    void DeleteRelationsByItem(Guid itemId);
    void DeleteContainer(Guid containerId);
    void DeleteItem(Guid itemId);
}
