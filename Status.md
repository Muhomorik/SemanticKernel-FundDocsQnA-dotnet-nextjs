# PDF Q&A Application - Implementation Status

Last Updated: 2026-02-09 (YieldRaccoon Fluent v2 theme system: created YieldRaccoonTheme.xaml with all yr.* design tokens, RuntimeThemeGenerator for system accent color, migrated all 6 views to use yr.* tokens)

**Tech Stack:**

- Preprocessor: .NET 9 Console App + Semantic Kernel (reads pre-extracted text from PdfTextExtractor)
- Backend: ASP.NET Core 9 + Semantic Kernel + OpenAI (default) / Groq API (optional)
- Frontend: Next.js 16 + TypeScript + Tailwind CSS + shadcn/ui

**Project Hosting:**

- **Repository:** GitHub (personal, public)
- **Deployment:** Azure (private infrastructure)
- **Services:** Azure App Service (backend), Azure Static Web Apps (frontend), Application Insights, Key Vault

---

## 🤖 Instructions for AI Agents

**CRITICAL:** This document is the source of truth for project status. AI assistants MUST:

1. **Before starting work:** Review this file to understand what's implemented, in-progress, or planned
2. **During implementation:** Update relevant status from ❌ to ⏳ (in progress)
3. **After completion:** Update status to ✅ and document new features in the appropriate section
4. **Always maintain accuracy:** Keep this document synchronized with actual codebase state
5. **Update timestamp:** Change "Last Updated" date when making significant updates
6. **Keep costs low:** When suggesting infrastructure, tiers, or services, prioritize free/low-cost options (Azure free tier, free APIs). Only suggest paid upgrades if strictly necessary and mention the cost impact.

**Do not duplicate work.** Always verify current status before implementing features.

---

## Icon Legend

| Icon | Meaning |
| ------ | --------- |
| ✅ | Complete |
| ⏳ | In Progress |
| ❌ | Not Planned |

---

## Part 1: Preprocessor ✅ COMPLETED

### Implementation Status

| Component | Status | Notes |
| ----------- | -------- | ------- |
| Console Application | ✅ | .NET 9 with CommandLineParser |
| Text File Reading | ✅ | **UPDATED 2026-01-28**: TextFileExtractor reads pre-extracted text files from PdfTextExtractor (pattern: {basename}_page_{N}.txt), validates sequential pages, comprehensive tests (14 tests) |
| Text Chunking | ✅ | **UPDATED 2026-01-28**: SemanticChunker only - paragraph-based splitting with 15% overlap (800 chars default), follows 2025 RAG best practices, preserves semantic boundaries, DI-based, AutoFixture + AutoMoq tests (23 tests) |
| Embedding Generation | ✅ | Supports Ollama, LM Studio, OpenAI |
| JSON Export | ✅ | Structured format (id, text, embedding, source, page) |
| Append Mode | ✅ | Incremental processing of new PDFs |
| CLI Options | ✅ | All parameters implemented and validated |
| Provider Abstraction | ✅ | Ollama/LM Studio/OpenAI with secure API key management |
| Cosmos DB Upload | ✅ | HTTP-based upload to backend API with rate limiting |
| Rate Limiting & Backoff | ✅ | **NEW 2026-01-11**: 8000ms default delay between batches (~290 RU/s avg, safe under 400 RU/s limit), exponential backoff for 429 throttling |
| Unit Tests | ✅ | NUnit + AutoFixture + AutoMoq tests for services and extraction (56 tests passing) |
| AI Evaluation Tests | ✅ | **NEW 2026-02-02**: `ExampleQueriesAIEvaluatedTests` - evaluates frontend queries for answerability against PRIIP/KID documents, generates markdown report with categories (single_doc_answerable, multi_doc_answerable, context_dependent, info_missing) and suggested rephrases. Requires OpenAI API key, marked `[Explicit]`. |
| Documentation | ✅ | README with usage examples and architecture documentation |

### Planned Features

| Feature | Status | Notes |
| --------- | -------- | ------- |
| Token Usage Tracking | ✅ | Logs actual token counts from provider response metadata (OpenAI, Ollama, LM Studio) |
| Cost Monitoring | ✅ | Documented cost calculation examples for OpenAI (~$0.02 per 1M tokens) |
| Application Insights Metrics | ❌ | Can be added when needed; currently logs to console |

### Not Planned

| Feature | Reason |
| --------- | -------- |
| Azure OpenAI Provider | Out of scope for hobby project |
| Other Cloud Providers | OpenAI sufficient for current needs |

---

## Part 2: Backend API ✅ COMPLETED

### Implementation Status

| Component | Status | Notes |
| ----------- | -------- | ------- |
| Solution Structure | ✅ | Backend.sln with API and Tests projects |
| DDD Architecture | ✅ | Domain, ApplicationCore, Infrastructure layers |
| RAG Pipeline | ✅ | DocumentRepository → VectorStore → LLM Provider |
| LLM Providers | ✅ | OpenAI (gpt-4o-mini) default, Groq optional |
| Vector Storage | ✅ | InMemory (default) + Cosmos DB (optional persistent storage) |
| Semantic Search | ✅ | OpenAI embeddings (text-embedding-3-small) + InMemoryVectorStore / CosmosDbSemanticSearch |
| API Endpoints | ✅ | POST /api/ask, POST /api/embeddings (+ PUT, DELETE), health checks, Swagger |
| Authentication | ✅ | API key authentication for embedding endpoints (Cosmos DB only) |
| Security | ✅ | Input validation, sanitization, rate limiting (10/min/IP), constant-time API key comparison |
| Azure Deployment | ✅ | App Service F1, Key Vault, Application Insights, Cosmos DB (optional) |
| CI/CD | ✅ | GitHub Actions (.github/workflows/deploy-backend.yml) |
| Unit Tests | ✅ | 69 tests passing (Domain, ApplicationCore, Infrastructure) |
| Documentation | ✅ | README with DDD architecture + Cosmos DB setup guide |

