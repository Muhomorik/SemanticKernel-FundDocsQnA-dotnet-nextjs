using AutoFixture.Kernel;
using PdfTextExtractor.Core.Configuration;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class LMStudioParametersBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(LMStudioParameters))
        {
            return new LMStudioParameters
            {
                PdfFolderPath = Path.Combine(Path.GetTempPath(), "PdfTextExtractor.Tests", "pdfs"),
                OutputFolderPath = Path.Combine(Path.GetTempPath(), "PdfTextExtractor.Tests", "output"),
                LMStudioUrl = "http://localhost:1234",
                VisionModelName = "llava-v1.6-mistral-7b",
                RasterizationDpi = 300,
                MaxTokens = 2000,
                ExtractionPrompt = "TEST: Extract text from image"
            };
        }

        return new NoSpecimen();
    }
}
