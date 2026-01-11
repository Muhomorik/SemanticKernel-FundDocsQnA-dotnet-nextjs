using Backend.API.Configuration;

namespace Backend.API.Middleware;

/// <summary>
/// Middleware for validating API key authentication on embedding management endpoints.
/// Only active when VectorStorageType is CosmosDb.
/// Expected header format: Authorization: ApiKey {key}
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private const string ApiKeyHeaderName = "Authorization";
    private const string ApiKeyScheme = "ApiKey";

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, BackendOptions options)
    {
        // Only protect /api/embeddings endpoints
        if (!context.Request.Path.StartsWithSegments("/api/embeddings", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Only apply authentication when Cosmos DB storage is enabled
        if (options.VectorStorageType != VectorStorageType.CosmosDb)
        {
            await _next(context);
            return;
        }

        // Check if API key is configured
        if (string.IsNullOrWhiteSpace(options.EmbeddingApiKey))
        {
            _logger.LogError("EmbeddingApiKey not configured but Cosmos DB storage is enabled");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Server configuration error: API key not configured"
            });
            return;
        }

        // Extract Authorization header
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var authHeader))
        {
            _logger.LogWarning("Missing Authorization header for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Missing Authorization header. Expected format: 'Authorization: ApiKey <key>'"
            });
            return;
        }

        // Parse Authorization header (format: "ApiKey <key>")
        var authHeaderValue = authHeader.ToString();
        if (!authHeaderValue.StartsWith($"{ApiKeyScheme} ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Invalid Authorization header format for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = $"Invalid Authorization header format. Expected: 'Authorization: {ApiKeyScheme} <key>'"
            });
            return;
        }

        var providedKey = authHeaderValue.Substring(ApiKeyScheme.Length + 1).Trim();

        // Validate API key (constant-time comparison to prevent timing attacks)
        if (!IsValidApiKey(providedKey, options.EmbeddingApiKey))
        {
            _logger.LogWarning("Invalid API key provided for {Path} from {IP}",
                context.Request.Path,
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid API key"
            });
            return;
        }

        // API key valid - proceed to next middleware
        await _next(context);
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks.
    /// </summary>
    private static bool IsValidApiKey(string providedKey, string expectedKey)
    {
        if (string.IsNullOrEmpty(providedKey) || string.IsNullOrEmpty(expectedKey))
        {
            return false;
        }

        // Use SequenceEqual for constant-time comparison
        var providedBytes = System.Text.Encoding.UTF8.GetBytes(providedKey);
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expectedKey);

        if (providedBytes.Length != expectedBytes.Length)
        {
            return false;
        }

        var areEqual = true;
        for (int i = 0; i < providedBytes.Length; i++)
        {
            areEqual &= providedBytes[i] == expectedBytes[i];
        }

        return areEqual;
    }
}
