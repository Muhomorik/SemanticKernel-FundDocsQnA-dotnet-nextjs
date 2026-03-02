using System.Reactive.Linq;
using System.Reactive.Subjects;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Exposes backend sync status as an observable stream for UI consumption.
/// </summary>
/// <remarks>
/// Wraps a shared <see cref="Subject{T}"/> that the DualWrite decorators push status into.
/// Registered as a singleton so all decorators and the ViewModel share the same stream.
/// </remarks>
public class BackendSyncStatusProvider : IBackendSyncStatusProvider
{
    private readonly Subject<BackendSyncStatus> _subject;

    public BackendSyncStatusProvider(Subject<BackendSyncStatus> subject)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
    }

    /// <inheritdoc />
    public IObservable<BackendSyncStatus> Status => _subject.AsObservable();
}

/// <summary>
/// No-op implementation of <see cref="IBackendSyncStatusProvider"/> for non-DualWrite modes.
/// </summary>
public class NullBackendSyncStatusProvider : IBackendSyncStatusProvider
{
    /// <inheritdoc />
    public IObservable<BackendSyncStatus> Status => Observable.Empty<BackendSyncStatus>();
}
