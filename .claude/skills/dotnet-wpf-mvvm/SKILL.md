---
name: dotnet-wpf-mvvm
description: Implements Model-View-ViewModel patterns in .NET WPF desktop applications. Use when implementing MVVM architecture, creating or refactoring ViewModels, setting up data binding, implementing ICommand, converting windows to MahApps.Metro, or working with WPF projects (.csproj files, XAML views, C# code-behind). Supports DevExpress MVVM framework, Autofac dependency injection, and Rx.NET reactive patterns. DO NOT use for web frameworks (Next.js, React, JavaScript, TypeScript, Node.js).
allowed-tools: Read, Edit, Write, Bash, Grep, Glob
---

# Implementing MVVM in WPF (.NET Desktop Applications)

> **Scope**: This skill applies ONLY to .NET WPF (Windows Presentation Foundation) desktop applications.
>
> **Use for**: C# projects, .csproj files, XAML views, Windows desktop apps
>
> **DO NOT use for**: Next.js, React, web applications, JavaScript, TypeScript, Node.js, or any web frontend projects

Comprehensive guidance for implementing Model-View-ViewModel (MVVM) patterns in WPF applications using DevExpress MVVM, Autofac dependency injection, and Rx.NET for reactive programming.

## ⚠️ Check MS Learn Before Implementing Common Patterns

Before manually implementing WPF/Windows infrastructure (settings, validation, collections, etc.), **use the `microsoft_docs_search` MCP tool** to find built-in .NET solutions.

Examples of patterns with built-in support:

- User settings → `ApplicationSettingsBase`
- Input validation → `INotifyDataErrorInfo`
- Collection filtering/sorting → `ICollectionView`
- Weak events → `WeakEventManager`

**Always query MS Learn first** to avoid reinventing the wheel.

## Quick Start

### Minimal ViewModel

```csharp
using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;

namespace MyApp.ViewModels
{
    public sealed class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IScheduler _uiScheduler;
        private readonly CompositeDisposable _disposables = new();

        private string _title = "My Application";

        // Runtime constructor (DI)
        public MainViewModel(ILogger logger, IScheduler uiScheduler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uiScheduler = uiScheduler ?? throw new ArgumentNullException(nameof(uiScheduler));

            LoadedCommand = new DelegateCommand(OnLoaded);
        }

        // Design-time constructor
        public MainViewModel()
        {
            _logger = LogManager.GetCurrentClassLogger();
            _uiScheduler = DispatcherScheduler.Current;
            LoadedCommand = new DelegateCommand(() => { });
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value, nameof(Title));
        }

        public ICommand LoadedCommand { get; }

        private void OnLoaded()
        {
            _logger.Info("ViewModel loaded");
            // Initialize subscriptions, load data
        }

        public void Dispose() => _disposables.Dispose();
    }
}
```

### Minimal View (XAML)

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:dxmvvm="http://schemas.devexpress.com/winfx/2008/xaml/mvvm"
        xmlns:viewModels="clr-namespace:MyApp.ViewModels"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=viewModels:MainViewModel, IsDesignTimeCreatable=True}"
        Title="{Binding Title}"
        Width="800" Height="450">

    <!-- Bind Window Loaded event to ViewModel command -->
    <dxmvvm:Interaction.Behaviors>
        <dxmvvm:EventToCommand EventName="Loaded" Command="{Binding LoadedCommand}" />
    </dxmvvm:Interaction.Behaviors>

    <Grid>
        <TextBlock Text="Hello MVVM" />
    </Grid>
</Window>
```

## ViewModel Patterns

### Core Structure

ViewModels inherit from `DevExpress.Mvvm.ViewModelBase` and implement `IDisposable`:

```csharp
public sealed class MyViewModel : ViewModelBase, IDisposable
{
    // 1. ILogger first (convention)
    private readonly ILogger _logger;

    // 2. IScheduler for UI thread marshalling
    private readonly IScheduler _uiScheduler;

    // 3. Subscription management
    private readonly CompositeDisposable _disposables = new();

    // 4. Backing fields for properties
    private string _status = "Ready";

    // Runtime constructor with dependencies
    public MyViewModel(ILogger logger, IScheduler uiScheduler, /* other services */)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uiScheduler = uiScheduler ?? throw new ArgumentNullException(nameof(uiScheduler));

