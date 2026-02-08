# Entity Design and Aggregate Updates

Guide to designing domain entities and aggregates in .NET using Domain Driven Design tactical patterns.

## Strongly-Typed IDs

### Why Use Strongly-Typed IDs?

Using `Guid` or `int` directly for entity IDs leads to errors:

```csharp
// ❌ BAD: Easy to mix up IDs
public class OrderService
{
    public void TransferOrder(Guid orderId, Guid customerId)
    {
        // Oops, swapped parameters!
        repository.TransferOrder(customerId, orderId); // Compiles, but wrong
    }
}
```

Strongly-typed IDs prevent this:

```csharp
// ✅ GOOD: Type safety prevents mixups
public readonly record struct OrderId(Guid Value);
public readonly record struct CustomerId(Guid Value);

public class OrderService
{
    public void TransferOrder(OrderId orderId, CustomerId customerId)
    {
        // Won't compile if you swap them!
        repository.TransferOrder(orderId, customerId);
    }
}
```

### Implementation Pattern

```csharp
// Record struct for value semantics, readonly for immutability
public readonly record struct OrderId(Guid Value)
{
    public static OrderId NewId() => new(Guid.NewGuid());

    public static OrderId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}

// Usage
var orderId = OrderId.NewId();
var parsed = OrderId.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
```

### Benefits

- **Type Safety:** Compiler prevents mixing different ID types
- **Readability:** Method signatures clearly indicate entity types
- **Domain Modeling:** IDs become part of ubiquitous language
- **Refactoring:** Easier to find all uses of a specific entity ID type

## Entities and Aggregates

### Entity Basics

An **entity** has a unique identity that persists over time. Two entities with the same property values but different IDs are different entities.

```csharp
public class Order
{
    // Identity
    public OrderId Id { get; private init; }

    // Properties
    public CustomerId CustomerId { get; private init; }
    public DateTimeOffset CreatedAt { get; private init; }
    public OrderStatus Status { get; private set; }

    // Private constructor for persistence infrastructure
    private Order() { }

    // Factory method for creation
    public static Order CreateNew(CustomerId customerId)
    {
        return new Order
        {
            Id = OrderId.NewId(),
            CustomerId = customerId,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = OrderStatus.Draft
        };
    }

    // Equality based on identity, not properties
    public override bool Equals(object? obj) =>
        obj is Order other && Id.Equals(other.Id);

    public override int GetHashCode() => Id.GetHashCode();
}
```

### Aggregate Roots

An **aggregate** is a cluster of entities and value objects treated as a single unit. The **aggregate root** is the entry point for all operations.

**Key Rules:**