### Security Implementation ✅ (2026-01-01)

| Component | Status | Notes |
| ----------- | -------- | ------- |
| Input Validation | ✅ | [MaxLength(500)], [Required], [MinLength(3)] |
| Custom Validation | ✅ | [SafeQuestion] detects injection patterns |
| Input Sanitization | ✅ | Removes control chars, normalizes whitespace |
| System Prompt | ✅ | Hardened with anti-jailbreak instructions |
| Rate Limiting | ✅ | 10 req/min/IP, 2 request queue |
| Request Size Limits | ✅ | 10KB max body size |

### Planned Features

| Feature | Status | Notes |
| --------- | -------- | ------- |
| Token Usage Tracking | ⏳ | Extract counts via ChatMessageContent.InnerContent |
| Cost Monitoring | ⏳ | Estimate API costs per request |
| Application Insights Metrics | ⏳ | Custom metrics for production cost analysis |

### Not Planned

| Feature | Reason |
| --------- | -------- |
| Controller Integration Tests | Low priority, manual testing sufficient |
| Caching Layer | Premature optimization for current scale |
| Authentication/Authorization | Out of scope for demo app |
| Indirect Prompt Injection Detection | Complex, low ROI for hobby project |

---

## Part 3: Frontend ✅ COMPLETED

**IMPORTANT for AI Agents:** Use the `frontend-design` plugin for UI work.

### Implementation Status

| Component | Status | Notes |
| ----------- | -------- | ------- |
| Next.js 16 Setup | ✅ | App Router, TypeScript, Tailwind CSS, ESLint |
| shadcn/ui Integration | ✅ | New York style, Neutral theme, 8 components |
| Chat Interface | ✅ | ChatMessage, ChatInput, ExampleQueries components |
| Theme Support | ✅ | Light/dark toggle via next-themes |
| API Client | ✅ | Type-safe client in lib/api.ts with error handling |
| Loading States | ✅ | Skeleton loading for messages |
| Error Handling | ✅ | Alert with retry functionality |
| Responsive Design | ✅ | Mobile-first approach |
| Testing | ✅ | Jest + React Testing Library, 4 tests passing |
| Code Quality | ✅ | EditorConfig, Prettier with Tailwind plugin |
| Documentation | ✅ | README with setup instructions |

### Planned Features

| Feature | Status | Notes |
| --------- | -------- | ------- |
| 503 Error Handling | ⏳ | User-friendly message for Azure free tier quota limits |

### Not Planned

| Feature | Reason |
| --------- | -------- |
| File Upload | Out of scope for Q&A demo |
| Authentication | Not needed for public demo |
| Chat Persistence | Stateless design by choice |
| User Accounts | Out of scope |

---

## Part 4: Cosmos DB Vector Database Integration ✅ COMPLETED

Azure Cosmos DB optional vector database backend for persistent embeddings storage. Default remains `embeddings.json` (InMemory). Switch via strictly typed enum with configuration priority: User Secrets > Environment variable > Default. Full backward compatibility maintained. Production-ready with Managed Identity authentication.

### Design Decisions

| Decision | Details |
| --------- | ------- |
| **Storage Type** | Strictly typed enum: `VectorStorageType { InMemory = 0, CosmosDb = 1 }` |
| **Enum Conversion** | Early binding in Program.cs (environment variable → enum during startup) |
| **Configuration Priority** | CLI argument > User Secrets > Environment variable > Default (InMemory) |
| **Default Behavior** | Unchanged: InMemory with embeddings.json (backward compatible) |
| **Preprocessor Verbs** | `json` (→ embeddings.json file) and `cosmosdb` (→ Cosmos DB database) |

### Implementation Status

| Phase | Component | Status | Notes |
| ------ | ----------- | -------- | ------- |
| **Phase 1** | Backend Infrastructure | ✅ | **Completed 2026-01-10**: NuGet packages (Microsoft.Azure.Cosmos 3.43.1), VectorStorageType enum, CosmosDbDocumentDto, CosmosDbDocumentRepository (full CRUD), CosmosDbSemanticSearch (native vector search with VectorDistance), IDocumentRepository extensions, conditional DI registration in Program.cs |
| **Phase 2** | Backend Integration | ✅ | **Completed 2026-01-10**: EmbeddingDtos (request/response models), EmbeddingsController (4 protected endpoints: POST, PUT, DELETE, POST replace-all), ApiKeyAuthenticationMiddleware (constant-time comparison), middleware registration, CosmosDbHealthCheck (connectivity + count query), health check registration |
| **Phase 3** | Preprocessor Updates | ✅ | **Completed 2026-01-07**: New CLI verbs (`json` and `cosmosdb`), IEmbeddingOutput interface, JsonEmbeddingOutput and CosmosDbEmbeddingOutput implementations, HTTP client with API key authentication |
| **Phase 4** | Azure Infrastructure | ✅ | **Completed 2026-01-10**: Manual setup documentation in AZURE-DEPLOYMENT.md (8-step guide), Managed Identity configuration, RBAC role assignment, Key Vault secrets, cost analysis (free tier), troubleshooting guide, rollback instructions |
| **Phase 5** | Testing & Documentation | ✅ | **Completed 2026-01-10**: SECRETS-MANAGEMENT.md (Cosmos DB configuration section with two-layer authentication, connection strings, Managed Identity setup, API key generation), backend/README.md (vector storage switching guide, API endpoints, authentication), Status.md updated |
| **Phase 6** | Production Deployment | ✅ | **Completed 2026-01-11**: Cosmos DB account created (free tier), database/container configured (partition key /sourceFile), Key Vault secrets added (BackendOptions--CosmosDb*), App Service Managed Identity granted Cosmos DB RBAC access (Built-in Data Contributor), embeddings uploaded from Preprocessor, production API verified working end-to-end |

