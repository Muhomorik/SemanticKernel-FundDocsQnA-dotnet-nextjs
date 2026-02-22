using System.Reactive.Disposables;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// Extension methods for IDisposable.
/// </summary>
internal static class DisposableExtensions
{
    /// <summary>
    /// Adds the disposable to a CompositeDisposable for lifecycle management.
    /// </summary>
    public static T DisposeWith<T>(this T disposable, CompositeDisposable composite) where T : IDisposable
    {
        composite.Add(disposable);
        return disposable;
    }
}