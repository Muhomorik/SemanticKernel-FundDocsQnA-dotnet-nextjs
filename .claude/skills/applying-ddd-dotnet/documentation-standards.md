# Documentation Standards for DDD in .NET

Guide to writing clear, self-contained XML documentation for interfaces, classes, and domain models in .NET DDD applications.

## Goal: Self-Contained Documentation

Documentation should be explicit, complete, and understandable **without loading the implementation**. Agents and developers should understand the contract from the interface documentation alone.

## General Principles

### For Each Method and Property

- **Clearly describe purpose and contract**
- **Document all parameters** using `<param>` tags with expected values, constraints, and nullability
- **Document all exceptions** using `<exception>` tags with when and why they're thrown
- **Note important side effects**, relationships, or constraints
- **Add remarks or links** for complex logic or edge cases when relevant

### For the Interface as a Whole

- **Summarize overall purpose** and role in the application
- **Describe relationships** with other interfaces or components
- **State usage constraints** or expectations for implementers

### Use Consistent Terminology

- Terminology must align with Domain/Application/Infrastructure layers
- Ensure documentation is up-to-date and free of ambiguity
- For public or critical interfaces, provide detailed summaries
- For internal and simple interfaces, keep concise but complete

## DebuggerDisplay Attribute

**Always add `[DebuggerDisplay]` attribute** to DTOs, entities, aggregates, and value objects to improve debugging experience.

```csharp
using System.Diagnostics;

// ✅ Entity with DebuggerDisplay
[DebuggerDisplay("{Id}: {CustomerName} ({Status})")]
public class Order
{
    public OrderId Id { get; private init; }
    public string CustomerName { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }
}

// ✅ DTO with DebuggerDisplay
[DebuggerDisplay("Order {OrderId}: {ItemCount} items, Total={TotalAmount} {Currency}")]
public sealed record OrderDto
{
    public required Guid OrderId { get; init; }
    public required int ItemCount { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
}

// ✅ Value object with DebuggerDisplay
[DebuggerDisplay("{Amount} {Currency}")]
public readonly record struct Money(decimal Amount, string Currency);

// ✅ Strongly-typed ID with DebuggerDisplay
[DebuggerDisplay("OrderId: {Value}")]
public readonly record struct OrderId(Guid Value);
```

**Guidelines:**

- Show **key identity** (ID, name)
- Show **important state** (status, count, total)
- Keep **concise** (one line)
- Use **meaningful property names**
- Include units where applicable (currency, quantity)

## Interface Documentation

### Complete Interface Example

```csharp
/// <summary>
/// Manages the lifecycle of the application, providing methods to initiate shutdown
/// and handle graceful termination. This service is owned by the Presentation layer
/// and is the only component that may terminate the process.
/// </summary>
/// <remarks>
/// Lower layers (Application, Infrastructure) emit intent signals via IObservable&lt;T&gt;,
/// and the Presentation layer uses this service to respond to those intents.
/// Only call these methods from the Presentation layer or in response to explicit user actions.
/// </remarks>
public interface IApplicationLifetime
{
    /// <summary>
    /// Initiates application shutdown with the specified exit code.
    /// This method terminates the process immediately after cleanup.
    /// </summary>
    /// <param name="exitCode">
    /// The exit code to return to the operating system. Use 0 for normal exit,
    /// non-zero for errors or abnormal termination.
    /// </param>
    /// <remarks>
    /// This method performs minimal cleanup and exits quickly. For graceful shutdown
    /// with resource cleanup, use <see cref="ShutdownGracefullyAsync"/> instead.
    /// Must be called from the UI thread.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called from a non-UI thread or if shutdown is already in progress.
    /// </exception>
    void InitiateShutdown(int exitCode);

    /// <summary>
    /// Initiates application shutdown with a descriptive reason.
    /// The application logs the reason and terminates the process.
    /// </summary>
    /// <param name="reason">
    /// A descriptive message explaining why the application is shutting down.
    /// Must not be null or whitespace.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="reason"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="reason"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if shutdown is already in progress.
    /// </exception>
    void InitiateShutdown(string reason);

    /// <summary>
    /// Gracefully shuts down the application, allowing time for resource cleanup,
    /// pending operations to complete, and state to be persisted.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. If cancelled, shutdown proceeds
    /// immediately without waiting for cleanup to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous shutdown operation. The task completes
    /// when shutdown is finished and the process is about to terminate.
    /// </returns>
    /// <remarks>
    /// This method ensures all IDisposable resources are disposed, ongoing tasks are
    /// given a chance to complete (up to a timeout), and application state is saved.
    /// Uses ConfigureAwait(true) to resume on the UI thread after awaits.
    /// If cancellation is requested, cleanup is skipped and shutdown proceeds immediately.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if shutdown is already in progress.
    /// </exception>
    Task ShutdownGracefullyAsync(CancellationToken cancellationToken);
}
```

