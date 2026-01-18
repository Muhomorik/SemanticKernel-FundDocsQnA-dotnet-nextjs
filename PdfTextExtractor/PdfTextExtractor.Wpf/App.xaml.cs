using System;
using System.Linq;
using System.Windows;
using Autofac;
using PdfTextExtractor.Wpf.Modules;
using PdfTextExtractor.Wpf.ViewModels;

namespace PdfTextExtractor.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
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

        // Register presentation module
        builder.RegisterModule<PresentationModule>();

        // Auto-discover and register modules from other assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("PdfTextExtractor") == true
                     && !a.FullName.Contains("Wpf"))
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

