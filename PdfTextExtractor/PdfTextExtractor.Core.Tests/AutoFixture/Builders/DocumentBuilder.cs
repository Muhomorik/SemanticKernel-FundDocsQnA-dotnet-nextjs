using AutoFixture;
using AutoFixture.Kernel;
using PdfTextExtractor.Core.Domain.Entities;
using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class DocumentBuilder : ISpecimenBuilder
{
    private static readonly Random _random = new();

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(Document))
        {
            var filePath = SpecimenFactory.Create<FilePath>(context);
            var fileSizeBytes = _random.Next(1000, 10_000_000); // 1KB to 10MB
            var correlationId = SpecimenFactory.Create<CorrelationId>(context);

            return Document.Create(filePath, fileSizeBytes, correlationId);
        }

        return new NoSpecimen();
    }
}