1. External references only point to aggregate root (not child entities)
2. Aggregate root enforces invariants for the entire cluster
3. Transactions apply to single aggregate (don't span aggregates)
4. Aggregate boundaries define consistency boundaries

```csharp
// Aggregate Root
public class Order
{
    public OrderId Id { get; private init; }
    public CustomerId CustomerId { get; private init; }
    public Money Total { get; private set; }

    // Child entities (private collection)
    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    // ✅ Behavior method enforces invariants
    public Result AddItem(ProductId productId, int quantity, Money unitPrice)
    {
        // Invariant: Quantity must be positive
        if (quantity <= 0)
            return Result.Failure("Quantity must be positive");

        // Invariant: No duplicate products
        if (_items.Any(i => i.ProductId == productId))
            return Result.Failure("Product already in order");

        // Safe to mutate after validation
        var item = OrderItem.Create(productId, quantity, unitPrice);
        _items.Add(item);
        Total += item.LineTotal;

        return Result.Success();
    }

    // ✅ Behavior method maintains consistency
    public Result RemoveItem(ProductId productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            return Result.Failure("Item not found");

        _items.Remove(item);
        Total -= item.LineTotal;

        return Result.Success();
    }
}

// Child entity (not an aggregate root)
public class OrderItem
{
    public OrderItemId Id { get; private init; }
    public ProductId ProductId { get; private init; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private init; }
    public Money LineTotal => UnitPrice * Quantity;

    private OrderItem() { }

    internal static OrderItem Create(ProductId productId, int quantity, Money unitPrice)
    {
        return new OrderItem
        {
            Id = OrderItemId.NewId(),
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}
```

## Tell, Don't Ask Principle

### The Problem: Asking and Mutating

```csharp
// ❌ BAD: Client code queries state and decides what to do
var order = repository.Get(orderId);
if (order.Status == OrderStatus.Draft)
{
    if (order.Total + newItemPrice <= customer.CreditLimit)
    {
        order.Items.Add(new OrderItem(productId, quantity, newItemPrice));
        order.Total += newItemPrice;
        order.Status = OrderStatus.Pending;
    }
}
repository.Save(order);
```

**Problems:**

- Business logic leaks into Application layer
- Invariants can be violated (what if two threads add items simultaneously?)
- Aggregate's internal state exposed for mutation
- Rules can't be enforced consistently

### The Solution: Tell the Aggregate What to Do

```csharp
// ✅ GOOD: Tell the aggregate, let it enforce rules
var order = repository.Get(orderId);
var result = order.AddItem(productId, quantity, newItemPrice, customer.CreditLimit);

if (result.IsSuccess)
{
    repository.Save(order);
}
else
{
    // Handle validation failure
    logger.Warn("Failed to add item: {0}", result.Error);
}
```

**Aggregate enforces invariants:**

```csharp
public class Order
{
    public Result AddItem(ProductId productId, int quantity, Money price, Money creditLimit)
    {
        // Check status
        if (Status != OrderStatus.Draft)
            return Result.Failure("Cannot add items to non-draft order");

        // Check credit limit
        var newTotal = Total + (price * quantity);
        if (newTotal > creditLimit)
            return Result.Failure("Would exceed credit limit");

        // Check quantity
        if (quantity <= 0)
            return Result.Failure("Quantity must be positive");

        // All invariants satisfied - mutate state
        var item = OrderItem.Create(productId, quantity, price);
        _items.Add(item);
        Total = newTotal;

        return Result.Success();
    }
}
```

### Encapsulation Guidelines

- **Private setters** for properties (or `private init`)
- **Private collections** with `IReadOnlyList<T>` public accessors
- **Behavior methods** for all state changes
- **Factory methods** for object creation
- **Result types** for operations that can fail

## Update Patterns

### Pattern: Load, Tell, Save

```csharp
// Application Service
public async Task<Result<OrderId>> AddItemToOrderAsync(
    OrderId orderId,
    ProductId productId,
    int quantity,
    Money price)
{
    // 1. Load aggregate
    var order = await _orderRepository.GetAsync(orderId);
    if (order == null)
        return Result.Failure<OrderId>("Order not found");

    var customer = await _customerRepository.GetAsync(order.CustomerId);

    // 2. Tell aggregate what to do
    var result = order.AddItem(productId, quantity, price, customer.CreditLimit);
    if (!result.IsSuccess)
        return Result.Failure<OrderId>(result.Error);

    // 3. Save aggregate
    await _orderRepository.SaveAsync(order);

    return Result.Success(order.Id);
}
```

### Pattern: Validate in Aggregate

```csharp
public class Order
{
    public Result Submit()
    {
        // Validate state before transition
        if (!_items.Any())
            return Result.Failure("Cannot submit empty order");

        if (Status != OrderStatus.Draft)
            return Result.Failure($"Cannot submit order in {Status} status");

        // Transition state
        Status = OrderStatus.Submitted;
        SubmittedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}
```

### Pattern: Child Entity Updates Through Root

```csharp
// ❌ BAD: Bypassing aggregate root
var order = repository.Get(orderId);
var item = order.Items.First(i => i.ProductId == productId);
item.Quantity = newQuantity; // Violates encapsulation!
item.LineTotal = item.UnitPrice * newQuantity; // Easy to forget
order.Total = order.Items.Sum(i => i.LineTotal); // Must recalculate

// ✅ GOOD: Update through aggregate root
var order = repository.Get(orderId);
order.UpdateItemQuantity(productId, newQuantity);
// Aggregate handles LineTotal and Total recalculation
```

```csharp
public class Order
{
    public Result UpdateItemQuantity(ProductId productId, int newQuantity)
    {
        if (newQuantity <= 0)
            return Result.Failure("Quantity must be positive");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            return Result.Failure("Item not found");

        // Update item via internal method
        var oldLineTotal = item.LineTotal;
        item.UpdateQuantity(newQuantity); // Internal method on OrderItem
        var newLineTotal = item.LineTotal;

        // Maintain aggregate invariant (Total)
        Total = Total - oldLineTotal + newLineTotal;

        return Result.Success();
    }
}

public class OrderItem
{
    // Internal - only Order can call this
    internal void UpdateQuantity(int quantity)
    {
        Quantity = quantity;
    }
}
```

## Antipatterns to Avoid

### 1. Anemic Domain Model

```csharp
// ❌ BAD: Entity is just a data holder
public class Order
{
    public OrderId Id { get; set; }
    public List<OrderItem> Items { get; set; }
    public Money Total { get; set; }
    public OrderStatus Status { get; set; }
}

// ❌ BAD: Logic lives in service
public class OrderService
{
    public void AddItem(Order order, OrderItem item)
    {
        order.Items.Add(item);
        order.Total += item.LineTotal;
    }
}
```

**Fix:** Move logic into entity behavior methods.

### 2. Public Setters on Aggregates

```csharp
// ❌ BAD: Anyone can mutate state
public class Order
{
    public OrderStatus Status { get; set; }
    public Money Total { get; set; }
}

// Invariants can be violated:
order.Status = OrderStatus.Completed; // Without checking Items, Total, etc.
order.Total = Money.Zero; // Oops, lost the total!
```

**Fix:** Use `private set` or `private init` and behavior methods.

### 3. Exposing Mutable Collections

```csharp
// ❌ BAD: Callers can bypass invariants
public class Order
{
    public List<OrderItem> Items { get; } = new();
}

// Can add items without validation:
order.Items.Add(new OrderItem(...)); // Bypasses Order.AddItem validation!
```

**Fix:** Private collection with readonly public accessor.

```csharp
private readonly List<OrderItem> _items = new();
public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
```

### 4. Modifying Child Entities Directly

```csharp
// ❌ BAD: Updating child without going through root
var item = order.Items.First();
item.Quantity = 10; // Aggregate doesn't know!
// Order.Total is now incorrect!
```

**Fix:** Update via aggregate root behavior method.

### 5. Cross-Aggregate Transactions

```csharp
// ❌ BAD: Updating multiple aggregates in single transaction
using var transaction = dbContext.BeginTransaction();

var order = orderRepository.Get(orderId);
order.Submit();

var inventory = inventoryRepository.Get(order.ProductId);
inventory.Reserve(order.Quantity);

await dbContext.SaveChangesAsync(); // Both aggregates in same transaction
transaction.Commit();
```

**Fix:** Use eventual consistency with domain events.

```csharp
// 1. Submit order (publishes OrderSubmitted event)
var order = orderRepository.Get(orderId);
order.Submit();
await orderRepository.SaveAsync(order);

// Application service publishes event
_eventBus.Publish(new OrderSubmittedEvent(order.Id, order.ProductId, order.Quantity));

// 2. Event handler reserves inventory (separate transaction)
public class OrderSubmittedHandler : IEventHandler<OrderSubmittedEvent>
{
    public async Task HandleAsync(OrderSubmittedEvent evt)
    {
        var inventory = await _inventoryRepository.GetAsync(evt.ProductId);
        inventory.Reserve(evt.Quantity);
        await _inventoryRepository.SaveAsync(inventory);
    }
}
```

## Value Objects

Value objects have no identity—equality is based on their values.

```csharp
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");

        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator *(Money money, int multiplier) =>
        new(money.Amount * multiplier, money.Currency);
}

// Usage
var price = new Money(19.99m, "USD");
var total = price * 5; // Money(99.95, "USD")
```

## Checklist: Entity Design

- [ ] Entities use strongly-typed IDs (not `Guid` or `int` directly)
- [ ] IDs are readonly record structs with value semantics
- [ ] Entities have private constructors (use factory methods)
- [ ] Properties use `private set` or `private init`
- [ ] Collections are private with `IReadOnlyList<T>` public accessors
- [ ] State changes go through behavior methods (tell, don't ask)
- [ ] Behavior methods validate invariants before mutating state
- [ ] Child entities updated through aggregate root, not directly
- [ ] Aggregates enforce consistency boundaries
- [ ] Single aggregate per transaction (use events for cross-aggregate)
- [ ] Value objects are immutable (readonly record structs)
- [ ] Equality: Entities by ID, value objects by value

## Example: Complete Aggregate

```csharp
using System.Diagnostics;

// Strongly-typed IDs
public readonly record struct OrderId(Guid Value)
{
    public static OrderId NewId() => new(Guid.NewGuid());
}

public readonly record struct ProductId(Guid Value);
public readonly record struct CustomerId(Guid Value);

// Value object
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money operator +(Money a, Money b) =>
        a.Currency == b.Currency
            ? new(a.Amount + b.Amount, a.Currency)
            : throw new InvalidOperationException("Currency mismatch");

    public static Money operator -(Money a, Money b) =>
        a.Currency == b.Currency
            ? new(a.Amount - b.Amount, a.Currency)
            : throw new InvalidOperationException("Currency mismatch");

    public static Money operator *(Money m, int qty) =>
        new(m.Amount * qty, m.Currency);
}

// Aggregate Root
[DebuggerDisplay("{Id}: {Items.Count} items, Total={Total.Amount} {Total.Currency}")]
public class Order
{
    public OrderId Id { get; private init; }
    public CustomerId CustomerId { get; private init; }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // For EF Core

    public static Order CreateNew(CustomerId customerId, string currency)
    {
        return new Order
        {
            Id = OrderId.NewId(),
            CustomerId = customerId,
            Status = OrderStatus.Draft,
            Total = new Money(0, currency),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public Result AddItem(ProductId productId, int quantity, Money unitPrice, Money creditLimit)
    {
        if (Status != OrderStatus.Draft)
            return Result.Failure("Can only add items to draft orders");

        if (quantity <= 0)
            return Result.Failure("Quantity must be positive");

        if (_items.Any(i => i.ProductId == productId))
            return Result.Failure("Product already in order");

        var lineTotal = unitPrice * quantity;
        var newTotal = Total + lineTotal;

        if (newTotal.Amount > creditLimit.Amount)
            return Result.Failure("Would exceed credit limit");

        var item = OrderItem.Create(productId, quantity, unitPrice);
        _items.Add(item);
        Total = newTotal;

        return Result.Success();
    }

    public Result Submit()
    {
        if (!_items.Any())
            return Result.Failure("Cannot submit empty order");

        if (Status != OrderStatus.Draft)
            return Result.Failure($"Cannot submit order in {Status} status");

        Status = OrderStatus.Submitted;
        return Result.Success();
    }
}

// Child entity
[DebuggerDisplay("ProductId={ProductId.Value}, Qty={Quantity}, Total={LineTotal.Amount}")]
public class OrderItem
{
    public ProductId ProductId { get; private init; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private init; }
    public Money LineTotal => UnitPrice * Quantity;

    private OrderItem() { }

    internal static OrderItem Create(ProductId productId, int quantity, Money unitPrice)
    {
        return new OrderItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    internal void UpdateQuantity(int newQuantity)
    {
        Quantity = newQuantity;
    }
}

public enum OrderStatus { Draft, Submitted, Completed, Cancelled }
```