### Completed Features

**Backend API Components:**

- ✅ `VectorStorageType` enum with InMemory (default) and CosmosDb options
- ✅ `CosmosDbDocumentDto` - Document schema with vector embedding (1536 dimensions)
- ✅ `CosmosDbDocumentRepository` - Full CRUD implementation (InitializeAsync, GetAllChunksAsync, AddChunksAsync, UpdateChunksAsync, DeleteChunksBySourceAsync, ReplaceAllChunksAsync)
- ✅ `CosmosDbSemanticSearch` - Native vector search using `VectorDistance()` SQL function with cosine similarity
- ✅ `EmbeddingsController` - 4 protected REST endpoints (POST /api/embeddings, PUT /api/embeddings/{sourceFile}, DELETE /api/embeddings/{sourceFile}, POST /api/embeddings/replace-all)
- ✅ `ApiKeyAuthenticationMiddleware` - Constant-time API key comparison, only protects /api/embeddings endpoints
- ✅ `CosmosDbHealthCheck` - Connectivity check, container verification, document count query
- ✅ Conditional DI registration based on `VectorStorageType` (Program.cs)
- ✅ CosmosClient with Managed Identity (production) and Connection String (development) support

**Authentication:**

- ✅ Two-layer authentication architecture (Preprocessor→Backend via API Key, Backend→Cosmos DB via Managed Identity/Connection String)
- ✅ API key header format: `Authorization: ApiKey <key>`
- ✅ Secure key storage (User Secrets for dev, Key Vault for prod)
- ✅ 32+ character cryptographically secure key generation

**Documentation:**

- ✅ SECRETS-MANAGEMENT.md - Complete Cosmos DB configuration section (development setup with connection strings, production setup with Managed Identity, API key generation, switching storage types, validation)
- ✅ AZURE-DEPLOYMENT.md - 8-step manual setup guide (create account with free tier, database/container with vector indexing, Managed Identity configuration, RBAC role assignment, Key Vault configuration, embeddings upload, verification, troubleshooting)
- ✅ backend/README.md - Vector storage switching guide (InMemory vs Cosmos DB comparison, setup instructions, API endpoints, authentication, health checks)

### Architecture

**Default (InMemory):** `Preprocessor → embeddings.json → Backend (in-memory) → Frontend`

**Optional (Cosmos DB):** `Preprocessor ←(API)→ Backend ←→ Cosmos DB (Vector Store) → Frontend`

**Authentication Flow:**

```text
Preprocessor --[Authorization: ApiKey]-> Backend API --[Managed Identity/Connection String]-> Cosmos DB
```

### Preprocessor CLI Verbs

| Verb | Purpose | Output | Command |
| --- | --- | --- | --- |
| **`json`** | Generate embeddings → save to local JSON file | `embeddings.json` | `dotnet run -- json -i ./pdfs -o ./embeddings.json` |
| **`cosmosdb`** | Generate embeddings → upload to Cosmos DB | Cosmos DB database | `dotnet run -- cosmosdb -i ./pdfs --url https://backend.app --key apikey123` |

### JSON Verb Options

- `-i, --input` - Input PDF folder (default: `pdfs`)
- `-o, --output` - Output JSON file path (default: `./embeddings.json`)
- `-a, --append` - Append to existing embeddings.json (default: false)
- `-p, --provider` - Embedding provider: openai/ollama/lmstudio (default: openai)
- `-e, --embedding-model` - Embedding model name (default: text-embedding-3-small)
- `-m, --method` - PDF extraction method (default: pdfpig)

### CosmosDB Verb Options

- `-i, --input` - Input PDF folder (default: `pdfs`)
- `-u, --url` - Backend API URL (default: `http://localhost:5000`)
- `-k, --key` - API key or env: `FUNDDOCS_API_KEY` (required)
- `-o, --operation` - Operation: `add` (default), `update`, `replace-all`
- `-p, --provider` - Embedding provider: openai/ollama/lmstudio (default: openai)
- `-e, --embedding-model` - Embedding model name (default: text-embedding-3-small)
- `-b, --batch-size` - Embeddings per API request (default: 100)

### Backend Configuration

**VectorStorageType Enum:**

```csharp
public enum VectorStorageType
{
    InMemory = 0,    // Default: embeddings.json
    CosmosDb = 1     // Persistent vector database
}
```

**Configuration Priority (highest to lowest):**

1. CLI argument (Preprocessor only)
2. User Secrets (development)
3. Environment variable (`BackendOptions__VectorStorageType`)
4. Default: InMemory

**Backend Environment Variables:**

- `BackendOptions__VectorStorageType` - InMemory (default) | CosmosDb
- `BackendOptions__CosmosDbEndpoint` - Cosmos DB endpoint URL
- `BackendOptions__CosmosDbDatabaseName` - Database name
- `BackendOptions__CosmosDbContainerName` - Container name (default: embeddings)
- `BackendOptions__EmbeddingApiKey` - API key for preprocessor authentication

### Backend API Endpoints (Protected by API Key)

| Endpoint | Method | Purpose | Auth |
| --- | --- | --- | --- |
| `/api/embeddings` | POST | Add new embeddings | ApiKey |
| `/api/embeddings/{sourceFile}` | PUT | Update embeddings for a file | ApiKey |
| `/api/embeddings/{sourceFile}` | DELETE | Delete embeddings for a file | ApiKey |
| `/api/embeddings/replace-all` | POST | Replace all embeddings | ApiKey |

