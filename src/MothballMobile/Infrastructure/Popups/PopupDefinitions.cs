using System.Collections.Generic;

namespace MothballMobile.Infrastructure.Popups;

public sealed record AlertPopupDefinition(
    string Title,
    string Message,
    string Cancel = "OK");

public sealed record ConfirmationPopupDefinition(
    string Title,
    string Message,
    string Accept,
    string Cancel = "Cancel");

public sealed record PopupOption<T>(
    string Label,
    T Value);

public sealed record OptionPickerPopupDefinition<T>(
    string Title,
    string Cancel,
    IReadOnlyList<PopupOption<T>> Options);

public sealed record NumberPickerPopupDefinition(
    string Title,
    int Min,
    int Max,
    int InitialValue,
    string Accept = "Set",
    string Cancel = "Cancel",
    string Placeholder = "Enter quantity",
    string Message = "")
{
    public string InvalidNumberMessage => $"Enter a number between {Min} and {Max}.";

    public string OutOfRangeMessage => $"Value must be between {Min} and {Max}.";
}
