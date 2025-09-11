using System;

namespace MothballMobile.Infrastructure;

public interface IDebouncer
{
    void Debounce(Action action);
}