**Authentication Header:** `Authorization: ApiKey <your-api-key>`

### Cosmos DB Schema

| Setting | Value |
| --- | --- |
| Database | `<your-database-name>` |
| Container | `embeddings` |
| Partition Key | `/sourceFile` |
| Vector Dimensions | 1536 (OpenAI text-embedding-3-small) |
| Vector Index Type | `quantizedFlat` (cost optimized) |

### Cost Analysis

| Tier | Throughput | Storage | Monthly Cost | Best For |
| --- | --- | --- | --- | --- |
| **Free Tier** | 1000 RU/s | 25 GB | **$0** | Hobby projects, development |
| Serverless | Pay per RU | 1 TB max | ~$0.25 per 1M RU | Sporadic workloads |
| Provisioned (400 RU/s) | 400 RU/s | Variable | ~$23/month | Consistent low traffic |

**Estimated for this project:** $0/month (within free tier limits)

---

## Part 5: PdfTextExtractor.Core Library ✅ COMPLETED

Reusable .NET library for PDF text extraction using Domain-Driven Design architecture. Supports multiple extraction methods (PdfPig, LM Studio OCR, Ollama OCR) with reactive event streams via Rx.NET.

### Implementation Status

| Component | Status | Notes |
| ----------- | -------- | ------- |
| Project Setup | ✅ | .NET 9.0 class library with 7 NuGet packages |
| DDD Architecture | ✅ | Domain/ApplicationCore/Infrastructure layers, pure domain logic |
| Autofac DI | ✅ | PdfTextExtractorModule with automatic service registration |
| Rx.NET Events | ✅ | ReactiveEventPublisher exposing IObservable<PdfExtractionEventBase> |
| Domain Events | ✅ | 23 events across 5 categories (Batch, Document, Page, OCR, TextProcessing, Infrastructure) |
| Value Objects | ✅ | FilePath, PageNumber, ChunkContent, ExtractorType, SessionId, CorrelationId (immutable, self-validating) |
| Domain Entities | ✅ | Document, Page, TextChunk with identity and lifecycle |
| Aggregate Root | ✅ | ExtractionSession controlling documents and enforcing invariants |
| PdfPig Extractor | ✅ | Complete implementation with text extraction, chunking, and event publishing |
| LM Studio Extractor | ✅ | Stub implementation (planned for future) |
| Ollama Extractor | ✅ | Stub implementation (planned for future) |
| File System Services | ✅ | FileSystemService, TextFileWriter with async support |
| Public API | ✅ | IPdfTextExtractorLib with three async extraction methods |
| Configuration DTOs | ✅ | PdfPigParameters, LMStudioParameters, OllamaParameters |
| Build Verification | ✅ | 0 build errors, 7 nullable warnings (expected for EF Core) |
| Unit Testing | ✅ | 19 test classes, 70 test methods, 90% pass rate (63/70 passing), NUnit + AutoFixture + Moq |
| Documentation | ✅ | Comprehensive README with icons, 5 Mermaid diagrams, tech stack, API reference |

### Domain Events (23 Total)

| Category | Events | Status |
| --------- | -------- | -------- |
| **Batch Events** | BatchExtractionStarted, Completed, Failed, Cancelled | ✅ Complete |
| **Document Events** | DocumentExtractionStarted, Completed, Failed, Cancelled | ✅ Complete |
| **Page Events** | PageExtractionStarted, Completed, Failed, EmptyPageDetected | ✅ Complete |
| **OCR Events** | PageRasterizationStarted, Completed, Failed, OcrProcessingStarted, Completed, Failed | ✅ Complete |
| **Text Processing** | TextChunked, ChunkCreated | ✅ Complete |
| **Infrastructure** | TempImageSaved, TempFilesCleanedUp, ExtractionProgressUpdated | ✅ Complete |

### Architecture

**DDD Layers:**
```
Entry Point (PdfTextExtractorLib)
    ↓
ApplicationCore (Use cases, DTOs, orchestration)
    ↓
Domain (Entities, Value Objects, Events - PURE)
    ↓
Infrastructure (Extractors, File System, Event Bus)
```

**Key Patterns:**
- Aggregate Root pattern (ExtractionSession)
- Factory methods for entity creation
- Immutable value objects with validation
- Event-driven architecture with reactive streams
- Repository Pattern (IExtractionSessionRepository)
- Separation of concerns (Domain has ZERO external dependencies)

### NuGet Dependencies

| Package | Version | Purpose |
| --------- | --------- | --------- |
| Autofac | 8.0.0 | Dependency injection container |
| System.Reactive | 6.0.0 | Rx.NET for IObservable event streams |
| PdfPig | 0.1.12 | PDF text extraction |
| SixLabors.ImageSharp | 3.1.12 | Image processing for OCR (future) |
| Microsoft.Extensions.Http | 9.0.0 | HTTP client for LM Studio/Ollama APIs (future) |
| System.Text.Json | 9.0.0 | JSON serialization |
| Microsoft.Extensions.Logging.Abstractions | 9.0.0 | Logging infrastructure |

### Public API Methods

| Method | Parameters | Return Type | Status |
| -------- | ------------ | ------------- | -------- |
| `ExtractWithPdfPigAsync` | PdfPigParameters | Task<ExtractionResult> | ✅ Implemented |
| `ExtractWithLMStudioAsync` | LMStudioParameters | Task<ExtractionResult> | 🚧 Stub |
| `ExtractWithOllamaAsync` | OllamaParameters | Task<ExtractionResult> | 🚧 Stub |
| `GetPdfFiles` | string folderPath | string[] | ✅ Implemented |
| `GetTextFiles` | string folderPath | string[] | ✅ Implemented |
| `Events` | - | IObservable<PdfExtractionEventBase> | ✅ Implemented |

### Documentation

