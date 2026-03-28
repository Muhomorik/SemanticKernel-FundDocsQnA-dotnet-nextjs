using System.Text.Json.Serialization;

namespace YieldRaccoon.Infrastructure.Models;

/// <summary>
/// Anti-corruption DTO for the <c>_api/fund-reference/reference/{orderBookId}</c> response.
/// Only the fields needed by the ingestion pipeline are mapped.
/// </summary>
public sealed record FundReferenceResponse(
    [property: JsonPropertyName("description")] string? Description);
