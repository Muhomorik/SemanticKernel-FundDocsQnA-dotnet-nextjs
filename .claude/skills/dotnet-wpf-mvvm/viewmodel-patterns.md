# ViewModel Patterns Reference

Complete reference for ViewModel implementation patterns in WPF applications using DevExpress MVVM, Autofac DI, and Rx.NET.

## Table of Contents

- ViewModel Lifecycle
- Constructor Patterns (Runtime vs Design-Time)
- Property Change Notification
- Command Patterns
- Subscription Management with Rx.NET
- IDisposable Implementation
- Testing ViewModels

## ViewModel Lifecycle

### Creation

ViewModels are created in one of two contexts:

1. **Runtime** (via DI container): Uses constructor with dependencies
2. **Design-time** (by XAML designer): Uses parameterless constructor

### Initialization

Initialization happens in the `LoadedCommand` handler, not the constructor:

```csharp
public ICommand LoadedCommand { get; }

public MyViewModel(ILogger logger, IScheduler uiScheduler, IDataService dataService)
{
    _logger = logger;
    _uiScheduler = uiScheduler;
    _dataService = dataService;

    // Initialize commands
    LoadedCommand = new DelegateCommand(OnLoaded);

    // DO NOT start subscriptions here - View not loaded yet
}

private void OnLoaded()
{
    // NOW start subscriptions, load data
    _logger.Info("ViewModel loaded, starting initialization");

    // Start observables
    _dataService.DataStream
        .ObserveOn(_uiScheduler)
        .Subscribe(data => ProcessData(data))
        .DisposeWith(_disposables);
}
```

**Why wait for Loaded?**
- View is fully constructed and rendered
- Data binding is established
- Window handle is available for UI thread operations
- Avoids potential race conditions

### Disposal

ViewModels must implement `IDisposable` to clean up:

```csharp
public sealed class MyViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public void Dispose()
    {
        _disposables.Dispose();  // Disposes all subscriptions
        _logger.Info("ViewModel disposed");
    }
}
```

Dispose is called when:
- Window is closed
- ViewModel is replaced in a navigation scenario
- Application exits

## Constructor Patterns

### Runtime Constructor (DI)

Used when the ViewModel is resolved from the DI container at runtime:

```csharp
public MyViewModel(
    ILogger logger,                    // First parameter (convention)
    IScheduler uiScheduler,            // UI thread marshalling
    IDataService dataService,          // Business logic services
    IEventAggregator eventAggregator)  // Other dependencies
{
    // Validate required dependencies
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _uiScheduler = uiScheduler ?? throw new ArgumentNullException(nameof(uiScheduler));
    _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
    _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

    // Initialize commands
    LoadedCommand = new DelegateCommand(OnLoaded);
    SaveCommand = new AsyncCommand(SaveAsync, CanSave);
    CancelCommand = new DelegateCommand(OnCancel);

    // Initialize collections
    Items = new ObservableCollection<ItemViewModel>();

    // DO NOT start subscriptions here - wait for OnLoaded
}
```

### Design-Time Constructor

Used by Visual Studio/Rider XAML designer for preview and IntelliSense:

```csharp
public MyViewModel()
{
    // Provide safe defaults for designer
    _logger = LogManager.GetCurrentClassLogger();
    _uiScheduler = DispatcherScheduler.Current;

    // Initialize commands with no-ops
    LoadedCommand = new DelegateCommand(() => { });
    SaveCommand = new AsyncCommand(async () => { });
    CancelCommand = new DelegateCommand(() => { });

    // Initialize collections with sample data
    Items = new ObservableCollection<ItemViewModel>
    {
        new ItemViewModel { Name = "Sample Item 1" },
        new ItemViewModel { Name = "Sample Item 2" }
    };

    // Set sample property values
    Title = "Design-Time Preview";
    Status = "Ready";
}
```

**Design-time constructor best practices:**
- Never throw exceptions
- Provide reasonable default values
- Use sample data for collections
- Keep logic minimal
- Don't make network calls or access files

### Detecting Design-Time vs Runtime

If you need to conditionally execute logic:

```csharp
public MyViewModel(ILogger logger, IScheduler uiScheduler)
{
    _logger = logger;
    _uiScheduler = uiScheduler;

    if (IsInDesignMode())
    {
        // Design-time specific logic
        Items = CreateSampleData();
    }
    else
    {
        // Runtime specific logic
        Items = new ObservableCollection<Item>();
    }
}

private bool IsInDesignMode()
{
    return System.ComponentModel.DesignerProperties.GetIsInDesignMode(
        new System.Windows.DependencyObject());
}
```

