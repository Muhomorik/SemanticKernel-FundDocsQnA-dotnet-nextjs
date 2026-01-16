# Logging Conventions with NLog

Best practices for logging in .NET DDD applications using NLog.

## NLog vs Microsoft.Extensions.Logging

**Always use `NLog.ILogger`**, never `Microsoft.Extensions.Logging.ILogger`.

```csharp
// ❌ WRONG: Microsoft.Extensions.Logging
using Microsoft.Extensions.Logging;

public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger) // ❌ NO
    {
        _logger = logger;
    }
}

// ✅ CORRECT: NLog
using NLog;

public class OrderService
{
    private readonly ILogger _logger;

    public OrderService(ILogger logger) // ✅ YES
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

**Why NLog?**

- More mature and feature-rich than Microsoft.Extensions.Logging
- Better structured logging support
- More flexible configuration
- Superior performance with deferred formatting
- Richer output targets and layout options

## Logger as First Constructor Parameter

**Always** place the logger as the **first** constructor parameter.

```csharp
// ✅ CORRECT: Logger is first parameter
public class OrderApplicationService
{
    private readonly ILogger _logger;
    private readonly IOrderRepository _repository;
    private readonly IProductRepository _productRepository;

    public OrderApplicationService(
        ILogger logger, // ✅ First parameter
        IOrderRepository repository,
        IProductRepository productRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }
}

// ❌ WRONG: Logger not first
public class OrderApplicationService
{
    public OrderApplicationService(
        IOrderRepository repository,
        ILogger logger) // ❌ Should be first
    {
        // ...
    }
}
```

**Why first?**

- **Consistency:** Easy to find logger in every constructor
- **Convention:** Establishes team-wide pattern
- **Visibility:** Signals logging is a first-class concern

## Resolve Logger from DI

Always resolve `NLog.ILogger` from dependency injection. Never create loggers directly.

```csharp
// ✅ CORRECT: Resolve from DI
public class OrderService
{
    private readonly ILogger _logger;

    public OrderService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}

// ❌ WRONG: Creating logger directly
public class OrderService
{
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger(); // ❌ NO

    public OrderService()
    {
    }
}
```

**DI Registration (example):**

```csharp
// In your DI container setup
services.AddTransient<ILogger>(provider =>
    LogManager.GetLogger(provider.GetType().FullName));

// Or use NLog.Extensions.DependencyInjection
services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
    loggingBuilder.AddNLog();
});
```

## Exception Logging: Log Exception Object Directly

When logging exceptions, **always log the exception object directly** without a custom message.

```csharp
// ❌ WRONG: Custom message with exception
try
{
    await _repository.SaveAsync(order);
}
catch (Exception ex)
{
    _logger.Error(ex, "Failed to save order"); // ❌ NO
    throw;
}

// ✅ CORRECT: Log exception directly
try
{
    await _repository.SaveAsync(order);
}
catch (Exception ex)
{
    _logger.Error(ex); // ✅ YES
    throw;
}
```

**Why no custom message?**

- Exception message already describes what went wrong
- Custom messages add noise and redundancy
- NLog automatically includes exception type, message, and stack trace
- Reduces verbosity and improves log clarity

### When Context is Needed

If you absolutely must add context, include it in structured properties BEFORE the catch:

```csharp
// ✅ ACCEPTABLE: Add context before operation
_logger.Trace("Saving order={0}", orderId);
try
{
    await _repository.SaveAsync(order);
}
catch (Exception ex)
{
    _logger.Error(ex); // Exception logged, context already in previous log
    throw;
}
```

## Deferred Formatting: Use Format Strings

**Never use string interpolation ($"...") or string.Format** when logging. Pass format string and arguments separately so NLog defers formatting until the message is actually written.

```csharp
// ❌ WRONG: Eager formatting with interpolation
_logger.Trace($"Processing order {orderId} for customer {customerId}");

// ❌ WRONG: Eager formatting with string.Format
_logger.Trace(string.Format("Processing order {0} for customer {1}", orderId, customerId));

// ✅ CORRECT: Deferred formatting
_logger.Trace("Processing order {0} for customer {1}", orderId, customerId);
```

**Why deferred formatting?**

- **Performance:** If log level is disabled (e.g., Trace in production), formatting is skipped entirely
- **Structured Logging:** NLog can extract arguments as structured properties
- **Zero allocation:** Avoids string concatenation when logging is disabled

### More Examples

```csharp
// ❌ WRONG
_logger.Debug($"Order total: {order.Total.Amount} {order.Total.Currency}");
_logger.Info($"User {userId} submitted {orders.Count} orders");
_logger.Warn($"Inventory low: {product.Name} has {inventory.Quantity} units");

