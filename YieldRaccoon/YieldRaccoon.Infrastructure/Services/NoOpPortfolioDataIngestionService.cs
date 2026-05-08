using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;


/// <summary>
/// No-op implementation of <see cref="IPortfolioDataIngestionService"/> used when the
/// InMemory database provider is selected (no persistence layer to write to).
/// </summary>
public class NoOpPortfolioDataIngestionService : IPortfolioDataIngestionService
{
    /// <inheritdoc />
    public Task<int> IngestPortfolioDataAsync(
        AboutFundPageData pageData,
        IsinId isinId,
        CancellationToken cancellationToken = default) => Task.FromResult(0);
}
