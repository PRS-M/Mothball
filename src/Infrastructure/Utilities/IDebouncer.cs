using System;

namespace Infrastructure.Utilities;

public interface IDebouncer
{
    void Debounce(Action action);
}
