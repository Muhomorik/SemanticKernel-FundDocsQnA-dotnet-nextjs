using Autofac;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using PdfTextExtractor.Core;
using System.Reactive.Concurrency;

namespace PdfTextExtractor.Wpf.Modules;

/// <summary>
/// Autofac module for registering presentation layer components (ViewModels, Views, etc.).
/// </summary>
public class PresentationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register Microsoft.Extensions.Logging.ILoggerFactory backed by NLog
        // This allows Core services to use ILogger<T> while logging through NLog
        builder.Register<ILoggerFactory>(ctx => new NLogLoggerFactory())
            .As<ILoggerFactory>()
            .SingleInstance();

        // Register generic ILogger<T> using the ILoggerFactory
        // Ensures all ILogger<T> dependencies resolve to real loggers
        builder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>))
            .SingleInstance();

        // Register PdfTextExtractorLib as singleton with ILoggerFactory injection
        builder.Register(ctx =>
        {
            var loggerFactory = ctx.Resolve<ILoggerFactory>();
            return new PdfTextExtractorLib(loggerFactory);
        })
        .As<IPdfTextExtractorLib>()
        .SingleInstance();

        // Register all ViewModels with type-aware ILogger and UI scheduler injection
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

        // Register MainWindow
        builder.RegisterType<MainWindow>()
            .AsSelf()
            .InstancePerDependency();
    }
}
