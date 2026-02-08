# UI Patterns Reference

Complete reference for WPF UI patterns including MahApps.Metro integration, XAML binding patterns, and visual tree manipulation.

## Table of Contents

- MahApps.Metro Integration
- Design-Time DataContext
- EventToCommand Binding
- Data Binding Patterns
- Visual Tree Navigation
- Attached Behaviors
- Common XAML Patterns

## Fluent UI System Icons

Use Windows built-in Segoe Fluent Icons font for icons (no NuGet packages needed):

```xml
<TextBlock Text="&#xE8BB;" FontFamily="Segoe Fluent Icons" FontSize="16" />
<TextBlock Text="&#xE74D;" FontFamily="Segoe Fluent Icons" FontSize="16" />  <!-- Delete -->
<TextBlock Text="&#xE710;" FontFamily="Segoe Fluent Icons" FontSize="16" />  <!-- Add -->
<TextBlock Text="&#xE713;" FontFamily="Segoe Fluent Icons" FontSize="16" />  <!-- Settings -->
```

Use `microsoft_docs_search` MCP tool to find icon codes (search "Segoe Fluent Icons").

## MahApps.Metro Integration

### Complete Setup Guide

MahApps.Metro provides modern, flat UI styling for WPF applications.

**Step 1: Update App.xaml Resource Dictionaries**

Add MahApps.Metro resource dictionaries. **All file names are Case Sensitive!**

```xml
<Application x:Class="MyApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- MahApps.Metro resources (file names Case Sensitive!) -->
                <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml" />
                <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml" />
                <!-- Theme: Light.Blue, Dark.Blue, Light.Red, Dark.Red, etc. -->
                <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**Step 2: Convert Window to MetroWindow**

Add MahApps.Metro namespace and replace `<Window>` tags:

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
        <!-- Your content -->
    </Grid>
</mah:MetroWindow>
```

**Step 3: Update Code-Behind**

```csharp
using MahApps.Metro.Controls;

namespace MyApp
{
    // Option 1: Remove base class (inherits from XAML)
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    // Option 2: Explicitly inherit
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
```

### Available Themes

MahApps.Metro provides multiple themes:

**Light themes:**

- `Light.Blue.xaml`
- `Light.Red.xaml`
- `Light.Green.xaml`
- `Light.Purple.xaml`
- `Light.Orange.xaml`

**Dark themes:**

- `Dark.Blue.xaml`
- `Dark.Red.xaml`
- `Dark.Green.xaml`
- `Dark.Purple.xaml`
- `Dark.Orange.xaml`

Change the theme by updating the resource dictionary reference in App.xaml.

### MetroWindow Features

**Title Bar Customization:**

```xml
<mah:MetroWindow
    Title="My App"
    TitleCharacterCasing="Normal"
    ShowIconOnTitleBar="True"
    Icon="app.ico"
    WindowTitleBrush="{DynamicResource MahApps.Brushes.Accent}"
    BorderThickness="1"
    BorderBrush="{DynamicResource MahApps.Brushes.Accent}">
    <!-- Content -->
</mah:MetroWindow>
```

**Window Buttons:**

```xml
<mah:MetroWindow
    ShowMinButton="True"
    ShowMaxRestoreButton="True"
    ShowCloseButton="True">
    <!-- Content -->
</mah:MetroWindow>
```

**Glow Effect:**

```xml
<mah:MetroWindow
    GlowBrush="{DynamicResource MahApps.Brushes.Accent}"
    NonActiveGlowBrush="Gray">
    <!-- Content -->
</mah:MetroWindow>
```

## Design-Time DataContext

### Basic Setup

Enable IntelliSense and design-time preview:

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:viewModels="clr-namespace:MyApp.ViewModels"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=viewModels:MainViewModel, IsDesignTimeCreatable=True}"
        Title="{Binding Title}"
        Width="800" Height="450">
    <!-- Content -->
</Window>
```

Key attributes:

- `mc:Ignorable="d"` - Ignore design-time attributes at runtime
- `d:DataContext` - Design-time only DataContext
- `IsDesignTimeCreatable=True` - Calls parameterless constructor

### Design Data

Create sample data for designer:

```xml
<Window.Resources>
    <!-- Design-time sample data -->
    <viewModels:MainViewModel x:Key="DesignViewModel">
        <viewModels:MainViewModel.Items>
            <collections:ArrayList>
                <system:String>Sample Item 1</system:String>
                <system:String>Sample Item 2</system:String>
            </collections:ArrayList>
        </viewModels:MainViewModel.Items>
    </viewModels:MainViewModel>
