# PDF Q&A Application - Implementation Status

Last Updated: 2026-01-02 (Migrated to Semantic Kernel VectorStore for vector search)

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

## Part 1: Preprocessor ✅ COMPLETED

### Implementation Status

| Component | Status | Notes |
|-----------|--------|-------|
| Console Application | ✅ Complete | .NET 9 with CommandLineParser |
| PdfPig Extraction | ✅ Complete | Word-based text extraction with smart chunking |
| Embedding Generation | ✅ Complete | Supports Ollama, LM Studio, and OpenAI |
| OllamaEmbeddingService | ✅ Complete | With connection testing and error handling |
| JSON Export | ✅ Complete | Structured format with id, text, embedding, source, page |
| CLI Options | ✅ Complete | All parameters implemented and validated |
| Unit Tests | ✅ Complete | NUnit tests for services and extraction |
| Documentation | ✅ Complete | README with usage examples |

### Features Implemented

- ✅ Extract text from PDFs using PdfPig
- ✅ Generate embeddings using Ollama (nomic-embed-text)
- ✅ Generate embeddings using LM Studio (OpenAI-compatible API)
- ✅ Generate embeddings using OpenAI (text-embedding-3-small)
- ✅ Export to JSON format
- ✅ Append mode for incremental processing
- ✅ Configurable chunking and models
- ✅ Comprehensive error handling and logging
- ✅ Provider abstraction (Ollama/LM Studio/OpenAI)
- ✅ Secure API key management (environment variables + CLI args)

### Planned Features

- ⏳ **Token usage tracking and cost monitoring:**
  - Extract token counts from OpenAI embedding API responses
  - Calculate and log estimated API costs per batch
  - Structured logging to console for development monitoring
  - Custom metrics to Application Insights for production monitoring

### Not Planned

- ❌ Other cloud embedding providers (Azure OpenAI, etc.)

---

## Part 2: Backend API ✅ COMPLETED (Production-Ready)

### Implementation Status

| Component | Status | Notes |
|-----------|--------|-------|
| Solution Structure | ✅ Complete | Separate Backend.sln with API and Tests projects |
| DDD Architecture | ✅ Complete | Domain-Driven Design with layered architecture |
| Domain Layer | ✅ Complete | Pure business logic (models, value objects, interfaces, services) |
| ApplicationCore Layer | ✅ Complete | Use cases and orchestration (DTOs, services) |
| Infrastructure Layer | ✅ Complete | External dependencies (LLM providers, repository, search) |
| Configuration | ✅ Complete | BackendOptions + LLM-specific interfaces (IOpenAiConfiguration, IGroqConfiguration) |
| DocumentRepository | ✅ Complete | File-based repository with domain model mapping |
| QuestionAnsweringService | ✅ Complete | RAG pipeline orchestration in ApplicationCore |
| LLM Provider Abstraction | ✅ Complete | Factory pattern for OpenAI/Groq provider selection |
| AskController | ✅ Complete | POST /api/ask endpoint with ApplicationCore DTOs |
| Health Checks | ✅ Complete | ASP.NET Core health checks (/health/live, /health/ready) |
| Program.cs | ✅ Complete | DDD layers DI registration, Application Insights, Key Vault |
| Semantic Kernel Setup | ✅ Complete | OpenAI embeddings + configurable LLM providers |
| CORS Configuration | ✅ Complete | Configured for Next.js frontend |
| Swagger/OpenAPI | ✅ Complete | Auto-generated API docs |
| Application Insights | ✅ Complete | Monitoring for production (free tier) |
| Secrets Management | ✅ Complete | User Secrets (local), Azure Key Vault (prod), GitHub Secrets (CI/CD) |
| Azure Deployment | ✅ Complete | App Service F1, CI/CD with GitHub Actions |
| Documentation | ✅ Complete | README with DDD architecture details |
| Unit Tests | ✅ Complete | Comprehensive test coverage for services and infrastructure |

### Features Implemented

**Core Functionality:**

- ✅ Load embeddings.json on startup via DocumentRepository
- ✅ Initialize in-memory vector store with embeddings
- ✅ Semantic search using OpenAI embeddings (text-embedding-3-small)
- ✅ Configurable LLM provider: OpenAI (gpt-4o-mini, default) or Groq (llama-3.3-70b-versatile, optional)
- ✅ Environment variable support (LLM_PROVIDER, GROQ_API_KEY, OPENAI_API_KEY, EMBEDDINGS_PATH)
- ✅ Error handling and logging
- ✅ Source references in responses

**DDD Architecture Benefits:**

- ✅ **Domain Layer**: Pure business logic with zero external dependencies
  - DocumentChunk, EmbeddingVector, DocumentMetadata value objects
  - ILlmProvider, IDocumentRepository, ISemanticSearch abstractions
  - CosineSimilarityCalculator domain service (deprecated - replaced by VectorStore)
- ✅ **ApplicationCore Layer**: Use case orchestration
  - QuestionAnsweringService coordinates RAG pipeline
  - DTOs for API contracts (AskQuestionRequest, AskQuestionResponse)
