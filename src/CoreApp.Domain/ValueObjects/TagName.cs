namespace CoreApp.Domain.ValueObjects;

/// <summary>
/// Represents a user-facing tag name and its stable lookup form.
/// </summary>
public sealed record TagName
{
    /// <summary>
    /// Creates a tag name. A leading hashtag is presentation syntax and is removed
    /// before the value is stored; comparison uses the invariant uppercase form.
    /// </summary>
    /// <param name="value">The tag text, optionally prefixed with <c>#</c>.</param>
    /// <exception cref="ArgumentException">Thrown when the tag is empty after normalization.</exception>
    public TagName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();
        if (trimmed.StartsWith('#'))
        {
            trimmed = trimmed[1..].Trim();
        }

        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Tag name cannot be empty.", nameof(value));
        }

        Value = trimmed;
        NormalizedValue = trimmed.ToUpperInvariant();
    }

    /// <summary>
    /// Gets the display form without the optional leading hashtag.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the invariant form used for uniqueness and lookup.
    /// </summary>
    public string NormalizedValue { get; }

    public override string ToString() => Value;
}
