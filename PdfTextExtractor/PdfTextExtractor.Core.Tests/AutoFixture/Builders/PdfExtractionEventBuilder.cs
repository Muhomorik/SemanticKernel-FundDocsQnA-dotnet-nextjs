using AutoFixture.Kernel;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Domain.Events.Document;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class PdfExtractionEventBuilder : ISpecimenBuilder
{
    private static readonly Random _random = new();

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && typeof(PdfExtractionEventBase).IsAssignableFrom(type))
        {
            // Example: Build DocumentExtractionStarted event
            if (type == typeof(DocumentExtractionStarted))
            {
                return new DocumentExtractionStarted
                {
                    CorrelationId = Guid.NewGuid(),
                    SessionId = Guid.NewGuid(),
                    ExtractorName = "PdfPig",
                    FilePath = $"test-document-{Guid.NewGuid():N}.pdf",
                    FileName = "test.pdf",
                    FileSizeBytes = _random.Next(1000, 10_000_000)
                };
            }
        }

        return new NoSpecimen();
    }
}
