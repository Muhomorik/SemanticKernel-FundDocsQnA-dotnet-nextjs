using Backend.API.ApplicationCore.DTOs.OwnershipFlow;

namespace Backend.API.ApplicationCore.Services;

/// <summary>
/// Computes ownership flow data for Sankey chart visualization.
/// </summary>
public interface IOwnershipFlowService
{
    /// <summary>
    /// Returns available weekly and monthly time periods for the ownership flow selector.
    /// Pure computation based on the current date — no database access.
    /// </summary>
    OwnershipFlowPeriodsResponse GetAvailablePeriods();

    /// <summary>
    /// Computes ownership flow data (fund-level and category-level) for a given date range.
    /// </summary>
    /// <param name="from">Start date of the period (inclusive).</param>
    /// <param name="to">End date of the period (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sankey chart data for both category and fund diagrams.</returns>
    Task<OwnershipFlowResponse> GetOwnershipFlowAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
