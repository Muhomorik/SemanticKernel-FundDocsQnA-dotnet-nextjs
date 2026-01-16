# Reactive Patterns with Rx.NET

Guide to using Reactive Extensions (Rx.NET) for domain events, IDisposable management, and reactive programming in DDD applications.

## Domain Events and Reactive Patterns

### Keep Entities Free of Event Publishers

**Principle:** Entities hold state and invariants. They do NOT publish events directly. Application services publish events after successful persistence.

```csharp
// ❌ BAD: Entity publishes events
public class Order
{
    private readonly Subject<OrderSubmittedEvent> _submitted = new();
    public IObservable<OrderSubmittedEvent> Submitted => _submitted;

    public void Submit()
    {
        Status = OrderStatus.Submitted;
        _submitted.OnNext(new OrderSubmittedEvent(Id)); // ❌ NO
    }
}

// ✅ GOOD: Entity is pure, Application service publishes
public class Order
{
    public Result Submit()
    {
        if (!_items.Any())
            return Result.Failure("Cannot submit empty order");

        Status = OrderStatus.Submitted;
        return Result.Success(); // No event publishing here
    }
}

public class OrderApplicationService
{
    private readonly IOrderRepository _repository;
    private readonly Subject<OrderSubmittedEvent> _orderSubmitted = new();

    public IObservable<OrderSubmittedEvent> OrderSubmitted => _orderSubmitted.AsObservable();

    public async Task<Result> SubmitOrderAsync(OrderId orderId)
    {
        var order = await _repository.GetAsync(orderId);
        var result = order.Submit();

        if (!result.IsSuccess)
            return result;

        // Persist first
        await _repository.SaveAsync(order);

        // Publish event AFTER successful persistence
        _orderSubmitted.OnNext(new OrderSubmittedEvent(order.Id, order.CustomerId));

        return Result.Success();
    }
}
```

### Why Application Services Publish Events

1. **Ensures persistence:** Events only published after successful save
2. **Maintains purity:** Domain layer has no infrastructure dependencies
3. **Testability:** Can test entity logic without mocking event streams
4. **Transaction boundaries:** Events published after transaction commits

## Event Contracts

Events are immutable messages representing meaningful changes.

```csharp
// Event as readonly record
public readonly record struct OrderSubmittedEvent(
    OrderId OrderId,
    CustomerId CustomerId,
    DateTimeOffset SubmittedAt);

// Event with richer data
public sealed record OrderItemAddedEvent
{
    public required OrderId OrderId { get; init; }
    public required ProductId ProductId { get; init; }
    public required int Quantity { get; init; }
    public required Money UnitPrice { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
```

**Guidelines:**

- Use `readonly record struct` for small events (less GC pressure)
- Use `sealed record` for larger events with many properties
- Events are immutable (init-only properties)
- Include timestamp and relevant IDs
- Name events past tense (OrderSubmitted, not SubmitOrder)

## Application Service Event Publishing

### Pattern: Publish After Persistence

```csharp
public class OrderApplicationService : IDisposable
{
    private readonly IOrderRepository _repository;
    private readonly ILogger _logger;

    // Event publishers (Subject)
    private readonly Subject<OrderCreatedEvent> _orderCreated = new();
    private readonly Subject<OrderSubmittedEvent> _orderSubmitted = new();
    private readonly Subject<OrderCancelledEvent> _orderCancelled = new();

    // Expose as IObservable (hide ability to call OnNext)
    public IObservable<OrderCreatedEvent> OrderCreated => _orderCreated.AsObservable();
    public IObservable<OrderSubmittedEvent> OrderSubmitted => _orderSubmitted.AsObservable();
    public IObservable<OrderCancelledEvent> OrderCancelled => _orderCancelled.AsObservable();

    public async Task<Result<OrderId>> CreateOrderAsync(CustomerId customerId)
    {
        var order = Order.CreateNew(customerId, "USD");

        await _repository.SaveAsync(order);

        // Publish after save
        _orderCreated.OnNext(new OrderCreatedEvent(order.Id, customerId, DateTimeOffset.UtcNow));

        return Result.Success(order.Id);
    }

    public void Dispose()
    {
        _orderCreated.Dispose();
        _orderSubmitted.Dispose();
        _orderCancelled.Dispose();
    }
}
```

