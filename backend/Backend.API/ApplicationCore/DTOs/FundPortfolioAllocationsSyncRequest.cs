using System.ComponentModel.DataAnnotations;

namespace Backend.API.ApplicationCore.DTOs;

/// <summary>
/// Request body for <c>POST /api/funds/portfolio-allocations</c>.
/// Carries the latest country and sector allocation snapshots for a single fund.
/// </summary>
public sealed record FundPortfolioAllocationsSyncRequest
{
    /// <summary>Fund ISIN identifier (12 characters).</summary>
    [Required]
    [StringLength(12, MinimumLength = 12)]
    public required string Isin { get; init; }

    /// <summary>Country allocations from the latest portfolio-data response.</summary>
    public IReadOnlyList<ApiCountryAllocationDto> Countries { get; init; } = [];

    /// <summary>Sector allocations from the latest portfolio-data response.</summary>
    public IReadOnlyList<ApiSectorAllocationDto> Sectors { get; init; } = [];
}

/// <summary>Per-country allocation entry.</summary>
public sealed record ApiCountryAllocationDto
{
    /// <summary>Display name (e.g., "USA", "Kanada").</summary>
    [Required]
    [StringLength(200)]
    public required string DisplayName { get; init; }

    /// <summary>ISO 3166-1 alpha-2 code (e.g., "US"). Nullable.</summary>
    [StringLength(2)]
    public string? CountryCode { get; init; }

    /// <summary>Percentage of the fund's portfolio (0–100).</summary>
    [Range(0, 100)]
    public required double Percentage { get; init; }
}

/// <summary>Per-sector allocation entry.</summary>
public sealed record ApiSectorAllocationDto
{
    /// <summary>Display name (e.g., "Teknik", "Råvaror").</summary>
    [Required]
    [StringLength(200)]
    public required string DisplayName { get; init; }

    /// <summary>Percentage of the fund's portfolio (0–100).</summary>
    [Range(0, 100)]
    public required double Percentage { get; init; }
}
