using AutoFixture.Kernel;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for <see cref="IsinId"/>.
/// Generates valid random Swedish ISINs (SE + 9 digits + check digit).
/// </summary>
public class IsinIdBuilder : ISpecimenBuilder
{
    private int _counter;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(IsinId))
        {
            var number = Interlocked.Increment(ref _counter);
            // Generate a valid ISIN: SE + 9 zero-padded digits + last digit as check
            var middle = number.ToString().PadLeft(9, '0');
            var checkDigit = number % 10;
            return IsinId.Create($"SE{middle}{checkDigit}");
        }

        return new NoSpecimen();
    }
}