### Pattern: Error Handling in Event Publishers

```csharp
public async Task<Result> SubmitOrderAsync(OrderId orderId)
{
    try
    {
        var order = await _repository.GetAsync(orderId);
        if (order == null)
            return Result.Failure("Order not found");

        var result = order.Submit();
        if (!result.IsSuccess)
            return result;

        await _repository.SaveAsync(order);

        // Publish event
        _orderSubmitted.OnNext(new OrderSubmittedEvent(order.Id, order.CustomerId));

        return Result.Success();
    }
    catch (Exception ex)
    {
        _logger.Error(ex);
        // Do NOT publish event on failure
        return Result.Failure("Failed to submit order");
    }
}
```

## Event Aggregation

Event aggregators compose multiple event streams into unified observables.

```csharp
public interface IOrderEventAggregator
{
    IObservable<OrderCreatedEvent> OrderCreated { get; }
    IObservable<OrderSubmittedEvent> OrderSubmitted { get; }
    IObservable<OrderEvent> AllOrderEvents { get; }
}

public class OrderEventAggregator : IOrderEventAggregator, IDisposable
{
    private readonly OrderApplicationService _orderService;
    private readonly ILogger _logger;
    private readonly CompositeDisposable _disposables = new();

    public IObservable<OrderCreatedEvent> OrderCreated { get; }
    public IObservable<OrderSubmittedEvent> OrderSubmitted { get; }
    public IObservable<OrderEvent> AllOrderEvents { get; }

    public OrderEventAggregator(
        ILogger logger,
        OrderApplicationService orderService)
    {
        _logger = logger;
        _orderService = orderService;

        // Forward individual streams
        OrderCreated = _orderService.OrderCreated;
        OrderSubmitted = _orderService.OrderSubmitted;

        // Merge into unified stream
        AllOrderEvents = Observable.Merge(
            OrderCreated.Select(e => new OrderEvent("Created", e.OrderId)),
            OrderSubmitted.Select(e => new OrderEvent("Submitted", e.OrderId))
        );

        // Log all events (side effect subscription)
        AllOrderEvents
            .Subscribe(e => _logger.Info("Order event: {0} - {1}", e.Type, e.OrderId))
            .DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}

public readonly record struct OrderEvent(string Type, OrderId OrderId);
```

**Guidelines:**

- Aggregators compose and forward streams, no side effects in composition
- Use `CompositeDisposable` to manage subscriptions
- Log errors by logging the exception object (see logging-conventions.md)
- Keep aggregators stateless (just stream composition)

## IDisposable and Subscription Management

### CompositeDisposable Pattern

Always use `CompositeDisposable` to manage Rx subscriptions.

```csharp
public class OrderViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IOrderEventAggregator _eventAggregator;
    private readonly IScheduler _uiScheduler;

    public OrderViewModel(
        ILogger logger,
        IOrderEventAggregator eventAggregator,
        IScheduler uiScheduler)
    {
        _eventAggregator = eventAggregator;
        _uiScheduler = uiScheduler;

        // Subscribe and add to CompositeDisposable
        _eventAggregator.OrderCreated
            .ObserveOn(_uiScheduler)
            .Subscribe(OnOrderCreated)
            .DisposeWith(_disposables);

        _eventAggregator.OrderSubmitted
            .ObserveOn(_uiScheduler)
            .Subscribe(OnOrderSubmitted)
            .DisposeWith(_disposables);
    }

    private void OnOrderCreated(OrderCreatedEvent evt)
    {
        // Update UI
        OrderCount++;
        RaisePropertyChanged(nameof(OrderCount));
    }

    private void OnOrderSubmitted(OrderSubmittedEvent evt)
    {
        // Update UI
        SubmittedCount++;
        RaisePropertyChanged(nameof(SubmittedCount));
    }

    public void Dispose()
    {
        _disposables.Dispose(); // Disposes ALL subscriptions at once
    }

    // INotifyPropertyChanged implementation...
}
```

### DisposeWith Extension

Use `.DisposeWith(_disposables)` to add subscriptions to CompositeDisposable fluently:

```csharp
// Instead of:
var subscription = observable.Subscribe(handler);
_disposables.Add(subscription);

// Use:
observable
    .Subscribe(handler)
    .DisposeWith(_disposables);
```

### IDisposable Implementation Checklist

- [ ] Class implements `IDisposable`
- [ ] Declare `CompositeDisposable` as private readonly field
- [ ] Add all Rx subscriptions to CompositeDisposable using `.DisposeWith()`
- [ ] Call `_disposables.Dispose()` in `Dispose()` method
- [ ] Dispose in proper order (subscriptions before publishers)

## Subscription Lifecycle

### Create Subscriptions in Constructor/Initialization

```csharp
public class NotificationService : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public NotificationService(IOrderEventAggregator events, IScheduler scheduler)
    {
        // Subscribe immediately in constructor
        events.OrderSubmitted
            .ObserveOn(scheduler)
            .Subscribe(SendNotification)
            .DisposeWith(_disposables);
    }

    private void SendNotification(OrderSubmittedEvent evt)
    {
        // Send email, push notification, etc.
    }

    public void Dispose() => _disposables.Dispose();
}
```

### Thread Scheduling with ObserveOn

Use `ObserveOn` to control which thread handles the event:

```csharp
// UI-bound subscription (WPF, WinForms)
events.OrderCreated
    .ObserveOn(_uiScheduler) // Resume on UI thread
    .Subscribe(evt =>
    {
        // Safe to update UI here
        StatusText = $"Order {evt.OrderId} created";
    })
    .DisposeWith(_disposables);

// Background processing
events.OrderSubmitted
    .ObserveOn(Scheduler.Default) // Background thread
    .Subscribe(async evt =>
    {
        await ProcessOrderAsync(evt.OrderId);
    })
    .DisposeWith(_disposables);
```

**Common Schedulers:**

- `Scheduler.CurrentThread` - Current thread (synchronous)
- `Scheduler.Default` - Background thread pool
- `DispatcherScheduler.Current` (WPF) - UI thread
- Custom scheduler for testing

## Error Handling in Subscriptions

### Log Errors by Exception Object

```csharp
// ❌ BAD: Custom error message
events.OrderCreated
    .Subscribe(
        onNext: HandleOrder,
        onError: ex => _logger.Error(ex, "Failed to handle order") // ❌ NO
    )
    .DisposeWith(_disposables);

// ✅ GOOD: Log exception directly
events.OrderCreated
    .Subscribe(
        onNext: HandleOrder,
        onError: ex => _logger.Error(ex) // ✅ YES
    )
    .DisposeWith(_disposables);
```

### Retry and Error Recovery

```csharp
events.OrderSubmitted
    .Retry(3) // Retry up to 3 times on error
    .Subscribe(
        onNext: ProcessOrder,
        onError: ex => _logger.Error(ex)
    )
    .DisposeWith(_disposables);

// Exponential backoff retry
events.OrderSubmitted
    .RetryWhen(errors => errors
        .SelectMany((ex, attempt) =>
            Observable.Timer(TimeSpan.FromSeconds(Math.Pow(2, attempt)))))
    .Subscribe(ProcessOrder)
    .DisposeWith(_disposables);
```

## Usage Guidance

### Higher-Level Orchestrators Depend on Application Services

```csharp
// ❌ BAD: Presentation subscribes directly to repository events
public class OrderListViewModel
{
    public OrderListViewModel(IOrderRepository repository)
    {
        // ❌ Repository shouldn't expose events
        repository.OrderSaved.Subscribe(...);
    }
}

// ✅ GOOD: Presentation subscribes to Application service events
public class OrderListViewModel
{
    public OrderListViewModel(OrderApplicationService orderService)
    {
        // ✅ Application service publishes domain events
        orderService.OrderCreated.Subscribe(...);
    }
}
```

### Route Changes Through Application Services for Event Consistency

```csharp
// ❌ BAD: Presentation directly mutates entity
public class OrderViewModel
{
    private readonly IOrderRepository _repository;

    public async Task SubmitAsync(OrderId orderId)
    {
        var order = await _repository.GetAsync(orderId);
        order.Submit(); // Direct mutation
        await _repository.SaveAsync(order);
        // No event published!
    }
}

// ✅ GOOD: Go through Application service
public class OrderViewModel
{
    private readonly OrderApplicationService _orderService;

    public async Task SubmitAsync(OrderId orderId)
    {
        await _orderService.SubmitOrderAsync(orderId);
        // Application service publishes OrderSubmitted event
    }
}
```

