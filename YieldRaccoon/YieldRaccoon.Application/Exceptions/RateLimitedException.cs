namespace YieldRaccoon.Application.Exceptions;

/// <summary>
/// Thrown when the Backend API returns HTTP 429 and all retry attempts are exhausted.
/// </summary>
public class RateLimitedException : Exception
{
    public int AttemptsExhausted { get; }

    public RateLimitedException(int attempts)
        : base($"Rate limited by Backend API after {attempts} retry attempts")
    {
        AttemptsExhausted = attempts;
    }
}
