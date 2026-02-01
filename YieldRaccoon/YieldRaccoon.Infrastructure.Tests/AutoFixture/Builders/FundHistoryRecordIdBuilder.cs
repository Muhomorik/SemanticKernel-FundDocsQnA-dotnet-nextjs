using AutoFixture.Kernel;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Tests.AutoFixture.Builders;

/// <summary>
/// Specimen builder that generates valid <see cref="FundHistoryRecordId"/> instances.
/// </summary>
public class FundHistoryRecordIdBuilder : ISpecimenBuilder
{
    private static long _counter = 1;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(FundHistoryRecordId))
        {
            return new NoSpecimen();
        }

        return FundHistoryRecordId.Create(Interlocked.Increment(ref _counter));
    }
}
