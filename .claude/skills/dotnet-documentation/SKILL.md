---
name: dotnet-documentation
description: Documentation conventions for .NET (.csproj, C#) including XML docs, DebuggerDisplay attributes, and Mermaid diagrams for architecture documentation. Use when adding XML docs, DebuggerDisplay, or creating architecture diagrams in markdown. DO NOT use for web frameworks (Next.js, React, JavaScript, TypeScript, Node.js).
allowed-tools: Read, Edit, Write, Glob, Grep
---

# .NET Documentation Standards

## DebuggerDisplay

Add `[DebuggerDisplay]` to all DTOs, entities, aggregates, and value objects.

```csharp
[DebuggerDisplay("{Id}: {CustomerName} ({Status})")]
public class Order { ... }

[DebuggerDisplay("Order {OrderId}: {ItemCount} items")]
public sealed record OrderDto { ... }

[DebuggerDisplay("{Amount} {Currency}")]
public readonly record struct Money(decimal Amount, string Currency);

[DebuggerDisplay("OrderId: {Value}")]
public readonly record struct OrderId(Guid Value);
```

**Show:** Key identity (ID, name), important state (status, count), keep concise.

## Interface Documentation

```csharp
/// <summary>
/// Brief description of purpose.
/// </summary>
/// <remarks>
/// Relationships, usage patterns, layer ownership.
/// </remarks>
public interface IMyService
{
    /// <summary>
    /// What the method does.
    /// </summary>
    /// <param name="id">Description. Must not be default.</param>
    /// <param name="cancellationToken">Cancellation behavior.</param>
    /// <returns>What is returned, nullability.</returns>
    /// <exception cref="ArgumentException">When thrown.</exception>
    /// <exception cref="OperationCanceledException">On cancellation.</exception>
    Task<Result> DoWorkAsync(EntityId id, CancellationToken cancellationToken);
}
```

## Async Method Documentation

Document:
- Cancellation behavior
- ConfigureAwait usage (true for UI context, false for library code)
- Thread safety

```csharp
/// <remarks>
/// Uses ConfigureAwait(false); safe to call from any thread.
/// Honors cancellation at query boundaries.
/// </remarks>
```

## UI Thread Requirements

```csharp
/// <remarks>
/// Must be called from the UI thread.
/// Uses Dispatcher.Invoke internally.
/// </remarks>
```

## Architecture Diagrams

When documenting architecture in markdown files, **always use Mermaid diagrams** instead of ASCII art.

## Checklist

- [ ] `[DebuggerDisplay]` on DTOs, entities, value objects
- [ ] `<summary>` on all public interfaces/methods
- [ ] `<param>` with nullability and constraints
- [ ] `<returns>` with nullability
- [ ] `<exception>` with trigger conditions
- [ ] `<see cref="..."/>` for cross-references
- [ ] Async methods document cancellation and ConfigureAwait
- [ ] UI-thread requirements stated explicitly
