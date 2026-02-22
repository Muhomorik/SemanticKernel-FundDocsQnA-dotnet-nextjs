using NLog;
using YieldRaccoon.Application.Configuration;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Generates randomized delays within a configurable interval.
/// Used to space out page interactions so they resemble natural browsing behavior.
/// </summary>
/// <remarks>
/// <para>
/// Registered in DI with min/max bounds. The consumer calls <see cref="NextDelay"/>
/// to get each independent random delay, then schedules it via <c>IScheduler</c>:
/// </para>
/// <code>
/// var delay = _delayProvider.NextDelay();
/// Observable.Timer(delay, _scheduler).Subscribe(_ => { ... });
/// </code>
/// <para>
/// To get the initial page-load delay before the first interaction, simply call
/// <see cref="NextDelay"/> once before starting the sequence.
/// </para>
/// </remarks>
public class RandomDelayProvider : IRandomDelayProvider
{
    private readonly ILogger _logger;
    private readonly Random _random = new();
    private readonly int _minSeconds;
    private readonly int _maxSeconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomDelayProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="options">Configurable delay bounds.</param>
    public RandomDelayProvider(ILogger logger, RandomDelayProviderOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(options);

        if (options.MinDelaySeconds < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "MinDelaySeconds must be >= 1.");
        if (options.MaxDelaySeconds < options.MinDelaySeconds)
            throw new ArgumentOutOfRangeException(nameof(options),
                $"MaxDelaySeconds must be >= MinDelaySeconds ({options.MinDelaySeconds}).");

        _minSeconds = options.MinDelaySeconds;
        _maxSeconds = options.MaxDelaySeconds;

        _logger.Debug("RandomDelayProvider initialized ({0}-{1}s)", _minSeconds, _maxSeconds);
    }

    /// <summary>
    /// Returns the next randomized delay within the configured bounds.
    /// </summary>
    public TimeSpan NextDelay()
    {
        var seconds = _random.Next(_minSeconds, _maxSeconds + 1);
        _logger.Trace("NextDelay: {0}s", seconds);
        return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Returns the next randomized delay using <paramref name="minDelay"/> as the lower bound
    /// and <paramref name="minDelay"/> + 20 seconds as the upper bound.
    /// </summary>
    public TimeSpan NextDelay(TimeSpan minDelay)
    {
        var minSeconds = (int)minDelay.TotalSeconds;
        var maxSeconds = minSeconds + 20;
        var seconds = _random.Next(minSeconds, maxSeconds + 1);
        _logger.Trace("NextDelay(min={0}s): {1}s", minSeconds, seconds);
        return TimeSpan.FromSeconds(seconds);
    }
}