using System;
using CoreApp.Interfaces;
using SQLite;

namespace MothballMobile.Infrastructure;

public interface IRepositoryExtended<T> : IRepository<T> where T : new()
{
    /// <summary>
    /// Provides low-level access to sqlite-net's query builder for advanced scenarios.
    /// Prefer high-level methods in this interface for common cases.
    /// </summary>
    AsyncTableQuery<T> Table();
}