- ✅ **Infrastructure Layer**: External integrations
  - OpenAiProvider and GroqProvider implementations
  - LlmProviderFactory for runtime provider selection
  - FileBasedDocumentRepository for embeddings persistence
  - SemanticKernelEmbeddingGenerator adapter
  - InMemorySemanticSearch using Semantic Kernel VectorStore
- ✅ **Clear separation of concerns**: Easy to test, swap providers, maintain code

### Production-Ready Features ✅

- ✅ ASP.NET Core Health Checks (liveness + readiness probes)
- ✅ OpenAI embeddings for query generation
- ✅ Application Insights telemetry (free tier, 5GB/month)
- ✅ Azure Key Vault integration (production secrets via Managed Identity)
- ✅ GitHub Actions CI/CD pipeline (.github/workflows/deploy-backend.yml)
- ✅ Azure App Service deployment ready (F1 free tier)
- ✅ Azure setup script (backend/azure-setup.sh)
- ✅ Comprehensive deployment documentation
- ✅ Secrets management guide

### Production Deployment Ready

**Azure Resources Created:**

- Azure App Service (F1 Free tier) - Zero-cost hosting
- Application Insights - Free tier monitoring (5GB/month)
- Azure Key Vault - Secure secrets management (~$0.03/month)
- Managed Identity - Secure access to Key Vault
- GitHub Actions - Automated CI/CD

**Total Monthly Cost: ~$0.03**

### Planned Features

- ⏳ **Token usage tracking and cost monitoring:**
  - Extract token counts from LLM responses via `ChatMessageContent.InnerContent` property
  - Track both chat completion tokens (OpenAI/Groq) and embedding tokens
  - Calculate estimated API costs per request based on provider pricing
  - Structured logging to console for development monitoring
  - Custom metrics to Application Insights for production cost analysis
  - Support for both OpenAI (gpt-4o-mini) and Groq (llama-3.3-70b-versatile) providers

### Input Validation & Prompt Injection Protection ✅ (2026-01-01)

**Defense-in-Depth Security Implementation:**

| Component | Status | Details |
| --- | --- | --- |
| Input Validation | ✅ Complete | ASP.NET Core Data Annotations: [MaxLength(500)], [Required], [MinLength(3)] |
| Custom Validation | ✅ Complete | [SafeQuestion] attribute detects: "IGNORE PREVIOUS", "SYSTEM:", special tokens, excessive repetition |
| Input Sanitization | ✅ Complete | Domain service removes control chars, normalizes whitespace/newlines |
| System Prompt | ✅ Complete | Environment-based with factory pattern, hardened default prompt with anti-jailbreak instructions |
| Rate Limiting | ✅ Complete | 10 requests per minute per IP, 2 request queue, IP-based partitioning |
| Request Size Limits | ✅ Complete | Kestrel configured for 10KB max body size |

**Test Coverage:**

- ✅ 13 unit tests for UserQuestionSanitizer (control chars, whitespace, newlines, injection attempts)
- ✅ 8 unit tests for SafeQuestionAttribute validation (patterns, tokens, repetition)
- ✅ 6 integration tests for full pipeline (jailbreak, role-play, control chars, newlines, tokens, legitimate questions)
- ✅ Test infrastructure with AutoFixture builders and customizations
- ✅ All 51 tests passing

**Security Features:**

- Input validated at DTO level with ASP.NET Core pipeline
- Sanitization applied before semantic search and LLM processing
- XML-delimited prompts with explicit security instructions
- Rate limiting prevents DoS via request flooding
- Request size limits prevent DoS via large payloads

### Not Yet Implemented

- ❌ Integration tests for controller endpoints
- ❌ Caching layer
- ❌ Authentication/Authorization
- ❌ Indirect prompt injection detection (via PDF content)

---

## Part 3: Frontend ✅ COMPLETED

**Current Phase:** All core features implemented - chat interface with theme support, responsive design, and error handling