| Component | Status | Notes |
| ----------- | -------- | ------- |
| README.md | ✅ Complete | AI-agent optimized with icons, 5 Mermaid.js diagrams, tech stack section |
| Table of Contents | ✅ Complete | Anchor links for all sections |
| Quick Start Guide | ✅ Complete | 3-step guide with code examples |
| API Reference | ✅ Complete | All methods with signatures, parameters, return types |
| Event Catalog | ✅ Complete | All 23 events documented with code examples |
| Project Structure | ✅ Complete | ASCII tree view of DDD layers |
| Usage Examples | ✅ Complete | CLI and WPF integration examples |
| Tech Stack Badges | ✅ Complete | .NET 9.0, C# 12.0 badges with tables |
| Mermaid Diagrams | ✅ Complete | DDD layers, project structure, domain layer, sequence diagram, event flow |

### Mermaid Diagrams (5 Total)

1. **DDD Layered Architecture** - Graph showing Entry Point → ApplicationCore → Domain → Infrastructure
2. **Project Structure** - Relationship graph of all major components
3. **Domain Layer Structure** - Detailed view of Aggregates, Entities, Value Objects, Events
4. **Event Sequence Diagram** - User → API → Extractor → EventPublisher flow
5. **Event Flow Diagram** - Decision tree for event types and categories

### Future Enhancements (Planned)

| Feature | Phase | Status | Notes |
| --------- | ------- | -------- | ------- |
| LM Studio OCR | Phase 6 | 📅 Planned | Rasterize PDF pages, call LM Studio vision API, extract text from response |
| Ollama OCR | Phase 7 | 📅 Planned | Rasterize PDF pages, call Ollama vision API, extract text from response |
| Page Rasterization | Phase 8 | 📅 Planned | Configurable DPI, temp file management |
| Parallel Processing | Phase 8 | 📅 Planned | Batch processing optimization, parallel page processing |
| Retry Logic | Phase 8 | 📅 Planned | API failure recovery for OCR endpoints |
| Confidence Scoring | Phase 8 | 📅 Planned | OCR quality metrics |

### Build Status

```bash
cd PdfTextExtractor/PdfTextExtractor.Core
dotnet build
# Result: Build succeeded
# Errors: 0
# Warnings: 7 (nullable references - expected for EF Core entities)
```

### Project Location

`PdfTextExtractor/PdfTextExtractor.Core/` (50+ files)

---

## Part 6: YieldRaccoon.Wpf Desktop Application ✅ COMPLETED

WPF desktop application implementing Model-View-ViewModel pattern using DevExpress MVVM framework, Autofac dependency injection, and MahApps.Metro modern UI styling.

### Implementation Status

| Component | Status | Notes |
| ----------- | -------- | ------- |
| Project Setup | ✅ | .NET 9.0 WPF with Windows 10.0.26100.0 target framework |
| MVVM Architecture | ✅ | **Completed 2026-01-28**: DevExpress ViewModelBase, property change notification, ICommand implementation |
| MainWindowViewModel | ✅ | **Completed 2026-01-28**: Title and StatusMessage properties, RefreshCommand, IDisposable implementation |
| Autofac DI Container | ✅ | **Completed 2026-01-28**: Configured in App.xaml.cs OnStartup, ViewModel and View registration, constructor injection |
| MahApps.Metro UI | ✅ | **Completed 2026-01-28**: MetroWindow conversion, Light.Blue theme, resource dictionaries in App.xaml |
| Fluent Design v2 Theme | ✅ | **Implemented 2026-02-09**: `Themes/YieldRaccoonTheme.xaml` — 31 design tokens (7 semantic brushes, 6 spacing doubles, 5 thickness tokens, 7 typography styles, 2 panel styles, 3 button styles, 1 splitter style). `RuntimeThemeGenerator` in App.xaml.cs reads Windows accent color. All 6 views migrated to yr.* tokens: sentence casing, 4px-grid spacing, semantic color brushes, button wrappers with 4px corners, two-tier panel elevation. |
| Design Guidelines Doc | ✅ | **NEW 2026-02-08**: `DESIGN-GUIDELINES.md` — comprehensive design system reference covering typography, spacing, color, geometry, elevation, buttons, iconography (Segoe Fluent Icons glyph catalog), micro-interactions, WPF gotchas, and do's/don'ts |
| Data Binding | ✅ | **Completed 2026-01-28**: Title binding, StatusMessage binding, Command binding |
| Constructor Injection | ✅ | **Completed 2026-01-28**: MainWindow receives MainWindowViewModel via constructor, DataContext set in constructor |
| Fund Repository Integration | ✅ | **Completed 2026-01-29**: IFundRepository DI registration, FundMapper (InterceptedFund → Fund), repository persistence in OnFundDataReceived, thread-safe in-memory storage via ConcurrentDictionary |
| InMemory Repository Provider | ✅ | **NEW 2026-01-29**: InMemoryFundProfileRepository + InMemoryFundHistoryRepository implementations with ConcurrentDictionary storage, DatabaseProvider enum for switching (InMemory/SQLite), conditional DI registration in PresentationModule, README updated with mermaid architecture diagram |
| Fund Ingestion Integration | ✅ | **NEW 2026-01-29**: ICrawlSessionOrchestrator now coordinates database persistence via IFundIngestionService. NotifyBatchLoaded accepts FundDataDto collection, maps to domain entities (FundProfile + FundHistoryRecord), and persists to configured repository (InMemory or SQLite). Added FundDataDtoMapper (InterceptedFund → FundDataDto). |
| Streaming Mode Privacy | ✅ | **Completed 2026-01-29**: ToggleSwitch in browser toolbar, WebView2 screenshot capture via `CapturePreviewAsync`, Magick.NET OilPaint effect (radius: 6, sigma: 1), "🔴 STREAMING" overlay indicator, auto-update on navigation complete |
| Build Verification | ✅ | **Completed 2026-01-29**: Clean build with 0 errors, 0 warnings, all nullability warnings resolved |

