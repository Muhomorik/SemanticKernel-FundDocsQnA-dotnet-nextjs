---
name: dotnet-domain-driven-design
description: Applies Domain-Driven Design patterns in C# and .NET projects. Use when implementing domain models, creating aggregates and entities, implementing repository pattern, adding value objects, creating domain events, or setting up layered architecture (Domain/Application/Infrastructure layers) in .NET solutions. Supports ASP.NET Core, WPF, Blazor, and console applications with .csproj files. DO NOT use for web frameworks (JavaScript, TypeScript, Next.js, React, Node.js).
allowed-tools: Read, Edit, Write, Grep, Glob
---

# Applying Domain Driven Design in .NET

This skill provides comprehensive DDD guidance for .NET projects, covering tactical patterns, architectural boundaries, and .NET-specific best practices.

## Quick Reference: Layer Responsibilities

### Presentation Layer

- **Purpose:** UI, data binding, user interaction, process lifetime management
- **Owns:** Application/process lifetime decisions, UI state, user input validation
- **Responsibilities:**
  - Translates user actions into application commands
  - Subscribes to application intents (`IObservable<T>`) and handles UI updates
  - Owns shutdown/termination logic via dedicated lifetime service
  - Marshals operations to UI thread when needed (WPF Dispatcher, etc.)

**Key Rule:** Only the Presentation layer may terminate the process or interact with UI shutdown APIs.

### Application Layer

- **Purpose:** Use-case orchestration, workflow coordination
- **Does NOT contain:** Business rules, domain logic, invariants
- **Responsibilities:**
  - Orchestrates domain operations by calling methods on aggregates
  - Loads/saves aggregates via repositories
  - Publishes domain events after state changes
  - Exposes intent signals (`IObservable<T>`) for Presentation layer

**Key Rule:** Never calls `Application.Current.Shutdown`, `Environment.Exit`, or performs process termination.

### Domain Layer

- **Purpose:** Core business logic, invariants, domain models
- **Contains:** Entities, aggregates, value objects, domain services, domain events
- **Responsibilities:**
  - Enforces business rules and invariants
  - Encapsulates state changes through behavior methods
  - Validates operations before mutating state
  - Remains pure (no infrastructure dependencies)

**Key Rule:** Domain entities do NOT publish events directly. Application services publish events after persisting state.

### Infrastructure Layer

- **Purpose:** Technical concerns (I/O, databases, external APIs, messaging)
- **Responsibilities:**
  - Implements repository interfaces
  - Handles serialization, networking, file system
  - May emit intent signals (`IObservable<T>`) but never controls UI or process lifetime

**Key Rule:** Never terminates process, never directly interacts with UI frameworks.

## Intent Signal Pattern

Lower layers (Application, Infrastructure) emit **intents** as observable streams. Presentation layer interprets intents and decides actions.

**Example:**

```csharp
// Infrastructure: Signal intent (neutral)
public interface IConnectionMonitor
{
    IObservable<ServerDisconnectedIntent> ServerDisconnected { get; }
}

// Presentation: Handle intent and decide action
_connectionMonitor.ServerDisconnected
    .ObserveOn(_uiScheduler)
    .Subscribe(intent =>
    {
        // Presentation layer decides: show error? retry? shutdown?
        _lifetimeService.InitiateShutdown(intent.Reason);
    });
```

This pattern maintains layer boundaries: Infrastructure knows "server disconnected" but doesn't decide to shut down. Presentation makes that decision.

## Key Prohibitions (Non-Presentation Layers)

- ❌ No `Application.Current.Shutdown` or `Environment.Exit`
- ❌ No `Dispatcher.Invoke` or UI thread manipulation
- ❌ No background tasks whose purpose is to end the process
- ❌ No direct dependency on WPF, WinForms, or UI frameworks
- ✅ Instead: Emit intent signals (`IObservable<T>`) for Presentation to handle

## Async/Await Best Practices