// ✅ CORRECT
_logger.Debug("Order total: {0} {1}", order.Total.Amount, order.Total.Currency);
_logger.Info("User {0} submitted {1} orders", userId, orders.Count);
_logger.Warn("Inventory low: {0} has {1} units", product.Name, inventory.Quantity);
```

## Log Levels

Use appropriate log levels:

| Level   | Purpose                                              | Example                                    |
| ------- | ---------------------------------------------------- | ------------------------------------------ |
| `Trace` | Detailed diagnostic information for debugging        | "Processing order={0}", orderId            |
| `Debug` | Debugging information, more verbose than Info        | "Loaded {0} items from cache", count       |
| `Info`  | Informational messages about normal operations       | "Application started", "Order submitted"   |
| `Warn`  | Potentially harmful situations that aren't errors    | "Retry attempt {0}", attemptNumber         |
| `Error` | Error events that might still allow app to continue  | _logger.Error(ex)                          |
| `Fatal` | Very severe errors causing application to abort      | _logger.Fatal(ex)                          |

### Examples

```csharp
public class OrderApplicationService
{
    private readonly ILogger _logger;
    private readonly IOrderRepository _repository;

    public OrderApplicationService(ILogger logger, IOrderRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task<Result<OrderId>> CreateOrderAsync(CustomerId customerId)
    {
        // Trace: detailed flow
        _logger.Trace("CreateOrderAsync called for customer={0}", customerId);

        try
        {
            var order = Order.CreateNew(customerId, "USD");

            // Trace: internal state
            _logger.Trace("Created order={0} with {1} items", order.Id, order.Items.Count);

            await _repository.SaveAsync(order);

            // Info: important business event
            _logger.Info("Order created: {0}", order.Id);

            return Result.Success(order.Id);
        }
        catch (DbUpdateException ex)
        {
            // Error: exception occurred
            _logger.Error(ex);
            return Result.Failure<OrderId>("Database error");
        }
        catch (Exception ex)
        {
            // Fatal: unexpected exception
            _logger.Fatal(ex);
            throw;
        }
    }

    public async Task<Result> SubmitOrderAsync(OrderId orderId)
    {
        _logger.Trace("SubmitOrderAsync called for order={0}", orderId);

        var order = await _repository.GetAsync(orderId);
        if (order == null)
        {
            // Warn: expected condition that's noteworthy
            _logger.Warn("Order not found: {0}", orderId);
            return Result.Failure("Order not found");
        }

        var result = order.Submit();
        if (!result.IsSuccess)
        {
            // Debug: validation failure (expected in normal flow)
            _logger.Debug("Order submit validation failed: {0}", result.Error);
            return result;
        }

        await _repository.SaveAsync(order);

        _logger.Info("Order submitted: {0}", orderId);

        return Result.Success();
    }
}
```

## Structured Logging

Use structured properties for better queryability:

```csharp
// Instead of:
_logger.Info("Order {0} created by user {1} with total {2}", orderId, userId, total);

// Use structured properties:
_logger.Info("Order created")
    .WithProperty("OrderId", orderId)
    .WithProperty("UserId", userId)
    .WithProperty("Total", total);
```

Or use NLog's structured logging syntax:

```csharp
_logger.Info("Order created: {@Order}", new
{
    OrderId = orderId,
    UserId = userId,
    Total = total
});
```

## Conditional Logging

For expensive operations, check if logging is enabled:

```csharp
// ❌ WRONG: Always formats, even if Trace disabled
_logger.Trace("Large data: {0}", SerializeExpensiveObject(data));

// ✅ CORRECT: Only formats if Trace enabled
if (_logger.IsTraceEnabled)
{
    _logger.Trace("Large data: {0}", SerializeExpensiveObject(data));
}
```

**When to use:**

- Expensive serialization or string building
- Large collections (avoid logging 1000+ items)
- Computed values that require significant CPU

**When NOT needed:**

- Simple property access (orderId, customerId)
- Deferred formatting handles this automatically for cheap operations

## Complete Example

```csharp
using NLog;

public class OrderApplicationService : IDisposable
{
    private readonly ILogger _logger; // First field
    private readonly IOrderRepository _repository;
    private readonly ICustomerRepository _customerRepository;

    // Logger is first constructor parameter
    public OrderApplicationService(
        ILogger logger,
        IOrderRepository repository,
        ICustomerRepository customerRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
    }