</Window.Resources>

<!-- Use design data -->
<Grid d:DataContext="{StaticResource DesignViewModel}">
    <ListBox ItemsSource="{Binding Items}" />
</Grid>
```

## EventToCommand Binding

### Basic EventToCommand

Convert XAML events to ViewModel commands:

```xml
xmlns:dxmvvm="http://schemas.devexpress.com/winfx/2008/xaml/mvvm"

<!-- Window Loaded event -->
<Window>
    <dxmvvm:Interaction.Behaviors>
        <dxmvvm:EventToCommand EventName="Loaded" Command="{Binding LoadedCommand}" />
    </dxmvvm:Interaction.Behaviors>
</Window>

<!-- Button Click event -->
<Button Content="Save">
    <dxmvvm:Interaction.Behaviors>
        <dxmvvm:EventToCommand EventName="Click" Command="{Binding SaveCommand}" />
    </dxmvvm:Interaction.Behaviors>
</Button>

<!-- TextBox TextChanged event -->
<TextBox>
    <dxmvvm:Interaction.Behaviors>
        <dxmvvm:EventToCommand EventName="TextChanged" Command="{Binding SearchCommand}" />
    </dxmvvm:Interaction.Behaviors>
</TextBox>
```

### EventToCommand with Parameters

Pass event arguments to command:

```xml
<ListBox>
    <dxmvvm:Interaction.Behaviors>
        <dxmvvm:EventToCommand EventName="SelectionChanged"
                               Command="{Binding SelectionChangedCommand}"
                               PassEventArgsToCommand="True" />
    </dxmvvm:Interaction.Behaviors>
</ListBox>
```

ViewModel:

```csharp
public ICommand SelectionChangedCommand { get; }

public MainViewModel(ILogger logger, IScheduler uiScheduler)
{
    _logger = logger;
    _uiScheduler = uiScheduler;

    SelectionChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(OnSelectionChanged);
}

private void OnSelectionChanged(SelectionChangedEventArgs args)
{
    if (args.AddedItems.Count > 0)
    {
        var selectedItem = args.AddedItems[0];
        _logger.Info($"Selected: {selectedItem}");
    }
}
```

### EventToCommand with Custom Parameter

Pass custom value instead of event args:

```xml
<Button Content="Save">
    <dxmvvm:Interaction.Behaviors>
        <dxmvvm:EventToCommand EventName="Click"
                               Command="{Binding ProcessCommand}"
                               CommandParameter="SaveOperation" />
    </dxmvvm:Interaction.Behaviors>
</Button>
```

## Data Binding Patterns

### OneWay Binding (Default)

Source → Target (ViewModel → View):

```xml
<TextBlock Text="{Binding Status}" />
<TextBlock Text="{Binding Title, Mode=OneWay}" />
```

### TwoWay Binding

Source ↔ Target (bidirectional):

```xml
<TextBox Text="{Binding SearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<CheckBox IsChecked="{Binding IsEnabled, Mode=TwoWay}" />
```

`UpdateSourceTrigger` options:

- `PropertyChanged` - Update immediately on every keystroke
- `LostFocus` - Update when control loses focus (default for TextBox)
- `Explicit` - Update only when UpdateSource() is called

### OneWayToSource Binding

Target → Source (View → ViewModel):

```xml
<!-- Useful for read-only dependency properties -->
<PasswordBox>
    <i:Interaction.Behaviors>
        <behaviors:PasswordBindingBehavior Password="{Binding Password, Mode=OneWayToSource}" />
    </i:Interaction.Behaviors>
</PasswordBox>
```

### OneTime Binding

Single update from source, then no updates:

```xml
<TextBlock Text="{Binding InitialMessage, Mode=OneTime}" />
```

### Binding Converters

Convert values for display:

```xml
<Window.Resources>
    <converters:BoolToVisibilityConverter x:Key="BoolToVisibility" />
    <converters:InverseBoolConverter x:Key="InverseBool" />
</Window.Resources>

<TextBlock Text="Loading..."
           Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibility}}" />

<Button Content="Cancel"
        IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBool}}" />
```

Example converter:

```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}
```

### Multi-Binding

Bind multiple sources:

```xml
<TextBlock>
    <TextBlock.Text>
        <MultiBinding StringFormat="{}{0} {1}">
            <Binding Path="FirstName" />
            <Binding Path="LastName" />
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

## Visual Tree Navigation

### Finding Elements in Visual Tree

