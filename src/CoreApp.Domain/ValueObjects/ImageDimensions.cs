namespace CoreApp.Domain.ValueObjects;

/// <summary>
/// Represents image dimensions and their display aspect ratio.
/// </summary>
public readonly record struct ImageDimensions(int Width, int Height)
{
    public double AspectRatio => Height > 0 ? (double)Width / Height : 16d / 9d;
}
