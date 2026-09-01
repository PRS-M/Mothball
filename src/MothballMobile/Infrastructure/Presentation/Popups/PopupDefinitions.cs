using System.Collections.Generic;

namespace MothballMobile.Infrastructure.Presentation.Popups;

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
    public string InvalidNumberMessage => LocalizationManager.Current.Format("Enter a number between {0} and {1}.", Min, Max);

    public string OutOfRangeMessage => LocalizationManager.Current.Format("Value must be between {0} and {1}.", Min, Max);
}
