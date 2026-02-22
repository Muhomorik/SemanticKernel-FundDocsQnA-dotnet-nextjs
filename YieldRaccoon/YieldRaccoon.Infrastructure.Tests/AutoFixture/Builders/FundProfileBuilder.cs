using AutoFixture.Kernel;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Tests.AutoFixture.Builders;

/// <summary>
/// Specimen builder that generates valid <see cref="FundProfile"/> instances.
/// </summary>
public class FundProfileBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(FundProfile))
        {
            return new NoSpecimen();
        }

        var fundId = (IsinId)context.Resolve(typeof(IsinId));

        return new FundProfile
        {
            Id = fundId,
            Name = $"Test Fund {Guid.NewGuid():N}",
            FirstSeenAt = DateTimeOffset.UtcNow
        };
    }
}
