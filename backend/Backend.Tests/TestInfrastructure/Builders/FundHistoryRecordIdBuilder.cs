using AutoFixture.Kernel;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for <see cref="FundHistoryRecordId"/>.
/// Returns <see cref="FundHistoryRecordId.New()"/> (value 0) so EF Core assigns the real PK.
/// </summary>
public class FundHistoryRecordIdBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(FundHistoryRecordId))
        {
            return FundHistoryRecordId.New();
        }

        return new NoSpecimen();
    }
}
