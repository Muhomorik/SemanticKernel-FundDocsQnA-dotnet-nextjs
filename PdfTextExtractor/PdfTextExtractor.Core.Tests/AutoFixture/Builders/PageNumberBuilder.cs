using AutoFixture.Kernel;
using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class PageNumberBuilder : ISpecimenBuilder
{
    private static readonly Random _random = new();

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(PageNumber))
        {
            return PageNumber.Create(_random.Next(1, 1000));
        }

        return new NoSpecimen();
    }
}
