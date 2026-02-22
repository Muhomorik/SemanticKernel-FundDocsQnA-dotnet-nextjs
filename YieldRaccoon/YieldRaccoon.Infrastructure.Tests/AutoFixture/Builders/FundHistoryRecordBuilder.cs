using AutoFixture.Kernel;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Tests.AutoFixture.Builders;

/// <summary>
/// Specimen builder that generates valid <see cref="FundHistoryRecord"/> instances.
/// </summary>
public class FundHistoryRecordBuilder : ISpecimenBuilder
{
    private static readonly Random Random = new();

    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(FundHistoryRecord))
        {
            return new NoSpecimen();
        }

        var fundId = (IsinId)context.Resolve(typeof(IsinId));

        return new FundHistoryRecord
        {
            IsinId = fundId,
            Nav = Math.Round((decimal)(Random.NextDouble() * 1000), 4),
            NavDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
    }
}
