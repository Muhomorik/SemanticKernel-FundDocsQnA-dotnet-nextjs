# PdfTextExtraction

Project is using domain-driven design (DDD) principles to separate concerns and improve maintainability.

## Project Structure

### PdfTextExtractor.Core (Class Library)

Contains core extraction logic, interfaces, and models. Reusable by all consumers. References PDF libraries (e.g., iTextSharp, PdfPig, or Tesseract for OCR).

### PdfTextExtractor.Cli (Console Application)

Command-line interface. Executable name: `pdftextextractor.exe` or `pdfextract.exe`. References PdfTextExtractor.Core.

### PdfTextExtractor.Wpf (WPF Application)

Windows desktop GUI. Executable name: `PdfTextExtractor.exe`. References PdfTextExtractor.Core.

## Domain Events Architecture

### Overview

The PdfTextExtractor system uses **domain events** to enable real-time monitoring and decoupled communication during PDF text extraction operations. This event-driven architecture provides several key benefits:

- **Real-time GUI updates**: WPF application receives live progress, text previews, and page images during extraction
- **Comprehensive logging**: CLI and services can subscribe to events for detailed operation tracking
- **Monitoring and diagnostics**: Track extraction performance, errors, and completion status
- **Decoupling**: Extractors don't need direct dependencies on UI or logging components
- **Extensibility**: New consumers can subscribe to events without modifying extraction logic

All events inherit from a common base class with rich metadata to support batch processing, correlation tracking, and error analysis.

### Event Infrastructure

#### Base Event Class

All extraction events inherit from `PdfExtractionEventBase`:

```csharp
public abstract class PdfExtractionEventBase
{
    // Unique identifier for this specific event instance
    public Guid EventId { get; init; }

    // Timestamp when event was raised (UTC)
    public DateTimeOffset Timestamp { get; init; }

    // Correlation ID - tracks a single file extraction operation
    // All events for one PDF file share the same CorrelationId
    public Guid CorrelationId { get; init; }

    // Session ID - tracks a batch extraction operation
    // All events for a batch of PDFs share the same SessionId
    public Guid SessionId { get; init; }

    // Name of the extractor raising this event (e.g., "PdfPig", "Tesseract")
    public string ExtractorName { get; init; }
}
```

**Metadata Properties:**

