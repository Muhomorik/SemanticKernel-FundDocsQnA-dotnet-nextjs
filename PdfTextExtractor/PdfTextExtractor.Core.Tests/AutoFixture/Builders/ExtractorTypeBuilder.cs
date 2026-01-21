using AutoFixture.Kernel;
using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class ExtractorTypeBuilder : ISpecimenBuilder
{
    private static readonly Random _random = new();
    private static readonly string[] _types = ["PdfPig", "LMStudio"];

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(ExtractorType))
        {
            return ExtractorType.FromString(_types[_random.Next(_types.Length)]);
        }

        return new NoSpecimen();
    }
}
