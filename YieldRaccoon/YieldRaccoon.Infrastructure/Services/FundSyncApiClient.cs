using System.Net.Http.Json;
using NLog;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// HTTP client for the Backend API fund sync endpoints.
/// </summary>
/// <remarks>
/// The <see cref="HttpClient"/> is pre-configured by DI with
/// <c>BaseAddress</c> and <c>Authorization: ApiKey {key}</c> header.
/// </remarks>
public class FundSyncApiClient : IFundSyncApiClient
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

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

        var response = await _httpClient.PostAsJsonAsync("api/funds/list", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FundSyncResponse>(cancellationToken);
        return result ?? throw new InvalidOperationException("Backend API returned null response");
    }

    /// <inheritdoc />
    public async Task<FundSyncResponse> SyncFundAboutAsync(
        FundAboutSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Syncing about-fund data for {0} to POST /api/funds/about", request.Profile.Isin);

        var response = await _httpClient.PostAsJsonAsync("api/funds/about", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FundSyncResponse>(cancellationToken);
        return result ?? throw new InvalidOperationException("Backend API returned null response");
    }
}
