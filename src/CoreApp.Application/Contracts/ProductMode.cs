namespace CoreApp.Application.Contracts;

/// <summary>Describes the product experience selected for a workspace.</summary>
public enum ProductMode
{
    PersonalStorageSimple,
    PersonalStorageAdvanced,
    WmsExperimental,
}

/// <summary>Defines capabilities exposed by a product experience.</summary>
public sealed record ProductCapabilities(
    bool CanEditQuantities,
    bool CanUseOperationalMovements,
    bool CanUseExperimentalWms);

/// <summary>Maps a product mode to stable application capabilities.</summary>
public static class ProductModePolicy
{
    /// <summary>Gets the capabilities for a product mode.</summary>
    public static ProductCapabilities GetCapabilities(ProductMode mode)
        => mode switch
        {
            ProductMode.PersonalStorageSimple => new(false, false, false),
            ProductMode.PersonalStorageAdvanced => new(true, false, false),
            ProductMode.WmsExperimental => new(true, true, true),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown product mode."),
        };
}
