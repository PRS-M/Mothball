using System;
using System.Threading;
using System.Threading.Tasks;

namespace MothballMobile.Infrastructure.Resilience;

public interface IDebouncer
{
    Task DebounceAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
