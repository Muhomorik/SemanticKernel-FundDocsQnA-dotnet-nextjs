# PDF Q&A Application - Implementation Status

Last Updated: 2026-05-08 (Portfolio allocation ingestion completed — country/sector data from `_api/fund-reference/portfolio-data/` with full backend mirror + dual-write)

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
| AI Evaluation Tests | ✅ | `ExampleQueriesAIEvaluatedTests` - evaluates frontend queries for answerability against PRIIP/KID documents, generates markdown report with categories (single_doc_answerable, multi_doc_answerable, context_dependent, info_missing) and suggested rephrases. Requires OpenAI API key, marked `[Explicit]`. |
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
| LLM Providers | ✅ | OpenAI (gpt-4.1-mini) default, Groq optional |
| Vector Storage | ✅ | InMemory (default) + Cosmos DB (optional persistent storage) |
| Semantic Search | ✅ | OpenAI embeddings (text-embedding-3-small) + InMemoryVectorStore / CosmosDbSemanticSearch |
| API Endpoints | ✅ | POST /api/ask, POST /api/embeddings (+ PUT, DELETE), POST /api/funds/list, POST /api/funds/about, POST /api/funds/full-sync, health checks, Swagger |
| Fund Data Sync | ✅ | EF Core + Azure SQL: FundProfiles + FundHistoryRecords tables, auto-migration, upsert/insert-only patterns, transient retry policy |
| Authentication | ✅ | API key authentication for embedding + fund data endpoints |
| Security | ✅ | Input validation, sanitization, rate limiting (10/min/IP), constant-time API key comparison |
| Azure Deployment | ✅ | App Service F1, Key Vault, Application Insights, Cosmos DB (optional) |
| CI/CD | ✅ | GitHub Actions (.github/workflows/deploy-backend.yml) |
| Unit Tests | ✅ | 140 tests passing (Domain, ApplicationCore, Infrastructure, Fund Data, FundDataPlugin integration) |
| Documentation | ✅ | README with DDD architecture + Cosmos DB setup guide |

### Security Implementation ✅ (2026-01-01)

| Component | Status | Notes |
| ----------- | -------- | ------- |
| Input Validation | ✅ | [MaxLength(500)], [Required], [MinLength(3)] |
| Custom Validation | ✅ | [SafeQuestion] detects injection patterns |
| Input Sanitization | ✅ | Removes control chars, normalizes whitespace |
| System Prompt | ✅ | Hardened with anti-jailbreak instructions |
| Rate Limiting | ✅ | 60 req/min, queue of 5, `Retry-After: 5` header on 429 |
| Request Size Limits | ✅ | 10KB max body size |

### Fund Data Sync (Azure SQL) ✅ COMPLETED (2026-03-02)

Backend API endpoints for syncing YieldRaccoon fund data to Azure SQL Database. EF Core with SQL Server provider, auto-migration on startup, API key authentication.

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Domain** | `IsinId` value object (12-char ISIN validation) | ✅ |
| **Domain** | `FundHistoryRecordId` value object (auto-increment long) | ✅ |
| **Domain** | `FundProfile` aggregate root (~35 properties, keyed by ISIN) | ✅ |
| **Domain** | `FundHistoryRecord` entity (Nav, NavDate, Capital, Risk metrics) | ✅ |
| **Domain** | `IFundProfileRepository` / `IFundHistoryRepository` interfaces | ✅ |
| **ApplicationCore** | API DTOs mirrored from YieldRaccoon (8 files) | ✅ |
| **ApplicationCore** | `IFundSyncService` / `FundSyncService` (DTO→entity mapping, validation) | ✅ |
| **Infrastructure** | `FundDataDbContext` (EF Core, SQL Server types) | ✅ |
| **Infrastructure** | EF Core configurations (NCHAR(12), DECIMAL(18,6), DATE, single unique descending index) | ✅ |
| **Infrastructure** | `EfCoreFundProfileRepository` (upsert + insert-if-not-exists, preserves FirstSeenAt) | ✅ |
| **Infrastructure** | `EfCoreFundHistoryRepository` (batch-load upsert + insert-if-not-exists + sparse upsert) | ✅ |
| **Controller** | `POST /api/funds/list` (batch crawl session sync) | ✅ |
| **Controller** | `POST /api/funds/about` (single fund + chart history) | ✅ |
| **Controller** | `POST /api/funds/full-sync` (CloudSync full history with sparse upsert) | ✅ |
| **Middleware** | `ApiKeyAuthenticationMiddleware` expanded for `/api/funds` | ✅ |
| **Health** | `AzureSqlHealthCheck` (connectivity + profile count) | ✅ |
| **Migration** | `InitialFundData` (FundProfiles + FundHistoryRecords tables) | ✅ |
| **Tests** | `IsinIdTests` (8), `FundHistoryRecordIdTests` (7), `FundSyncServiceTests` (23), `EfCoreFundProfileRepository_UpsertAsyncTests` (9), `EfCoreFundProfileRepository_InsertIfNotExistsAsyncTests` (5), `EfCoreFundHistoryRepository_UpsertRangeAsyncTests` (6), `EfCoreFundHistoryRepository_InsertIfNotExistsRangeAsyncTests` (6), `EfCoreFundHistoryRepository_UpsertSparseRangeAsyncTests` (9) | ✅ |

**Endpoints:**

