---
name: dotnet-nlog-logging
description: NLog logging conventions for .NET projects (.csproj, C#). Use when adding logging, configuring NLog, injecting ILogger, or setting up log targets. DO NOT use for web frameworks (Next.js, React, JavaScript, TypeScript, Node.js).
allowed-tools: Read, Edit, Write, Glob, Grep
---

# NLog Logging Conventions

## Rules

1. **Use `NLog.ILogger`** - Never `Microsoft.Extensions.Logging.ILogger`
2. **Logger is FIRST constructor parameter** - Always
3. **Resolve from DI** - Never `LogManager.GetCurrentClassLogger()` in classes
4. **Null-check logger** - `_logger = logger ?? throw new ArgumentNullException(nameof(logger));`

## Exception Logging

```csharp
// ❌ BAD
_logger.Error(ex, "Failed to save order");

// ✅ GOOD
_logger.Error(ex);
```

## Deferred Formatting

```csharp
// ❌ BAD - eager formatting
_logger.Trace($"Processing order {orderId}");

// ✅ GOOD - deferred formatting
_logger.Trace("Processing order {0}", orderId);
```

## Log Levels

| Level | Use |
|-------|-----|
| Trace | Detailed diagnostic |
| Debug | Debugging info |
| Info | Normal operations |
| Warn | Noteworthy but not error |
| Error | Errors (log exception directly) |
| Fatal | Application abort |

## Conditional Logging

For expensive operations only:

```csharp
if (_logger.IsTraceEnabled)
{
    _logger.Trace("Large data: {0}", SerializeExpensiveObject(data));
}
```

## Autofac Registration

```csharp
// Type-aware logger injection
builder.RegisterAssemblyTypes(typeof(Module).Assembly)
    .Where(t => t.Name.EndsWith("Service") || t.Name.EndsWith("ViewModel"))
    .WithParameter(new ResolvedParameter(
        (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
        (pi, ctx) => LogManager.GetLogger(pi.Member.DeclaringType?.FullName ?? "Unknown")))
    .InstancePerDependency();
```

## NLog.config

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      autoReload="true">
  <targets>
    <target xsi:type="Console" name="console"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}" />
    <target xsi:type="File" name="file"
            fileName="${basedir}/logs/${shortdate}.log"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}" />
  </targets>
  <rules>
    <logger name="*" minlevel="Trace" writeTo="console" />
    <logger name="*" minlevel="Debug" writeTo="file" />
  </rules>
</nlog>
```

## Checklist

- [ ] `NLog.ILogger` (not Microsoft.Extensions.Logging)
- [ ] Logger is first constructor parameter
- [ ] Logger resolved from DI
- [ ] `_logger.Error(ex)` - no custom message
- [ ] Deferred formatting: `_logger.Trace("msg {0}", arg)`
- [ ] Conditional logging for expensive operations
