---
name: dotnet-extensions-logging
description: Microsoft.Extensions.Logging conventions for .NET projects (.csproj, C#). Use when adding logging with ILogger<T>. DO NOT use for web frameworks (Next.js, React, JavaScript, TypeScript, Node.js). For NLog.ILogger, see dotnet-nlog-logging.
allowed-tools: Read, Edit, Write, Glob, Grep
---

# Microsoft.Extensions.Logging Conventions

For .NET projects using `Microsoft.Extensions.Logging.ILogger<T>`.

## Rules

1. **Use `ILogger<T>`** - Inject `ILogger<TClass>` where `TClass` is the containing type
2. **Logger is FIRST constructor parameter** - Always
3. **Null-check logger** - `_logger = logger ?? throw new ArgumentNullException(nameof(logger));`
4. **Deferred formatting** - Use structured logging placeholders, not string interpolation

## Constructor Pattern

```csharp
using Microsoft.Extensions.Logging;

public class MyService : IMyService
{
    private readonly ILogger<MyService> _logger;
    private readonly IRepository _repository;

    public MyService(
        ILogger<MyService> logger,  // First parameter
        IRepository repository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
}
```

## Deferred Formatting

```csharp
// ❌ BAD - eager string interpolation
_logger.LogInformation($"Processing order {orderId}");

// ✅ GOOD - deferred structured logging
_logger.LogInformation("Processing order {OrderId}", orderId);
```

Structured logging:
- Placeholders are named (not positional like NLog)
- Use PascalCase for placeholder names: `{OrderId}`, `{UserId}`
- Values captured as structured data for log aggregation

## Exception Logging

```csharp
// ❌ BAD - exception in message
_logger.LogError($"Failed: {ex.Message}");

// ✅ GOOD - exception as first parameter
_logger.LogError(ex, "Failed to process order {OrderId}", orderId);
```

## Log Levels

| Level | Use |
|-------|-----|
| Trace | Detailed diagnostic (verbose) |
| Debug | Debugging info |
| Information | Normal operations |
| Warning | Noteworthy but not error |
| Error | Errors (pass exception as first param) |
| Critical | Application failure |

## Conditional Logging

For expensive operations only:

```csharp
if (_logger.IsEnabled(LogLevel.Debug))
{
    _logger.LogDebug("Large payload: {Payload}", SerializeExpensiveObject(data));
}
```

## Autofac Registration

```csharp
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

public class MyModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register ILoggerFactory (backed by NLog or other provider)
        builder.Register<ILoggerFactory>(ctx => new NLogLoggerFactory())
            .As<ILoggerFactory>()
            .SingleInstance();

        // Register generic ILogger<T>
        builder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>))
            .SingleInstance();
    }
}
```

## NuGet Packages

```xml
<!-- Core abstractions -->
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />

<!-- Bridge to NLog (if using NLog as provider) -->
<PackageReference Include="NLog.Extensions.Logging" Version="6.1.0" />
```

## Checklist

- [ ] `ILogger<T>` (not `ILogger` or `NLog.ILogger`)
- [ ] Logger is first constructor parameter
- [ ] Logger null-checked in constructor
- [ ] Deferred formatting: `_logger.LogInformation("msg {Param}", value)`
- [ ] PascalCase placeholder names
- [ ] Exceptions as first parameter: `_logger.LogError(ex, "msg")`

## Related Skills

- **`dotnet-nlog-logging`** - For NLog.ILogger usage