## Async APIs with Cancellation

Prefer async APIs with `CancellationToken` support:

```csharp
public class OrderApplicationService
{
    public async Task<Result<Order>> GetOrderAsync(
        OrderId orderId,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetAsync(orderId, cancellationToken);
        if (order == null)
            return Result.Failure<Order>("Order not found");

        return Result.Success(order);
    }

    public async Task<Result> ProcessOrdersAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var orders = await _repository.GetPendingOrdersAsync(cancellationToken);

            foreach (var order in orders)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await ProcessOrderAsync(order, cancellationToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }

        return Result.Success();
    }
}
```

## Complete Example: Service with Events

```csharp
public interface IOrderApplicationService : IDisposable
{
    IObservable<OrderCreatedEvent> OrderCreated { get; }
    IObservable<OrderSubmittedEvent> OrderSubmitted { get; }

    Task<Result<OrderId>> CreateOrderAsync(CustomerId customerId, CancellationToken ct);
    Task<Result> SubmitOrderAsync(OrderId orderId, CancellationToken ct);
}

public class OrderApplicationService : IOrderApplicationService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger _logger;

    // Event publishers
    private readonly Subject<OrderCreatedEvent> _orderCreated = new();
    private readonly Subject<OrderSubmittedEvent> _orderSubmitted = new();

    // Expose as observable
    public IObservable<OrderCreatedEvent> OrderCreated => _orderCreated.AsObservable();
    public IObservable<OrderSubmittedEvent> OrderSubmitted => _orderSubmitted.AsObservable();

    public OrderApplicationService(ILogger logger, IOrderRepository repository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<OrderId>> CreateOrderAsync(
        CustomerId customerId,
        CancellationToken ct)
    {
        try
        {
            var order = Order.CreateNew(customerId, "USD");

            await _repository.SaveAsync(order, ct);

            _logger.Trace("Created order={0} for customer={1}", order.Id, customerId);

            // Publish after successful save
            _orderCreated.OnNext(new OrderCreatedEvent(
                order.Id,
                customerId,
                DateTimeOffset.UtcNow));

            return Result.Success(order.Id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return Result.Failure<OrderId>("Failed to create order");
        }
    }

    public async Task<Result> SubmitOrderAsync(OrderId orderId, CancellationToken ct)
    {
        try
        {
            var order = await _repository.GetAsync(orderId, ct);
            if (order == null)
                return Result.Failure("Order not found");

            var result = order.Submit();
            if (!result.IsSuccess)
                return result;

            await _repository.SaveAsync(order, ct);

            _logger.Trace("Submitted order={0}", orderId);

            // Publish after successful save
            _orderSubmitted.OnNext(new OrderSubmittedEvent(
                order.Id,
                order.CustomerId,
                DateTimeOffset.UtcNow));

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return Result.Failure("Failed to submit order");
        }
    }

    public void Dispose()
    {
        _orderCreated?.Dispose();
        _orderSubmitted?.Dispose();
    }
}
```

## Checklist: Reactive Patterns

- [ ] Domain entities do NOT publish events directly
- [ ] Application services publish events AFTER successful persistence
- [ ] Events are immutable (readonly record struct or sealed record)
- [ ] Event publishers are `Subject<T>`, exposed as `IObservable<T>`
- [ ] Services own lifecycle of publishers (dispose in service's Dispose)
- [ ] All subscriptions managed via `CompositeDisposable`
- [ ] Use `.DisposeWith(_disposables)` for fluent subscription management
- [ ] `CompositeDisposable` disposed in class's Dispose method
- [ ] Use `ObserveOn(_uiScheduler)` for UI-bound subscriptions
- [ ] Errors logged by logging exception object (no custom message)
- [ ] Event aggregators compose streams without side effects
- [ ] Orchestrators depend on Application services, not repositories
- [ ] Changes routed through Application services for event consistency
- [ ] Async methods accept `CancellationToken`
