using AutoFixture.Kernel;
using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class DocumentPageBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(DocumentPage))
        {
            return new DocumentPage
            {
                SourceFile = (string?)context.Resolve(typeof(string)) ?? "sample.pdf",
                PageNumber = (int?)context.Resolve(typeof(int)) ?? 1,
                PageText = (string?)context.Resolve(typeof(string)) ?? "Sample page text"
            };
        }

        return new NoSpecimen();
    }
}