| Method | Path | Purpose |
| -------- | ------ | --------- |
| POST | `/api/funds/list` | Batch upsert profiles + daily snapshots from fund list crawl |
| POST | `/api/funds/about` | Upsert profile + insert-only chart history from fund detail page |
| POST | `/api/funds/full-sync` | CloudSync: insert-if-not-exists profile + sparse upsert full history (all time-varying fields) |

### FundDataPlugin — Semantic Kernel Function Calling ✅ COMPLETED (2026-03-07)

LLM can now answer structured fund data queries (performance, ownership, categories, profiles, search) via Semantic Kernel function calling, alongside RAG-based document Q&A. Six `[KernelFunction]` methods registered on the Kernel, called automatically by the LLM when the question requires structured data.

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Infrastructure** | `FundDataPlugin` (6 kernel functions: categories, profile, search, performance, owner change, category performance) | ✅ |
| **Infrastructure** | Result records: `FundProfileResult`, `FundSearchResult`, `FundPerformanceResult`, `FundOwnerChangeResult`, `CategoryPerformanceResult` | ✅ |
| **Infrastructure** | `OpenAiProvider` updated with `Kernel` + `FunctionChoiceBehavior.Auto()` | ✅ |
| **ApplicationCore** | `SystemPromptFactory` hybrid prompt (RAG + function-calling instructions) | ✅ |
| **Program.cs** | `FundDataPlugin` registered on Kernel via `AddFromObject()` after `app.Build()` | ✅ |
| **Program.cs** | `AddDbContextFactory<FundDataDbContext>` (singleton plugin + scoped DbContext) | ✅ |
| **Tests** | 14 plugin-only SK integration tests (Tasks 1-6) | ✅ |
| **Tests** | 10 hybrid integration tests (function calling + real RAG over 15 SEB fund factsheet embeddings) | ✅ |
| **Tests** | `SystemPromptFactoryTests` (3 tests) | ✅ |
| **Tests** | `TestDataPaths` helper, `TestFundDataDbContextFactory`, `test_embeddings.json` (287 chunks) | ✅ |

**Plan:** `.claude/plans/curried-riding-hoare.md`

### Ownership Flow — Frontend ✅ COMPLETED (2026-03-11)

Frontend page at `/ownership-flow` with declarative JSX SVG Sankey renderer, period selector, and full dark/light mode support.

| Component | Details | Status |
| ----------- | --------- | -------- |
| `lib/ownership-flow.ts` | Types, API functions (`fetchOwnershipPeriods`, `fetchOwnershipFlow`), layout math (`computeSankeyLayout`, `buildLinkPath`), formatting helpers | ✅ |
| `ownership-flow-page.tsx` | `"use client"` orchestrator — two-effect fetch chain, shared tooltip state, AbortController cleanup | ✅ |
| `period-selector.tsx` | Controlled weekly/monthly pill tabs, `aria-pressed`, warm gradient active style, disabled during fetch | ✅ |
| `sankey-chart.tsx` | Declarative JSX SVG — `useId()` for gradient IDs, `useTheme()` for hub colors, mouse event tooltip handlers | ✅ |
| `sankey-card.tsx` | Card chrome, loading skeleton (`animate-pulse`), outflow/inflow count badges | ✅ |
| `sankey-empty.tsx` | Three empty state variants (no data / only-in / only-out) | ✅ |
| `sankey-tooltip.tsx` | Portal-based tooltip via `createPortal`, SSR-safe `mounted` guard | ✅ |
| `app/ownership-flow/page.tsx` | Static route shell with Next.js `Metadata` | ✅ |
| `header.tsx` | Nav link: "Ownership Flow" (home) / "← Fund Insights" (flow page) via `usePathname` | ✅ |
| **Tests** | 76 tests: `sankey-layout` (45), `api.ownership-flow` (11), `period-selector` (8), `sankey-card` (10), `sankey-chart` (7), `sankey-empty` (3) | ✅ |
| Dark/light mode | Page chrome via Tailwind CSS variables; SVG hub colors via `useTheme()` (dark warm / light tan) | ✅ |

### Ownership Flow API — Sankey Chart Backend ✅ COMPLETED (2026-03-10)

Backend API endpoints for ownership flow Sankey visualization. Computes investor movement (NumberOfOwners deltas) across funds and categories for weekly/monthly time periods.

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Domain** | `CategoryMacroGroup` value object (Swedish category → 10 macro-groups) | ✅ |
| **ApplicationCore** | DTOs: `OwnershipFlowPeriodsResponse`, `OwnershipFlowResponse`, `OwnershipFlowGroup`, `OwnershipFlowItem`, `TimePeriod` | ✅ |
| **ApplicationCore** | `IOwnershipFlowService` interface | ✅ |
| **Infrastructure** | `OwnershipFlowService` (IDbContextFactory, IMemoryCache, period computation, delta calculation, category aggregation) | ✅ |
| **Controller** | `GET /api/ownership-flow/periods` (weekly + monthly time periods) | ✅ |
| **Controller** | `GET /api/ownership-flow?from=&to=` (Sankey data for both charts, input validation) | ✅ |
| **Program.cs** | Service registration + `AddMemoryCache()` | ✅ |
| **Infrastructure** | `OwnershipFlowService` bug fix: look-back baseline — query now loads all records `NavDate <= to` (no lower bound); start snapshot = most recent record ≤ from, fallback to earliest if none exists. Fixes empty results for weekly/monthly periods with sparse NumberOfOwners data. | ✅ |
| **Tests** | `CategoryMacroGroupTests` (20), `OwnershipFlowService_GetAvailablePeriodsTests` (11), `OwnershipFlowService_GetOwnershipFlowAsyncTests` (22) — includes 6 weekly-period tests + 2 TDD look-back regression tests. InternalsVisibleTo for internal method testing. | ✅ |
| **Tests** | `OwnershipFlowControllerTests` (17) — validation (from≥to, range>365, from in future), 503 guard, happy path, exception→500, error message content | ✅ |