However, prefer separate constructors over conditional logic.

## Property Change Notification

### Basic Pattern with SetProperty

```csharp
private string _status;

public string Status
{
    get => _status;
    set => SetProperty(ref _status, value, nameof(Status));
}
```

`SetProperty` from `ViewModelBase`:
- Compares new value with current value
- Only sets if different
- Raises `PropertyChanged` event
- Returns `true` if value changed, `false` otherwise

### Property with Side Effects

Execute logic when property changes:

```csharp
private string _searchText;

public string SearchText
{
    get => _searchText;
    set
    {
        if (SetProperty(ref _searchText, value, nameof(SearchText)))
        {
            // Property changed, perform search
            PerformSearch(_searchText);
        }
    }
}
```

### Computed Properties

Notify dependent properties when source changes:

```csharp
private string _firstName;
private string _lastName;

public string FirstName
{
    get => _firstName;
    set
    {
        if (SetProperty(ref _firstName, value, nameof(FirstName)))
        {
            RaisePropertyChanged(nameof(FullName));  // Notify dependent
        }
    }
}

public string LastName
{
    get => _lastName;
    set
    {
        if (SetProperty(ref _lastName, value, nameof(LastName)))
        {
            RaisePropertyChanged(nameof(FullName));  // Notify dependent
        }
    }
}

public string FullName => $"{FirstName} {LastName}";
```

### Observable Collections

Use `ObservableCollection<T>` for bindable collections.

**IMPORTANT:** Objects in `ObservableCollection<T>` MUST inherit `BindableBase` (or implement INPC).

```csharp
// ❌ BAD: Plain class - property changes won't update UI
public class FundItem
{
    public string Name { get; set; }
}

// ✅ GOOD: BindableBase for INPC support
public class FundItemViewModel : BindableBase
{
    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}

public ObservableCollection<FundItemViewModel> Items { get; }
```

`ObservableCollection<T>` only notifies when items are added/removed, NOT when item properties change.

## Command Patterns

### DelegateCommand

For synchronous operations:

```csharp
public ICommand ClearCommand { get; }

public MyViewModel(ILogger logger, IScheduler uiScheduler)
{
    _logger = logger;
    _uiScheduler = uiScheduler;

    // Command with execute and canExecute
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

Update `CanExecute` state:

```csharp
private void OnItemsChanged()
{
    (ClearCommand as DelegateCommand)?.RaiseCanExecuteChanged();
}
```

### AsyncCommand

For asynchronous operations:

```csharp
public ICommand SaveCommand { get; }

public MyViewModel(ILogger logger, IScheduler uiScheduler, IDataService dataService)
{
    _logger = logger;
    _uiScheduler = uiScheduler;
    _dataService = dataService;

    SaveCommand = new AsyncCommand(SaveAsync, CanSave);
}

private async Task SaveAsync()
{
    IsBusy = true;
    try
    {
        _logger.Info("Saving data...");
        await _dataService.SaveAsync(CurrentItem);

        Status = "Saved successfully";
        _logger.Info("Data saved successfully");
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Failed to save data");
        Status = $"Save failed: {ex.Message}";
    }
    finally
    {
        IsBusy = false;
    }
}

private bool CanSave()
{
    return CurrentItem != null && CurrentItem.IsValid && !IsBusy;
}
```

`AsyncCommand` automatically:
- Disables command during execution
- Handles exceptions
- Supports cancellation (if you provide `CancellationToken`)

### Command with Parameters

```csharp
public ICommand DeleteItemCommand { get; }

public MyViewModel(ILogger logger, IScheduler uiScheduler)
{
    _logger = logger;
    _uiScheduler = uiScheduler;

    DeleteItemCommand = new DelegateCommand<ItemViewModel>(OnDeleteItem, CanDeleteItem);
}

private void OnDeleteItem(ItemViewModel item)
{
    if (item != null)
    {
        Items.Remove(item);
        _logger.Info($"Deleted item: {item.Name}");
    }
}

private bool CanDeleteItem(ItemViewModel item)
{
    return item != null;
}
```

XAML binding:

```xml
<Button Content="Delete"
        Command="{Binding DeleteItemCommand}"
        CommandParameter="{Binding}" />
