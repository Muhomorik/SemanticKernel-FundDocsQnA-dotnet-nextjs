using AutoFixture;
using AutoFixture.Kernel;
using PdfTextExtractor.Core.Domain.Entities;
using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class PageBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(Page))
        {
            var pageNumber = SpecimenFactory.Create<PageNumber>(context);
            return Page.Create(pageNumber);
        }

        return new NoSpecimen();
    }
}
