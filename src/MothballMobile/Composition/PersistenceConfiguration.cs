namespace MothballMobile.Composition;

public static class PersistenceConfiguration
{
    public const string BackendKey = "Persistence:Backend";
    public const string SqliteBackend = "SQLite";
    public const string JsonBackend = "Json";

    public static bool UseJsonBackend(string? backend)
        => string.Equals(backend, JsonBackend, StringComparison.OrdinalIgnoreCase)
           || string.Equals(backend, "JsonOperationalStore", StringComparison.OrdinalIgnoreCase);
}