```csharp
using System.Windows;
using System.Windows.Media;

public static class VisualTreeHelper
{
    // Find parent of specific type
    public static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);

        if (parent == null)
            return null;

        if (parent is T typedParent)
            return typedParent;

        return FindParent<T>(parent);
    }

    // Find child of specific type
    public static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
            return null;

        int childCount = VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild)
                return typedChild;

            var result = FindChild<T>(child);
            if (result != null)
                return result;
        }

        return null;
    }

    // Find all children of specific type
    public static IEnumerable<T> FindChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
            yield break;

        int childCount = VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild)
                yield return typedChild;

            foreach (var descendant in FindChildren<T>(child))
                yield return descendant;
        }
    }
}
```

Usage:

```csharp
// Find parent window
var window = VisualTreeHelperExtensions.FindParent<Window>(button);

// Find child TextBox
var textBox = VisualTreeHelperExtensions.FindChild<TextBox>(grid);

// Find all buttons
var buttons = VisualTreeHelperExtensions.FindChildren<Button>(panel);
```

## Attached Behaviors

### Creating Custom Attached Behavior

```csharp
using System.Windows;
using System.Windows.Controls;

public static class FocusBehavior
{
    public static readonly DependencyProperty IsFocusedProperty =
        DependencyProperty.RegisterAttached(
            "IsFocused",
            typeof(bool),
            typeof(FocusBehavior),
            new PropertyMetadata(false, OnIsFocusedChanged));

    public static bool GetIsFocused(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsFocusedProperty);
    }

    public static void SetIsFocused(DependencyObject obj, bool value)
    {
        obj.SetValue(IsFocusedProperty, value);
    }

    private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && (bool)e.NewValue)
        {
            element.Focus();
        }
    }
}
```

Usage:

```xml
<TextBox behaviors:FocusBehavior.IsFocused="{Binding ShouldFocus}" />
```

### Watermark TextBox Behavior

```csharp
public static class WatermarkBehavior
{
    public static readonly DependencyProperty WatermarkProperty =
        DependencyProperty.RegisterAttached(
            "Watermark",
            typeof(string),
            typeof(WatermarkBehavior),
            new PropertyMetadata(string.Empty, OnWatermarkChanged));

    public static string GetWatermark(DependencyObject obj)
    {
        return (string)obj.GetValue(WatermarkProperty);
    }

    public static void SetWatermark(DependencyObject obj, string value)
    {
        obj.SetValue(WatermarkProperty, value);
    }

    private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            textBox.GotFocus -= RemoveWatermark;
            textBox.LostFocus -= ShowWatermark;

            textBox.GotFocus += RemoveWatermark;
            textBox.LostFocus += ShowWatermark;

            ShowWatermark(textBox, null);
        }
    }

    private static void RemoveWatermark(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.Text == GetWatermark(textBox))
        {
            textBox.Text = string.Empty;
            textBox.Foreground = SystemColors.ControlTextBrush;
        }
    }

    private static void ShowWatermark(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
        {
            textBox.Text = GetWatermark(textBox);
            textBox.Foreground = SystemColors.GrayTextBrush;
        }
    }
}
```

Usage:

```xml
<TextBox behaviors:WatermarkBehavior.Watermark="Enter search term..." />
```

## Common XAML Patterns

### Responsive Layouts with Grid

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />        <!-- Toolbar -->
        <RowDefinition Height="*" />           <!-- Content (fills) -->
        <RowDefinition Height="Auto" />        <!-- Status bar -->
    </Grid.RowDefinitions>

    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="200" />       <!-- Sidebar -->
        <ColumnDefinition Width="*" />         <!-- Main content -->
    </Grid.ColumnDefinitions>

    <Menu Grid.Row="0" Grid.ColumnSpan="2" />
    <TreeView Grid.Row="1" Grid.Column="0" />
    <ContentControl Grid.Row="1" Grid.Column="1" Content="{Binding CurrentView}" />
    <StatusBar Grid.Row="2" Grid.ColumnSpan="2" />
</Grid>
```

### Data Templates

Define how objects are displayed:

```xml
<Window.Resources>
    <DataTemplate x:Key="PersonTemplate">
        <StackPanel Orientation="Horizontal">
            <Image Source="{Binding Avatar}" Width="32" Height="32" Margin="0,0,8,0" />
            <StackPanel>
                <TextBlock Text="{Binding FullName}" FontWeight="Bold" />
                <TextBlock Text="{Binding Email}" FontSize="10" Foreground="Gray" />
            </StackPanel>
        </StackPanel>
    </DataTemplate>
