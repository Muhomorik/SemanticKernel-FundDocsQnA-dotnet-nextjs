using Microsoft.Extensions.Logging;

using Moq;

using Preprocessor.Extractors;
using Preprocessor.Services;

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
public class PdfPigExtractorPlaygroundTests
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
        _testPdfPath = Path.Combine(testDataDir, TestPdfFileName);
    }

    [Test]
    public void Extractor_v1()
    {
        var sourcePdfPath = _testPdfPath;
        var outputPath = "marked_pdf.pdf";
        var pageNumber = 1;
        using (var document = PdfDocument.Open(sourcePdfPath))
        {
            var builder = new PdfDocumentBuilder { };
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);
            var pageBuilder = builder.AddPage(document, pageNumber);
            pageBuilder.SetStrokeColor(0, 255, 0);
            var page = document.GetPage(pageNumber);

            var letters = page.Letters; // no preprocessing

            // 1. Extract words
            var wordExtractor = NearestNeighbourWordExtractor.Instance;

            var words = wordExtractor.GetWords(letters);

            // 2. Segment page
            var pageSegmenter = DocstrumBoundingBoxes.Instance;

            var textBlocks = pageSegmenter.GetBlocks(words);

            // 3. Postprocessing
            var readingOrder = UnsupervisedReadingOrderDetector.Instance;
            var orderedTextBlocks = readingOrder.Get(textBlocks);

            // 4. Add debug info - Bounding boxes and reading order
            foreach (var block in orderedTextBlocks)
            {
                var bbox = block.BoundingBox;
                pageBuilder.DrawRectangle(bbox.BottomLeft, bbox.Width, bbox.Height);
                pageBuilder.AddText(block.ReadingOrder.ToString(), 8, bbox.TopLeft, font);
            }

            // 5. Write result to a file
            var fileBytes = builder.Build();
            File.WriteAllBytes(outputPath, fileBytes); // save to file
        }
    }

    [Test]
    public void Extractor_Recursive_XY_Cut()
    {
        var sourcePdfPath = _testPdfPath;
        var outputPath = "marked_pdf_recursive_xy_cut.pdf";
        var textOutputPath = "marked_pdf_recursive_xy_cut.txt";

        // By default, NearestNeighbourWordExtractor uses internal logic for distance threshold (typically 0.2 for axis-aligned text).
        // To make grouping looser, relax the Filter function to allow more candidates to be grouped as a word.
        var wordExtractorOptions = new NearestNeighbourWordExtractor.NearestNeighbourWordExtractorOptions
        {
            Filter = (pivot, candidate) =>
            {
                // Looser grouping: allow grouping unless candidate is whitespace
                return !string.IsNullOrWhiteSpace(candidate.Value);
            }
        };
        var wordExtractor = new NearestNeighbourWordExtractor(wordExtractorOptions);
        var pageSegmenter = RecursiveXYCut.Instance;
        var readingOrder = UnsupervisedReadingOrderDetector.Instance;

        using (var document = PdfDocument.Open(sourcePdfPath))
        {
            var builder = new PdfDocumentBuilder { };
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);
            var allText = new System.Text.StringBuilder();

            for (var pageIdx = 0; pageIdx < document.NumberOfPages; pageIdx++)
            {
                var pageNumber = pageIdx + 1;
                var pageBuilder = builder.AddPage(document, pageNumber);
                pageBuilder.SetStrokeColor(0, 0, 255); // Use blue for XY Cut
                var page = document.GetPage(pageNumber);

                var letters = page.Letters; // no preprocessing
                var words = wordExtractor.GetWords(letters);
                var textBlocks = pageSegmenter.GetBlocks(words);
                var orderedTextBlocks = readingOrder.Get(textBlocks);

                foreach (var block in orderedTextBlocks)
                {
                    foreach (var line in block.TextLines)
                    {
                        // Normalize spaces between words in the line
                        var lineText = string.Join(" ", line.Words.Select(w => w.Text));
                        allText.AppendLine(lineText);
                    }

                    var bbox = block.BoundingBox;
                    pageBuilder.DrawRectangle(bbox.BottomLeft, bbox.Width, bbox.Height);
                    pageBuilder.AddText(block.ReadingOrder.ToString(), 8, bbox.TopLeft, font);
                }
            }

            var fileBytes = builder.Build();
            File.WriteAllBytes(outputPath, fileBytes); // save to file
            File.WriteAllText(textOutputPath, allText.ToString()); // save extracted text
        }
    }

    [Test]
    public void Extractor_Recursive_XY_Cut_export_page_format()
    {
        var sourcePdfPath = _testPdfPath;
        var outputPath = "marked_pdf_recursive_xy_cut_page.xml";
        var wordExtractor = NearestNeighbourWordExtractor.Instance;
        var pageSegmenter = RecursiveXYCut.Instance;
        var readingOrder = UnsupervisedReadingOrderDetector.Instance;
        var exporter = new UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PageXmlTextExporter(
            wordExtractor,
            pageSegmenter,
            readingOrder);

        using (var document = PdfDocument.Open(sourcePdfPath))
        {
            var page = document.GetPage(1);
            var xml = exporter.Get(page);
            File.WriteAllText(outputPath, xml);
        }
    }
}