- **EventId**: Unique identifier for each event instance (useful for deduplication, event sourcing)
- **Timestamp**: When the event occurred (UTC), enables timeline reconstruction and performance analysis
- **CorrelationId**: Links all events for a single PDF file (track one file's complete extraction flow)
- **SessionId**: Links all events for a batch operation (track multi-file batch processing)
- **ExtractorName**: Identifies which extractor implementation raised the event ("PdfPig", "Tesseract", etc.)

#### Publisher/Subscriber Pattern

The event infrastructure uses the classic publisher/subscriber pattern:

**IEventPublisher** - Used by extractors to publish events:

```csharp
public interface IEventPublisher
{
    void Publish<TEvent>(TEvent @event) where TEvent : PdfExtractionEventBase;
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : PdfExtractionEventBase;
}
```

**IEventSubscriber** - Used by consumers (CLI, WPF) to subscribe to events:

```csharp
public interface IEventSubscriber
{
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : PdfExtractionEventBase;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : PdfExtractionEventBase;
}
```

**InMemoryEventBus** - Thread-safe implementation routing events to subscribers:

```csharp
public class InMemoryEventBus : IEventPublisher, IEventSubscriber
{
    // Thread-safe dictionary of event type -> list of handlers
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers;

    // Subscribe to specific event type
    public void Subscribe<TEvent>(Action<TEvent> handler) { ... }

    // Unsubscribe from specific event type
    public void Unsubscribe<TEvent>(Action<TEvent> handler) { ... }

    // Publish event to all subscribers (catches and logs handler exceptions)
    public void Publish<TEvent>(TEvent @event) { ... }
}
```

### Event Catalog

#### Batch/Session Events

Events tracking batch operations (multiple PDF files processed together).

**BatchExtractionStarted**

Raised before processing any PDF files in a batch.

```csharp
public class BatchExtractionStarted : PdfExtractionEventBase
{
    public string[] FilePaths { get; init; }      // All PDF file paths to process
    public int TotalFiles { get; init; }          // Total number of files in batch
}
```

**BatchExtractionCompleted**

Raised after all PDF files in batch processed successfully.

```csharp
public class BatchExtractionCompleted : PdfExtractionEventBase
{
    public string[] OutputFilePaths { get; init; } // Output text file paths
    public int TotalFilesProcessed { get; init; }  // Number of files successfully processed
    public TimeSpan TotalDuration { get; init; }   // Total batch processing time
}
```

**BatchExtractionFailed**

Raised when batch processing fails and cannot continue.

```csharp
public class BatchExtractionFailed : PdfExtractionEventBase
{
    public string ErrorMessage { get; init; }              // Error description
    public string ExceptionType { get; init; }             // Exception type name
    public int FilesProcessedBeforeFailure { get; init; }  // Files completed before failure
}
```

**BatchExtractionCancelled**

Raised when user or system cancels the batch operation.

```csharp
public class BatchExtractionCancelled : PdfExtractionEventBase
{
    public string Reason { get; init; }                        // Cancellation reason
    public int FilesProcessedBeforeCancellation { get; init; } // Files completed before cancel
}
```

#### Document Events

Events tracking individual PDF document processing.

**DocumentExtractionStarted**

Raised before opening/processing a PDF document.

```csharp
public class DocumentExtractionStarted : PdfExtractionEventBase
{
    public string FilePath { get; init; }      // Full path to PDF file
    public string FileName { get; init; }      // PDF filename only
    public long FileSizeBytes { get; init; }   // File size in bytes
}
```

**DocumentExtractionCompleted**

Raised after successfully processing all pages in the document.

```csharp
public class DocumentExtractionCompleted : PdfExtractionEventBase
{
    public string FilePath { get; init; }        // Full path to PDF file
    public int TotalPages { get; init; }         // Total pages processed
    public int TotalChunks { get; init; }        // Total text chunks created
    public string OutputFilePath { get; init; }  // Output text file path
    public TimeSpan Duration { get; init; }      // Document processing time
}
```

**DocumentExtractionFailed**

Raised when document processing fails.

```csharp
public class DocumentExtractionFailed : PdfExtractionEventBase
{
    public string FilePath { get; init; }            // Full path to PDF file
    public string ErrorMessage { get; init; }        // Error description
    public string ExceptionType { get; init; }       // Exception type name
    public int? PageNumberWhereFailed { get; init; } // Page number where failure occurred (if known)
}
```

**DocumentExtractionCancelled**

Raised when document processing is cancelled.

```csharp
public class DocumentExtractionCancelled : PdfExtractionEventBase
{
    public string FilePath { get; init; }                      // Full path to PDF file
    public int PagesProcessedBeforeCancellation { get; init; } // Pages completed before cancel
}
```

#### Page Events

Events tracking individual page processing.

**PageExtractionStarted**

Raised before processing a specific page.

```csharp
public class PageExtractionStarted : PdfExtractionEventBase
{
    public string FilePath { get; init; }  // Full path to PDF file
    public int PageNumber { get; init; }   // Page number (1-based)
    public int TotalPages { get; init; }   // Total pages in document
}
```

**PageExtractionCompleted**

Raised after page is fully processed (text extracted and available).

```csharp
public class PageExtractionCompleted : PdfExtractionEventBase
{
    public string FilePath { get; init; }         // Full path to PDF file
    public int PageNumber { get; init; }          // Page number (1-based)
    public int ExtractedTextLength { get; init; } // Length of extracted text
    public int ChunkCount { get; init; }          // Number of text chunks created
}
```

**PageExtractionFailed**

Raised when page processing fails.

```csharp
public class PageExtractionFailed : PdfExtractionEventBase
{
    public string FilePath { get; init; }      // Full path to PDF file
    public int PageNumber { get; init; }       // Page number (1-based)
    public string ErrorMessage { get; init; }  // Error description
    public string ExceptionType { get; init; } // Exception type name
}
```

**EmptyPageDetected**

Raised when a page has no extractable text content.

```csharp
public class EmptyPageDetected : PdfExtractionEventBase
{
    public string FilePath { get; init; }  // Full path to PDF file
    public int PageNumber { get; init; }   // Page number (1-based)
}
```

#### Rasterization/OCR Events

Events tracking page rasterization (conversion to image) and OCR processing.

**PageRasterizationStarted**

Raised before converting a page to an image.

```csharp
public class PageRasterizationStarted : PdfExtractionEventBase
{
    public string FilePath { get; init; }    // Full path to PDF file
    public int PageNumber { get; init; }     // Page number (1-based)
    public string ImageFormat { get; init; } // Target image format (e.g., "PNG", "JPEG")
}
```

**PageRasterizationCompleted**

Raised after page is rasterized to an image.

```csharp
public class PageRasterizationCompleted : PdfExtractionEventBase
{
    public string FilePath { get; init; }       // Full path to PDF file
    public int PageNumber { get; init; }        // Page number (1-based)
    public string TempImagePath { get; init; }  // Path to temporary image file
    public int ImageWidth { get; init; }        // Image width in pixels
    public int ImageHeight { get; init; }       // Image height in pixels
    public long ImageSizeBytes { get; init; }   // Image file size in bytes
}
```

**PageRasterizationFailed**

Raised when page rasterization fails.

```csharp
public class PageRasterizationFailed : PdfExtractionEventBase
{
    public string FilePath { get; init; }      // Full path to PDF file
    public int PageNumber { get; init; }       // Page number (1-based)
    public string ErrorMessage { get; init; }  // Error description
}
```

**OcrProcessingStarted**

Raised before OCR processing on a page image.

```csharp
public class OcrProcessingStarted : PdfExtractionEventBase
{
    public string FilePath { get; init; }       // Full path to PDF file
    public int PageNumber { get; init; }        // Page number (1-based)
    public string TempImagePath { get; init; }  // Path to temporary image file
    public string OcrEngine { get; init; }      // OCR engine name (e.g., "Tesseract")
}
```

**OcrProcessingCompleted**

Raised after OCR text extraction is completed.

```csharp
public class OcrProcessingCompleted : PdfExtractionEventBase
{
    public string FilePath { get; init; }         // Full path to PDF file
    public int PageNumber { get; init; }          // Page number (1-based)
    public int ExtractedTextLength { get; init; } // Length of extracted text
    public double? ConfidenceScore { get; init; } // OCR confidence (0.0 to 1.0, if available)
    public string ExtractedText { get; init; }    // Full extracted text (for GUI preview)
}
```

**OcrProcessingFailed**

Raised when OCR processing fails.

```csharp
public class OcrProcessingFailed : PdfExtractionEventBase
{
    public string FilePath { get; init; }      // Full path to PDF file
    public int PageNumber { get; init; }       // Page number (1-based)
    public string ErrorMessage { get; init; }  // Error description
}
```

#### Text Processing Events

Events tracking text chunking and processing.

**TextChunked**

Raised after text is split into chunks (for embedding generation).

```csharp
public class TextChunked : PdfExtractionEventBase
{
    public string FilePath { get; init; }   // Full path to PDF file
    public int PageNumber { get; init; }    // Page number (1-based)
    public int ChunkCount { get; init; }    // Number of chunks created
    public int[] ChunkSizes { get; init; }  // Size of each chunk (in characters)
}
```

**ChunkCreated**

Raised when an individual text chunk is created.

```csharp
public class ChunkCreated : PdfExtractionEventBase
{
    public string FilePath { get; init; }        // Full path to PDF file
    public int PageNumber { get; init; }         // Page number (1-based)
    public int ChunkIndex { get; init; }         // Chunk index (0-based)
    public int ContentLength { get; init; }      // Chunk content length
    public string ContentPreview { get; init; }  // First 100 characters of chunk
}
```

#### Infrastructure Events

Events tracking infrastructure operations (temp files, progress updates).

**TempImageSaved**

Raised when a rasterized page image is saved to the temp folder.

```csharp
public class TempImageSaved : PdfExtractionEventBase
{
    public string FilePath { get; init; }       // Full path to PDF file
    public int PageNumber { get; init; }        // Page number (1-based)
    public string TempFilePath { get; init; }   // Path to temporary image file
    public long FileSizeBytes { get; init; }    // Image file size in bytes
}
```

**TempFilesCleanedUp**

Raised when temporary files are deleted after processing.

```csharp
public class TempFilesCleanedUp : PdfExtractionEventBase
{
    public string[] DeletedFilePaths { get; init; } // Paths of deleted temporary files
    public long TotalBytesFreed { get; init; }      // Total disk space freed (bytes)
}
```

**ExtractionProgressUpdated**

Raised periodically during long operations to report progress (for progress bars).

```csharp
public class ExtractionProgressUpdated : PdfExtractionEventBase
{
    public string FilePath { get; init; }          // Full path to PDF file
    public double OverallPercentage { get; init; } // Overall progress (0.0 to 100.0)
    public int PagesProcessed { get; init; }       // Pages processed so far
    public int TotalPages { get; init; }           // Total pages in document
    public string CurrentOperation { get; init; }  // Description of current operation
}
```

### Event Sequence Flow

Typical event flow for extracting a single PDF document:

```text
BatchExtractionStarted
  │
  └─> DocumentExtractionStarted
      │
      ├─> PageExtractionStarted (Page 1)
      │   ├─> PageRasterizationStarted
      │   ├─> PageRasterizationCompleted
      │   ├─> TempImageSaved
      │   ├─> OcrProcessingStarted
      │   ├─> OcrProcessingCompleted
      │   ├─> TextChunked
      │   ├─> ChunkCreated (chunk 0)
      │   ├─> ChunkCreated (chunk 1)
      │   ├─> ChunkCreated (chunk n)
      │   ├─> PageExtractionCompleted
      │   └─> ExtractionProgressUpdated
      │
      ├─> PageExtractionStarted (Page 2)
      │   └─> ... (same as Page 1)
      │
      ├─> PageExtractionStarted (Page N)
      │   └─> ... (same as Page 1)
      │
      ├─> TempFilesCleanedUp
      └─> DocumentExtractionCompleted
  │
  └─> BatchExtractionCompleted
```

**Error/Cancellation Flow:**

If an error or cancellation occurs at any stage, the corresponding failure/cancellation event is raised:

```text
PageExtractionStarted (Page 5)
  ├─> PageRasterizationStarted
  ├─> PageRasterizationFailed  ← Error occurs
  └─> PageExtractionFailed
  └─> DocumentExtractionFailed
  └─> BatchExtractionFailed
```

### Usage Examples

#### Publishing Events (Extractor Implementation)

Extractors publish events at each stage of processing:

```csharp
public class PdfPigExtractor : IPdfExtractor
{
    public async Task<IEnumerable<DocumentChunk>> ExtractAsync(
        string filePath,
        IEventPublisher eventPublisher,
        Guid correlationId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;

        // Publish document started event
        eventPublisher.Publish(new DocumentExtractionStarted
        {
            CorrelationId = correlationId,
            SessionId = sessionId,
            ExtractorName = "PdfPig",
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            FileSizeBytes = new FileInfo(filePath).Length
        });

        try
        {
            using var document = PdfDocument.Open(filePath);
            var totalPages = document.NumberOfPages;
            int pagesProcessed = 0;

            foreach (var page in document.GetPages())
            {
                // Publish page started event
                eventPublisher.Publish(new PageExtractionStarted
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "PdfPig",
                    FilePath = filePath,
                    PageNumber = page.Number,
                    TotalPages = totalPages
                });

                // ... extract text ...

                // Publish progress event
                pagesProcessed++;
                eventPublisher.Publish(new ExtractionProgressUpdated
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "PdfPig",
                    FilePath = filePath,
                    OverallPercentage = (double)pagesProcessed / totalPages * 100,
                    PagesProcessed = pagesProcessed,
                    TotalPages = totalPages,
                    CurrentOperation = $"Processing page {page.Number}"
                });

                // Publish page completed event
                eventPublisher.Publish(new PageExtractionCompleted
                {
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    ExtractorName = "PdfPig",
                    FilePath = filePath,
                    PageNumber = page.Number,
                    ExtractedTextLength = pageText.Length,
                    ChunkCount = chunks.Count
                });
            }

            // Publish document completed event
            eventPublisher.Publish(new DocumentExtractionCompleted
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "PdfPig",
                FilePath = filePath,
                TotalPages = totalPages,
                TotalChunks = allChunks.Count,
                Duration = DateTimeOffset.UtcNow - startTime
            });

            return allChunks;
        }
        catch (Exception ex)
        {
            // Publish failure event
            eventPublisher.Publish(new DocumentExtractionFailed
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                ExtractorName = "PdfPig",
                FilePath = filePath,
                ErrorMessage = ex.Message,
                ExceptionType = ex.GetType().Name
            });
            throw;
        }
    }
}
```

#### Subscribing to Events (Consumer Implementation)

**CLI Logging Example:**

```csharp
public class CliProgram
{
    public static void Main(string[] args)
    {
        var eventBus = new InMemoryEventBus();

        // Subscribe to events for logging
        eventBus.Subscribe<DocumentExtractionStarted>(e =>
            Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] Starting extraction: {e.FileName}"));

        eventBus.Subscribe<PageExtractionCompleted>(e =>
            Console.WriteLine($"  Page {e.PageNumber} completed ({e.ChunkCount} chunks)"));

        eventBus.Subscribe<ExtractionProgressUpdated>(e =>
            Console.WriteLine($"  Progress: {e.OverallPercentage:F1}% ({e.PagesProcessed}/{e.TotalPages})"));

        eventBus.Subscribe<DocumentExtractionCompleted>(e =>
            Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] Completed: {Path.GetFileName(e.FilePath)} " +
                            $"({e.TotalPages} pages, {e.TotalChunks} chunks, {e.Duration.TotalSeconds:F1}s)"));

        eventBus.Subscribe<DocumentExtractionFailed>(e =>
            Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] ERROR: {e.ErrorMessage}"));

        // Run extraction with event bus
        var extractor = new PdfPigExtractor();
        var sessionId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var result = await extractor.ExtractAsync("document.pdf", eventBus, correlationId, sessionId);
    }
}
```

**WPF GUI Example:**

```csharp
public class MainViewModel : INotifyPropertyChanged
{
    private readonly IEventBus _eventBus;
    private double _progressPercentage;
    private string _currentOperation;
    private string _extractedText;

    public MainViewModel(IEventBus eventBus)
    {
        _eventBus = eventBus;

        // Subscribe to progress updates
        _eventBus.Subscribe<ExtractionProgressUpdated>(e =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ProgressPercentage = e.OverallPercentage;
                CurrentOperation = e.CurrentOperation;
            });
        });

        // Subscribe to OCR completion for live text preview
        _eventBus.Subscribe<OcrProcessingCompleted>(e =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ExtractedText += $"\n--- Page {e.PageNumber} ---\n{e.ExtractedText}\n";
            });
        });

        // Subscribe to errors for notifications
        _eventBus.Subscribe<PageExtractionFailed>(e =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Error on page {e.PageNumber}: {e.ErrorMessage}",
                              "Extraction Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        });

        // Subscribe to completion for final actions
        _eventBus.Subscribe<DocumentExtractionCompleted>(e =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Extraction completed!\n\nPages: {e.TotalPages}\n" +
                              $"Chunks: {e.TotalChunks}\nTime: {e.Duration.TotalSeconds:F1}s",
                              "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                IsExtractionComplete = true;
            });
        });
    }

    public double ProgressPercentage
    {
        get => _progressPercentage;
        set { _progressPercentage = value; OnPropertyChanged(); }
    }

    public string CurrentOperation
    {
        get => _currentOperation;
        set { _currentOperation = value; OnPropertyChanged(); }
    }

    public string ExtractedText
    {
        get => _extractedText;
        set { _extractedText = value; OnPropertyChanged(); }
    }
}
```

### Integration with PdfTextExtractor Projects

#### Core Library (PdfTextExtractor.Core)

The Core library contains all event infrastructure:

**Event Classes:**

- `Events/PdfExtractionEventBase.cs` - Base class with metadata
- `Events/Batch/*.cs` - 4 batch event classes
- `Events/Document/*.cs` - 4 document event classes
- `Events/Page/*.cs` - 4 page event classes
- `Events/Ocr/*.cs` - 6 OCR/rasterization event classes
- `Events/TextProcessing/*.cs` - 2 text processing event classes
- `Events/Infrastructure/*.cs` - 3 infrastructure event classes

**Event Infrastructure:**

- `Events/IEventPublisher.cs` - Publisher interface
- `Events/IEventSubscriber.cs` - Subscriber interface
- `Events/InMemoryEventBus.cs` - Thread-safe event bus implementation

**Modified Interfaces:**

- `IPdfExtractor.ExtractAsync()` signature includes `IEventPublisher`, `correlationId`, `sessionId` parameters

#### CLI Application (PdfTextExtractor.Cli)

The CLI subscribes to events for console logging and progress reporting:

```csharp
// Subscribe to key events
eventBus.Subscribe<DocumentExtractionStarted>(e => LogDocumentStarted(e));
eventBus.Subscribe<ExtractionProgressUpdated>(e => UpdateProgressBar(e));
eventBus.Subscribe<DocumentExtractionCompleted>(e => LogDocumentCompleted(e));
eventBus.Subscribe<DocumentExtractionFailed>(e => LogError(e));
```

**Use Cases:**

- Log extraction progress to console
- Display progress bar (ASCII art or percentage)
- Report errors with context
- Track batch statistics (total time, success/failure counts)

#### WPF Application (PdfTextExtractor.Wpf)

The WPF GUI subscribes to events for real-time UI updates:

```csharp
// Progress tracking
eventBus.Subscribe<ExtractionProgressUpdated>(e => UpdateProgressBar(e));

// Live text preview
eventBus.Subscribe<OcrProcessingCompleted>(e => AppendTextPreview(e));

// Page image display
eventBus.Subscribe<TempImageSaved>(e => DisplayPageImage(e.TempFilePath));

// Error handling
eventBus.Subscribe<PageExtractionFailed>(e => ShowErrorDialog(e));
eventBus.Subscribe<DocumentExtractionFailed>(e => ShowErrorDialog(e));

// Completion handling
eventBus.Subscribe<DocumentExtractionCompleted>(e => EnableSaveButton(e));
```

### WPF GUI Features Enabled by Events

Domain events enable rich, real-time GUI functionality:

1. **Real-time Progress Tracking**
   - Subscribe to `ExtractionProgressUpdated` to update progress bar
   - Show current operation description ("Processing page 5 of 10")
   - Display percentage complete with smooth updates

2. **Live Text Preview**
   - Subscribe to `OcrProcessingCompleted` to display extracted text as pages are processed
   - Show text in scrollable text box, appending each page
   - Enable "Copy to Clipboard" as text becomes available

3. **Page Image Display**
   - Subscribe to `TempImageSaved` to display rasterized page images
   - Show thumbnail gallery of processed pages
   - Allow users to click thumbnails to view full-size images

4. **Error Notifications**
   - Subscribe to `*Failed` events to show error dialogs
   - Display specific error details (page number, error message)
   - Offer retry/skip/cancel options

5. **Cancellation Support**
   - Subscribe to `ExtractionProgressUpdated` to enable "Cancel" button
   - Show cancellation confirmation dialog
   - Subscribe to `*Cancelled` events to update UI state

6. **Performance Monitoring**
   - Subscribe to `DocumentExtractionCompleted` to display statistics
   - Show total time, pages/second, chunks created
   - Track historical extraction performance

7. **Batch Processing UI**
   - Subscribe to `BatchExtractionStarted` to show batch progress
   - Display multi-file progress (e.g., "Processing file 3 of 10")
   - Show overall batch completion percentage

### Future Enhancements

**Event Persistence/Replay:**

- Store events to SQLite or JSON for debugging and auditing
- Replay events to reconstruct extraction timeline
- Enable "what went wrong" analysis for failed extractions

**Event Filtering:**

- Filter events by `CorrelationId` for single-file tracking
- Filter events by `SessionId` for batch-level analysis
- Filter by event type for focused monitoring

**Async Event Handlers:**

- Support async event handlers for I/O-bound operations
- Enable background processing (save events to database, send to monitoring service)

**External Event Buses:**

- Integrate with SignalR for real-time web dashboard
- Publish to RabbitMQ/Azure Service Bus for distributed processing
- Enable remote monitoring of extraction operations

**Event Aggregation:**

- Aggregate events for batch-level statistics
- Calculate average processing time per page
- Identify slowest pages/documents for optimization