### Documentation Authoring Checklist

- [ ] Did you document all parameters with nullability and ranges/constraints?
- [ ] Did you clearly state return value nullability and ownership?
- [ ] Did you list every exception and its trigger condition?
- [ ] Did you explain relationships and expected usage with other components?
- [ ] Is terminology consistent with Domain/Application/Infrastructure layers?
- [ ] For async methods, did you document cancellation and context-capture behavior?
- [ ] For WPF-facing APIs, did you specify Dispatcher/UI-thread requirements?
- [ ] Is the documentation self-contained (readers don't need to open implementations)?
- [ ] For all DTOs, entities, and aggregates, did you add `[DebuggerDisplay]` attribute?

## Threading and WPF Documentation

### UI Thread Requirements

If a member **must be called from the UI thread**, state it explicitly and explain why:

```csharp
/// <summary>
/// Updates the order status in the UI and notifies subscribers.
/// Must be called from the UI thread.
/// </summary>
/// <param name="orderId">The ID of the order to update. Must not be default.</param>
/// <param name="newStatus">The new status to apply.</param>
/// <remarks>
/// This method uses Dispatcher.Invoke to marshal the update to the UI thread.
/// Throws <see cref="InvalidOperationException"/> if the Dispatcher is not available.
/// </remarks>
/// <exception cref="ArgumentException">
/// Thrown if <paramref name="orderId"/> is default.
/// </exception>
/// <exception cref="InvalidOperationException">
/// Thrown if no Dispatcher is available (e.g., called from background thread without UI context).
/// </exception>
void UpdateOrderStatus(OrderId orderId, OrderStatus newStatus);
```

### Async Method Documentation

For async methods, document:

- **Cancellation behavior:** How `CancellationToken` is honored
- **What happens on cancellation:** `OperationCanceledException` vs. partial work
- **ConfigureAwait behavior:** Context capture (true) or suppression (false)

```csharp
/// <summary>
/// Asynchronously loads all orders for the specified customer.
/// </summary>
/// <param name="customerId">The ID of the customer. Must not be default.</param>
/// <param name="cancellationToken">
/// A token to monitor for cancellation requests. Cancellation stops the operation
/// and throws <see cref="OperationCanceledException"/>.
/// </param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains
/// a list of orders, or an empty list if no orders are found. Never returns null.
/// </returns>
/// <remarks>
/// This method uses ConfigureAwait(false) as it does not require the original
/// synchronization context. Safe to call from any thread.
/// Honors cancellation at database query boundaries; partial results are discarded.
/// </remarks>
/// <exception cref="ArgumentException">
/// Thrown if <paramref name="customerId"/> is default.
/// </exception>
/// <exception cref="OperationCanceledException">
/// Thrown if the operation is cancelled via <paramref name="cancellationToken"/>.
/// </exception>
/// <exception cref="DbException">
/// Thrown if a database error occurs during the operation.
/// </exception>
Task<List<Order>> GetOrdersAsync(CustomerId customerId, CancellationToken cancellationToken);
```

## Repository Interface Example

```csharp
/// <summary>
/// Provides data access for Order aggregates.
/// Repositories are implemented in the Infrastructure layer and expose domain entities.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Asynchronously retrieves an order by its unique identifier.
    /// </summary>
    /// <param name="orderId">The unique identifier of the order. Must not be default.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. Cancellation stops the query
    /// and throws <see cref="OperationCanceledException"/>.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the Order if found, or null if no order with the specified ID exists.
    /// </returns>
    /// <remarks>
    /// Includes all related OrderItems in the returned aggregate.
    /// Uses ConfigureAwait(false) internally; safe to call from any thread.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="orderId"/> is default.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is cancelled via <paramref name="cancellationToken"/>.
    /// </exception>
    Task<Order?> GetAsync(OrderId orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously persists an order aggregate to the data store.
    /// </summary>
    /// <param name="order">The order to save. Must not be null.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. Cancellation aborts the transaction
    /// and throws <see cref="OperationCanceledException"/>.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous save operation.
    /// </returns>
    /// <remarks>
    /// If the order already exists (matched by ID), it is updated.
    /// If the order is new, it is inserted.
    /// All child OrderItems are saved as part of the aggregate transaction.
    /// Uses ConfigureAwait(false) internally; safe to call from any thread.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="order"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is cancelled via <paramref name="cancellationToken"/>.
    /// </exception>
    /// <exception cref="DbUpdateException">
    /// Thrown if a database constraint violation occurs (e.g., duplicate key).
    /// </exception>
    Task SaveAsync(Order order, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously retrieves all orders with status Pending.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a list of pending orders, or an empty list if none exist. Never returns null.
    /// </returns>
    /// <remarks>
    /// Orders are returned sorted by CreatedAt ascending (oldest first).
    /// Does not include OrderItems; call <see cref="GetAsync"/> to load full aggregate.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is cancelled via <paramref name="cancellationToken"/>.
    /// </exception>
    Task<List<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken);
}
```

## Application Service Interface Example

```csharp
/// <summary>
/// Provides application-level operations for managing orders.
/// Orchestrates domain logic and publishes domain events after successful persistence.
/// </summary>
/// <remarks>
/// This service coordinates between Order aggregates and repositories.
/// It publishes domain events (OrderCreated, OrderSubmitted) via IObservable streams
/// after transactions complete successfully.
/// Implements IDisposable to clean up event publisher resources.
/// </remarks>
public interface IOrderApplicationService : IDisposable
{
    /// <summary>
    /// Observable stream of OrderCreated events.
    /// Published after an order is successfully created and persisted.
    /// </summary>
    IObservable<OrderCreatedEvent> OrderCreated { get; }

    /// <summary>
    /// Observable stream of OrderSubmitted events.
    /// Published after an order is successfully submitted and persisted.
    /// </summary>
    IObservable<OrderSubmittedEvent> OrderSubmitted { get; }

    /// <summary>
    /// Asynchronously creates a new order for the specified customer.
    /// </summary>
    /// <param name="customerId">The ID of the customer placing the order. Must not be default.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. Cancellation aborts the operation
    /// and throws <see cref="OperationCanceledException"/>.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a Result with the new OrderId if successful, or a failure message if validation fails.
    /// </returns>
    /// <remarks>
    /// Creates an order in Draft status with USD currency.
    /// Publishes OrderCreated event via <see cref="OrderCreated"/> stream after successful save.
    /// Uses ConfigureAwait(false); safe to call from any thread.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="customerId"/> is default.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is cancelled via <paramref name="cancellationToken"/>.
    /// </exception>
    Task<Result<OrderId>> CreateOrderAsync(CustomerId customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously submits an existing order, transitioning it from Draft to Submitted status.
    /// </summary>
    /// <param name="orderId">The ID of the order to submit. Must not be default.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a Result indicating success or failure with a descriptive error message.
    /// </returns>
    /// <remarks>
    /// Validates that the order exists and is in Draft status before submitting.
    /// Publishes OrderSubmitted event via <see cref="OrderSubmitted"/> stream after successful save.
    /// Uses ConfigureAwait(false); safe to call from any thread.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="orderId"/> is default.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is cancelled via <paramref name="cancellationToken"/>.
    /// </exception>
    Task<Result> SubmitOrderAsync(OrderId orderId, CancellationToken cancellationToken);
}
```

## Domain Entity Documentation

```csharp
/// <summary>
/// Represents an order aggregate root containing customer information, order items,
/// and total amount. Enforces business invariants and encapsulates order lifecycle.
/// </summary>
/// <remarks>
/// Order is an aggregate root that owns OrderItem child entities.
/// All modifications to OrderItems must go through Order behavior methods
/// to maintain aggregate consistency (e.g., Total must match sum of item line totals).
/// Orders follow lifecycle: Draft → Submitted → Completed or Cancelled.
/// </remarks>
[DebuggerDisplay("{Id}: {Items.Count} items, Total={Total.Amount} {Total.Currency}, Status={Status}")]
public class Order
{
    /// <summary>
    /// Gets the unique identifier for this order.
    /// </summary>
    public OrderId Id { get; private init; }

    /// <summary>
    /// Gets the ID of the customer who owns this order.
    /// </summary>
    public CustomerId CustomerId { get; private init; }

    /// <summary>
    /// Gets the current status of the order.
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Gets the total amount of the order, including all items.
    /// This value is automatically recalculated when items are added or removed.
    /// </summary>
    public Money Total { get; private set; }

    /// <summary>
    /// Gets the timestamp when the order was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Gets a read-only collection of items in this order.
    /// To modify items, use <see cref="AddItem"/> or <see cref="RemoveItem"/>.
    /// </summary>
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private readonly List<OrderItem> _items = new();

    /// <summary>
    /// Creates a new order for the specified customer.
    /// </summary>
    /// <param name="customerId">The ID of the customer. Must not be default.</param>
    /// <param name="currency">The currency code (e.g., "USD", "EUR"). Must not be null or whitespace.</param>
    /// <returns>A new Order in Draft status with zero total.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="customerId"/> is default or <paramref name="currency"/> is null/whitespace.
    /// </exception>
    public static Order CreateNew(CustomerId customerId, string currency)
    {
        // Implementation...
    }

    /// <summary>
    /// Adds an item to the order, validating against business rules.
    /// </summary>
    /// <param name="productId">The ID of the product to add. Must not be default.</param>
    /// <param name="quantity">The quantity to order. Must be positive.</param>
    /// <param name="unitPrice">The price per unit. Amount must be non-negative.</param>
    /// <param name="creditLimit">
    /// The customer's credit limit. Used to validate that adding this item
    /// does not cause the order total to exceed the limit.
    /// </param>
    /// <returns>
    /// A Result indicating success or failure. Failure reasons include:
    /// - Order is not in Draft status
    /// - Quantity is zero or negative
    /// - Product already exists in the order
    /// - Adding the item would exceed the customer's credit limit
    /// </returns>
    /// <remarks>
    /// Updates the <see cref="Total"/> property automatically when successful.
    /// This method enforces aggregate invariants before mutating state.
    /// </remarks>
    public Result AddItem(ProductId productId, int quantity, Money unitPrice, Money creditLimit)
    {
        // Implementation...
    }

    /// <summary>
    /// Submits the order, transitioning from Draft to Submitted status.
    /// </summary>
    /// <returns>
    /// A Result indicating success or failure. Failure reasons include:
    /// - Order has no items (empty order cannot be submitted)
    /// - Order is not in Draft status
    /// </returns>
    /// <remarks>
    /// Once submitted, items can no longer be added or removed.
    /// </remarks>
    public Result Submit()
    {
        // Implementation...
    }
}
```

## Checklist: Documentation Standards

- [ ] All public interfaces have `<summary>` describing purpose and role
- [ ] All public interfaces have `<remarks>` describing relationships and usage patterns
- [ ] All parameters documented with `<param>`, including nullability and constraints
- [ ] All return values documented with `<returns>`, including nullability
- [ ] All exceptions documented with `<exception>` and trigger conditions
- [ ] Async methods document cancellation behavior and ConfigureAwait usage
- [ ] UI-thread requirements explicitly stated when applicable
- [ ] Terminology consistent with DDD layers (Domain, Application, Infrastructure, Presentation)
- [ ] Documentation is self-contained (no need to read implementation)
- [ ] All DTOs, entities, aggregates have `[DebuggerDisplay]` attribute
- [ ] DebuggerDisplay shows key identity and important state
- [ ] Cross-references use `<see cref="..."/>` for navigation
