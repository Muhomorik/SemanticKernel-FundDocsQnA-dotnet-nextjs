using System.Text.Json;

using Backend.API.Configuration;

namespace Backend.API.ApplicationCore.Services;

/// <inheritdoc />
/// <remarks>
/// Method ids are only ever looked up in <c>methods.json</c>, so request input never becomes
/// part of a path. The manifest is cached and re-read when its last write time changes.
/// </remarks>
public class FundClusterFiles : IFundClusterFiles
{
    private const string ManifestFile = "methods.json";
    private const string FundsFile = "funds.json";

    private readonly string _directory;
    private readonly ILogger<FundClusterFiles> _logger;
    private readonly Lock _lock = new();
    private DateTime _manifestWriteTime;
    private Dictionary<string, string> _methodFiles = [];

    public FundClusterFiles(
        BackendOptions options,
        IHostEnvironment environment,
        ILogger<FundClusterFiles> logger)
    {
        _directory = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.FundClustersDirectory)));
        _logger = logger;
    }

    /// <inheritdoc />
    public string? GetManifestPath() => ExistingFile(ManifestFile);

    /// <inheritdoc />
    public string? GetFundsPath() => ExistingFile(FundsFile);

    /// <inheritdoc />
    public string? GetMethodPath(string methodId)
    {
        var methodFiles = LoadMethodFiles();
        return methodFiles.TryGetValue(methodId, out var file) ? ExistingFile(file) : null;
    }

    private string? ExistingFile(string name)
    {
        var path = Path.GetFullPath(Path.Combine(_directory, name));
        var insideDirectory = Path.GetDirectoryName(path) == _directory;
        return insideDirectory && File.Exists(path) ? path : null;
    }

    private Dictionary<string, string> LoadMethodFiles()
    {
        var manifestPath = ExistingFile(ManifestFile);
        if (manifestPath is null)
            return [];

        var writeTime = File.GetLastWriteTimeUtc(manifestPath);
        lock (_lock)
        {
            if (writeTime == _manifestWriteTime)
                return _methodFiles;

            _methodFiles = ReadManifest(manifestPath);
            _manifestWriteTime = writeTime;
            return _methodFiles;
        }
    }

    private Dictionary<string, string> ReadManifest(string manifestPath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            return document.RootElement.GetProperty("methods")
                .EnumerateArray()
                .ToDictionary(
                    m => m.GetProperty("id").GetString()!,
                    m => m.GetProperty("file").GetString()!);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            _logger.LogError(ex, "Invalid fund cluster manifest at {Path}", manifestPath);
            return [];
        }
    }
}
