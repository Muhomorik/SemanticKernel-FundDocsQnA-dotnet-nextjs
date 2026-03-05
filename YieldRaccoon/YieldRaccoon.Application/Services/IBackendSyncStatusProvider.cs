using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Provides an observable stream of backend sync status notifications for UI display.
/// </summary>
/// <remarks>
/// Active when the <c>DualWrite</c> database provider is configured.
/// The status bar subscribes to <see cref="Status"/> to show sync success/error messages.
/// </remarks>
public interface IBackendSyncStatusProvider
{
    /// <summary>
    /// Emits the latest backend sync status after each sync attempt.
    /// </summary>
    IObservable<BackendSyncStatus> Status { get; }
}
