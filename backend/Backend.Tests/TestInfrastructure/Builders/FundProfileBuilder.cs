using AutoFixture.Kernel;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for <see cref="FundProfile"/>.
/// Creates valid fund profiles with required fields populated.
/// </summary>
public class FundProfileBuilder : ISpecimenBuilder
{
    private int _counter;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(FundProfile))
        {
            var number = Interlocked.Increment(ref _counter);
            var isinId = (IsinId)context.Resolve(typeof(IsinId));

            return new FundProfile
            {
                Id = isinId,
                Name = $"Test Fund {number}",
                FirstSeenAt = DateTimeOffset.UtcNow,
                CrawlerLastUpdatedAt = DateTimeOffset.UtcNow,
                Category = "Equity",
                Capital = 1_000_000m + number
            };
        }

        return new NoSpecimen();
    }
}
