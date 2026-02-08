---
name: dotnet-reactive-patterns
description: Rx.NET reactive programming patterns for .NET (.csproj, C#). Use when working with IObservable, CompositeDisposable, ObserveOn, or reactive streams. DO NOT use for web frameworks (Next.js, React, JavaScript, TypeScript, Node.js).
allowed-tools: Read, Edit, Write, Glob, Grep
---

# Rx.NET Reactive Patterns

## Rules

1. **Entities do NOT publish events** - Application services publish after persistence
2. **CompositeDisposable for subscriptions** - Always use `_disposables` field
3. **DisposeWith pattern** - `.Subscribe(...).DisposeWith(_disposables)`
4. **ObserveOn for thread marshalling** - `ObserveOn(_uiScheduler)` before UI updates

## Subscription Management

```csharp
public class MyService : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public MyService(IEventStream events, IScheduler uiScheduler)
    {
        events.OrderCreated
            .ObserveOn(uiScheduler)
            .Subscribe(OnOrderCreated)
            .DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
```

## DisposeWith Extension

```csharp
internal static class DisposableExtensions
{
    public static T DisposeWith<T>(this T disposable, CompositeDisposable composite)
        where T : IDisposable
    {
        composite.Add(disposable);
        return disposable;
    }
}
```

## Event Publishing (Application Services)

```csharp
public class OrderApplicationService : IDisposable
{
    private readonly Subject<OrderCreatedEvent> _orderCreated = new();
    public IObservable<OrderCreatedEvent> OrderCreated => _orderCreated.AsObservable();

    public async Task<Result<OrderId>> CreateOrderAsync(CustomerId customerId)
    {
        var order = Order.CreateNew(customerId, "USD");
        await _repository.SaveAsync(order);  // Persist first

        // Publish AFTER successful save
        _orderCreated.OnNext(new OrderCreatedEvent(order.Id, customerId));

        return Result.Success(order.Id);
    }

    public void Dispose() => _orderCreated.Dispose();
}
```

## Schedulers

| Scheduler | Use |
|-----------|-----|
| `DispatcherScheduler.Current` | WPF UI thread |
| `Scheduler.Default` | Background thread pool |
| `Scheduler.CurrentThread` | Synchronous |

## Error Handling

```csharp
// ❌ BAD
.Subscribe(onNext: Handle, onError: ex => _logger.Error(ex, "Failed"))

// ✅ GOOD
.Subscribe(onNext: Handle, onError: ex => _logger.Error(ex))
```

## Checklist

- [ ] Entities do NOT publish events directly
- [ ] Application services publish events AFTER persistence
- [ ] All subscriptions in CompositeDisposable
- [ ] `.DisposeWith(_disposables)` pattern
- [ ] `ObserveOn(_uiScheduler)` for UI-bound properties
- [ ] `_disposables.Dispose()` in Dispose method
- [ ] Events are immutable records