**Endpoints:**

| Method | Path | Purpose |
| -------- | ------ | --------- |
| GET | `/api/ownership-flow/periods` | Available weekly (4 ISO weeks) + monthly (1/2/3 months) time periods |
| GET | `/api/ownership-flow?from=&to=` | Sankey data: fund-level (top 10 out/in) + category-level (aggregated by macro-group) |

**Plan:** `.claude/plans/elegant-marinating-kettle.md`

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
| Daily Auto-Start | ✅ | **NEW 2026-04-14**: Settings window now has "Daily auto-start" section (toggle + HH:mm picker + `--auto-list` checkbox). Creates a per-user Windows scheduled task under `\YieldRaccoon\YieldRaccoon-AutoStart` via `TaskScheduler` NuGet with `InteractiveToken` + `LUA` run level (no UAC needed on normal installs). Reconciles persisted setting with actual task state on window open. UAC fallback: on access-denied, prompts user to restart as admin with `--elevated-settings` flag, elevated instance auto-reopens Settings for retry. Files: `AutoStartSchedulerService`, `UserSettings` (new `AutoStart*` fields), `AutoStartOptions.OpenSettingsOnStartup`, `SettingsWindowViewModel` (new properties + `TryApplyAutoStartSchedule` + `TryRestartElevated`). |
| Auto-Start DB Cold-Start Buffer | ✅ | **NEW 2026-04-14**: When launched with `--auto-list` or `--auto-overview`, the first crawler call is deferred by `AutoStartOptions.ColdStartDelay` (60s) measured from app launch. Prevents the first EF Core / SQLite call from colliding with model-building on a cold DB. `AutoStartOptions` gained `LaunchedAtUtc` and `GetColdStartRemaining(TimeSpan)`. `MainWindowViewModel` schedules `ExecuteStartSession` via `Observable.Timer(delay, _uiScheduler)` (disposed with the VM). `AboutFundWindowViewModel` awaits `Task.Delay(delay)` in `ExecuteLoaded` before `LoadScheduleAsync` when `AutoOverview` is active. Subsequent session steps run at normal cadence; manual launches are unaffected. |
| Weekly Stats Export — Manual Open Auto-Close Fix | ✅ | **FIX 2026-04-23**: Manually opening the Statistics Export window could close instantly if the app had previously handled a scheduled run. `App.TriggerWeeklyStatsExportWindow` flipped the shared `AutoStartOptions.AutoWeeklyStats` singleton flag to `true` but never cleared it, so a later manual open spun up a fresh VM whose `ExecuteLoaded` saw the stale flag, auto-fired Export, and called `CurrentWindowService.Close()`. Fix: `FundStatisticsExportWindowViewModel.ExecuteLoaded` now resets `_autoStartOptions.AutoWeeklyStats = false` after consuming it (the scheduled run still completes — `isScheduledRun` is captured synchronously before the first await). Regression test added: `FundStatisticsExportWindowViewModelTests.ExecuteLoaded_WhenAutoWeeklyStatsIsTrue_ClearsFlagSoSubsequentManualOpensDoNotAutoFire`. |
| Weekly Statistics Export Scheduling | ✅ | **NEW 2026-04-18**: Weekly-recurring auto-export of fund statistics via Windows Task Scheduler (default Thursday 22:00). Settings window has a new "Weekly statistics export" section with `ToggleSwitch` + day-of-week `ComboBox` + `mah:TimePicker` + last-run caption. Export parameters live entirely in the Statistics Export window and persist via `UserSettings.StatsExport*` (window days, lookback days, min owners, company filter, both output paths) — the scheduled run always picks up the user's latest manual configuration. Default lookback raised from 6 months to 1 year; `FundStatisticsExportWindowViewModel` pre-populates from settings on open and writes back on successful export. Date-stamped filenames for scheduled runs (e.g., `YieldRaccoon_summary_2weeks_1year_2026-04-23.csv`). New `--auto-weekly-stats` CLI flag triggers the export-and-close flow via the VM's `LoadedCommand` (no code-behind). Single-instance enforced via `WindowsFormsApplicationBase` in `Program.cs`; second-instance command line is forwarded to running process via `OnStartupNextInstance` and routed to `App.HandleAutoWeeklyStatsTrigger()` which cooperates with crawl sessions, already-open export windows, and in-flight exports. SQLite WAL mode applied on startup via `PRAGMA journal_mode=WAL` so concurrent crawl-writers and export-readers don't block each other. New main-window status-bar chip shows "Weekly export: next Thu 22:00" / last-run summary. Files: `AutoStartSchedulerService` (EnableWeeklyStatsExport/DisableWeeklyStatsExport/IsWeeklyStatsExportEnabled/GetNextWeeklyStatsExportRun with WeeklyTrigger), `UserSettings` (new `WeeklyExport*` + `StatsExport*` fields), `AutoStartOptions.AutoWeeklyStats`, `SettingsWindow` (new section), `SettingsWindowViewModel.TryApplyWeeklyExportSchedule`, `FundStatisticsExportWindowViewModel` (LoadedCommand + persist-on-save + date-stamp helper), `Program.cs` (new — WindowsFormsApplicationBase wrapper), `App.xaml.cs` (TriggerWeeklyStatsExportWindow/HandleAutoWeeklyStatsTrigger + WAL pragma), `MainWindow.xaml` (status chip), `MainWindowViewModel.UpdateWeeklyExportStatus`. csproj adds `<UseWindowsForms>true</UseWindowsForms>` + `<StartupObject>YieldRaccoon.Wpf.Program</StartupObject>` + removes WinForms implicit usings to avoid collisions with WPF types. All 315 unit tests (WPF + Infrastructure) passing. |

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
| **TaskScheduler** | 2.11.0 | Windows Task Scheduler wrapper for daily auto-start feature |

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
- **Delisting filter (2026-04-14) ✅** — `GetFundsOrderedByLastVisitAsync` excludes funds whose `CrawlerLastUpdatedAt` is null or older than one month. The list crawler has effectively stopped seeing these funds, so the about-fund orchestrator no longer wastes its 80/day budget on dead pages. 4 new filter tests in the repository test suite (`Stale crawler filter` region).