        // Initialize commands
        LoadedCommand = new DelegateCommand(OnLoaded);
        SaveCommand = new AsyncCommand(SaveAsync);
    }

    // Design-time constructor
    public MyViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _uiScheduler = DispatcherScheduler.Current;

        LoadedCommand = new DelegateCommand(() => { });
        SaveCommand = new AsyncCommand(async () => { });
    }

    public void Dispose() => _disposables.Dispose();
}
```

### Property Change Notification

Use `SetProperty` from `ViewModelBase` for automatic `INotifyPropertyChanged`:

```csharp
private string _status;

public string Status
{
    get => _status;
    set => SetProperty(ref _status, value, nameof(Status));
}
```

The `SetProperty` method:

- Compares new value with current value
- Sets the field if different
- Raises `PropertyChanged` event automatically

### Subscription Management

Use `CompositeDisposable` to manage Rx.NET subscriptions:

```csharp
private void OnLoaded()
{
    // Create observable subscription
    var subscription = Observable
        .Interval(TimeSpan.FromSeconds(1))
        .ObserveOn(_uiScheduler)  // Marshal to UI thread
        .Subscribe(tick =>
        {
            Status = $"Tick {tick}";  // Safe to update UI properties
        });

    // Add to disposables for automatic cleanup
    _disposables.Add(subscription);
}
```

When `Dispose()` is called, all subscriptions in `_disposables` are automatically disposed.

### UI Thread Marshalling

Always use `ObserveOn(_uiScheduler)` before updating UI-bound properties:

```csharp
// Background observable
Observable
    .Return(LoadDataAsync())
    .SelectMany(x => x)
    .ObserveOn(_uiScheduler)  // Switch to UI thread
    .Subscribe(data =>
    {
        DataItems = data;  // Safe: now on UI thread
    })
    .DisposeWith(_disposables);
```

**For complete ViewModel patterns, lifecycle management, and testing strategies, see [viewmodel-patterns.md](viewmodel-patterns.md).**

## Commands

### DelegateCommand (Synchronous)

For simple synchronous operations:

```csharp
public ICommand ClearCommand { get; }

public MyViewModel(ILogger logger, IScheduler uiScheduler)
{
    // ...
    ClearCommand = new DelegateCommand(OnClear, CanClear);
}

private void OnClear()
{
    Items.Clear();
    _logger.Info("Items cleared");
}

private bool CanClear()
{
    return Items.Count > 0;
}
```

### AsyncCommand (Asynchronous)

For async operations:

```csharp
public ICommand SaveCommand { get; }

public MyViewModel(ILogger logger, IScheduler uiScheduler, IDataService dataService)
{
    // ...
    SaveCommand = new AsyncCommand(SaveAsync, CanSave);
}

private async Task SaveAsync()
{
    IsBusy = true;
    try
    {
        await _dataService.SaveAsync(CurrentItem);
        Status = "Saved successfully";
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Failed to save");
        Status = "Save failed";
    }
    finally
    {
        IsBusy = false;
    }
}

private bool CanSave()
{
    return CurrentItem != null && CurrentItem.IsValid;
}
```

### Loaded Command Pattern

Wire Window `Loaded` event to ViewModel command using `EventToCommand`:

**ViewModel:**

```csharp
public ICommand LoadedCommand { get; }

public MyViewModel(ILogger logger, IScheduler uiScheduler)
{
    // ...
    LoadedCommand = new DelegateCommand(OnLoaded);
}

private void OnLoaded()
{
    // Initialize data, start subscriptions
    _logger.Info("View loaded, initializing...");

    // Example: Start polling
    Observable
        .Interval(TimeSpan.FromSeconds(5))
        .ObserveOn(_uiScheduler)
        .Subscribe(_ => RefreshData())
        .DisposeWith(_disposables);
}
```

**XAML:**

```xml
<Window ...>
    <dxmvvm:Interaction.Behaviors>
        <dxmvvm:EventToCommand EventName="Loaded" Command="{Binding LoadedCommand}" />
    </dxmvvm:Interaction.Behaviors>
    <!-- ... -->
</Window>
```

## ViewModel→View Communication

When ViewModel needs to trigger View-specific operations (WebView2 control, HWND manipulation, screenshots):

**Anti-pattern:**

```csharp
// ❌ EventHandler in ViewModel breaks layer separation
public event EventHandler? BrowserReloadRequested;
```

**Correct pattern:** Service interface

```csharp
// Interface in Presentation layer
public interface IBrowserController
{
    void Reload();
    void ScrollToEnd();
    Task<ImageSource?> CaptureScreenshotAsync();
}

