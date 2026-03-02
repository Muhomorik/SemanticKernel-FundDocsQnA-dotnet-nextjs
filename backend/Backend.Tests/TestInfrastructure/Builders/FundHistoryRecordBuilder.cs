using AutoFixture.Kernel;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for <see cref="FundHistoryRecord"/>.
/// Creates valid history records with sensible default values.
/// </summary>
public class FundHistoryRecordBuilder : ISpecimenBuilder
{
    private int _counter;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(FundHistoryRecord))
        {
            var number = Interlocked.Increment(ref _counter);
            var isinId = (IsinId)context.Resolve(typeof(IsinId));

            return new FundHistoryRecord
            {
                Id = FundHistoryRecordId.New(),
                IsinId = isinId,
                Nav = 100m + number,
                NavDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-number)),
                Capital = 1_000_000m + number,
                NumberOfOwners = 1000 + number,
                Risk = 4,
                SharpeRatio = 1.5m,
                StandardDeviation = 12.3m
            };
        }

        return new NoSpecimen();
    }
}
