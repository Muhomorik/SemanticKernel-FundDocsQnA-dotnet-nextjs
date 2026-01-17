using AutoFixture;
using AutoFixture.Kernel;
using PdfTextExtractor.Core.Domain.Aggregates;
using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class ExtractionSessionBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(ExtractionSession))
        {
            var extractorType = SpecimenFactory.Create<ExtractorType>(context);
            return ExtractionSession.Create(extractorType);
        }

        return new NoSpecimen();
    }
}
