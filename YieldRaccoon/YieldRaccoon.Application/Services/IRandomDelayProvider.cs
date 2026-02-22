namespace YieldRaccoon.Application.Services;

/// <summary>
/// Generates randomized delays within a configurable interval.
/// Used to space out page interactions so they resemble natural browsing behavior.
/// </summary>
public interface IRandomDelayProvider
{
    /// <summary>
    /// Returns the next randomized delay within the configured bounds.
    /// </summary>
    TimeSpan NextDelay();

    /// <summary>
    /// Returns the next randomized delay using the specified minimum as the lower bound
    /// and <paramref name="minDelay"/> + 20 seconds as the upper bound.
    /// </summary>
    /// <param name="minDelay">Minimum delay (inclusive). The maximum is <paramref name="minDelay"/> + 20s.</param>
    TimeSpan NextDelay(TimeSpan minDelay);
}
