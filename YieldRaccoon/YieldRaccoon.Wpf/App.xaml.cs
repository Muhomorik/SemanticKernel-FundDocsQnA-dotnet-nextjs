using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Autofac;
using CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Web.WebView2.Core;
using NLog;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Modules;
using YieldRaccoon.Wpf.Views;

namespace YieldRaccoon.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private IContainer? _container;
    private ILifetimeScope? _appScope;
    private MainWindow? _mainWindow;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Handles the application startup event.
    /// Configures WebView2, dependency injection container, and displays the main window.
    /// </summary>
    /// <param name="e">Startup event arguments.</param>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize NLog from NLog.config (before DI container)
        LogManager.Setup().LoadConfigurationFromFile("NLog.config");
        Logger.Info("Application starting...");

        // Parse command-line arguments for auto-start modes
        var autoStartOptions = Parser.Default
            .ParseArguments<AutoStartOptions>(e.Args)
            .MapResult(opts => opts, _ => AutoStartOptions.None);

        Logger.Info("CLI args: AutoList={0}, AutoOverview={1}, OverviewFundCount={2}, ElevatedSettings={3}",
            autoStartOptions.AutoList, autoStartOptions.AutoOverview,
            autoStartOptions.OverviewFundCount, autoStartOptions.OpenSettingsOnStartup);

        // Apply YieldRaccoon theme (system accent color via RuntimeThemeGenerator)
        ApplyYieldRaccoonTheme();

        // Initialize WebView2 environment with Chrome user agent
        await InitializeWebView2EnvironmentAsync();

        // Build configuration from User Secrets and appsettings.json
        var configuration = BuildConfiguration();

        // Configure Autofac container
        var builder = new ContainerBuilder();

        // Register configuration options
        var options = configuration.GetSection("YieldRaccoon").Get<YieldRaccoonOptions>() ?? new YieldRaccoonOptions();
        builder.RegisterInstance(options).AsSelf().SingleInstance();

        // Register CLI auto-start options
        builder.RegisterInstance(autoStartOptions).AsSelf().SingleInstance();

        // Load database options from appsettings.json
        var databaseOptions = configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();

        // Load and apply user settings (overrides appsettings.json)
        var userSettings = LoadUserSettings();
        ApplyUserSettings(databaseOptions, userSettings);
        builder.RegisterInstance(databaseOptions).AsSelf().SingleInstance();
        builder.RegisterInstance(userSettings).AsSelf().SingleInstance();

        Logger.Info($"Database provider: {databaseOptions.Provider}");
        Logger.Info($"Database connection: {databaseOptions.ConnectionString}");

        // Auto-inject NLog.ILogger into all components (must be registered before other modules)
        builder.RegisterModule<NLogModule>();

        // Register presentation module (ViewModels, Views, Logging infrastructure)
        builder.RegisterModule(new PresentationModule(databaseOptions, options));

        // Build container and create app-level lifetime scope
        _container = builder.Build();
        _appScope = _container.BeginLifetimeScope();

        // Initialize database if using SQLite provider
        await InitializeDatabaseAsync(databaseOptions);

        Logger.Info("DI container configured");

        // Resolve and show main window (DataContext is set by MainWindow constructor)
        _mainWindow = _appScope.Resolve<MainWindow>();
        _mainWindow.Show();

        // If the app was restarted as admin to retry a blocked scheduled-task operation,
        // immediately reopen the Settings window so the user can click Save again without
        // hunting for it in the menu.
        if (autoStartOptions.OpenSettingsOnStartup)
        {
            try
            {
                Logger.Info("Reopening Settings window after elevated restart");
                var settingsWindow = _appScope.Resolve<SettingsWindow>();
                settingsWindow.Owner = _mainWindow;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to reopen Settings window after elevated restart");
            }
        }

        // Scheduled weekly stats run: open the Statistics Export window and auto-fire export.
        // The VM itself triggers the export on Loaded (via LoadedCommand) when AutoWeeklyStats is set.
        if (autoStartOptions.AutoWeeklyStats)
            TriggerWeeklyStatsExportWindow();

        Logger.Info("Application started successfully");
    }

    /// <summary>
    /// Opens the Statistics Export window in scheduled-run mode. Called on cold start when
    /// <c>--auto-weekly-stats</c> is set, and also by <see cref="HandleAutoWeeklyStatsTrigger"/>
    /// when a second process forwards the flag to the running instance.
    /// </summary>
    public void TriggerWeeklyStatsExportWindow()
    {
        if (_appScope is null || _mainWindow is null)
        {
            Logger.Warn("TriggerWeeklyStatsExportWindow called before DI container ready — ignoring");
            return;
        }

        try
        {
            Logger.Info("Opening Statistics Export window for scheduled weekly run");

            // Ensure the shared AutoStartOptions singleton reports AutoWeeklyStats=true so the
            // freshly constructed VM auto-fires Export on Loaded, even when the flag arrived via
            // a forwarded command line from a second instance rather than this process's args.
            var autoStartOptions = _appScope.Resolve<AutoStartOptions>();
            autoStartOptions.AutoWeeklyStats = true;

            var window = _appScope.Resolve<FundStatisticsExportWindow>();
            window.Owner = _mainWindow;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open Statistics Export window for scheduled weekly run");
        }
    }

    /// <summary>
    /// Called by the single-instance wrapper when a second process is launched with
    /// <c>--auto-weekly-stats</c>. Brings the main window to the foreground and opens the
    /// Statistics Export window, unless one is already open and exporting.
    /// </summary>
    public void HandleAutoWeeklyStatsTrigger()
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (_mainWindow is null)
            {
                Logger.Warn("HandleAutoWeeklyStatsTrigger called before main window ready — ignoring");
                return;
            }

            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();

            foreach (Window window in Windows)
            {
                if (window is FundStatisticsExportWindow existing
                    && existing.DataContext is ViewModels.FundStatisticsExportWindowViewModel vm)
                {
                    if (vm.IsExporting)
                    {
                        Logger.Info("Weekly export already running, ignoring scheduled trigger");
                        existing.Activate();
                        return;
                    }

                    Logger.Info("Triggering Export on existing idle Statistics Export window");
                    existing.Activate();
                    if (vm.ExportCommand.CanExecute(null))
                        vm.ExportCommand.Execute(null);
                    return;
                }
            }

            TriggerWeeklyStatsExportWindow();
        });
    }

    /// <summary>
    /// Handles the application exit event.
    /// Disposes the dependency injection container.
    /// </summary>
    /// <param name="e">Exit event arguments.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Application exiting...");

        // Dispose lifetime scope and container
        _appScope?.Dispose();
        _container?.Dispose();

        // Shutdown NLog
        LogManager.Shutdown();

        base.OnExit(e);
    }

    /// <summary>
    /// Applies the YieldRaccoon theme using the Windows system accent color.
    /// Uses RuntimeThemeGenerator to create a proper MahApps theme that generates
    /// all 200+ theme resources from the accent color.
    /// Light.Blue.xaml in App.xaml serves as a XAML designer fallback only.
    /// </summary>
    private static void ApplyYieldRaccoonTheme()
    {
        try
        {
            var accentColor = SystemParameters.WindowGlassColor;

            // Fall back to Windows 11 default blue if transparent or black
            if (accentColor.A == 0 || (accentColor.R == 0 && accentColor.G == 0 && accentColor.B == 0))
                accentColor = (Color)ColorConverter.ConvertFromString("#0078D4")!;

            var theme = ControlzEx.Theming.RuntimeThemeGenerator.Current
                .GenerateRuntimeTheme("Light", accentColor);

            if (theme is null)
            {
                Logger.Warn("RuntimeThemeGenerator returned null, falling back to Light.Blue");
                return;
            }

            ControlzEx.Theming.ThemeManager.Current
                .ChangeTheme(System.Windows.Application.Current, theme);

            Logger.Info($"Applied YieldRaccoon theme with accent #{accentColor.R:X2}{accentColor.G:X2}{accentColor.B:X2}");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to apply YieldRaccoon theme, falling back to Light.Blue");
        }
    }

    /// <summary>
    /// Initializes WebView2 environment with settings that make it appear as Chrome browser.
    /// </summary>
    private static async Task InitializeWebView2EnvironmentAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YieldRaccoon",
            "WebView2Cache");

        var options = new CoreWebView2EnvironmentOptions
        {
            // Set user agent to Microsoft Edge (standard Edge browser)
            AdditionalBrowserArguments =
                "--user-agent=\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.2903.112\" --lang=en-US"
        };

        // Create WebView2 environment with custom settings
        var environment = await CoreWebView2Environment.CreateAsync(
            null, // Use installed Edge WebView2 Runtime
            userDataFolder,
            options);

        // Store environment for use by WebView2 controls
        // Note: Individual WebView2 controls will use this environment
    }

    /// <summary>
    /// Builds the configuration from appsettings.json and User Secrets.
    /// </summary>
    /// <returns>The configuration root.</returns>
    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<App>(); // Load secrets from UserSecretsId in .csproj (overrides appsettings.json)

        return builder.Build();
    }

    /// <summary>
    /// Loads user settings from the local application data folder.
    /// Called before DI container is built, so cannot use IUserSettingsService.
    /// </summary>
    /// <returns>User settings, or defaults if file does not exist.</returns>
    private static UserSettings LoadUserSettings()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsPath = Path.Combine(localAppData, "YieldRaccoon", "settings.json");

            if (!File.Exists(settingsPath))
            {
                Logger.Debug("User settings file not found, using defaults");
                return new UserSettings();
            }

            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

            Logger.Info($"Loaded user settings from {settingsPath}");
            return settings ?? new UserSettings();
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to load user settings, using defaults");
            return new UserSettings();
        }
    }

    /// <summary>
    /// Applies user settings to database options, overriding appsettings.json values.
    /// </summary>
    /// <param name="databaseOptions">Database options to modify.</param>
    /// <param name="userSettings">User settings containing overrides.</param>
    private static void ApplyUserSettings(DatabaseOptions databaseOptions, UserSettings userSettings)
    {
        if (userSettings.DatabaseProvider.HasValue)
        {
            databaseOptions.Provider = userSettings.DatabaseProvider.Value;
            Logger.Info($"Applied user database provider: {userSettings.DatabaseProvider.Value}");
        }

        if (!string.IsNullOrWhiteSpace(userSettings.DatabasePath))
        {
            databaseOptions.ConnectionString = $"Data Source={userSettings.DatabasePath}";
            Logger.Info($"Applied user database path: {userSettings.DatabasePath}");
        }

        if (!string.IsNullOrWhiteSpace(userSettings.BackendApiUrl))
        {
            databaseOptions.BackendApiUrl = userSettings.BackendApiUrl;
            Logger.Info($"Applied user Backend API URL: {userSettings.BackendApiUrl}");
        }

        if (!string.IsNullOrWhiteSpace(userSettings.BackendApiKey))
        {
            databaseOptions.BackendApiKey = userSettings.BackendApiKey;
            Logger.Info("Applied user Backend API key (value hidden)");
        }
    }

    /// <summary>
    /// Initializes the database when using a persistent provider (SQLite or DualWrite).
    /// Ensures the database and tables are created.
    /// </summary>
    /// <param name="databaseOptions">The database configuration options.</param>
    private async Task InitializeDatabaseAsync(DatabaseOptions databaseOptions)
    {
        if (databaseOptions.Provider == DatabaseProvider.InMemory)
        {
            Logger.Debug("Database initialization skipped (using InMemory provider)");
            return;
        }

        try
        {
            Logger.Info($"Initializing SQLite database: {databaseOptions.ConnectionString}");

            var dbContext = _appScope!.Resolve<YieldRaccoonDbContext>();

            // One-time baseline stamp: legacy databases were created via EnsureCreatedAsync
            // and have no __EFMigrationsHistory table. Insert the InitialCreate row so
            // MigrateAsync only applies the genuinely-new migrations (AddFundAllocations).
            await StampBaselineMigrationIfNeededAsync(dbContext);

            // Apply pending migrations (creates schema on first run via the baseline migration)
            await dbContext.Database.MigrateAsync();

            // Enable WAL so readers (e.g., scheduled statistics export) don't block on writers
            // (the active crawl session). WAL setting is persistent per database file.
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

            Logger.Info("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize database");
            throw;
        }
    }

    /// <summary>
    /// Detects pre-migration databases (created by an earlier <c>EnsureCreatedAsync</c> flow)
    /// and stamps the InitialCreate baseline migration as already applied. Without this,
    /// <see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/> would try to re-create
    /// existing tables and fail. New databases are untouched and proceed through the normal
    /// migration flow.
    /// </summary>
    private static async Task StampBaselineMigrationIfNeededAsync(YieldRaccoonDbContext dbContext)
    {
        var hasFundProfiles = await TableExistsAsync(dbContext, "FundProfiles");
        var hasMigrationHistory = await TableExistsAsync(dbContext, "__EFMigrationsHistory");

        if (!hasFundProfiles || hasMigrationHistory)
            return;

        Logger.Info("Detected pre-migration database — stamping InitialCreate baseline");

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260508134923_InitialCreate', '9.0.0');
            """);
    }

    private static async Task<bool> TableExistsAsync(YieldRaccoonDbContext dbContext, string tableName)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        var nameParam = cmd.CreateParameter();
        nameParam.ParameterName = "$name";
        nameParam.Value = tableName;
        cmd.Parameters.Add(nameParam);

        var result = await cmd.ExecuteScalarAsync();
        return result is not null && Convert.ToInt32(result) > 0;
    }
    
}
