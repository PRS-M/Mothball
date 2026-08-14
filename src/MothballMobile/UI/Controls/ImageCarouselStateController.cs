using System.Collections;

namespace MothballMobile.UI.Controls;

internal sealed class ImageCarouselStateController
{
    private readonly Dictionary<string, double> aspectRatioCache = new(StringComparer.Ordinal);
    private int sizingRequestId;

    public int NextSizingRequestId() => ++sizingRequestId;

    public bool IsCurrentSizingRequest(int requestId, IEnumerable? imagePaths, int position, string imagePath)
        => requestId == sizingRequestId && string.Equals(GetImagePathAt(imagePaths, position), imagePath, StringComparison.Ordinal);

    public bool TryGetAspectRatio(string imagePath, out double aspectRatio)
        => aspectRatioCache.TryGetValue(imagePath, out aspectRatio);

    public void CacheAspectRatio(string imagePath, double aspectRatio)
        => aspectRatioCache[imagePath] = aspectRatio;

    public ImageCarouselCounterState GetCounterState(IEnumerable? imagePaths, bool showCounter, int position)
    {
        var total = CountImages(imagePaths);

        if (!showCounter || total <= 1)
        {
            return new ImageCarouselCounterState(IsVisible: false, Text: string.Empty, Position: position);
        }

        var normalizedPosition = position;
        if (normalizedPosition < 0 || normalizedPosition >= total)
        {
            normalizedPosition = 0;
        }

        return new ImageCarouselCounterState(
            IsVisible: true,
            Text: $"{normalizedPosition + 1}/{total}",
            Position: normalizedPosition);
    }

    public static int CountImages(IEnumerable? source)
    {
        if (source is null)
        {
            return 0;
        }

        var count = 0;
        foreach (var _ in source)
        {
            count++;
        }

        return count;
    }

    public static string? GetImagePathAt(IEnumerable? imagePaths, int position)
    {
        if (position < 0 || imagePaths is null)
        {
            return null;
        }

        var index = 0;
        foreach (var item in imagePaths)
        {
            if (index == position)
            {
                return item as string;
            }

            index++;
        }

        return null;
    }
}
