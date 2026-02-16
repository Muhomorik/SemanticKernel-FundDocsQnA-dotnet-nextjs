using System.Reactive.Concurrency;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using YieldRaccoon.Application.Configuration;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Infrastructure.Data.Repositories;
using YieldRaccoon.Infrastructure.EventStore;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Services;

namespace YieldRaccoon.Wpf.Modules;

/// <summary>
/// Autofac module for registering presentation layer components (ViewModels, Views, logging infrastructure).
/// </summary>
public class PresentationModule : Module
{
    private readonly DatabaseOptions _databaseOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresentationModule"/> class.
    /// </summary>
    /// <param name="databaseOptions">Database configuration options.</param>
    public PresentationModule(DatabaseOptions databaseOptions)
    {
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));
    }

    protected override void Load(ContainerBuilder builder)
    {
        // Logging infrastructure
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

        // Event store registration
        // Register crawl event store as singleton for session-wide event sourcing
        builder.RegisterType<InMemoryCrawlEventStore>()
            .As<ICrawlEventStore>()
            .SingleInstance();

        // Register about-fund event store as singleton for browsing session event sourcing
        builder.RegisterType<InMemoryAboutFundEventStore>()
            .As<IAboutFundEventStore>()
            .SingleInstance();

        // User settings service registration
        // Register settings service for persisting user preferences
        builder.RegisterType<UserSettingsService>()
            .As<IUserSettingsService>()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(typeof(UserSettingsService).FullName!)))
            .SingleInstance();

        // Settings dialog service registration
        // Register dialog service for showing settings window from ViewModels
        builder.RegisterType<SettingsDialogService>()
            .As<ISettingsDialogService>()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(typeof(SettingsDialogService).FullName!)))
            .InstancePerDependency();

        // AboutFund window service registration
        // Register window service for showing AboutFund browser window from ViewModels
        builder.RegisterType<AboutFundWindowService>()
            .As<IAboutFundWindowService>()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(typeof(AboutFundWindowService).FullName!)))
            .SingleInstance();

        // Session scheduler registration
        // Register session scheduler for pre-calculating batch timings with randomized delays
        builder.RegisterType<CrawlSessionScheduler>()
            .As<ICrawlSessionScheduler>()
            .SingleInstance();

        // Session orchestrator registration
        // Register orchestrator for session lifecycle, batch workflow, and timer management
        builder.RegisterType<CrawlSessionOrchestrator>()
            .As<ICrawlSessionOrchestrator>()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(typeof(CrawlSessionOrchestrator).FullName!)))
            .SingleInstance();

        // AboutFund page data collector registration
        // Accumulates per-fund page data from multiple fetch steps, signals when complete
        builder.RegisterType<AboutFundPageDataCollector>()
            .As<IAboutFundPageDataCollector>()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(typeof(AboutFundPageDataCollector).FullName!)))
            .SingleInstance();

        // Fund details URL builder options
        // Maps Wpf-layer config to Application-layer options record at composition root
        builder.Register(ctx =>
                new FundDetailsUrlBuilderOptions(ctx.Resolve<YieldRaccoonOptions>().FundDetailsPageUrlTemplate))
            .SingleInstance();

        // Response parser options
        // Endpoint URL patterns for mapping intercepted responses to data slots
        // Defined here at composition root — not exposed in user-facing config
        builder.Register(ctx => new ResponseParserOptions(new[]
            {
                new EndpointPattern("chart/timeperiods/", nameof(AboutFundPageData.ChartTimePeriods)),
                // Uncomment when SEK performance endpoint pattern is discovered:
                // new EndpointPattern("chart/sekdata/", nameof(AboutFundPageData.SekPerformance)),
            }))
            .SingleInstance();

        // Fund details URL builder registration
        // Builds strongly-typed Uri instances for fund detail page navigation
        builder.RegisterType<FundDetailsUrlBuilder>()
            .As<IFundDetailsUrlBuilder>()
            .SingleInstance();

        // AboutFund orchestrator registration
        // Register orchestrator for fund browsing session lifecycle and navigation
        builder.RegisterType<AboutFundOrchestrator>()
            .As<IAboutFundOrchestrator>()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(typeof(AboutFundOrchestrator).FullName!)))
            .SingleInstance();

        // AboutFund page interactor registration
        // Executes post-navigation interactions (e.g., clicking tabs) on fund detail pages
        builder.RegisterType<WebView2AboutFundPageInteractor>()
            .As<IAboutFundPageInteractor>()
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(typeof(WebView2AboutFundPageInteractor).FullName!)))
            .SingleInstance();

        // AboutFund response interceptor registration
        // Captures ALL network traffic from AboutFund WebView2 browser for debugging
        builder.RegisterType<AboutFundResponseInterceptor>()
            .As<IAboutFundResponseInterceptor>()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(typeof(AboutFundResponseInterceptor).FullName!)))
            .InstancePerDependency();

        // Database provider registration
        RegisterDatabaseProvider(builder);

        // Scheduler registration
        // Register UI scheduler for marshalling observable subscriptions to UI thread
        builder.Register(ctx => DispatcherScheduler.Current)
            .As<IScheduler>()
            .SingleInstance();

        // ViewModel registration
        // Register all ViewModels with type-aware NLog.ILogger and UI scheduler injection
        // Convention: All classes ending with "ViewModel" in this assembly
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(pi.Member.DeclaringType?.FullName ?? "Unknown")))
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(IScheduler),
                (pi, ctx) => ctx.Resolve<IScheduler>()))
            .InstancePerDependency();

        // View registration
        // Register MainWindow with logger injection
        builder.RegisterType<MainWindow>()
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(pi.Member.DeclaringType?.FullName ?? "Unknown")))
            .InstancePerDependency();

        // Register other views/windows with logger injection (if needed)
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => (t.Name.EndsWith("View") || t.Name.EndsWith("Window"))
                     && t != typeof(MainWindow))
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(NLog.ILogger),
                (pi, ctx) => LogManager.GetLogger(pi.Member.DeclaringType?.FullName ?? "Unknown")))
            .InstancePerDependency();
    }

    /// <summary>
    /// Registers database-related services based on the configured provider.
    /// </summary>
    private void RegisterDatabaseProvider(ContainerBuilder builder)
    {
        if (_databaseOptions.Provider == DatabaseProvider.SQLite)
        {
            // Register DbContext with SQLite provider
            builder.Register(ctx =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<YieldRaccoonDbContext>();
                    optionsBuilder.UseSqlite(_databaseOptions.ConnectionString);
                    return new YieldRaccoonDbContext(optionsBuilder.Options);
                })
                .AsSelf()
                .SingleInstance();

            // Register EF Core repositories
            builder.RegisterType<EfCoreFundProfileRepository>()
                .As<IFundProfileRepository>()
                .InstancePerDependency();

            builder.RegisterType<EfCoreFundHistoryRepository>()
                .As<IFundHistoryRepository>()
                .InstancePerDependency();
        }
        else // InMemory provider
        {
            // Register InMemory repositories as singletons (session-scoped, volatile)
            builder.RegisterType<InMemoryFundProfileRepository>()
                .As<IFundProfileRepository>()
                .SingleInstance();

            builder.RegisterType<InMemoryFundHistoryRepository>()
                .As<IFundHistoryRepository>()
                .SingleInstance();
        }

        // Register ingestion service (works with both provider types via interfaces)
        builder.RegisterType<FundIngestionService>()
            .As<IFundIngestionService>()
            .InstancePerDependency();
    }
}
