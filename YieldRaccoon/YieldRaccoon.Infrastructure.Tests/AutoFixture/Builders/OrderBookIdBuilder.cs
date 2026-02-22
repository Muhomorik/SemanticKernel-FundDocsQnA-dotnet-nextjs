using AutoFixture.Kernel;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Tests.AutoFixture.Builders;

/// <summary>
/// Specimen builder that generates valid <see cref="OrderBookId"/> instances.
/// </summary>
public class OrderBookIdBuilder : ISpecimenBuilder
{
    private static long _counter;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(OrderBookId))
            return new NoSpecimen();

        var id = Interlocked.Increment(ref _counter);
        return OrderBookId.Create($"OB-{id:D6}");
    }
}