</Window.Resources>

<ListBox ItemsSource="{Binding People}"
         ItemTemplate="{StaticResource PersonTemplate}" />
```

### Triggers and Styles

```xml
<Window.Resources>
    <Style x:Key="HighlightButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="White" />
        <Setter Property="Foreground" Value="Black" />
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="LightBlue" />
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Opacity" Value="0.5" />
            </Trigger>
        </Style.Triggers>
    </Style>
</Window.Resources>

<Button Style="{StaticResource HighlightButtonStyle}" Content="Click Me" />
```

### Data Triggers

```xml
<Window.Resources>
    <Style x:Key="StatusStyle" TargetType="TextBlock">
        <Style.Triggers>
            <DataTrigger Binding="{Binding Status}" Value="Success">
                <Setter Property="Foreground" Value="Green" />
            </DataTrigger>
            <DataTrigger Binding="{Binding Status}" Value="Error">
                <Setter Property="Foreground" Value="Red" />
            </DataTrigger>
            <DataTrigger Binding="{Binding Status}" Value="Warning">
                <Setter Property="Foreground" Value="Orange" />
            </DataTrigger>
        </Style.Triggers>
    </Style>
</Window.Resources>

<TextBlock Text="{Binding StatusMessage}" Style="{StaticResource StatusStyle}" />
```

### Loading Indicator

```xml
<Grid>
    <!-- Main content -->
    <ContentControl Content="{Binding CurrentView}" />

    <!-- Loading overlay -->
    <Grid Background="#80000000"
          Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibility}}">
        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
            <mah:ProgressRing IsActive="True" Foreground="White" />
            <TextBlock Text="Loading..." Foreground="White" Margin="0,16,0,0" />
        </StackPanel>
    </Grid>
</Grid>
```

### Context Menu Binding

```xml
<ListBox ItemsSource="{Binding Items}">
    <ListBox.ItemContainerStyle>
        <Style TargetType="ListBoxItem">
            <Setter Property="ContextMenu">
                <Setter.Value>
                    <ContextMenu>
                        <MenuItem Header="Edit"
                                  Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=ListBox}}"
                                  CommandParameter="{Binding}" />
                        <MenuItem Header="Delete"
                                  Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=ListBox}}"
                                  CommandParameter="{Binding}" />
                    </ContextMenu>
                </Setter.Value>
            </Setter>
        </Style>
    </ListBox.ItemContainerStyle>
</ListBox>
```

### Validation

```xml
<TextBox>
    <TextBox.Text>
        <Binding Path="Email" UpdateSourceTrigger="PropertyChanged">
            <Binding.ValidationRules>
                <rules:EmailValidationRule />
            </Binding.ValidationRules>
        </Binding>
    </TextBox.Text>
</TextBox>
```

Validation rule:

```csharp
public class EmailValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is string email && IsValidEmail(email))
        {
            return ValidationResult.ValidResult;
        }

        return new ValidationResult(false, "Invalid email address");
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
```

## Best Practices

### ✓ DO: Use Resource Dictionaries for Reusable Styles

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Styles/Colors.xaml" />
            <ResourceDictionary Source="Styles/Buttons.xaml" />
            <ResourceDictionary Source="Styles/TextBoxes.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### ✓ DO: Use x:Name Sparingly

Prefer data binding over code-behind:

```xml
<!-- ✓ Preferred: Data binding -->
<TextBlock Text="{Binding Status}" />

<!-- ✗ Avoid: x:Name for data access -->
<TextBlock x:Name="StatusTextBlock" />
```

Use `x:Name` only when necessary (e.g., for animations, focus management).

### ✓ DO: Keep XAML Declarative

Move logic to ViewModels, keep XAML for presentation:

```xml
<!-- ✓ Declarative -->
<Button Content="Save"
        Command="{Binding SaveCommand}"
        IsEnabled="{Binding CanSave}" />

<!-- ✗ Code-behind logic -->
<Button Content="Save" Click="SaveButton_Click" />
```

### ✗ DON'T: Overuse Nested Grids

Prefer StackPanel or DockPanel for simple layouts:

```xml
<!-- ✓ Simple layout -->
<StackPanel Orientation="Horizontal">
    <Label Content="Name:" />
    <TextBox Text="{Binding Name}" Width="200" />
</StackPanel>

<!-- ✗ Unnecessary Grid -->
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    <Label Grid.Column="0" Content="Name:" />
    <TextBox Grid.Column="1" Text="{Binding Name}" />
</Grid>
```
