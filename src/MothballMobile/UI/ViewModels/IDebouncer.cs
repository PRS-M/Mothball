using System;

namespace MothballMobile.UI.ViewModels;

public interface IDebouncer
{
    void Debounce(Action action);
}