```

## Subscription Management with Rx.NET

### Creating Subscriptions

```csharp
private void OnLoaded()
{
    // Timer-based subscription
    Observable
        .Interval(TimeSpan.FromSeconds(5))
        .ObserveOn(_uiScheduler)
        .Subscribe(_ => RefreshStatus())
        .DisposeWith(_disposables);

    // Event-based subscription
    _eventAggregator.GetEvent<DataChangedEvent>()
        .ToObservable()
        .ObserveOn(_uiScheduler)
        .Subscribe(data => HandleDataChanged(data))
        .DisposeWith(_disposables);

    // Service stream subscription
    _dataService.DataStream
        .ObserveOn(_uiScheduler)
        .Subscribe(
            data => ProcessData(data),
            ex => _logger.Error(ex, "Error in data stream"),
            () => _logger.Info("Data stream completed"))
        .DisposeWith(_disposables);
}
```

### DisposeWith Extension

The `DisposeWith` extension method adds the subscription to `CompositeDisposable`:

```csharp
observable
    .Subscribe(x => HandleData(x))
    .DisposeWith(_disposables);
```

Equivalent to:

```csharp
var subscription = observable.Subscribe(x => HandleData(x));
_disposables.Add(subscription);
```

### ObserveOn for UI Thread Marshalling

Always use `ObserveOn(_uiScheduler)` before updating UI-bound properties:

```csharp
// ✓ CORRECT
Observable
    .Return(LoadDataAsync())
    .SelectMany(x => x)
    .ObserveOn(_uiScheduler)  // Switch to UI thread
    .Subscribe(data =>
    {
        DataItems = data;  // Safe: on UI thread
    })
    .DisposeWith(_disposables);

// ✗ WRONG
Observable
    .Return(LoadDataAsync())
    .SelectMany(x => x)
    .Subscribe(data =>
    {
        DataItems = data;  // ERROR: Cross-thread exception
    })
    .DisposeWith(_disposables);
```

### Combining Multiple Observables

```csharp
private void OnLoaded()
{
    // Combine latest values from two observables
    Observable
        .CombineLatest(
            _service1.DataStream,
            _service2.DataStream,
            (data1, data2) => new { Data1 = data1, Data2 = data2 })
        .ObserveOn(_uiScheduler)
        .Subscribe(combined =>
        {
            ProcessCombinedData(combined.Data1, combined.Data2);
        })
        .DisposeWith(_disposables);

    // Merge multiple observables into one
    Observable
        .Merge(
            _source1.Changes,
            _source2.Changes,
            _source3.Changes)
        .ObserveOn(_uiScheduler)
        .Subscribe(change => HandleChange(change))
        .DisposeWith(_disposables);
}
```

## IDisposable Implementation

### Basic Pattern

```csharp
public sealed class MyViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
```

### Disposing Custom Resources

If you have resources beyond Rx subscriptions:

```csharp
public sealed class MyViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IDataService _dataService;
    private Timer _timer;

    public void Dispose()
    {
        // Dispose Rx subscriptions
        _disposables.Dispose();

        // Dispose other resources
        _timer?.Dispose();

        // Clean up service resources if needed
        if (_dataService is IDisposable disposableService)
        {
            disposableService.Dispose();
        }
    }
}
```

### Preventing Multiple Disposal

```csharp
public sealed class MyViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposables.Dispose();
        _disposed = true;

        _logger.Debug("ViewModel disposed");
    }
}
```

## Testing ViewModels

### Unit Test Structure

```csharp
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using Microsoft.Reactive.Testing;

[TestFixture]
public class MainViewModelTests
{
    private IFixture _fixture;
    private Mock<ILogger> _loggerMock;
    private Mock<IDataService> _dataServiceMock;
    private TestScheduler _testScheduler;
    private MainViewModel _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _dataServiceMock = _fixture.Freeze<Mock<IDataService>>();

        // Use TestScheduler for testing Rx observables
        _testScheduler = new TestScheduler();

        _sut = new MainViewModel(
            _loggerMock.Object,
            _testScheduler,  // Use test scheduler instead of DispatcherScheduler
            _dataServiceMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _sut?.Dispose();
    }
}
```

### Testing Commands

```csharp
[Test]
public void ClearCommand_WhenItemsExist_ClearsItems()
{
    // Arrange
    _sut.Items.Add(new ItemViewModel { Name = "Item 1" });
    _sut.Items.Add(new ItemViewModel { Name = "Item 2" });

    // Act
    _sut.ClearCommand.Execute(null);

    // Assert
    Assert.That(_sut.Items, Is.Empty);
}

