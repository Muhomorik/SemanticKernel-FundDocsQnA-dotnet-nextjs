using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
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
    private readonly YieldRaccoonOptions _yieldRaccoonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresentationModule"/> class.
    /// </summary>
    /// <param name="databaseOptions">Database configuration options.</param>
    /// <param name="yieldRaccoonOptions">Application-wide configuration options.</param>
    public PresentationModule(DatabaseOptions databaseOptions, YieldRaccoonOptions yieldRaccoonOptions)
    {
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));
        _yieldRaccoonOptions = yieldRaccoonOptions ?? throw new ArgumentNullException(nameof(yieldRaccoonOptions));
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
        builder.RegisterType<InMemoryFundListEventStore>()
            .As<IFundListEventStore>()
            .SingleInstance();

        // Register about-fund event store as singleton for browsing session event sourcing
        builder.RegisterType<InMemoryAboutFundEventStore>()
            .As<IAboutFundEventStore>()
            .SingleInstance();

        // User settings service registration
        // Register settings service for persisting user preferences
        builder.RegisterType<UserSettingsService>()
            .As<IUserSettingsService>()
            .SingleInstance();

        // Settings dialog service registration
        // Register dialog service for showing settings window from ViewModels
        builder.RegisterType<SettingsDialogService>()
            .As<ISettingsDialogService>()
            .InstancePerDependency();

        // AboutFund window service registration
        // Register window service for showing AboutFund browser window from ViewModels
        builder.RegisterType<AboutFundWindowService>()
            .As<IAboutFundWindowService>()
            .SingleInstance();

        // Export window service registration
        // Register window service for showing Export window from ViewModels
        builder.RegisterType<ExportWindowService>()
            .As<IExportWindowService>()
            .InstancePerDependency();

        // Export data service registration
        // Exports filtered fund data to standalone SQLite database files
        builder.RegisterType<FundDataExportService>()
            .As<IFundDataExportService>()
            .InstancePerDependency();

        // Fund statistics CSV export service registration
        // Computes summary statistics from fund NAV data and exports to CSV
        builder.RegisterType<FundStatisticsCsvExportService>()
            .As<IFundStatisticsCsvExportService>()
            .InstancePerDependency();

        // Fund metadata CSV export service registration
        // Exports fund profile metadata (fees, risk metrics, classifications) to CSV
        builder.RegisterType<FundMetadataCsvExportService>()
            .As<IFundMetadataCsvExportService>()
            .InstancePerDependency();

        // Fund statistics export window service registration
        // Register window service for showing the statistics export window from ViewModels
        builder.RegisterType<FundStatisticsExportWindowService>()
            .As<IFundStatisticsExportWindowService>()
            .InstancePerDependency();

        // Schedule calculator registration
        // Register pure computation service for pre-calculating batch timings with randomized delays
        builder.RegisterType<FundListScheduleCalculator>()
            .As<IFundListScheduleCalculator>()
            .SingleInstance();

        // Session orchestrator registration
        // Register orchestrator for session lifecycle, batch workflow, and timer management
        builder.RegisterType<FundListOrchestrator>()
            .As<IFundListOrchestrator>()
            .SingleInstance();

        // AboutFund page data collector registration
        // Accumulates per-fund page data from multiple fetch steps, signals when complete
        builder.RegisterType<AboutFundPageDataCollector>()
            .As<IAboutFundPageDataCollector>()
            .SingleInstance();

        // Fund details URL builder options
        // Maps Wpf-layer config to Application-layer options record at composition root
        builder.Register(ctx =>
                new FundDetailsUrlBuilderOptions(ctx.Resolve<YieldRaccoonOptions>().FundDetailsPageUrlTemplate))
            .SingleInstance();

        // Response parser options
        // Endpoint URL patterns for mapping intercepted responses to data slots
        // Defined here at composition root — not exposed in user-facing config
        builder.Register(ctx => new ResponseParserOptions(
            [
                new EndpointPattern(["_api/fund-guide/chart/", "/one_month?raw=true"], AboutFundDataSlot.Chart1Month),
                new EndpointPattern(["_api/fund-guide/chart/", "/three_months?raw=true"],
                    AboutFundDataSlot.Chart3Months),
                new EndpointPattern(["_api/fund-guide/chart/", "/this_year?raw=true"],
                    AboutFundDataSlot.ChartYearToDate),
                new EndpointPattern(["_api/fund-guide/chart/", "/one_year?raw=true"], AboutFundDataSlot.Chart1Year),
                new EndpointPattern(["_api/fund-guide/chart/", "/three_years?raw=true"], AboutFundDataSlot.Chart3Years),
                new EndpointPattern(["_api/fund-guide/chart/", "/five_years?raw=true"], AboutFundDataSlot.Chart5Years),
                new EndpointPattern(["_api/fund-guide/chart/", "/infinity?raw=true"], AboutFundDataSlot.ChartMax)
            ]))
            .SingleInstance();

        // Delay options — minimal timings when FastMode is enabled, normal otherwise
        var fastMode = _yieldRaccoonOptions.FastMode;

        builder.Register(ctx => fastMode
                ? new RandomDelayProviderOptions(MinDelaySeconds: 3, MaxDelaySeconds: 8)
                : new RandomDelayProviderOptions(MinDelaySeconds: 10, MaxDelaySeconds: 25))
            .SingleInstance();

        builder.Register(ctx => fastMode
                ? new PageInteractorOptions(MinDelayMs: 1_000, PanelOpenDelayMs: 2_000)
                : new PageInteractorOptions(MinDelayMs: 4_000, PanelOpenDelayMs: 7_000))
            .SingleInstance();

        // Random delay provider
        // Generates randomized delays between page interactions to simulate human browsing
        builder.RegisterType<RandomDelayProvider>()
            .As<IRandomDelayProvider>()
            .SingleInstance();

        // Fund details URL builder registration
        // Builds strongly-typed Uri instances for fund detail page navigation
        builder.RegisterType<FundDetailsUrlBuilder>()
            .As<IFundDetailsUrlBuilder>()
            .SingleInstance();

        // AboutFund schedule calculator registration
        // Pure computation: builds session schedules with randomized delays
        builder.RegisterType<AboutFundScheduleCalculator>()
            .As<IAboutFundScheduleCalculator>()
            .SingleInstance();

        // AboutFund orchestrator registration
        // Register orchestrator for fund browsing session lifecycle and navigation
        builder.RegisterType<AboutFundOrchestrator>()
            .As<IAboutFundOrchestrator>()
            .SingleInstance();

        // AboutFund page interactor registration
        // Executes post-navigation interactions (e.g., clicking tabs) on fund detail pages
        builder.RegisterType<WebView2AboutFundPageInteractor>()
            .As<IAboutFundPageInteractor>()
            .AsSelf()
            .SingleInstance();

        // AboutFund response interceptor registration
        // Captures ALL network traffic from AboutFund WebView2 browser for debugging
        builder.RegisterType<AboutFundResponseInterceptor>()
            .As<IAboutFundResponseInterceptor>()
            .InstancePerDependency();

        // FundList page interactor registration
        // Executes page interactions (pagination clicks, scrolling) on the fund list page
        builder.RegisterType<WebView2FundListPageInteractor>()
            .As<IFundListPageInteractor>()
            .InstancePerDependency();

        // FundList response interceptor registration
        // Captures fund list API responses from WebView2 network traffic
        builder.RegisterType<FundListResponseInterceptor>()
            .As<IFundListResponseInterceptor>()
            .InstancePerDependency();

        // Cloud sync window service registration
        // Register window service for showing Cloud Sync window from ViewModels
        builder.RegisterType<CloudSyncWindowService>()
            .As<ICloudSyncWindowService>()
            .InstancePerDependency();

        // Backend API client registration (available when BackendApiUrl is configured)
        RegisterBackendApiClient(builder);

        // Database provider registration
        RegisterDatabaseProvider(builder);

        // Scheduler registration
        // Register UI scheduler for marshalling observable subscriptions to UI thread
        builder.Register(ctx => DispatcherScheduler.Current)
            .As<IScheduler>()
            .SingleInstance();

        // ViewModel registration
        // Register all ViewModels with UI scheduler injection
        // Convention: All classes ending with "ViewModel" in this assembly
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .WithParameter(new Autofac.Core.ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(IScheduler),
                (pi, ctx) => ctx.Resolve<IScheduler>()))
            .InstancePerDependency();

        // View registration
        // Register MainWindow
        builder.RegisterType<MainWindow>()
            .AsSelf()
            .InstancePerDependency();

        // Register other views/windows
        builder.RegisterAssemblyTypes(typeof(PresentationModule).Assembly)
            .Where(t => (t.Name.EndsWith("View") || t.Name.EndsWith("Window"))
                        && t != typeof(MainWindow))
            .AsSelf()
            .InstancePerDependency();
    }

    /// <summary>
    /// Registers database-related services based on the configured provider.
    /// </summary>
    private void RegisterDatabaseProvider(ContainerBuilder builder)
    {
        if (_databaseOptions.Provider is DatabaseProvider.SQLite or DatabaseProvider.DualWrite)
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

        if (_databaseOptions.Provider is DatabaseProvider.DualWrite)
        {
            RegisterDualWriteServices(builder);
        }
        else
        {
            // Non-DualWrite: register services directly, no-op sync status provider
            builder.RegisterType<FundListIngestionService>()
                .As<IFundListIngestionService>()
                .InstancePerDependency();

            builder.RegisterType<AboutFundChartIngestionService>()
                .As<IAboutFundChartIngestionService>()
                .InstancePerDependency();

            builder.RegisterType<NullBackendSyncStatusProvider>()
                .As<IBackendSyncStatusProvider>()
                .SingleInstance();
        }
    }

    /// <summary>
    /// Registers the pre-configured HttpClient and FundSyncApiClient for Backend API access.
    /// Called whenever <see cref="DatabaseOptions.BackendApiUrl"/> is configured,
    /// regardless of the provider — needed by both DualWrite mode and Cloud Sync.
    /// </summary>
    private void RegisterBackendApiClient(ContainerBuilder builder)
    {
        // Pre-configured HttpClient for Backend API
        // BaseAddress is null when BackendApiUrl is not configured — callers must guard
        builder.Register(ctx =>
            {
                var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

                if (!string.IsNullOrWhiteSpace(_databaseOptions.BackendApiUrl)
                    && Uri.TryCreate(_databaseOptions.BackendApiUrl, UriKind.Absolute, out var baseUri))
                {
                    client.BaseAddress = baseUri;

                    if (!string.IsNullOrWhiteSpace(_databaseOptions.BackendApiKey))
                        client.DefaultRequestHeaders.Add("Authorization", $"ApiKey {_databaseOptions.BackendApiKey}");
                }

                return client;
            })
            .SingleInstance();

        // Backend API client
        builder.RegisterType<FundSyncApiClient>()
            .As<IFundSyncApiClient>()
            .SingleInstance();

        // Cloud sync service registration
        // Orchestrates bulk-syncing local fund data to the Backend API
        builder.RegisterType<CloudSyncService>()
            .As<ICloudSyncService>()
            .InstancePerDependency();
    }

    /// <summary>
    /// Registers DualWrite infrastructure: inner SQLite services with named keys,
    /// sync status stream, and decorator services.
    /// HttpClient and FundSyncApiClient are registered by <see cref="RegisterBackendApiClient"/>.
    /// </summary>
    private void RegisterDualWriteServices(ContainerBuilder builder)
    {
        // Shared Rx Subject for sync status notifications (singleton)
        builder.Register(ctx => new Subject<BackendSyncStatus>())
            .AsSelf()
            .SingleInstance();

        // BackendSyncStatusProvider wraps the Subject for ViewModel consumption
        builder.RegisterType<BackendSyncStatusProvider>()
            .As<IBackendSyncStatusProvider>()
            .SingleInstance();

        // Inner SQLite services — registered with named keys so decorators can resolve them
        builder.RegisterType<FundListIngestionService>()
            .Named<IFundListIngestionService>("sqlite")
            .InstancePerDependency();

        builder.RegisterType<AboutFundChartIngestionService>()
            .Named<IAboutFundChartIngestionService>("sqlite")
            .InstancePerDependency();

        // DualWrite decorators — resolve inner services by named key
        // NLogModule injects ILogger via pipeline middleware, which doesn't apply to lambda
        // registrations — use LogManager.GetLogger() directly instead of ctx.Resolve<ILogger>()
        builder.Register(ctx => new DualWriteFundListIngestionService(
                LogManager.GetLogger(typeof(DualWriteFundListIngestionService).FullName!),
                ctx.ResolveNamed<IFundListIngestionService>("sqlite"),
                ctx.Resolve<IFundSyncApiClient>(),
                ctx.Resolve<IFundProfileRepository>(),
                ctx.Resolve<Subject<BackendSyncStatus>>()))
            .As<IFundListIngestionService>()
            .InstancePerDependency();

        builder.Register(ctx => new DualWriteChartIngestionService(
                LogManager.GetLogger(typeof(DualWriteChartIngestionService).FullName!),
                ctx.ResolveNamed<IAboutFundChartIngestionService>("sqlite"),
                ctx.Resolve<IFundSyncApiClient>(),
                ctx.Resolve<IFundProfileRepository>(),
                ctx.Resolve<Subject<BackendSyncStatus>>()))
            .As<IAboutFundChartIngestionService>()
            .InstancePerDependency();
    }
}