// ViewModel depends on abstraction
public class MainWindowViewModel : ViewModelBase
{
    private readonly IBrowserController _browser;

    public MainWindowViewModel(ILogger logger, IScheduler uiScheduler, IBrowserController browser)
    {
        _browser = browser;
    }

    private void ExecuteReload() => _browser.Reload();
}

// Implementation wraps WebView2 (registered in DI)
public class WebView2BrowserController : IBrowserController
{
    private readonly WebView2 _webView;
    // ...
}
```

## View Patterns

### Design-Time DataContext

Enable design-time IntelliSense and preview by specifying design-time DataContext:

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:viewModels="clr-namespace:MyApp.ViewModels"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=viewModels:MainViewModel, IsDesignTimeCreatable=True}"
        Title="{Binding Title}">
    <!-- Content -->
</Window>
```

Key points:

- `d:DataContext` is design-time only (ignored at runtime)
- `IsDesignTimeCreatable=True` calls parameterless constructor
- Enables IntelliSense for bindings in Visual Studio/Rider

### EventToCommand Binding

Convert XAML events to ViewModel commands:

```xml
<!-- Window Loaded -->
<dxmvvm:Interaction.Behaviors>
    <dxmvvm:EventToCommand EventName="Loaded" Command="{Binding LoadedCommand}" />
</dxmvvm:Interaction.Behaviors>

<!-- Button Click -->
<Button Content="Save">
    <dxmvvm:Interaction.Behaviors>
        <dxmvvm:EventToCommand EventName="Click" Command="{Binding SaveCommand}" />
    </dxmvvm:Interaction.Behaviors>
</Button>

<!-- Or use Command property directly on Button -->
<Button Content="Save" Command="{Binding SaveCommand}" />
```

### MahApps.Metro MetroWindow

Convert standard WPF Window to MetroWindow for modern UI:

#### Step 1: Update App.xaml with resources

```xml
<Application x:Class="MyApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- MahApps.Metro resources (file names are Case Sensitive!) -->
                <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml" />
                <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml" />
                <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

#### Step 2: Convert Window to MetroWindow

```xml
<mah:MetroWindow x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mah="clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Title="My Application" Height="450" Width="800">
    <Grid>
        <!-- Content -->
    </Grid>
</mah:MetroWindow>
```

#### Step 3: Update code-behind (optional)

```csharp
// Option 1: Remove base class (partial class inherits from XAML)
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

// Option 2: Explicitly inherit from MetroWindow
public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

**For advanced UI patterns including attached behaviors and visual tree navigation, see [ui-patterns.md](ui-patterns.md).**

## Dependency Injection

### Complete PresentationModule Pattern

Register ViewModels, logging infrastructure, and views in a dedicated Autofac module:

```csharp
using Autofac;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using System.Reactive.Concurrency;

namespace MyApp.Wpf.Modules;

/// <summary>
/// Autofac module for registering presentation layer components (ViewModels, Views, etc.).
/// </summary>
public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // ===== LOGGING INFRASTRUCTURE =====

        // Register Microsoft.Extensions.Logging.ILoggerFactory backed by NLog
        // This allows services to use ILogger<T> while logging through NLog
        builder.Register<ILoggerFactory>(ctx => new NLogLoggerFactory())
            .As<ILoggerFactory>()
            .SingleInstance();

        // Register generic ILogger<T> using the ILoggerFactory
        // Ensures all ILogger<T> dependencies resolve to real loggers
        builder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>))
            .SingleInstance();

        // ===== VIEWMODEL REGISTRATION =====

        // Register all ViewModels with type-aware NLog.ILogger and UI scheduler injection
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(pi.Member.DeclaringType?.FullName ?? "Unknown")))
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(IScheduler),
                (pi, ctx) => DispatcherScheduler.Current))
            .InstancePerDependency();

        // ===== VIEW REGISTRATION =====

        // Register MainWindow
        builder.RegisterType<MainWindow>()
            .AsSelf()
            .InstancePerDependency();

        // Register other views/windows (if needed)
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => (t.Name.EndsWith("View") || t.Name.EndsWith("Window"))
                     && t != typeof(MainWindow))
            .AsSelf()
            .InstancePerDependency();
    }
}
```

