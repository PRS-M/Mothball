namespace MothballMobile.Infrastructure.Presentation.Errors;

public interface IAppErrorPresenter
{
    string? Message { get; }
    bool IsVisible { get; }

    void Show(string message);
    void Dismiss();
}