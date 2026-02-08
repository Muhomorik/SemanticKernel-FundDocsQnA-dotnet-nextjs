# Dependency Injection Reference

Complete reference for setting up Autofac dependency injection in WPF applications using MVVM patterns.

## Table of Contents

- Autofac Module Setup
- ViewModel Registration
- ILogger Registration (Type-Aware)
- IScheduler Registration (UI and Background)
- Window Provider Pattern
- App Lifecycle Management
- Container Disposal
- Advanced Registration Patterns

## Autofac Module Setup

### Module per Layer Pattern

Create one Autofac module per layer/project:

```csharp
// Presentation layer module
using Autofac;

namespace MyApp.Modules
{
    public class PresentationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register ViewModels
            builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
                .Where(t => t.Name.EndsWith("ViewModel"))
                .AsSelf()
                .InstancePerDependency();

            // Register Views
            builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
                .Where(t => t.Name.EndsWith("View") || t.Name.EndsWith("Window"))
                .AsSelf()
                .InstancePerDependency();
        }
    }
}
```

### Application Module

```csharp
// Application services module
using Autofac;

namespace MyApp.Application.Modules
{
    public class ApplicationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register application services
            builder.RegisterAssemblyTypes(typeof(ApplicationModule).Assembly)
                .Where(t => t.Name.EndsWith("Service") || t.Name.EndsWith("Handler"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }
    }
}
```

### Infrastructure Module

```csharp
// Infrastructure layer module
using Autofac;

namespace MyApp.Infrastructure.Modules
{
    public class InfrastructureModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register repositories
            builder.RegisterAssemblyTypes(typeof(InfrastructureModule).Assembly)
                .Where(t => t.Name.EndsWith("Repository"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            // Register external service adapters
            builder.RegisterAssemblyTypes(typeof(InfrastructureModule).Assembly)
                .Where(t => t.Name.EndsWith("Adapter"))
                .AsImplementedInterfaces()
                .SingleInstance();
        }
    }
}
```

## ViewModel Registration

### Basic ViewModel Registration

```csharp
public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register all ViewModels by convention
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .InstancePerDependency();
    }
}
```

### ViewModel Registration with UI Scheduler

Inject `DispatcherScheduler.Current` for UI thread marshalling:

```csharp
using System.Reactive.Concurrency;
using Autofac;

public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(IScheduler),
                (pi, ctx) => DispatcherScheduler.Current))
            .InstancePerDependency();
    }
}
```

### Specific ViewModel Registration

Override convention for specific ViewModels:

```csharp
public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Convention-based registration for most ViewModels
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel") && t != typeof(MainViewModel))
            .AsSelf()
            .InstancePerDependency();

        // Specific registration for MainViewModel
        builder.RegisterType<MainViewModel>()
            .AsSelf()
            .As<IMainViewModel>()
            .WithParameter(new TypedParameter(typeof(IScheduler), DispatcherScheduler.Current))
            .InstancePerDependency();
    }
}
```

## ILogger Registration (Type-Aware)

> **Note:** For NLog conventions (deferred formatting, exception logging, log levels), see `dotnet-nlog-logging` skill.

### Automatic Logger Injection

Inject logger with component's type name automatically:

```csharp
using NLog;
using Autofac;

public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(ILogger),
                (pi, ctx) => LogManager.GetLogger(pi.Member.DeclaringType?.FullName ?? "Unknown")))
            .InstancePerDependency();
    }
}
```

This ensures each ViewModel gets a logger with its own type name:
- `MainViewModel` gets logger named `"MyApp.ViewModels.MainViewModel"`
- `SettingsViewModel` gets logger named `"MyApp.ViewModels.SettingsViewModel"`

### Combined Logger and Scheduler Injection

```csharp
using System.Reactive.Concurrency;
using NLog;
using Autofac;

public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(ILogger),
                (pi, ctx) => LogManager.GetLogger(pi.Member.DeclaringType?.FullName ?? "Unknown")))
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(IScheduler),
                (pi, ctx) => DispatcherScheduler.Current))
            .InstancePerDependency();
    }
}
```