[Test]
public void ClearCommand_WhenNoItems_CannotExecute()
{
    // Arrange
    _sut.Items.Clear();

    // Act
    var canExecute = _sut.ClearCommand.CanExecute(null);

    // Assert
    Assert.That(canExecute, Is.False);
}
```

### Testing Async Commands

```csharp
[Test]
public async Task SaveCommand_WhenValid_SavesDataSuccessfully()
{
    // Arrange
    var testItem = _fixture.Create<ItemModel>();
    _sut.CurrentItem = testItem;

    _dataServiceMock
        .Setup(x => x.SaveAsync(testItem))
        .ReturnsAsync(true);

    // Act
    await ((AsyncCommand)_sut.SaveCommand).ExecuteAsync(null);

    // Assert
    _dataServiceMock.Verify(x => x.SaveAsync(testItem), Times.Once);
    Assert.That(_sut.Status, Does.Contain("success"));
}

[Test]
public async Task SaveCommand_WhenServiceFails_LogsError()
{
    // Arrange
    var testItem = _fixture.Create<ItemModel>();
    _sut.CurrentItem = testItem;

    var exception = new Exception("Save failed");
    _dataServiceMock
        .Setup(x => x.SaveAsync(testItem))
        .ThrowsAsync(exception);

    // Act
    await ((AsyncCommand)_sut.SaveCommand).ExecuteAsync(null);

    // Assert
    _loggerMock.Verify(
        x => x.Error(exception, It.IsAny<string>()),
        Times.Once);
    Assert.That(_sut.Status, Does.Contain("failed"));
}
```

### Testing Observables with TestScheduler

```csharp
[Test]
public void OnLoaded_StartsPolling_UpdatesStatusPeriodically()
{
    // Arrange
    var expectedStatuses = new List<string>();
    _sut.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(_sut.Status))
            expectedStatuses.Add(_sut.Status);
    };

    // Act
    _sut.LoadedCommand.Execute(null);

    // Advance test scheduler by 5 seconds
    _testScheduler.AdvanceBy(TimeSpan.FromSeconds(5).Ticks);

    // Assert
    Assert.That(expectedStatuses, Is.Not.Empty);
    Assert.That(_sut.Status, Does.Contain("Updated"));
}
```

### Testing Property Change Notification

```csharp
[Test]
public void Status_WhenChanged_RaisesPropertyChangedEvent()
{
    // Arrange
    var propertyChangedRaised = false;
    _sut.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(_sut.Status))
            propertyChangedRaised = true;
    };

    // Act
    _sut.Status = "New Status";

    // Assert
    Assert.That(propertyChangedRaised, Is.True);
}
```

## Common Patterns and Anti-Patterns

### ✓ DO: Initialize Commands in Constructor

```csharp
public MyViewModel(ILogger logger, IScheduler uiScheduler)
{
    _logger = logger;
    _uiScheduler = uiScheduler;

    LoadedCommand = new DelegateCommand(OnLoaded);
    SaveCommand = new AsyncCommand(SaveAsync);
}
```

### ✗ DON'T: Create Commands in Property Getters

```csharp
// This creates a new command instance every time the property is accessed
public ICommand SaveCommand => new AsyncCommand(SaveAsync);
```

### ✓ DO: Use ObserveOn Before Updating UI Properties

```csharp
_observable
    .ObserveOn(_uiScheduler)
    .Subscribe(data => Status = data);
```

### ✗ DON'T: Update UI Properties from Background Threads

```csharp
Task.Run(() =>
{
    Status = "Updated";  // Cross-thread exception
});
```

### ✓ DO: Dispose All Subscriptions

```csharp
_observable
    .Subscribe(x => HandleData(x))
    .DisposeWith(_disposables);
```

### ✗ DON'T: Forget to Dispose Subscriptions

```csharp
// Memory leak - subscription never disposed
_observable.Subscribe(x => HandleData(x));
```

### ✓ DO: Validate Dependencies in Constructor

```csharp
public MyViewModel(ILogger logger, IDataService dataService)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
}
```

### ✗ DON'T: Allow Null Dependencies

```csharp
public MyViewModel(ILogger logger, IDataService dataService)
{
    _logger = logger;  // No validation
    _dataService = dataService;
}
```
