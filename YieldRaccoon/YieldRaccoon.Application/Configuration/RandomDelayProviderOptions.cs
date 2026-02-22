namespace YieldRaccoon.Application.Configuration;

/// <summary>
/// Configurable bounds for the randomized delay provider.
/// Registered at the composition root; consumed by the delay provider implementation.
/// </summary>
/// <param name="MinDelaySeconds">Minimum delay in seconds (inclusive). Must be >= 1.</param>
/// <param name="MaxDelaySeconds">Maximum delay in seconds (inclusive). Must be >= <paramref name="MinDelaySeconds"/>.</param>
public record RandomDelayProviderOptions(int MinDelaySeconds, int MaxDelaySeconds);
