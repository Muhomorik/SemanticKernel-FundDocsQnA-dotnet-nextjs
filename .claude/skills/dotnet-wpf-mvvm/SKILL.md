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

ViewModels inherit from `DevExpress.Mvvm.ViewModelBase`, implement `IDisposable`, and follow this structure:

- **Field order**: `ILogger` → `IScheduler` → `CompositeDisposable` → backing fields
- **Two constructors**: runtime (DI with null-checks) + design-time (parameterless, safe defaults)
- **Properties**: use `SetProperty(ref _field, value, nameof(Prop))` for change notification
- **Subscriptions**: always `ObserveOn(_uiScheduler)` before updating UI-bound properties, always `.DisposeWith(_disposables)`

**For complete patterns, lifecycle management, and testing strategies, see [viewmodel-patterns.md](viewmodel-patterns.md).**

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

### Window Closing Lifecycle

Use `CurrentWindowService` to handle window closing at the ViewModel level — no code-behind needed for disposal.

**XAML** — add alongside `EventToCommand`:

```xml
<dxmvvm:Interaction.Behaviors>
    <dxmvvm:EventToCommand EventName="Loaded" Command="{Binding LoadedCommand}" />
    <dxmvvm:CurrentWindowService ClosingCommand="{Binding WindowClosingCommand}" />
</dxmvvm:Interaction.Behaviors>
```

**ViewModel** — self-disposes on close, programmatic close via service:

```csharp
public ICommand WindowClosingCommand { get; }

protected ICurrentWindowService CurrentWindowService => GetService<ICurrentWindowService>();

public MyWindowViewModel(ILogger logger, IScheduler uiScheduler)
{
    // ...
    WindowClosingCommand = new DelegateCommand(ExecuteWindowClosing);
}

private void ExecuteWindowClosing()
{
    Dispose();
}

// Call from a command when the ViewModel needs to close its own window
private void ExecuteClose()
{
    CurrentWindowService?.Close();
}
```

**Key points:**

- `ClosingCommand` fires on `Window.Closing` (before close) — receives `CancelEventArgs` if you need to cancel
- `ICurrentWindowService.Close()` replaces `CloseRequested` events for ViewModel→View close requests
- Code-behind `OnClosed` should only handle view-owned resources (e.g., native control disposal, HWND cleanup)

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

**For MahApps.Metro MetroWindow conversion, attached behaviors, and visual tree navigation, see [ui-patterns.md](ui-patterns.md).**

## Dependency Injection

Uses Autofac with one module per layer. Key conventions:

- **PresentationModule** registers ViewModels (convention-based), Views, logging, and UI scheduler
- **Type-aware NLog.ILogger** injection via `ResolvedParameter` — each ViewModel gets logger named after its type
- **IScheduler** injection — `DispatcherScheduler.Current` for UI thread marshalling
- **App.xaml.cs** builds container, resolves MainWindow + ViewModel, assigns DataContext, disposes on exit
- **ViewModel disposal on window close** via `CurrentWindowService.ClosingCommand` (not code-behind)

**For complete setup (PresentationModule, NLog config, App.xaml.cs, window providers, advanced patterns), see [dependency-injection.md](dependency-injection.md).**

## Best Practices

1. **ILogger first parameter** — convention across all ViewModels, enables automated logger injection
2. **ObserveOn before UI updates** — always `ObserveOn(_uiScheduler)` before setting UI-bound properties
3. **DisposeWith for all subscriptions** — every `.Subscribe()` must end with `.DisposeWith(_disposables)`
4. **No logic in code-behind** — use commands and bindings; code-behind only for view-owned resources (HWND, native controls)
5. **Testable ViewModels** — inject all dependencies, use `TestScheduler` in tests
6. **Design-time constructors** — parameterless constructor with safe defaults for XAML designer

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
- [ ] `CurrentWindowService` with `ClosingCommand` for ViewModel disposal on window close
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
- **`wpf-fluent-design`** — Fluent v2 design tokens, MahApps theming, XAML styling patterns