### NuGet Dependencies

| Package | Version | Purpose |
| --------- | --------- | --------- |
| **Autofac** | 9.0.0 | Dependency injection container |
| **DevExpressMvvm** | 24.1.6 | MVVM framework (ViewModelBase, DelegateCommand) |
| **MahApps.Metro** | 2.4.11 | Modern WPF UI controls and themes |
| **System.Reactive** | 6.1.0 | Reactive Extensions for IObservable patterns |
| **NLog** | 6.0.7 | Logging framework (infrastructure ready) |
| **NLog.Extensions.Logging** | 6.1.0 | NLog integration with Microsoft.Extensions.Logging |
| **Magick.NET-Q8-AnyCPU** | 14.10.2 | ImageMagick for streaming mode OilPaint effect |

### Architecture

**MVVM Pattern:**
```
View (MainWindow.xaml)
    ↓ DataBinding
ViewModel (MainWindowViewModel)
    ↓ Business Logic
Model (Application/Infrastructure layers)
```

**Dependency Injection:**
```
App.xaml.cs (OnStartup)
    ↓ Configure
ContainerBuilder
    ↓ Register
ViewModels + Views
    ↓ Resolve
MainWindow(MainWindowViewModel)
```

**Key Patterns:**
- DevExpress ViewModelBase with GetProperty/SetProperty
- DelegateCommand for ICommand implementation
- Autofac constructor injection
- IDisposable for resource cleanup
- Tell-don't-ask principle (ViewModel drives UI state)

### Components

**ViewModels:**
- ✅ `MainWindowViewModel` - Main window ViewModel with Title, StatusMessage properties and RefreshCommand

**Views:**
- ✅ `MainWindow` - MetroWindow with data binding to MainWindowViewModel
- ✅ Status bar with StatusMessage display and Refresh button

**Application:**
- ✅ `App.xaml.cs` - Autofac container configuration, OnStartup/OnExit lifecycle management

**Theme:**