    public async Task<Result<OrderId>> CreateOrderAsync(
        CustomerId customerId,
        List<OrderItemDto> items,
        CancellationToken ct)
    {
        // Trace: method entry with parameters
        _logger.Trace("CreateOrderAsync customerId={0} itemCount={1}", customerId, items.Count);

        try
        {
            var customer = await _customerRepository.GetAsync(customerId, ct);
            if (customer == null)
            {
                // Warn: expected failure case
                _logger.Warn("Customer not found: {0}", customerId);
                return Result.Failure<OrderId>("Customer not found");
            }

            var order = Order.CreateNew(customerId, "USD");

            foreach (var itemDto in items)
            {
                // Trace: detailed loop iteration
                _logger.Trace("Adding item product={0} qty={1}", itemDto.ProductId, itemDto.Quantity);

                var result = order.AddItem(
                    itemDto.ProductId,
                    itemDto.Quantity,
                    itemDto.UnitPrice,
                    customer.CreditLimit);

                if (!result.IsSuccess)
                {
                    // Debug: validation failure
                    _logger.Debug("Failed to add item: {0}", result.Error);
                    return Result.Failure<OrderId>(result.Error);
                }
            }

            await _repository.SaveAsync(order, ct);

            // Info: important business event
            _logger.Info("Order created: {0}", order.Id);

            return Result.Success(order.Id);
        }
        catch (OperationCanceledException)
        {
            // Info: expected cancellation
            _logger.Info("CreateOrderAsync cancelled for customer={0}", customerId);
            throw;
        }
        catch (Exception ex)
        {
            // Error: unexpected exception (logged directly)
            _logger.Error(ex);
            return Result.Failure<OrderId>("Failed to create order");
        }
    }

    public async Task<Result> ProcessPendingOrdersAsync(CancellationToken ct)
    {
        _logger.Info("Processing pending orders");

        try
        {
            var orders = await _repository.GetPendingOrdersAsync(ct);

            // Conditional logging for expensive operation
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug("Found {0} pending orders: {1}",
                    orders.Count,
                    string.Join(", ", orders.Select(o => o.Id)));
            }

            foreach (var order in orders)
            {
                if (ct.IsCancellationRequested)
                    break;

                _logger.Trace("Processing order={0}", order.Id);
                await ProcessOrderAsync(order, ct);
            }

            _logger.Info("Processed {0} pending orders", orders.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return Result.Failure("Failed to process pending orders");
        }
    }

    private async Task ProcessOrderAsync(Order order, CancellationToken ct)
    {
        // Implementation...
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

## Checklist: Logging Conventions

- [ ] Always use `NLog.ILogger` (never `Microsoft.Extensions.Logging.ILogger`)
- [ ] Logger is **first** constructor parameter
- [ ] Logger resolved from DI (never `LogManager.GetCurrentClassLogger()`)
- [ ] Logger null-checked in constructor
- [ ] Exception logging uses `_logger.Error(ex);` (no custom message)
- [ ] All log messages use deferred formatting (no `$""` or `string.Format`)
- [ ] Format string with arguments: `_logger.Trace("msg {0}", arg);`
- [ ] Appropriate log levels (Trace, Debug, Info, Warn, Error, Fatal)
- [ ] Conditional logging for expensive operations (`if (_logger.IsTraceEnabled)`)
- [ ] Structured logging for complex data when beneficial
- [ ] No redundant messages (exception message already describes error)

## NLog Configuration Example

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      internalLogLevel="Info"
      internalLogFile="c:\temp\internal-nlog.txt">

  <targets>
    <!-- Console target -->
    <target xsi:type="Console" name="console"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}" />

    <!-- File target -->
    <target xsi:type="File" name="file"
            fileName="${basedir}/logs/${shortdate}.log"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}" />

    <!-- Structured JSON target -->
    <target xsi:type="File" name="jsonFile"
            fileName="${basedir}/logs/${shortdate}.json">
      <layout xsi:type="JsonLayout">
        <attribute name="time" layout="${longdate}" />
        <attribute name="level" layout="${level:upperCase=true}"/>
        <attribute name="logger" layout="${logger}"/>
        <attribute name="message" layout="${message}" />
        <attribute name="exception" layout="${exception:format=tostring}" />
      </layout>
    </target>
  </targets>

  <rules>
    <!-- All logs to console -->
    <logger name="*" minlevel="Trace" writeTo="console" />

    <!-- All logs to file -->
    <logger name="*" minlevel="Debug" writeTo="file" />

    <!-- Structured logs to JSON -->
    <logger name="*" minlevel="Info" writeTo="jsonFile" />
  </rules>
</nlog>
```