### Chart Data Ingestion Pipeline ✅ COMPLETED (2026-02-19)

Added `IAboutFundChartIngestionService` / `AboutFundChartIngestionService` — a dedicated pipeline that deserializes raw chart JSON from the 7 time-period slots collected by `AboutFundPageDataCollector`, merges overlapping time series with deduplication by NAV date, maps to `FundHistoryRecord` entities (Nav + NavDate only), and persists via `IFundHistoryRepository`. Wired into `AboutFundOrchestrator.OnPageDataCollected` as fire-and-forget with full error handling — persistence failures never break the browsing session.

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Application** | `IAboutFundChartIngestionService` interface | ✅ |
| **Infrastructure** | `AboutFundChartIngestionService` (JSON deserialization, merge, dedup, persist) | ✅ |
| **Orchestrator** | `PersistChartDataAsync` in `AboutFundOrchestrator` (async void, resolves ISIN from schedule) | ✅ |
| **DI** | Autofac registration in `PresentationModule` with NLog logger | ✅ |

### Portfolio Allocation Ingestion Pipeline ✅ COMPLETED (2026-05-08)

Captures country and sector portfolio allocations from the `_api/fund-reference/portfolio-data/{orderBookId}` endpoint into a normalized 4-table schema (`Countries`, `Sectors`, `FundCountryAllocations`, `FundSectorAllocations`) with Guid PKs. Latest-only (no history); diff-based upsert with delete-missing on re-crawl. Dual-writes to backend cloud sync. Switched YieldRaccoon from `EnsureCreatedAsync` to migration-based schema management with two migrations: `InitialCreate` (baseline of existing schema) and `AddFundAllocations` (new tables). Backend mirrors the schema with the same diff/upsert logic and a `POST /api/funds/portfolio-allocations` endpoint. JSON wire field `name`/`y` renamed via `[JsonPropertyName]` to descriptive `DisplayName`/`Percentage` properties end-to-end. `holdingChartData` and `previousY`/`portfolioDate` intentionally not persisted.

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Domain** | `Country`, `Sector`, `FundCountryAllocation`, `FundSectorAllocation` entities + Guid value-object IDs | ✅ |
| **Infrastructure** | `PortfolioDataResponse` anti-corruption DTO (`[JsonPropertyName]` for `name`/`y` rename) | ✅ |
| **Infrastructure** | EF Core configurations (4) + value converters + repositories | ✅ |
| **Application** | `IPortfolioDataIngestionService` + 4 repository interfaces | ✅ |
| **Infrastructure** | `PortfolioDataIngestionService` (diff-based upsert) + `DualWritePortfolioDataIngestionService` decorator + `NoOpPortfolioDataIngestionService` for InMemory provider | ✅ |
| **Migrations** | YR `InitialCreate` baseline + `AddFundAllocations`; backend `AddFundAllocations` | ✅ |
| **Capture** | Passive metadata capture in `AboutFundPageDataCollector.TryCaptureMetadata` (no new completion slot) | ✅ |
| **Orchestrator** | Wired into `PersistChartDataAsync` (auto mode) + `PersistChartDataForManualAsync` (manual mode) | ✅ |
| **DI** | `PresentationModule` registers EF repos (SQLite/DualWrite), real ingestion service (SQLite), no-op (InMemory), dual-write decorator (DualWrite) | ✅ |
| **Backend** | Mirror schema (4 entities + 4 value objects + 4 configurations + 4 repos) + `POST /api/funds/portfolio-allocations` controller + `FundSyncService.SyncPortfolioAllocationsAsync` | ✅ |
| **WPF Cloud Client** | `IFundSyncApiClient.SyncPortfolioAllocationsAsync` + `FundSyncApiClient` impl with retry | ✅ |
| **Schema migration** | YR `App.xaml.cs` switched from `EnsureCreatedAsync` → `MigrateAsync` with auto-stamp baseline (legacy `EnsureCreated` databases are detected on startup and have `__EFMigrationsHistory` populated with the InitialCreate row, so existing data is preserved — no manual DB rebuild required); `DesignTimeYieldRaccoonDbContextFactory` added so EF tools can resolve context (Autofac runtime DI doesn't apply at design time) | ✅ |
| **Tests** | YR: 24 new tests across 4 files (DTO deserialization × 4, ingestion service × 10, dual-write × 4, repository × 6). Backend: 4 new tests on `FundSyncService_SyncPortfolioAllocationsAsync`. All 311 YR Infrastructure + 54 YR WPF + 226 backend tests passing. | ✅ |

### Fund Data Export ✅ COMPLETED (2026-02-24)

Export window allowing users to filter the fund database by company name and time period, saving the filtered result as a standalone SQLite `.db` file. Original database is never modified — the pipeline copies first, then filters the copy.

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Application** | `IFundDataExportService` interface (ExportAsync with company + cutoff) | ✅ |
| **Infrastructure** | `FundDataExportService` (File.Copy → WAL checkpoint → DELETE non-matching → VACUUM) | ✅ |
| **Presentation** | `ExportWindow.xaml` (period dropdown, company field, browse output, progress, status) | ✅ |
| **Presentation** | `ExportWindowViewModel` (AsyncCommand, SaveFileDialog, auto-filename generation) | ✅ |
| **Presentation** | `ExportPeriod` model record (1 week, 2 weeks, 1 month, 3 months) | ✅ |
| **Presentation** | `IExportWindowService` / `ExportWindowService` (modal dialog via Autofac) | ✅ |
| **MainWindow** | Export button in title bar (between AboutFund and Settings) | ✅ |
| **DI** | `PresentationModule` registrations for service + window service | ✅ |
| **Tests** | `FundDataExportServiceTests` — 9 tests (company filter, orphan removal, cutoff, case-insensitive, file creation, source untouched, no-match, null company, source not found) | ✅ |

**Export Pipeline (SQLite only):**

1. `File.Copy` source → destination (+ WAL/SHM journal files if present)
2. `PRAGMA wal_checkpoint(TRUNCATE)` — merge WAL into main file
3. `DELETE FROM FundProfiles WHERE CompanyName IS NULL OR LOWER(CompanyName) != LOWER(@company)`
4. `DELETE FROM FundHistoryRecords WHERE FundId NOT IN (SELECT Isin FROM FundProfiles)`
5. `DELETE FROM FundHistoryRecords WHERE NavDate < @cutoff`
6. `PRAGMA journal_mode=DELETE` — switch from WAL to classic mode (checkpoints pending changes)
7. `VACUUM` — reclaim disk space (operates directly on main file, not WAL)
8. Close connection + clear connection pool
9. Clean up leftover `-wal` / `-shm` journal files

### Fund Statistics CSV Export ✅ COMPLETED (2026-02-26)

Compute 13 summary statistics per fund per time window from daily NAV data and export as CSV for exploratory data analysis with Claude. Each fund produces multiple rows — one per non-overlapping window. Source database is read-only (never modified). Uses MathNet.Numerics v5.0.0 for statistical computations.

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Application** | `IFundStatisticsCsvExportService` interface (ExportAsync with window size, company, min owners, cutoff date, progress) | ✅ |
| **Infrastructure** | `FundStatisticsCalculator` (static, pure math: 13 stats from `decimal[]` NAV values) | ✅ |
| **Infrastructure** | `FundSummaryStatistics` (internal record: 13 stats + isin + name + period dates) | ✅ |
| **Infrastructure** | `FundStatisticsCsvExportService` (read-only SQLite → windowing → stats → CSV, with cutoff date filtering) | ✅ |
| **Presentation** | `FundStatisticsExportWindow.xaml` (window size, lookback, min owners, company, browse output, progress bar) | ✅ |
| **Presentation** | `FundStatisticsExportWindowViewModel` (AsyncCommand, SaveFileDialog, auto-filename, progress reporting) | ✅ |
| **Presentation** | `IFundStatisticsExportWindowService` / `FundStatisticsExportWindowService` (modal dialog via Autofac) | ✅ |
| **MainWindow** | `OpenFundStatisticsExportCommand` on `MainWindowViewModel` | ✅ |
| **DI** | `PresentationModule` registrations for export service + window service | ✅ |
| **Tests** | `FundStatisticsCalculatorTests` — 19 tests (returns, volatility, drawdowns, Sharpe, skewness, edge cases) | ✅ |
| **Docs** | `docs/FUND-STATISTICS-EXPORT.md` — usage guide with Claude prompt templates | ✅ |

**13 Statistics:** total_return_pct, ann_volatility, max_drawdown_pct, current_drawdown_pct, sharpe_ratio, best_day_pct, worst_day_pct, pct_positive_days, skewness, first_nav, last_nav, nav_high, nav_low

**Window sizes:** 1 week (7d), 2 weeks (14d, default), 3 weeks (21d), 1 month (30d), 3 months (90d)

**Lookback periods:** 1 month, 2 months, 3 months, 6 months (default), 1 year

### Fund Metadata CSV Export ✅ COMPLETED (2026-03-01)

Export fund profile metadata (fees, risk metrics, classifications) as a companion CSV alongside the statistics export. Only funds with `Buyable = 1` are included in both statistics and metadata exports.

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Application** | `IFundMetadataCsvExportService` interface (ExportAsync with company, min owners) | ✅ |
| **Infrastructure** | `FundMetadataCsvExportService` (read-only SQLite → single query → 17-column CSV, Buyable filter) | ✅ |
| **Infrastructure** | `FundStatisticsCsvExportService` updated with `Buyable = 1` filter | ✅ |
| **Presentation** | `MetadataOutputPath` property + `BrowseMetadataCommand` on `FundStatisticsExportWindowViewModel` | ✅ |
| **Presentation** | New "Metadata output file" field in `FundStatisticsExportWindow.xaml` | ✅ |
| **DI** | `PresentationModule` registration for metadata export service | ✅ |
| **Tests** | `FundMetadataCsvExportServiceTests` — 13 tests (Buyable filter, company filter, owners, CSV format, edge cases) | ✅ |
| **Tests** | `FundStatisticsCsvExportServiceTests` — 3 tests (Buyable filter on statistics export) | ✅ |

**Metadata columns (17):** isin, name, company_name, currency_code, category, fund_type, is_index_fund, managed_type, total_fee, management_fee, risk, rating, sharpe_ratio, standard_deviation, recommended_holding_period, capital, number_of_owners

**Breaking change:** Both statistics and metadata exports now filter by `Buyable = 1`, excluding non-purchasable funds.

### Statistics Export v2 — snapshot.csv + ISO-week filenames + column renames ✅ COMPLETED (2026-04-30)

Restructured the Statistics Export to match the new design docs ([summary-csv-plan.md](YieldRaccoon/docs/summary-csv-plan.md), [snapshot-csv-plan.md](YieldRaccoon/docs/snapshot-csv-plan.md)). One Export click now writes three CSVs (summary + snapshot + metadata) under one ISO-week-tagged family. Re-running the same week overwrites the same files (immutability invariant).

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Application** | `IFundSnapshotCsvExportService` (new interface) | ✅ |
| **Infrastructure** | `FundQueryHelpers` (extracted shared SQL: `ReadFundProfilesAsync`, `ReadNavSeriesAsync`, `GetLatestNavDateAsync`) | ✅ |
| **Infrastructure** | `FundSnapshotStatistics` record + `FundSnapshotStatisticsCalculator` (pure math; 12w + 1y horizons) | ✅ |
| **Infrastructure** | `FundSnapshotCsvExportService` (10-column CSV anchored at MAX(NavDate); NaN-on-insufficient-history; duplicate-ISIN halt) | ✅ |
| **Infrastructure** | `FundSummaryStatistics` properties renamed: `Return2wPct`, `AnnVolatility2wPct`, `MaxDrawdown2wPct`, `Sharpe2w` | ✅ |
| **Infrastructure** | `FundStatisticsCalculator` — added `NearZeroVolatilityThresholdPct = 0.01` constant; Sharpe → NaN when vol < 0.01 % | ✅ |
| **Infrastructure** | `FundStatisticsCsvExportService` — new header (4 renames), `MinimumWindowDays = 7` drops partial trailing buckets, `(Isin, PeriodStart)` duplicate halt, NaN literal serialization | ✅ |
| **Presentation** | `IsoWeekFilenameBuilder` — `BuildFamilyTag` + `BuildIsoWeekTag` (uses `System.Globalization.ISOWeek`) | ✅ |
| **Presentation** | ViewModel: 3rd output path `SnapshotOutputPath`, `BrowseSnapshotCommand`, ISO-week-based default-path builder, dropped `AppendDateSuffix` and the auto-weekly date-suffix branches | ✅ |
| **Presentation** | XAML: third "Snapshot output file" TextBox + Browse; window height bumped to 500 | ✅ |
| **Configuration** | `UserSettings.StatsExportSnapshotOutputPath` for persistence | ✅ |
| **DI** | `FundSnapshotCsvExportService` registered in `PresentationModule` | ✅ |
| **Tests** | `FundSnapshotStatisticsCalculatorTests` (4 tests), `FundSnapshotCsvExportServiceTests` (5 tests), `IsoWeekFilenameBuilderTests` (7 tests) | ✅ |
| **Tests** | `FundStatisticsCalculatorTests` updated (renamed props + 2 new tests for volatility guard) | ✅ |
| **Tests** | `FundStatisticsCsvExportServiceTests` — 3 new tests (v2 header assertion + drop-partial-bucket + keep-7-day-bucket) | ✅ |
| **Tests** | `FundStatisticsExportWindowViewModelTests` — 3 new tests (ISO-week defaults, family tag, no date suffix) | ✅ |
| **Docs** | `docs/FUND-STATISTICS-EXPORT.md` updated for v2 schema + 3-file output + Claude prompt refresh | ✅ |
| **Docs** | `docs/FUND-STATISTICS-EXPORT-AGENT-GUIDE.md` (NEW — concise schema reference for AI agent context) | ✅ |
| **Docs** | `docs/snapshot-csv-plan.md` updated to use `_pct` suffix on volatility columns (consistency with summary) | ✅ |

**Filename change (replaces all date-based naming):**

| Scenario | Before | After |
| -------- | ------ | ----- |
| Default summary | `YieldRaccoon_summary_2weeks_1year.csv` | `YieldRaccoon_summary_all_2026-W18.csv` |
| Default metadata | `YieldRaccoon_metadata.csv` | `YieldRaccoon_metadata_all_2026-W18.csv` |
| Auto-weekly | `..._2weeks_1year_2026-04-30.csv` | `..._all_2026-W18.csv` (no date suffix) |
| Snapshot | (did not exist) | `YieldRaccoon_snapshot_all_2026-W18.csv` |

**Schema renames (clean cutover, no parallel emission):**

`total_return_pct → return_2w_pct`, `ann_volatility → ann_volatility_2w_pct`, `sharpe_ratio → sharpe_2w`, `max_drawdown_pct → max_drawdown_2w_pct`. Snapshot mirrors with `_pct` suffix on volatility columns for symmetry.

**Test results:** 287 / 287 Infrastructure + 52 / 52 WPF tests passing.

### Manual Data Collection Mode ✅ COMPLETED (2026-02-24)

Added manual collection mode: navigate to any fund URL, manually click period buttons in the browser, and each intercepted API response is saved to the database immediately — no session, no timers, no scheduler. The existing interception pipeline (`WebView2 → AboutFundResponseInterceptor → collector → URL pattern matching → slot routing`) now supports passive collection with per-slot persistence.

| Layer | Component | Status |
| ------- | ----------- | -------- |
| **Application** | `TryParseOrderBookId` on `IFundDetailsUrlBuilder` (extract OrderBookId from URL) | ✅ |
| **Application** | `GetIsinByOrderBookIdAsync` on `IFundProfileRepository` (DB lookup) | ✅ |
| **Application** | `BeginPassiveCollection` + `SlotUpdated` on `IAboutFundPageDataCollector` | ✅ |
| **Application** | `StartManualCollectionAsync` on `IAboutFundOrchestrator` | ✅ |
| **Application** | `ManualCollecting` value in `AboutFundSessionPhase` enum | ✅ |
| **Application** | `Phase` property on `AboutFundSessionState` | ✅ |
| **Infrastructure** | `FundDetailsUrlBuilder.TryParseOrderBookId` (URL template prefix/suffix parsing) | ✅ |
| **Infrastructure** | `EfCoreFundProfileRepository.GetIsinByOrderBookIdAsync` (SQLite query) | ✅ |
| **Infrastructure** | `AboutFundPageDataCollector` passive collection + `SlotUpdated` observable | ✅ |
| **Infrastructure** | `AboutFundOrchestrator` manual mode (parse URL → ISIN lookup → passive collection → per-slot persistence) | ✅ |
| **Presentation** | `ExecuteNavigate` in ViewModel delegates to `StartManualCollectionAsync` | ✅ |
| **Presentation** | Control panel: `IsManualMode` / `ShowSessionProgress` — hides timers, shows slot badges only | ✅ |
| **Tests** | `FundDetailsUrlBuilder_TryParseOrderBookIdTests` (11 tests) | ✅ |
| **Tests** | `AboutFundPageDataCollector_PassiveCollectionTests` (12 tests) | ✅ |
| **Tests** | `AboutFundOrchestrator_ManualCollectionTests` (18 tests) | ✅ |
| **Tests** | `EfCoreFundProfileRepository_GetIsinByOrderBookIdAsyncTests` (4 tests) | ✅ |

**Manual Mode Flow:**

1. User enters fund URL in textbox → `ExecuteNavigate` → `orchestrator.StartManualCollectionAsync(url)`
2. Orchestrator parses `OrderBookId` from URL via `TryParseOrderBookId`
3. Looks up ISIN: first from loaded schedule, then DB via `GetIsinByOrderBookIdAsync`
4. Calls `collector.BeginPassiveCollection(orderBookId)` — no timers, no interactions
5. Subscribes to `collector.SlotUpdated` — each slot resolution triggers `IngestChartDataAsync` immediately
6. If automated session is active, URL is navigated but manual collection is skipped (automated takes precedence)
7. Navigating to a new URL silently transitions — previous data already persisted per-slot

### Cloud Sync API DTOs ✅ COMPLETED (2026-03-02, updated 2026-03-11)

HTTP API contract DTOs in `YieldRaccoon.Application/DTOs/Api/` for syncing fund data to Backend API (Azure SQL). These are the source-of-truth for the wire format — Backend has its own identical copies (no project reference).

| File | Purpose |
| ------ | ------- |
| `ApiFundDto.cs` | Fund profile + daily snapshot (~35 fields, JSON-friendly types) |
| `ApiFundHistoryPointDto.cs` | Single NAV chart data point (ISIN + Nav + NavDate) |
| `FundListSyncRequest.cs` | Request for `POST /api/funds/list` (batch from crawl session) |
| `FundAboutSyncRequest.cs` | Request for `POST /api/funds/about` (single fund + chart history) |
| `FundSyncResponse.cs` | Response from both endpoints (success, message, counts) |
| `ApiFundFullSyncProfileMetadataDto.cs` | Static-only profile metadata for `POST /api/funds/full-sync` (excludes time-varying fields) |
| `ApiFundFullHistoryRecordDto.cs` | Full history record for `POST /api/funds/full-sync` (all 7 time-varying fields) |
| `FundFullHistorySyncRequest.cs` | Request for `POST /api/funds/full-sync` (profile metadata + full history records) |

### Cloud Sync Window ✅ COMPLETED (2026-03-04, updated 2026-03-11)

On-demand bulk sync window accessible from the title bar. Lets users push all (or filtered) fund profiles + history records to the Backend API with configurable throttling. Single-phase sync: per-fund `POST /api/funds/full-sync` with profile metadata (insert-if-not-exists) + full history records (all time-varying fields) and throttle delays.

| Component | Details |
| --------- | ------- |
| **Application** | `ICloudSyncService`, `CloudSyncProgress`, `CloudSyncResult` |
| **Infrastructure** | `CloudSyncService` — queries funds, maps to API DTOs, two-phase sync with throttling |
| **Presentation** | `CloudSyncWindow` / `CloudSyncWindowViewModel` — company filter, throttle, progress bar, cancellation |
| **Window Service** | `ICloudSyncWindowService` / `CloudSyncWindowService` — modal dialog launcher |
| **DI Refactor** | Extracted `RegisterBackendApiClient()` from `RegisterDualWriteServices()` — HttpClient + FundSyncApiClient available whenever BackendApiUrl is configured |
| **Repository** | Added `GetByCompanyNameFilterAsync` to `IFundProfileRepository` + both implementations |
| **Full History Path** | Uses only `POST /api/funds/full-sync`: sends `ApiFundFullSyncProfileMetadataDto` (insert-if-not-exists) + `ApiFundFullHistoryRecordDto[]` (sparse upsert — Capital/NumberOfOwners/Risk/SharpeRatio/StandardDeviation synced, Nav/NavDate never overwritten). Phase 1 `POST /api/funds/list` removed from CloudSync (still used by DualWrite crawl path). |
| **Tests** | 7 unit tests in `CloudSyncService_SyncAsyncTests` (Phase 1 list test removed) |
| **Docs** | [CLOUD-SYNC.md](docs/CLOUD-SYNC.md) — feature overview, sync phases, Mermaid sequence diagram |

### HTTP 429 Rate-Limit Handling ✅ COMPLETED (2026-03-04)

Rate-limit awareness across all Backend API interactions. `FundSyncApiClient` retries 429 responses with exponential backoff (2s, 4s, 8s), respects `Retry-After` header. GUI clearly shows when rate-limited.

| Component | Details |
| --------- | ------- |
| **Application** | `RateLimitedException` — marker exception thrown after retries exhausted |
| **Infrastructure** | `FundSyncApiClient` — `SendWithRetryAsync` with 3 retries + exponential backoff |
| **Infrastructure** | `CloudSyncService` — catches rate-limit, reports "Rate limited" phase, 10s cooldown |
| **Infrastructure** | `DualWriteFundIngestionService` / `DualWriteChartIngestionService` — catches rate-limit, publishes distinct status bar message |
| **Presentation** | Default throttle bumped from 500ms to 1200ms (~50 req/min, under 60/min limit) |
| **Backend** | Added `Retry-After: 5` header to 429 responses |
| **Tests** | 6 retry tests in `FundSyncApiClient_RetryTests`, 2 rate-limit tests in DualWrite test classes, fixed 8 pre-existing `CloudSyncService` test failures (missing `IsConfigured` mock) |

### Future Enhancements (Planned)

| Feature | Status | Notes |
| --------- | -------- | ------- |
| Cloud Sync Integration | ✅ | DualWrite provider: decorator pattern on ingestion services, fire-and-forget Backend API sync, status bar indicator with Rx.NET |
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
| OpenAI API | ✅ Configured | Embeddings (text-embedding-3-small) + Chat (gpt-4.1-mini, default) |
| Groq API | ✅ Configured | Optional free tier LLM (llama-3.3-70b-versatile) |
| Azure App Service | ✅ Ready | Backend API hosting (F1 Free tier) |
| Azure Static Web Apps | ✅ Ready | Frontend hosting (Free tier) |
| Application Insights | ✅ Ready | Monitoring configured (free tier) |
| Azure Key Vault | ✅ Ready | Secrets management via Managed Identity |
| CI/CD Workflows | ✅ Complete | Backend deploy, Frontend deploy, PR checks |
| Production Deployment | ✅ Ready | Complete deployment documentation |
| Cosmos DB Vector Database | ✅ Production Ready | Optional persistent vector storage via `cosmosdb` verb. **Local + Production deployed**: Backend API, authentication, Managed Identity RBAC, Key Vault secrets, embeddings uploaded. Free tier (1000 RU/s, 25GB) |
| Azure SQL Database | ✅ Production Ready | Sweden Central. Free tier, serverless, auto-pause. Microsoft Entra-only auth, Managed Identity. Connection string in Key Vault. **Backend EF Core integrated**: FundProfiles + FundHistoryRecords tables, auto-migration, fund data sync endpoints (`/api/funds/list`, `/api/funds/about`). |

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
| Domain Layer Tests | ✅ Complete | CosineSimilarityCalculator (6 tests, deprecated), UserQuestionSanitizer (13 tests), models, value objects, IsinId (8 tests), FundHistoryRecordId (7 tests) |
| ApplicationCore Tests | ✅ Complete | QuestionAnsweringService (10 tests), FundSyncService (17 tests), RAG pipeline orchestration |
| Infrastructure Tests | ✅ Complete | InMemorySemanticSearch (5 tests), DocumentChunkMapper (4 tests), VectorStore integration, EfCoreFundProfileRepository (9 tests), EfCoreFundHistoryRepository (12 tests) |
| Validation Tests | ✅ Complete | SafeQuestionAttribute (8 tests), prompt injection defense |
| Integration Tests | ✅ Complete | Full pipeline tests (6 tests), end-to-end validation |
| Controller Tests | ❌ Not Implemented | AskController, health checks |
| **Total Backend Tests** | **✅ 140 Complete** | 140 tests passing (includes VectorStore migration + Fund Data sync + EF Core repository + FundDataPlugin integration tests) |

### Frontend

| Test Suite | Status | Coverage |
| ------------- | -------- | ---------- |
| page.test.tsx | ✅ Complete | Homepage rendering, example queries |
| api-streaming.test.ts | ✅ Complete | SSE parsing, callback ordering, error handling, split chunks |

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
- ✅ Add streaming responses for better UX - Completed 2026-03-14: SSE streaming via `POST /api/ask/stream`, token-by-token rendering with blinking cursor
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
- **OpenAI Chat (gpt-4.1-mini)**: ~$0.50/month (100 questions/day estimate, ~$0.15 per 1M input tokens)
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
