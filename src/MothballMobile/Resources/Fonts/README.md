# Fonts

This app uses Font Awesome for toolbar icons.

Currently included:
- Font Awesome 7 Free Regular (alias: `FontAwesome` and temporarily `FontAwesomeSolid`)

To enable the Solid icons (recommended):
1. Download the Font Awesome Free Solid font file (e.g., `Font Awesome 7 Free-Solid-900.otf`).
2. Copy it into this folder: `Resources/Fonts/`.
3. Update `MauiProgram.cs` font registration to point `FontAwesomeSolid` at the Solid font file, e.g.:

```csharp
fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FontAwesomeSolid");
```

4. Rebuild and run. Toolbar icons that use `FontAwesomeSolid` will then render using the solid glyphs.

Notes:
- .NET MAUI automatically sets `Build Action: MauiFont` for files under `Resources/Fonts/`.
- Verify the glyph codepoints you use exist in the selected face. `F067` (plus) and `F1F8` (trash) exist in the Solid face.
