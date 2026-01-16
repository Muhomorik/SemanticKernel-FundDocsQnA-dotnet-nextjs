---
name: implementing-mvvm-wpf
description: Implements Model-View-ViewModel patterns in .NET WPF (Windows Presentation Foundation) desktop applications using DevExpress MVVM, Autofac DI, and Rx.NET. Use ONLY for .NET WPF projects with .csproj files, C# code, XAML views, and Windows desktop applications. Use when creating ViewModels, Views, implementing ICommand, data binding, or setting up MVVM architecture in WPF. DO NOT use for Next.js, React, web frameworks, JavaScript, TypeScript, or Node.js projects.
allowed-tools: Read, Edit, Write, Grep, Glob
---

# Implementing MVVM in WPF (.NET Desktop Applications)

> **Scope**: This skill applies ONLY to .NET WPF (Windows Presentation Foundation) desktop applications.
>
> **Use for**: C# projects, .csproj files, XAML views, Windows desktop apps
>
> **DO NOT use for**: Next.js, React, web applications, JavaScript, TypeScript, Node.js, or any web frontend projects

Comprehensive guidance for implementing Model-View-ViewModel (MVVM) patterns in WPF applications using DevExpress MVVM, Autofac dependency injection, and Rx.NET for reactive programming.

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

**Step 1: Update App.xaml with resources**

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

**Step 2: Convert Window to MetroWindow**

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

**Step 3: Update code-behind (optional)**

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

### ViewModel Registration

Register ViewModels with assembly scanning in Autofac module:

```csharp
using Autofac;
using System.Reactive.Concurrency;

public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register all ViewModels with UI scheduler injection
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(IScheduler),
                (pi, ctx) => DispatcherScheduler.Current))
            .InstancePerDependency();

        // Register Views (optional, if resolving via DI)
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("View") || t.Name.EndsWith("Window"))
            .AsSelf()
            .InstancePerDependency();
    }
}
```

### ILogger Registration (Type-Aware)

Automatically inject logger with component's type name:

```csharp
// In your module or container setup
builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
    .Where(t => t.Name.EndsWith("ViewModel"))
    .AsSelf()
    .WithParameter(new Autofac.Core.ResolvedParameter(
        (pi, ctx) => pi.ParameterType == typeof(ILogger),
        (pi, ctx) => LogManager.GetLogger(pi.Member.DeclaringType?.FullName ?? "Unknown")))
    .InstancePerDependency();
```

This ensures each ViewModel gets a logger with its own type name for better log filtering.

### App Startup (App.xaml.cs)

Configure container and launch main window:

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

        // Build DI container
        var builder = new ContainerBuilder();

        // Auto-discover modules from assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("MyApp") == true)
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

    protected override void OnExit(ExitEventArgs e)
    {
        _appScope?.Dispose();
        _container?.Dispose();
        base.OnExit(e);
    }
}
```

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

## Additional Resources

- **[viewmodel-patterns.md](viewmodel-patterns.md)** - Complete ViewModel lifecycle, patterns, and testing
- **[dependency-injection.md](dependency-injection.md)** - Autofac setup, module configuration, advanced DI patterns
- **[ui-patterns.md](ui-patterns.md)** - MahApps.Metro integration, XAML patterns, attached behaviors
