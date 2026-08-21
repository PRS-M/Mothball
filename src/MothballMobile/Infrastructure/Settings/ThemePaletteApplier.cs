using MothballMobile.Resources.Styles.Palettes;

namespace MothballMobile.Infrastructure.Settings;

public static class ThemePaletteApplier
{
    private static readonly IReadOnlyDictionary<ThemePalette, Func<AppTheme, ResourceDictionary>> PaletteFactories =
        new Dictionary<ThemePalette, Func<AppTheme, ResourceDictionary>>
        {
            [ThemePalette.OliveWorkshop] = static mode => mode == AppTheme.Dark ? new OliveWorkshopDark() : new OliveWorkshopLight(),
            [ThemePalette.BlueprintLedger] = static mode => mode == AppTheme.Dark ? new BlueprintLedgerDark() : new BlueprintLedgerLight(),
            [ThemePalette.TerracottaArchive] = static mode => mode == AppTheme.Dark ? new TerracottaArchiveDark() : new TerracottaArchiveLight(),
            [ThemePalette.SaffronUtility] = static mode => mode == AppTheme.Dark ? new SaffronUtilityDark() : new SaffronUtilityLight(),
            [ThemePalette.CoastalInventory] = static mode => mode == AppTheme.Dark ? new CoastalInventoryDark() : new CoastalInventoryLight(),
            [ThemePalette.BerryArchive] = static mode => mode == AppTheme.Dark ? new BerryArchiveDark() : new BerryArchiveLight(),
        };

    public static void Apply(ResourceDictionary resources, ThemePalette palette, AppTheme mode)
    {
        var factory = PaletteFactories.TryGetValue(palette, out var value)
            ? value
            : PaletteFactories[ThemePalette.BlueprintLedger];

        foreach (var activePalette in resources.MergedDictionaries.Where(static dictionary => dictionary is IPaletteResourceDictionary).ToList())
        {
            resources.MergedDictionaries.Remove(activePalette);
        }

        resources.MergedDictionaries.Add(factory(mode));
    }
}
