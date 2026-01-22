using AutoFixture.Kernel;
using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class PageContentBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(PageContent))
        {
            var content = context.Resolve(typeof(string)) as string;
            return PageContent.Create(string.IsNullOrWhiteSpace(content) ? "Sample page content" : content);
        }

        return new NoSpecimen();
    }
}