- ✅ MahApps.Metro with `RuntimeThemeGenerator` (reads Windows system accent color, falls back to #0078D4)
- ✅ MetroWindow with centered startup location and normal title casing
- ✅ Fluent Design v2 theme system (`Themes/YieldRaccoonTheme.xaml`) — 31 yr.* design tokens, all views migrated
- ✅ `DESIGN-GUIDELINES.md` — comprehensive design system reference document

### DevExpress MVVM Best Practices (Applied)

- ✅ Inherits from ViewModelBase
- ✅ Uses GetProperty<T>() / SetProperty<T>() for observable properties
- ✅ Uses DelegateCommand for ICommand
- ✅ Implements IDisposable for cleanup
- ✅ Follows tell-don't-ask principle
- ✅ ViewModels are testable (no UI dependencies)
- ✅ Constructor injection via Autofac

### Build Status

```bash
cd YieldRaccoon/YieldRaccoon/YieldRaccoon.Wpf
dotnet build
# Result: Build succeeded
# Errors: 0
# Warnings: 0
```

### Project Location

`YieldRaccoon/YieldRaccoon/YieldRaccoon.Wpf/` (MainWindow, App, ViewModels folder)

### Domain Events Redesign ✅ COMPLETED (2026-01-29)

Redesigned domain events for batch-based fund crawling from paginated list pages (clicking "Visa fler" ~74 times) instead of visiting individual fund pages with 4 tabs.

**Key Changes:**

| Change | Details |
| -------- | ------- |
| **Removed** | `FundPageTab.cs` value object (no tab navigation needed) |
| **Removed** | 11 old events: TabNavigation*, TabDataExtraction*, NextCrawlScheduled, CrawlDelay*, CrawlSessionTimedOut |
| **Added** | `BatchNumber.cs` value object (1-based, strongly-typed batch number) |
| **Added** | `Fund.cs` entity (domain entity with 25+ properties from fund list) |
| **Modified** | Session events (CrawlSessionStarted/Completed/Failed) with timing data and batch timestamps |
| **Added** | `CrawlSessionCancelled.cs` event (user cancellation) |
| **Added** | 6 BatchLoad events (Scheduled, Started, Completed, Failed, DelayStarted, DelayCompleted) |

**New Domain Events (12 Total):**

| Category | Events | Status |
| --------- | -------- | -------- |
| **Session Lifecycle** | CrawlSessionStarted, Completed, Failed, Cancelled | ✅ Complete |
| **Batch Load** | BatchLoadScheduled, Started, Completed, Failed | ✅ Complete |
| **Batch Delay** | BatchLoadDelayStarted, BatchLoadDelayCompleted | ✅ Complete |
| **Daily Scheduling** | DailyCrawlScheduled, DailyCrawlReady | ✅ Complete (simplified) |

**New Repositories & Event Store:**

| Component | Layer | Status | Notes |
| ----------- | ------- | -------- | ------- |
| `IFundRepository.cs` | Application | ✅ | Fund data access interface (CRUD by ISIN) |
| `InMemoryFundRepository.cs` | Infrastructure | ✅ | `ConcurrentDictionary<FundId, Fund>` implementation, thread-safe singleton |
| `ICrawlEventStore.cs` | Application | ✅ | Append-only event log with query projections |
| `InMemoryCrawlEventStore.cs` | Infrastructure | ✅ | `List<IDomainEvent>` with LINQ projections |
| `FundMapper.cs` | Mappers | ✅ | **NEW 2026-01-29**: Extension methods for InterceptedFund → Fund conversion with ISIN validation |
| **Repository Population** | Integration | ✅ | **NEW 2026-01-29**: Crawled funds now persisted to repository during `OnFundDataReceived()` via `AddOrUpdateRange()` |

**Architecture (Event-Driven with Rx.NET):**

```text
CrawlOrchestrationService
    ↓ Append events
ICrawlEventStore (query projections)
    ↓ Update funds
IFundRepository (CRUD by ISIN)
    ↓ Rx.NET timers
Observable.Timer (20-60s random delays)
```

### AboutFund 3-Column Layout + Fund Overview Browsing ✅ COMPLETED (2026-02-07)

Refactored the AboutFund window from a 2-column layout (WebView2 + Network Inspector) to a 3-column layout matching MainWindow (fund schedule | browser | control panel). Added independent domain events for fund browsing sessions with Rx.NET orchestration.

**Layout:**

| Column | Width | Content |
| -------- | ------- | --------- |
| Left | 280px | Fund schedule (sorted by history count ascending) |
| Middle | * | WebView2 browser (preserved) |
| Right | 350px | Overview control panel (session controls, events, options) |

**New Domain Events (Independent from ICrawlEvent):**

| Category | Events | Status |
| --------- | -------- | -------- |
| **Session Lifecycle** | AboutFundSessionStarted, Completed, Cancelled | ✅ Complete |
| **Navigation** | AboutFundNavigationStarted, Completed, Failed | ✅ Complete |

**New Components (20 new files, 8 modified):**

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Domain** | `IAboutFundEvent` interface, 6 event records, `AboutFundSessionId` value object | ✅ |
| **Application** | `AboutFundScheduleItem` DTO, `AboutFundSessionState` model, `IAboutFundEventStore`, `IAboutFundOrchestrator` | ✅ |
| **Infrastructure** | `InMemoryAboutFundEventStore`, `AboutFundOrchestrator` (Rx.NET with auto-advance timer) | ✅ |
| **Presentation** | `AboutFundScheduleView/VM`, `AboutFundControlPanelView/VM`, `AboutFundEventViewModel` | ✅ |
| **Extracted** | `InterceptorView/VM` (fully decoupled, own generic types: `InterceptedHttpRequest/VM`) | ✅ |
| **Wired Up** | `IAboutFundResponseInterceptor` registered in DI, initialized in `AboutFundWindow` code-behind | ✅ |
| **Data Flow** | `AboutFundInterceptedRequest` moved to Application layer, interceptor→orchestrator via code-behind forwarding, URL filtering added | ✅ |
| **Refactored** | `AboutFundWindow.xaml` (3-col), `AboutFundWebView2Behavior`, `PresentationModule` (DI), `YieldRaccoonOptions` | ✅ |

**Key Features:**

- Fund schedule loaded from DB, sorted by history record count (ascending - least data first)
- Manual "Start Overview" button + optional AutoStartOverview toggle
- Auto-advance timer (12s) when AutoStartOverview is enabled
- Session cancellation on window close or Stop button
- Event log panel showing real-time browsing events with icons
- URL template uses OrderbookId externally (`{0}` placeholder), ISIN internally

### Future Enhancements (Planned)

| Feature | Status | Notes |
| --------- | -------- | ------- |
| CrawlOrchestrationService | 📅 Planned | Application service coordinating crawl sessions with Rx.NET |
| Additional ViewModels | 📅 Planned | Create ViewModels for specific features |
| User Controls | 📅 Planned | Break down MainWindow into smaller user controls |
| Rx.NET Integration | 📅 Planned | Leverage System.Reactive for reactive patterns |
| NLog Configuration | 📅 Planned | Set up logging configuration when needed |
| Unit Tests | 📅 Planned | Test ViewModels with NUnit + AutoFixture + Moq |

---

## Infrastructure & Deployment

### Current State

| Component | Status | Notes |
| ----------- | -------- | ------- |
| Local Development | ✅ Working | Preprocessor and Backend run locally |
| OpenAI API | ✅ Configured | Embeddings (text-embedding-3-small) + Chat (gpt-4o-mini, default) |
| Groq API | ✅ Configured | Optional free tier LLM (llama-3.3-70b-versatile) |
| Azure App Service | ✅ Ready | Backend API hosting (F1 Free tier) |
| Azure Static Web Apps | ✅ Ready | Frontend hosting (Free tier) |
| Application Insights | ✅ Ready | Monitoring configured (free tier) |
| Azure Key Vault | ✅ Ready | Secrets management via Managed Identity |
| CI/CD Workflows | ✅ Complete | Backend deploy, Frontend deploy, PR checks |
| Production Deployment | ✅ Ready | Complete deployment documentation |
| Cosmos DB Vector Database | ✅ Production Ready | Optional persistent vector storage via `cosmosdb` verb. **Local + Production deployed**: Backend API, authentication, Managed Identity RBAC, Key Vault secrets, embeddings uploaded. Free tier (1000 RU/s, 25GB) |

### Deployment Setup Complete

- ✅ Backend → Azure App Service (F1 Free tier)
- ✅ Frontend → Azure Static Web Apps (Free tier)
- ✅ GitHub Actions → Automated CI/CD (3 workflows)
- ✅ PR Checks → Automated lint, test, build validation
- ✅ Azure Key Vault → Production secrets
- ✅ Application Insights → Monitoring and telemetry
- ✅ Health checks → Liveness and readiness probes

---

## Testing Status

### Preprocessor

| Test Suite | Status | Coverage |
| ------------- | -------- | ---------- |
| PreprocessorServiceTests | ✅ Complete | Core functionality |
| OllamaEmbeddingServiceTests | ✅ Complete | Service integration |
| PdfPigExtractorTests | ⚠️ Partial | Basic tests |

### Backend

| Test Suite | Status | Coverage |
| ------------- | -------- | ---------- |
| Domain Layer Tests | ✅ Complete | CosineSimilarityCalculator (6 tests, deprecated), UserQuestionSanitizer (13 tests), models, value objects |
| ApplicationCore Tests | ✅ Complete | QuestionAnsweringService (10 tests), RAG pipeline orchestration |
| Infrastructure Tests | ✅ Complete | InMemorySemanticSearch (5 tests), DocumentChunkMapper (4 tests), VectorStore integration |
| Validation Tests | ✅ Complete | SafeQuestionAttribute (8 tests), prompt injection defense |
| Integration Tests | ✅ Complete | Full pipeline tests (6 tests), end-to-end validation |
| Controller Tests | ❌ Not Implemented | AskController, health checks |
| **Total Backend Tests** | **✅ 69 Complete** | 69 tests passing (includes VectorStore migration) |

### Frontend

| Test Suite | Status | Coverage |
| ------------- | -------- | ---------- |
| page.test.tsx | ✅ Complete | Homepage rendering, example queries |

---

## Documentation Status

| Document | Status | Location |
| ---------- | -------- | ---------- |
| Preprocessor README | ✅ Complete | `Preprocessor/README.md` |
| Backend README | ✅ Complete | `backend/README.md` |
| Frontend README | ✅ Complete | `frontend/README.md` |
| API Documentation | ✅ Complete | Swagger UI at `/swagger` |
| Deployment Guide | ✅ Complete | `docs/AZURE-DEPLOYMENT.md` |
| Status Tracker | ✅ Complete | This file |

---

## Known Issues & Limitations

### Preprocessor

1. No progress bar for long-running operations

### Backend

1. In-memory DocumentRepository = data lost on restart (by design)
2. No caching = every search generates new embedding
3. Missing unit tests for DDD layers
4. More files and abstractions due to DDD structure (trade-off for maintainability)

### General

1. No authentication/authorization
2. No multi-user support
3. No chat history persistence
4. English-only (no i18n)

---

## Next Steps

### Immediate Priorities

1. ✅ **Azure Deployment Setup Complete** (see docs/AZURE-DEPLOYMENT.md)
   - Run `./azure-setup.sh` to create Azure resources
   - Configure GitHub Secrets and Variables
   - Push to main branch to trigger deployment
2. ✅ **DDD Refactoring Complete** (Domain, ApplicationCore, Infrastructure layers)
3. ✅ **Unit Tests Complete** (51 tests: Domain, ApplicationCore, Infrastructure layers)
   - New test files: QuestionAnsweringServiceTests.cs (8 tests), InMemorySemanticSearchTests.cs (7 tests)
   - Existing tests: 13 sanitizer tests, 8 validation tests, 6 calculator tests, 6 integration tests, 4 model tests
4. ✅ Create Next.js frontend application
5. ✅ Implement chat interface UI components
6. ⏳ Test end-to-end integration
7. ⏳ Controller tests (AskController, health checks) - optional enhancement

### Future Enhancements

- ✅ Migrate to modern Vector Store abstractions - Completed 2026-01-02: Using InMemoryVectorStore with VectorStoreCollection for built-in cosine similarity
- Implement caching layer
- Support multiple languages
- Add streaming responses for better UX
- Implement chat history
- Upgrade to Azure App Service B1 tier if F1 limitations are problematic

---

## Success Metrics

### Completed ✅

- [x] Extract text from PDFs
- [x] Generate embeddings
- [x] Store embeddings in JSON
- [x] Load embeddings in backend
- [x] Semantic search functionality
- [x] LLM-based question answering
- [x] REST API endpoints
- [x] Health monitoring

### In Progress ⏳

- [ ] Unit tests for backend
- [x] Frontend development
- [x] Frontend chat UI components
- [ ] End-to-end testing

### Planned ❌

- [x] Production deployment setup (Azure resources + CI/CD workflows)
- [x] Deploy to production (Azure resources created, embeddings generated, GitHub secrets configured)
- [ ] Performance optimization
- [ ] Advanced features (MCP, caching, etc.)

---

## Cost Analysis

### Development Costs: $0

- OpenAI API: Pay-per-use for embeddings (~$0.02 per 1M tokens)
- Groq API: Free tier (sufficient for hobby project)

### Production Costs (Actual)

**With OpenAI Chat (Default):**

- **Azure App Service F1**: $0/month (free tier, with limitations)
- **Application Insights**: $0/month (5GB free tier)
- **Azure Key Vault**: ~$0.03/month (10K operations free, then $0.03 per 10K)
- **OpenAI Chat (gpt-4o-mini)**: ~$0.50/month (100 questions/day estimate, ~$0.15 per 1M input tokens)
- **OpenAI Embeddings**: ~$0.003/month (100 questions/day estimate)

**Total Production Cost: ~$0.53/month**

**With Groq Chat (Optional, Free Tier):**

- **Azure App Service F1**: $0/month (free tier, with limitations)
- **Application Insights**: $0/month (5GB free tier)
- **Azure Key Vault**: ~$0.03/month (10K operations free, then $0.03 per 10K)
- **Groq LLM**: $0/month (free tier)
- **OpenAI Embeddings**: ~$0.003/month (100 questions/day estimate)

**Total Production Cost: ~$0.03/month**

**With Cosmos DB Vector Storage (Optional):**

- **Cosmos DB Free Tier**: $0/month (1000 RU/s, 25 GB storage - one per subscription)
- No additional cost when using free tier

**Total Production Cost with Cosmos DB: ~$0.03/month (Groq) or ~$0.53/month (OpenAI Chat)**

### Upgrade Options

- **Azure App Service B1**: ~$13/month (always-on, no cold starts, custom domains)
- **Application Insights Pay-as-you-go**: $2.30/GB after 5GB free tier
