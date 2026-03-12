using System.Net;
using System.Net.Http.Json;
using NLog;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Exceptions;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// HTTP client for the Backend API fund sync endpoints.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="HttpClient"/> is pre-configured by DI with
/// <c>BaseAddress</c> and <c>Authorization: ApiKey {key}</c> header.
/// </para>
/// <para>
/// Retries HTTP 429 (Too Many Requests) responses with exponential backoff,
/// respecting the <c>Retry-After</c> header when present.
/// Throws <see cref="RateLimitedException"/> after all retries are exhausted.
/// </para>
/// </remarks>
public class FundSyncApiClient : IFundSyncApiClient
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    private const int MaxRetries = 3;

    private static readonly TimeSpan[] BackoffDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8)
    ];

    public FundSyncApiClient(ILogger logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public bool IsConfigured => _httpClient.BaseAddress is not null;

    /// <inheritdoc />
    public async Task<FundSyncResponse> SyncFundListAsync(
        FundListSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Syncing {0} funds to POST /api/funds/list", request.Funds.Count);

        var response = await SendWithRetryAsync(
            ct => _httpClient.PostAsJsonAsync("api/funds/list", request, ct),
            "SyncFundList",
            cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<FundSyncResponse>(cancellationToken);
        return result ?? throw new InvalidOperationException("Backend API returned null response");
    }

    /// <inheritdoc />
    public async Task<FundSyncResponse> SyncFundAboutAsync(
        FundAboutSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Syncing about-fund data for {0} to POST /api/funds/about", request.Profile.Isin);

        var response = await SendWithRetryAsync(
            ct => _httpClient.PostAsJsonAsync("api/funds/about", request, ct),
            "SyncFundAbout",
            cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<FundSyncResponse>(cancellationToken);
        return result ?? throw new InvalidOperationException("Backend API returned null response");
    }

    /// <inheritdoc />
    public async Task<FundSyncResponse> SyncFundFullHistoryAsync(
        FundFullHistorySyncRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Full history sync for {0}: {1} records to POST /api/funds/full-sync",
            request.Profile.Isin, request.HistoryRecords.Count);

        var response = await SendWithRetryAsync(
            ct => _httpClient.PostAsJsonAsync("api/funds/full-sync", request, ct),
            "SyncFundFullHistory",
            cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<FundSyncResponse>(cancellationToken);
        return result ?? throw new InvalidOperationException("Backend API returned null response");
    }

    /// <summary>
    /// Sends an HTTP request with automatic retry on 429 (Too Many Requests).
    /// Uses exponential backoff (2s, 4s, 8s) or the server's Retry-After header value.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> sendRequest,
        string operationName,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            var response = await sendRequest(cancellationToken);

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                response.EnsureSuccessStatusCode();
                return response;
            }

            if (attempt == MaxRetries)
            {
                _logger.Warn("{0}: rate limited — all {1} retries exhausted", operationName, MaxRetries);
                throw new RateLimitedException(MaxRetries);
            }

            var delay = GetRetryDelay(response, attempt);
            _logger.Info("{0}: rate limited (429) — retrying in {1:F0}s (attempt {2}/{3})",
                operationName, delay.TotalSeconds, attempt + 1, MaxRetries);

            response.Dispose();
            await Task.Delay(delay, cancellationToken);
        }

        throw new InvalidOperationException("Unreachable");
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return delta;

        if (response.Headers.RetryAfter?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            return wait > TimeSpan.Zero ? wait : BackoffDelays[attempt];
        }

        return BackoffDelays[attempt];
    }
}
