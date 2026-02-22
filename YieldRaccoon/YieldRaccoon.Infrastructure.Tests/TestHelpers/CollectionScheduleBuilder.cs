using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Fluent builder for <see cref="AboutFundCollectionSchedule"/> with realistic step timings.
/// </summary>
public class CollectionScheduleBuilder
{
    private OrderBookId _orderBookId = OrderBookId.Create("OB-TEST-001");
    private DateTimeOffset _startTime = DateTimeOffset.UtcNow;
    private TimeSpan _safetyNetBuffer = TimeSpan.FromSeconds(15);
    private readonly List<AboutFundScheduledStep> _steps = [];

    /// <summary>
    /// Default delays per step kind, matching <c>IAboutFundPageInteractor.GetMinimumDelay</c>.
    /// </summary>
    public static IReadOnlyDictionary<AboutFundCollectionStepKind, TimeSpan> DefaultDelays { get; } =
        new Dictionary<AboutFundCollectionStepKind, TimeSpan>
        {
            [AboutFundCollectionStepKind.ActivateSekView] = TimeSpan.FromSeconds(30),
            [AboutFundCollectionStepKind.Select1Month] = TimeSpan.FromSeconds(10),
            [AboutFundCollectionStepKind.Select3Months] = TimeSpan.FromSeconds(10),
            [AboutFundCollectionStepKind.SelectYearToDate] = TimeSpan.FromSeconds(10),
            [AboutFundCollectionStepKind.Select1Year] = TimeSpan.FromSeconds(10),
            [AboutFundCollectionStepKind.Select3Years] = TimeSpan.FromSeconds(10),
            [AboutFundCollectionStepKind.Select5Years] = TimeSpan.FromSeconds(10),
            [AboutFundCollectionStepKind.SelectMax] = TimeSpan.FromSeconds(10),
        };

    public CollectionScheduleBuilder WithOrderBookId(OrderBookId id)
    {
        _orderBookId = id;
        return this;
    }

    public CollectionScheduleBuilder WithStartTime(DateTimeOffset start)
    {
        _startTime = start;
        return this;
    }

    public CollectionScheduleBuilder WithSafetyNetBuffer(TimeSpan buffer)
    {
        _safetyNetBuffer = buffer;
        return this;
    }

    /// <summary>
    /// Builds with all 8 steps using cumulative default delays.
    /// ActivateSekView at +0s, Select1Month at +30s, Select3Months at +40s, ..., SelectMax at +100s.
    /// </summary>
    public CollectionScheduleBuilder WithAllDefaultSteps()
    {
        return WithSteps([.. AboutFundCollectionStepKinds.All]);
    }

    /// <summary>
    /// Builds with the specified step kinds using cumulative default delays.
    /// </summary>
    public CollectionScheduleBuilder WithSteps(params AboutFundCollectionStepKind[] kinds)
    {
        _steps.Clear();
        var cumulative = TimeSpan.Zero;
        foreach (var kind in kinds)
        {
            _steps.Add(new AboutFundScheduledStep(kind, _startTime + cumulative));
            cumulative += DefaultDelays[kind];
        }
        return this;
    }

    public AboutFundCollectionSchedule Build()
    {
        var lastStepEnd = _steps.Count > 0
            ? _steps[^1].FireAt - _startTime + DefaultDelays[_steps[^1].Kind]
            : TimeSpan.Zero;

        return new AboutFundCollectionSchedule
        {
            OrderBookId = _orderBookId,
            StartTime = _startTime,
            StopTime = _startTime + lastStepEnd + _safetyNetBuffer,
            Steps = _steps.ToList(),
            InterPageDelay = TimeSpan.FromSeconds(5),
        };
    }
}
