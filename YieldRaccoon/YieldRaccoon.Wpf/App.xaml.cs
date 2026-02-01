using System.IO;
using System.Text.Json;
using System.Windows;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Web.WebView2.Core;
using NLog;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Wpf.Configuration;
using YieldRaccoon.Wpf.Modules;

namespace YieldRaccoon.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private IContainer? _container;
    private ILifetimeScope? _appScope;
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

        // Initialize WebView2 environment with Chrome user agent
        await InitializeWebView2EnvironmentAsync();

        // Build configuration from User Secrets and appsettings.json
        var configuration = BuildConfiguration();

        // Configure Autofac container
        var builder = new ContainerBuilder();

        // Register configuration options
        var options = configuration.GetSection("YieldRaccoon").Get<YieldRaccoonOptions>() ?? new YieldRaccoonOptions();
        builder.RegisterInstance(options).AsSelf().SingleInstance();

        // Load database options from appsettings.json
        var databaseOptions = configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();

        // Load and apply user settings (overrides appsettings.json)
        var userSettings = LoadUserSettings();
        ApplyUserSettings(databaseOptions, userSettings);
        builder.RegisterInstance(databaseOptions).AsSelf().SingleInstance();
        builder.RegisterInstance(userSettings).AsSelf().SingleInstance();

        Logger.Info($"Database provider: {databaseOptions.Provider}");
        Logger.Info($"Database connection: {databaseOptions.ConnectionString}");

        // Register presentation module (ViewModels, Views, Logging infrastructure)
        builder.RegisterModule(new PresentationModule(databaseOptions));

        // Build container and create app-level lifetime scope
        _container = builder.Build();
        _appScope = _container.BeginLifetimeScope();

        // Initialize database if using SQLite provider
        await InitializeDatabaseAsync(databaseOptions);

        Logger.Info("DI container configured");

        // Resolve and show main window (DataContext is set by MainWindow constructor)
        var mainWindow = _appScope.Resolve<MainWindow>();
        mainWindow.Show();

        Logger.Info("Application started successfully");
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
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
        if (!string.IsNullOrWhiteSpace(userSettings.DatabasePath))
        {
            databaseOptions.ConnectionString = $"Data Source={userSettings.DatabasePath}";
            Logger.Info($"Applied user database path: {userSettings.DatabasePath}");
        }
    }

    /// <summary>
    /// Initializes the database when using SQLite provider.
    /// Ensures the database and tables are created.
    /// </summary>
    /// <param name="databaseOptions">The database configuration options.</param>
    private async Task InitializeDatabaseAsync(DatabaseOptions databaseOptions)
    {
        if (databaseOptions.Provider != DatabaseProvider.SQLite)
        {
            Logger.Debug("Database initialization skipped (using InMemory provider)");
            return;
        }

        try
        {
            Logger.Info($"Initializing SQLite database: {databaseOptions.ConnectionString}");

            var dbContext = _appScope!.Resolve<YieldRaccoonDbContext>();

            // Ensure database is created (applies pending migrations or creates from model)
            await dbContext.Database.EnsureCreatedAsync();

            Logger.Info("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize database");
            throw;
        }
    }
}