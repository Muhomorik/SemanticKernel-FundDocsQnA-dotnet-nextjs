# PDF Q&A Application - Implementation Status

Last Updated: 2026-01-11 (Cosmos DB Production Deployment Complete)

**Tech Stack:**

- Preprocessor: .NET 9 Console App + PdfPig + Semantic Kernel
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
| PDF Text Extraction | ✅ | PdfPig with word-based smart chunking |
| Embedding Generation | ✅ | Supports Ollama, LM Studio, OpenAI |
| JSON Export | ✅ | Structured format (id, text, embedding, source, page) |
| Append Mode | ✅ | Incremental processing of new PDFs |
| CLI Options | ✅ | All parameters implemented and validated |
| Provider Abstraction | ✅ | Ollama/LM Studio/OpenAI with secure API key management |
| Unit Tests | ✅ | NUnit tests for services and extraction |
| Documentation | ✅ | README with usage examples |

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
3. No request throttling or rate limiting
4. Missing unit tests for DDD layers
5. More files and abstractions due to DDD structure (trade-off for maintainability)

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