**Key Points:**

1. **NLog.ILogger Injection**: Each ViewModel receives `NLog.ILogger` with its type name for better log filtering
2. **IScheduler Injection**: `DispatcherScheduler.Current` is injected for UI thread marshalling
3. **Type-Aware Logging**: `LogManager.GetLogger(pi.Member.DeclaringType?.FullName)` creates logger with ViewModel's full type name
4. **ILoggerFactory**: Supports both `NLog.ILogger` and `Microsoft.Extensions.Logging.ILogger<T>` in the same app

### NLog Configuration

Create `NLog.config` in your WPF project root with `CopyToOutputDirectory=PreserveNewest`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      throwConfigExceptions="true">

  <targets>
    <!-- Debugger output target (Visual Studio Output window) -->
    <target xsi:type="Debugger"
            name="debugger"
            layout="${longdate} [${level:uppercase=true}] ${logger:shortName=true} - ${message} ${exception:format=tostring}" />

    <!-- File output target (optional) -->
    <target xsi:type="File"
            name="logfile"
            fileName="${basedir}/logs/${shortdate}.log"
            layout="${longdate} [${level:uppercase=true}] ${logger} - ${message} ${exception:format=tostring}" />
  </targets>

  <rules>
    <!-- All loggers (NLog.ILogger and Microsoft.Extensions.Logging.ILogger<T>) -->
    <logger name="*" minlevel="Debug" writeTo="debugger" />
    <logger name="*" minlevel="Info" writeTo="logfile" />
  </rules>
</nlog>
```

**Update .csproj to copy NLog.config:**

```xml
<ItemGroup>
  <Content Include="NLog.config">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### App Startup (App.xaml.cs)

Configure container, register modules, and launch main window:

```csharp
using System;
using System.Linq;
using System.Windows;
using Autofac;
using MyApp.Wpf.Modules;
using MyApp.Wpf.ViewModels;

namespace MyApp.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IContainer? _container;
    private ILifetimeScope? _appScope;

    /// <summary>
    /// Handles application startup, configures DI container, and launches main window.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build DI container
        var builder = new ContainerBuilder();

        // Register presentation module explicitly
        builder.RegisterModule<PresentationModule>();

        // Auto-discover and register modules from other assemblies (Infrastructure, Application, Domain layers)
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("MyApp") == true
                     && !a.FullName.Contains("Wpf"))  // Exclude Wpf assembly (already registered above)
            .ToArray();

        builder.RegisterAssemblyModules(assemblies);

        _container = builder.Build();
        _appScope = _container.BeginLifetimeScope();

        // Create window and ViewModel
        var mainWindow = _appScope.Resolve<MainWindow>();
        var mainViewModel = _appScope.Resolve<MainViewModel>();

        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();
    }

    /// <summary>
    /// Handles application exit, disposes DI container.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        _appScope?.Dispose();
        _container?.Dispose();
        base.OnExit(e);
    }
}
```

**Key Points:**

1. **Explicit PresentationModule Registration**: Register the presentation module first before auto-discovery
2. **Assembly Filtering**: Exclude the Wpf assembly from auto-discovery (already registered via PresentationModule)
3. **Lifetime Scope**: Create app-level lifetime scope for proper disposal hierarchy
4. **Manual DataContext Assignment**: Resolve window and ViewModel separately, then assign DataContext
5. **Proper Disposal**: Dispose both lifetime scope and container on exit

**For complete DI setup including window providers, background schedulers, and advanced registration patterns, see [dependency-injection.md](dependency-injection.md).**

## Best Practices

### 1. ILogger First Parameter Convention

Always make `ILogger` the first constructor parameter:

```csharp
public MyViewModel(
    ILogger logger,              // First
    IScheduler uiScheduler,      // Second
    IDataService dataService,    // Other dependencies
    IEventAggregator events)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    // ...
}
```

This convention:

- Makes logging dependency explicit
- Consistent across all ViewModels
- Simplifies automated logger injection

### 2. Always Use UI Scheduler for Property Updates

When updating UI-bound properties from background observables:

```csharp
// ✓ CORRECT: ObserveOn before updating properties
Observable
    .Timer(TimeSpan.FromSeconds(1))
    .ObserveOn(_uiScheduler)
    .Subscribe(_ => Status = "Updated")
    .DisposeWith(_disposables);

// ✗ WRONG: Updating UI property from background thread
Observable
    .Timer(TimeSpan.FromSeconds(1))
    .Subscribe(_ => Status = "Updated")  // Cross-thread exception!
    .DisposeWith(_disposables);
```

### 3. Dispose Subscriptions with CompositeDisposable

Always add subscriptions to `CompositeDisposable`:

```csharp
private readonly CompositeDisposable _disposables = new();

private void OnLoaded()
{
    var sub1 = _observable1.Subscribe(x => HandleData(x));
    _disposables.Add(sub1);

    // Or use DisposeWith extension
    _observable2
        .Subscribe(x => HandleMore(x))
        .DisposeWith(_disposables);
}

public void Dispose()
{
    _disposables.Dispose();  // Disposes all subscriptions
}
```

Prevents memory leaks from undisposed subscriptions.

### 4. Avoid Code-Behind Logic

Keep Views declarative, move logic to ViewModels:

```csharp
// ✗ WRONG: Logic in code-behind
private void Button_Click(object sender, RoutedEventArgs e)
{
    var result = CalculateSomething();
    TextBlock.Text = result.ToString();
}

// ✓ CORRECT: Use commands and binding
// XAML: <Button Command="{Binding CalculateCommand}" />
// XAML: <TextBlock Text="{Binding Result}" />

// ViewModel:
public ICommand CalculateCommand { get; }

private void OnCalculate()
{
    Result = CalculateSomething().ToString();
}
```

### 5. Keep ViewModels Testable

Design ViewModels for testability:

```csharp
// ✓ Testable: Dependencies injected
public class MainViewModel : ViewModelBase
{
    private readonly IDataService _dataService;

    public MainViewModel(ILogger logger, IScheduler scheduler, IDataService dataService)
    {
        _dataService = dataService;
        // ...
    }

    public async Task LoadDataAsync()
    {
        var data = await _dataService.GetDataAsync();
        Items = data;
    }
}

// Test:
var mockService = new Mock<IDataService>();
mockService.Setup(x => x.GetDataAsync()).ReturnsAsync(testData);

var vm = new MainViewModel(logger, TestScheduler.Default, mockService.Object);
await vm.LoadDataAsync();

Assert.That(vm.Items, Is.EqualTo(testData));
```

### 6. Use Design-Time Constructors

Always provide parameterless constructor for designer support:

```csharp
// Runtime constructor (used at runtime via DI)
public MainViewModel(ILogger logger, IScheduler uiScheduler)
{
    _logger = logger;
    _uiScheduler = uiScheduler;
}

// Design-time constructor (used by Visual Studio designer)
public MainViewModel()
{
    _logger = LogManager.GetCurrentClassLogger();
    _uiScheduler = DispatcherScheduler.Current;
}
```

Enables XAML designer to create ViewModel instance for preview.

## Checklist

- [ ] ViewModels inherit `ViewModelBase` and implement `IDisposable`
- [ ] `ILogger` is first constructor parameter
- [ ] `IScheduler` is second constructor parameter
- [ ] Constructor parameters null-checked with `?? throw new ArgumentNullException`
- [ ] Design-time constructor provided (parameterless)
- [ ] `CompositeDisposable` for subscription management
- [ ] `ObserveOn(_uiScheduler)` before updating UI-bound properties
- [ ] Subscriptions added to `_disposables`
- [ ] `ObservableCollection<T>` items inherit `BindableBase`
- [ ] Commands initialized in constructor
- [ ] No logic in code-behind (use commands and bindings)
- [ ] `d:DataContext` set for design-time IntelliSense

## Additional Resources

- **[viewmodel-patterns.md](viewmodel-patterns.md)** - Complete ViewModel lifecycle, patterns, and testing
- **[dependency-injection.md](dependency-injection.md)** - Autofac setup, module configuration, advanced DI patterns
- **[ui-patterns.md](ui-patterns.md)** - MahApps.Metro integration, Fluent Icons, XAML patterns, attached behaviors

## Related Skills

Cross-cutting concerns are in separate skills for better auto-loading:

- **`dotnet-nlog-logging`** - NLog.ILogger conventions
- **`dotnet-reactive-patterns`** - Rx.NET, CompositeDisposable, event publishing
- **`dotnet-documentation`** - XML docs, DebuggerDisplay attributes