## IScheduler Registration

### UI Scheduler Registration

Register UI scheduler for ViewModels:

```csharp
using System.Reactive.Concurrency;
using Autofac;

public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register UI scheduler as named dependency
        builder.Register(c => DispatcherScheduler.Current)
            .Named<IScheduler>("UIScheduler")
            .SingleInstance();

        // Use in ViewModel registration
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(IScheduler),
                (pi, ctx) => ctx.ResolveNamed<IScheduler>("UIScheduler")))
            .InstancePerDependency();
    }
}
```

### Background Scheduler Registration

Register background scheduler for services:

```csharp
using System.Reactive.Concurrency;
using Autofac;

public class InfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register background scheduler
        builder.RegisterInstance(TaskPoolScheduler.Default)
            .Named<IScheduler>("BackgroundScheduler")
            .As<IScheduler>();

        // Register services with background scheduler
        builder.RegisterType<PollingService>()
            .As<IPollingService>()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(IScheduler),
                (pi, ctx) => ctx.ResolveNamed<IScheduler>("BackgroundScheduler")))
            .SingleInstance();
    }
}
```

### Multiple Schedulers Pattern

When components need both UI and background schedulers:

```csharp
public class MyViewModel
{
    private readonly IScheduler _uiScheduler;
    private readonly IScheduler _backgroundScheduler;

    public MyViewModel(
        ILogger logger,
        [Named("UIScheduler")] IScheduler uiScheduler,
        [Named("BackgroundScheduler")] IScheduler backgroundScheduler)
    {
        _logger = logger;
        _uiScheduler = uiScheduler;
        _backgroundScheduler = backgroundScheduler;
    }
}
```

Registration:

```csharp
builder.RegisterType<MyViewModel>()
    .AsSelf()
    .WithParameter(new Autofac.Core.ResolvedParameter(
        (pi, ctx) => pi.ParameterType == typeof(IScheduler) && pi.Name == "uiScheduler",
        (pi, ctx) => ctx.ResolveNamed<IScheduler>("UIScheduler")))
    .WithParameter(new Autofac.Core.ResolvedParameter(
        (pi, ctx) => pi.ParameterType == typeof(IScheduler) && pi.Name == "backgroundScheduler",
        (pi, ctx) => ctx.ResolveNamed<IScheduler>("BackgroundScheduler")))
    .InstancePerDependency();
```

## Window Provider Pattern

### Window Provider Interface

```csharp
public interface IWindowProvider
{
    Window? MainWindow { get; }
}

public interface IWindowProviderWithSettableMainWindow : IWindowProvider
{
    new Window? MainWindow { get; set; }
}
```

### Window Provider Implementation

```csharp
using System.Windows;

public class WindowProvider : IWindowProvider, IWindowProviderWithSettableMainWindow
{
    public Window? MainWindow { get; set; }
}
```

### Registration

```csharp
public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register window provider as singleton
        builder.RegisterType<WindowProvider>()
            .As<IWindowProvider>()
            .As<IWindowProviderWithSettableMainWindow>()
            .SingleInstance();

        // Register main window
        builder.RegisterType<MainWindow>()
            .AsSelf();
    }
}
```

### Usage in App.xaml.cs

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    var builder = new ContainerBuilder();
    builder.RegisterAssemblyModules(assemblies);

    _container = builder.Build();
    _appScope = _container.BeginLifetimeScope();

    // Create main window
    var mainWindow = _appScope.Resolve<MainWindow>();

    // Set window in provider BEFORE resolving ViewModels
    var windowProvider = _appScope.Resolve<IWindowProviderWithSettableMainWindow>();
    windowProvider.MainWindow = mainWindow;

    // Now resolve ViewModel (can access MainWindow via IWindowProvider)
    var mainViewModel = _appScope.Resolve<MainViewModel>();

    mainWindow.DataContext = mainViewModel;
    mainWindow.Show();
}
```

### Using Window Provider in ViewModels

```csharp
public class DialogViewModel
{
    private readonly IWindowProvider _windowProvider;

