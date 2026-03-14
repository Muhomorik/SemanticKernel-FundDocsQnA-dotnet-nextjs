namespace Backend.API.ApplicationCore.DTOs.OwnershipFlow;

/// <summary>
/// Response for <c>GET /api/ownership-flow/periods</c>.
/// Contains weekly and monthly time periods for the ownership flow time selector.
/// </summary>
public record OwnershipFlowPeriodsResponse(
    IReadOnlyList<TimePeriod> Weekly,
    IReadOnlyList<TimePeriod> Monthly);

/// <summary>
/// A selectable time period with a display label and ISO date range.
/// </summary>
/// <param name="Label">Display label (e.g., "Feb 10 – 16" or "1 month").</param>
/// <param name="From">Start date in ISO 8601 format (yyyy-MM-dd).</param>
/// <param name="To">End date in ISO 8601 format (yyyy-MM-dd).</param>
public record TimePeriod(string Label, string From, string To);