**IMPORTANT for AI Agents:** When working on frontend features or UI components, you MUST use the [Frontend Design Plugin](https://github.com/anthropics/claude-code/blob/main/plugins/frontend-design/README.md) to ensure production-grade design quality and avoid generic AI aesthetics.

### Implementation Status

| Component | Status | Notes |
|-----------|--------|-------|
| Next.js 16 Project Setup | ✅ Complete | App Router, TypeScript, Tailwind CSS, ESLint |
| EditorConfig | ✅ Complete | .editorconfig for consistent formatting |
| Prettier Configuration | ✅ Complete | .prettierrc and .prettierignore with Tailwind plugin |
| Environment Configuration | ✅ Complete | .env.local for API URL, .env.example template |
| shadcn/ui Integration | ✅ Complete | Initialized with New York style, Neutral theme |
| Basic UI Components | ✅ Complete | 8 shadcn components installed |
| Project Structure | ✅ Complete | App directory, components, lib utilities |
| API Service Client | ✅ Complete | Type-safe API client in lib/api.ts |
| Homepage Placeholder | ✅ Complete | Basic landing page |
| Root Layout | ✅ Complete | Inter font, metadata, global styles |
| Frontend README | ✅ Complete | Basic setup instructions and tech stack |
| Testing Infrastructure | ✅ Complete | Jest + React Testing Library |
| Sample Test | ✅ Complete | Homepage test with 4 passing tests |
| Theme Toggle (Light/Dark) | ✅ Complete | next-themes integration |
| Header Component | ✅ Complete | Title + theme toggle |
| Footer Component | ✅ Complete | GitHub link + tech stack info |
| Chat Interface Component | ✅ Complete | Main orchestrator |
| ChatMessage Component | ✅ Complete | User/AI messages with sources |
| ChatInput Component | ✅ Complete | Textarea + submit button |
| ExampleQueries Component | ✅ Complete | Clickable example questions |
| Loading States | ✅ Complete | Skeleton loading |
| Error Handling UI | ✅ Complete | Alert with retry |
| Responsive Design | ✅ Complete | Mobile-first approach |

### Features Implemented

- ✅ Next.js 16 project with App Router
- ✅ TypeScript configuration
- ✅ Tailwind CSS styling
- ✅ EditorConfig for consistent editor settings
- ✅ Prettier with Tailwind CSS plugin
- ✅ shadcn/ui component library (New York style, Neutral theme)
- ✅ Environment variable configuration (.env.local, .env.example)
- ✅ API service client with type safety
- ✅ Custom error handling (ApiError class)
- ✅ Health check utility
- ✅ Basic project structure and documentation
- ✅ 8 UI components installed (button, input, card, textarea, skeleton, alert, badge, separator)
- ✅ Jest testing framework
- ✅ React Testing Library
- ✅ Sample test with 3 passing tests

### Planned Features

- ✅ Next.js application setup
- ✅ Light/dark theme toggle
- ✅ Chat interface component
- ✅ Question input and submit
- ✅ Answer display with source references
- ✅ Loading states (skeleton)
- ✅ Error handling with retry
- ✅ Responsive design (mobile-first)
- ✅ Example queries (clickable)
- ✅ Footer with GitHub link and tech info

### Planned Enhancements

- ❌ Handle 503 Service Unavailable errors (show user-friendly message explaining Azure free tier quota limits)

### Not Planned

- No file upload functionality
- No authentication
- No chat persistence
- No user accounts

---

## Infrastructure & Deployment

### Current State

| Component | Status | Notes |
|-----------|--------|-------|
| Local Development | ✅ Working | Preprocessor and Backend run locally |
| OpenAI API | ✅ Configured | Embeddings (text-embedding-3-small) + Chat (gpt-4o-mini, default) |
| Groq API | ✅ Configured | Optional free tier LLM (llama-3.3-70b-versatile) |
| Azure App Service | ✅ Ready | Backend API hosting (F1 Free tier) |
| Azure Static Web Apps | ✅ Ready | Frontend hosting (Free tier) |
| Application Insights | ✅ Ready | Monitoring configured (free tier) |
| Azure Key Vault | ✅ Ready | Secrets management via Managed Identity |
| CI/CD Workflows | ✅ Complete | Backend deploy, Frontend deploy, PR checks |
| Production Deployment | ✅ Ready | Complete deployment documentation |

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
|-------------|--------|----------|
| PreprocessorServiceTests | ✅ Complete | Core functionality |
| OllamaEmbeddingServiceTests | ✅ Complete | Service integration |
| PdfPigExtractorTests | ⚠️ Partial | Basic tests |

### Backend

| Test Suite | Status | Coverage |
|-------------|--------|----------|
| Domain Layer Tests | ✅ Complete | CosineSimilarityCalculator (6 tests, deprecated), UserQuestionSanitizer (13 tests), models, value objects |
| ApplicationCore Tests | ✅ Complete | QuestionAnsweringService (10 tests), RAG pipeline orchestration |
| Infrastructure Tests | ✅ Complete | InMemorySemanticSearch (5 tests), DocumentChunkMapper (4 tests), VectorStore integration |
| Validation Tests | ✅ Complete | SafeQuestionAttribute (8 tests), prompt injection defense |
| Integration Tests | ✅ Complete | Full pipeline tests (6 tests), end-to-end validation |
| Controller Tests | ❌ Not Implemented | AskController, health checks |
| **Total Backend Tests** | **✅ 69 Complete** | 69 tests passing (includes VectorStore migration) |

### Frontend

| Test Suite | Status | Coverage |
|-------------|--------|----------|
| page.test.tsx | ✅ Complete | Homepage rendering, example queries |

---

## Documentation Status

| Document | Status | Location |
|----------|--------|----------|
| Project Plan | ✅ Complete | `C:\Users\dmitr\Documents\Projects_Docs\PDF-QA-App-Plan.md` |
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

- ✅ ~~Migrate to modern Vector Store abstractions~~ - Completed 2026-01-02: Using InMemoryVectorStore with VectorStoreCollection for built-in cosine similarity
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

### Upgrade Options

- **Azure App Service B1**: ~$13/month (always-on, no cold starts, custom domains)
- **Application Insights Pay-as-you-go**: $2.30/GB after 5GB free tier
