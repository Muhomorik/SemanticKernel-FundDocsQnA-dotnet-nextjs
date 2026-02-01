using YieldRaccoon.Application.DTOs;

namespace YieldRaccoon.Application.Services;

/// <summary>
/// Service interface for ingesting fund data into the persistence layer.
/// </summary>
/// <remarks>
/// <para>
/// This service orchestrates the mapping of <see cref="FundDataDto"/> to domain entities
/// (<see cref="Domain.Entities.FundProfile"/> and <see cref="Domain.Entities.FundHistoryRecord"/>).
/// It decides what goes into the profile (static data) vs history (time-varying data).
/// </para>
/// </remarks>
public interface IFundIngestionService
{
    /// <summary>
    /// Ingests multiple fund data DTOs in a batch asynchronously.
    /// </summary>
    /// <param name="fundDataList">The collection of fund data to ingest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of successfully ingested funds.</returns>
    Task<int> IngestBatchAsync(IEnumerable<FundDataDto> fundDataList, CancellationToken cancellationToken = default);
}
