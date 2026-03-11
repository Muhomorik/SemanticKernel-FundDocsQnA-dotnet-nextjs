namespace Backend.API.ApplicationCore.DTOs.OwnershipFlow;

/// <summary>
/// Response for <c>GET /api/ownership-flow</c>.
/// Contains Sankey chart data for both category-level and fund-level diagrams.
/// </summary>
/// <param name="PeriodLabel">Human-readable period label (e.g., "Feb 10 – 16").</param>
/// <param name="Cat">Category-level aggregated ownership flow data.</param>
/// <param name="Fund">Fund-level top-10 ownership flow data.</param>
public record OwnershipFlowResponse(
    string PeriodLabel,
    OwnershipFlowGroup Cat,
    OwnershipFlowGroup Fund);

/// <summary>
/// Outflow/inflow pair for a Sankey diagram.
/// </summary>
/// <param name="Out">Items losing owners (sorted by absolute delta descending). Can be empty.</param>
/// <param name="In">Items gaining owners (sorted by delta descending). Can be empty.</param>
public record OwnershipFlowGroup(
    IReadOnlyList<OwnershipFlowItem> Out,
    IReadOnlyList<OwnershipFlowItem> In);

/// <summary>
/// A single node in the Sankey diagram (fund or category).
/// </summary>
/// <param name="Name">Fund or category name.</param>
/// <param name="Value">Absolute owner change count (always positive).</param>
/// <param name="Pct">Percentage change (negative for outflows, positive for inflows).</param>
public record OwnershipFlowItem(string Name, int Value, double Pct);
