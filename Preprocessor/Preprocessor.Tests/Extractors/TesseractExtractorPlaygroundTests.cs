using Microsoft.Extensions.Logging;

using Moq;

using Preprocessor.Extractors;
using Preprocessor.Services;

using Tesseract;

using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Preprocessor.Tests.Extractors;

[TestFixture]
[Explicit("Layout helper that exports same pdf with layout draw.")]
[TestOf(typeof(PdfPigExtractor))]
public class TesseractExtractorPlaygroundTests
{
    private const string TestPdfFileName = "SEB Asienfond ex Japan D utd.pdf";

    private Mock<ILogger<PdfPigExtractor>> _loggerMock = null!;
    private PdfPigExtractor _extractor = null!;
    private string _testPdfPath = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<PdfPigExtractor>>();
        _extractor = new PdfPigExtractor(_loggerMock.Object, new SentenceBoundaryChunker(maxChunkSize: 1000));

        var testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");
        //_testPdfPath = Path.Combine(testDataDir, TestPdfFileName);
        _testPdfPath =
            @"C:\Users\dmitr\Documents\Projects_Docs\SemanticKernel-FundFactsQnA-dotnet-nextjs\pdf_page_3.png";
    }

    [Test]
    public void Extractor_v1()
    {
        var tessdataPath =
            @"C:\Users\dmitr\Documents\Repos\SemanticKernel-FundDocsQnA-dotnet-nextjs\Preprocessor\Preprocessor\bin\Debug\net9.0\tessdata";
        using (var engine = new TesseractEngine(tessdataPath, "swe", EngineMode.Default))
        {
            using (var img = Pix.LoadFromFile(_testPdfPath))
            {
                using (var page = engine.Process(img))
                {
                    var text = page.GetText();
                    Console.WriteLine("Mean confidence: {0}", page.GetMeanConfidence());

                    Console.WriteLine("Text (GetText): \r\n{0}", text);
                    Console.WriteLine("Text (iterator):");
                }
            }
        }
    }
}