using Microsoft.Extensions.Logging;

namespace MothballMobile.Infrastructure.Diagnostics;

internal static class MauiLogger
{
    public static ILogger? For<T>(Element? element = null)
        => Resolve(element)?.CreateLogger(typeof(T).FullName ?? typeof(T).Name);

    public static ILogger? For(Type categoryType, Element? element = null)
        => Resolve(element)?.CreateLogger(categoryType.FullName ?? categoryType.Name);

    public static ILogger? For(string categoryName, Element? element = null)
        => Resolve(element)?.CreateLogger(categoryName);

    private static ILoggerFactory? Resolve(Element? element)
        => element?.Handler?.MauiContext?.Services.GetService<ILoggerFactory>()
            ?? Application.Current?.Handler?.MauiContext?.Services.GetService<ILoggerFactory>();
}