    public DialogViewModel(ILogger logger, IScheduler uiScheduler, IWindowProvider windowProvider)
    {
        _logger = logger;
        _uiScheduler = uiScheduler;
        _windowProvider = windowProvider;
    }

    private void ShowDialog()
    {
        var dialog = new MyDialog
        {
            Owner = _windowProvider.MainWindow  // Set owner for modal dialog
        };

        dialog.ShowDialog();
    }
}
```

## App Lifecycle Management

### Complete App.xaml.cs Setup

```csharp
using Autofac;
using System;
using System.Linq;
using System.Windows;

public partial class App : Application
{
    private IContainer? _container;
    private ILifetimeScope? _appScope;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Initialize logging early
        InitializeLogging();

        // 2. Build DI container
        var builder = new ContainerBuilder();

        // Auto-discover modules from assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("MyApp") == true)
            .ToArray();

        builder.RegisterAssemblyModules(assemblies);

        _container = builder.Build();
        _appScope = _container.BeginLifetimeScope();

        // 3. Create main window
        var mainWindow = _appScope.Resolve<MainWindow>();

        // 4. Set window provider before resolving ViewModels
        var windowProvider = _appScope.Resolve<IWindowProviderWithSettableMainWindow>();
        windowProvider.MainWindow = mainWindow;

        // 5. Resolve main ViewModel
        var mainViewModel = _appScope.Resolve<MainViewModel>();

        // 6. Bind and show
        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appScope?.Dispose();
        _container?.Dispose();
        base.OnExit(e);
    }

    private void InitializeLogging()
    {
        // Initialize NLog from config file
        var config = new NLog.Config.XmlLoggingConfiguration("NLog.config");
        NLog.LogManager.Configuration = config;
    }
}
```

### Handling Unhandled Exceptions

```csharp
public partial class App : Application
{
    private ILogger _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        InitializeLogging();
        _logger = NLog.LogManager.GetCurrentClassLogger();

        // Handle unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // ... rest of startup
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _logger.Fatal(exception, "Unhandled exception");
        MessageBox.Show($"A fatal error occurred: {exception?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.Error(e.Exception, "Unhandled dispatcher exception");
        MessageBox.Show($"An error occurred: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;  // Prevent application crash
    }

    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();  // Prevent process termination
    }
}
```

## Container Disposal

### Proper Cleanup

```csharp
protected override void OnExit(ExitEventArgs e)
{
    try
    {
        _logger?.Info("Application exiting, disposing resources");

        // Dispose ViewModels that need cleanup
        if (_appScope != null)
        {
            // ViewModels registered as InstancePerDependency will be disposed
            // when their scope is disposed
            _appScope.Dispose();
        }

        // Dispose container
        _container?.Dispose();

        _logger?.Info("Application resources disposed successfully");
    }
    catch (Exception ex)
    {
        _logger?.Error(ex, "Error during application shutdown");
    }
    finally
    {
        base.OnExit(e);
    }
}
```

### Dispose ViewModels on Window Close

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object sender, EventArgs e)
    {
        // Dispose ViewModel if it implements IDisposable
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
```

## Advanced Registration Patterns

### Keyed Services

Register multiple implementations of the same interface:

```csharp
builder.RegisterType<FileLogger>()
    .Keyed<ILogger>("file")
    .SingleInstance();

builder.RegisterType<ConsoleLogger>()
    .Keyed<ILogger>("console")
    .SingleInstance();

// Resolve specific implementation
var fileLogger = container.ResolveKeyed<ILogger>("file");
```

### Conditional Registration

Register types based on configuration:

