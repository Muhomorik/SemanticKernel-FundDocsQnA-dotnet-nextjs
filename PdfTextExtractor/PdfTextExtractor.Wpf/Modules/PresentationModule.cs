using Autofac;
using NLog;
using System.Reactive.Concurrency;

namespace PdfTextExtractor.Wpf.Modules;

/// <summary>
/// Autofac module for registering presentation layer components (ViewModels, Views, etc.).
/// </summary>
public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register all ViewModels with type-aware ILogger and UI scheduler injection
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

        // Register MainWindow
        builder.RegisterType<MainWindow>()
            .AsSelf()
            .InstancePerDependency();
    }
}