### Never Block on Tasks

```csharp
// ❌ BAD: Blocking
var result = SomeAsyncMethod().Result;
SomeAsyncMethod().Wait();

// ✅ GOOD: Async all the way
var result = await SomeAsyncMethod();
```

### Use CancellationToken for Cleanup

```csharp
public async Task ProcessAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        await DoWorkAsync(cancellationToken);
    }
}
```

### Document ConfigureAwait Behavior

- Library code: Use `ConfigureAwait(false)` when context doesn't matter
- UI code: Omit `ConfigureAwait` (default `true`) to resume on UI thread
- Document the choice in code comments when non-obvious

## Database Operations

**All database/repository operations MUST be async.** No synchronous DB calls.

```csharp
// ❌ BAD
public Order GetById(OrderId id) => _context.Orders.Find(id);

// ✅ GOOD
public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
    => await _context.Orders.FindAsync(new object[] { id }, ct);
```

## Supporting Documentation

For detailed guidance on specific DDD patterns, see:

- **[layer-separation.md](layer-separation.md)** - Comprehensive layer boundaries, prohibited patterns, and layer interaction rules
- **[entity-design.md](entity-design.md)** - Strongly-typed IDs, aggregate updates, tell-don't-ask principle, antipatterns

## Related Skills

Cross-cutting concerns are in separate skills for better auto-loading:

- **`dotnet-nlog-logging`** - NLog.ILogger conventions
- **`dotnet-extensions-logging`** - `ILogger<T>` conventions
- **`dotnet-reactive-patterns`** - Rx.NET, CompositeDisposable, event publishing
- **`dotnet-documentation`** - XML docs, DebuggerDisplay attributes

## Common Patterns

### Strongly-Typed Entity IDs

```csharp
public readonly record struct OrderId(Guid Value);
public readonly record struct CustomerId(Guid Value);

// Type safety prevents mixing IDs
public class Order
{
    public OrderId Id { get; private init; }
    public CustomerId CustomerId { get; private init; }
}
```

### Tell, Don't Ask (Aggregate Updates)

```csharp
// ❌ BAD: Asking and mutating externally
var order = repository.Get(orderId);
if (order.Total + itemPrice <= customer.CreditLimit)
{
    order.Items.Add(new OrderItem(productId, quantity, itemPrice));
    order.Total += itemPrice;
}

// ✅ GOOD: Tell the aggregate what to do
var order = repository.Get(orderId);
order.AddItem(productId, quantity, itemPrice, customer.CreditLimit);
// Aggregate enforces invariants internally
```

## Validation Checklist

When reviewing DDD implementation, check:

- [ ] No shutdown/termination logic outside Presentation layer
- [ ] Application services orchestrate but don't enforce business rules
- [ ] Domain aggregates enforce invariants through behavior methods
- [ ] Infrastructure emits intents, doesn't control UI or process lifetime
- [ ] Entities use strongly-typed IDs (not primitive Guid/int)
- [ ] Updates use "tell, don't ask" pattern (call aggregate methods)
- [ ] Domain events published by Application services, not entities
- [ ] All database operations are async
- [ ] Async methods use `CancellationToken` for cleanup
- [ ] No blocking on tasks (`.Result`, `.Wait()`)

See also: `dotnet-nlog-logging`, `dotnet-reactive-patterns`, `dotnet-documentation` skills

## When to Use This Skill

This skill applies when you're:

- Designing layered architecture for .NET applications
- Implementing domain models with entities, aggregates, value objects
- Setting up repositories and domain services
- Working with domain events and event-driven patterns
- Using Rx.NET for reactive programming in .NET
- Establishing logging patterns with NLog
- Creating WPF/Blazor/ASP.NET applications with DDD
- Implementing CQRS or clean architecture in C#
- Refactoring toward better domain modeling

This skill is project-agnostic and works with WPF, ASP.NET Core, Blazor, console apps, or any .NET application requiring DDD tactical patterns.