```csharp
public class InfrastructureModule : Module
{
    private readonly AppConfiguration _config;

    public InfrastructureModule(AppConfiguration config)
    {
        _config = config;
    }

    protected override void Load(ContainerBuilder builder)
    {
        if (_config.UseRealDatabase)
        {
            builder.RegisterType<SqlServerRepository>()
                .As<IDataRepository>()
                .InstancePerLifetimeScope();
        }
        else
        {
            builder.RegisterType<InMemoryRepository>()
                .As<IDataRepository>()
                .SingleInstance();
        }
    }
}
```

### Property Injection

Inject properties after construction:

```csharp
builder.RegisterType<MainViewModel>()
    .AsSelf()
    .PropertiesAutowired()  // Automatically inject public properties
    .InstancePerDependency();
```

ViewModel with property injection:

```csharp
public class MainViewModel : ViewModelBase
{
    // Constructor injection (required dependencies)
    public MainViewModel(ILogger logger, IScheduler uiScheduler)
    {
        _logger = logger;
        _uiScheduler = uiScheduler;
    }

    // Property injection (optional dependencies)
    public IOptionalService? OptionalService { get; set; }
}
```

### Decorator Pattern

Wrap registrations with decorators:

```csharp
// Register base implementation
builder.RegisterType<DataService>()
    .Named<IDataService>("base")
    .InstancePerLifetimeScope();

// Register decorator
builder.RegisterDecorator<IDataService>(
    (context, service) => new CachingDataService(service, context.Resolve<ICache>()),
    fromKey: "base");
```

### Module with Parameters

Pass parameters to modules:

```csharp
public class ConfigurableModule : Module
{
    private readonly string _connectionString;

    public ConfigurableModule(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(c => new DatabaseConnection(_connectionString))
            .As<IConnection>()
            .SingleInstance();
    }
}

// Usage
var builder = new ContainerBuilder();
builder.RegisterModule(new ConfigurableModule(connectionString));
```

## Lifetime Scopes

### Available Scopes

1. **InstancePerDependency** (Transient): New instance every time
   - Use for: ViewModels, lightweight services

2. **SingleInstance** (Singleton): One instance for entire application
   - Use for: Stateless services, repositories, configuration

3. **InstancePerLifetimeScope**: One instance per scope
   - Use for: Services that maintain state within a request/operation

### Choosing Lifetimes

```csharp
public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Transient: New instance each time
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .InstancePerDependency();

        // Singleton: One instance for application
        builder.RegisterType<ConfigurationService>()
            .As<IConfigurationService>()
            .SingleInstance();

        // Scoped: One instance per lifetime scope
        builder.RegisterType<UnitOfWork>()
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();
    }
}
```

## Validation and Diagnostics

### Validate Container Configuration

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    var builder = new ContainerBuilder();
    builder.RegisterAssemblyModules(assemblies);

    _container = builder.Build();

    // Validate critical registrations
    ValidateContainer(_container);

    _appScope = _container.BeginLifetimeScope();
    // ... rest of startup
}

private void ValidateContainer(IContainer container)
{
    try
    {
        // Try to resolve critical types
        using (var scope = container.BeginLifetimeScope())
        {
            var mainWindow = scope.Resolve<MainWindow>();
            var mainViewModel = scope.Resolve<MainViewModel>();
            var logger = scope.Resolve<ILogger>();

            _logger.Info("Container validation passed");
        }
    }
    catch (Exception ex)
    {
        _logger.Fatal(ex, "Container validation failed");
        throw new InvalidOperationException("DI container configuration is invalid", ex);
    }
}
```

### Debug Container Contents

```csharp
private void LogContainerRegistrations(IContainer container)
{
    var registrations = container.ComponentRegistry.Registrations
        .SelectMany(r => r.Services.OfType<TypedService>())
        .Select(s => s.ServiceType.Name)
        .OrderBy(name => name);

    _logger.Debug("Registered types:");
    foreach (var name in registrations)
    {
        _logger.Debug($"  - {name}");
    }
}
```
