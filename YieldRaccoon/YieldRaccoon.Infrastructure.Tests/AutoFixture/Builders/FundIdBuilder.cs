using AutoFixture.Kernel;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Tests.AutoFixture.Builders;

/// <summary>
/// Specimen builder that generates valid <see cref="IsinId"/> instances.
/// </summary>
public class FundIdBuilder : ISpecimenBuilder
{
    private static readonly Random Random = new();
    private const string AlphaNumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type type || type != typeof(IsinId))
        {
            return new NoSpecimen();
        }

        // Generate valid ISIN: 2 uppercase letters + 9 alphanumeric + 1 digit
        // Example: SE0008613939
        var middle = new string(Enumerable.Range(0, 9)
            .Select(_ => AlphaNumeric[Random.Next(AlphaNumeric.Length)])
            .ToArray());
        var checkDigit = Random.Next(0, 10);
        var isin = $"SE{middle}{checkDigit}";

        return IsinId.Create(isin);
    }
}
