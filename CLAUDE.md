# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## CRITICAL: Status Tracking

**Source of truth for project status: [Status.md](Status.md)**

Before starting any work:

1. Review Status.md to understand what's implemented, in-progress, or planned
2. During implementation: Update relevant status from ❌ to ⏳ (in progress)
3. After completion: Update status to ✅ and document new features
4. Always maintain accuracy: Keep Status.md synchronized with actual codebase state
5. Update timestamp: Change "Last Updated" date when making significant updates
6. Keep costs low: When suggesting infrastructure, tiers, or services, prioritize free/low-cost options (Azure free tier, free APIs). Only suggest paid upgrades if strictly necessary and mention the cost impact.

**Do not duplicate work.** Always verify current status before implementing features.

## Project Overview

Fund Factsheet Q&A Generator - A full-stack RAG application enabling semantic search and AI-powered question answering over PDF documents (investment fund factsheets/PRIIP KID documents).

**Project Hosting:**

- **Repository:** GitHub (personal, public)
- **Deployment:** Azure (private infrastructure)
- **Services:** Azure App Service (backend), Azure Static Web Apps (frontend), Application Insights, Key Vault

## Documentation

| Document | Description |
|----------|-------------|
| [Status.md](Status.md) | Implementation progress tracker (check first!) |
| [README.md](README.md) | Project overview, quick start, architecture |
| [Preprocessor/README.md](Preprocessor/README.md) | PDF processing and embedding options |
| [backend/README.md](backend/README.md) | Semantic search & Q&A API setup, troubleshooting |
| [frontend/README.md](frontend/README.md) | Next.js chat UI, scripts, testing |
| [docs/SECRETS-MANAGEMENT.md](docs/SECRETS-MANAGEMENT.md) | Environment variables, API keys, configuration |
| [docs/AZURE-DEPLOYMENT.md](docs/AZURE-DEPLOYMENT.md) | Production deployment guide |

## Build & Run Commands

### Preprocessor

```bash
cd Preprocessor/Preprocessor

# Run with OpenAI (default - requires OPENAI_API_KEY env var)
$env:OPENAI_API_KEY = "sk-..."
dotnet run

# Run with Ollama (local, free)
dotnet run -- --provider ollama --embedding-model nomic-embed-text

# Custom input/output paths
dotnet run -- -i ./custom/pdfs -o ./custom/embeddings.json

# Append more PDFs to existing embeddings
dotnet run -- -i ./new-pdfs --append

# Run tests
dotnet test Preprocessor.Tests/Preprocessor.Tests.csproj
```

### Backend

```bash
cd backend

# Set API keys (local development)
cd Backend.API

# Option 1: Use OpenAI (default, recommended)
dotnet user-secrets set "BackendOptions:OpenAIApiKey" "sk-..."
dotnet user-secrets set "BackendOptions:LlmProvider" "OpenAI"

# Option 2: Use Groq (free tier alternative)
dotnet user-secrets set "BackendOptions:OpenAIApiKey" "sk-..."  # Still needed for embeddings
dotnet user-secrets set "BackendOptions:GroqApiKey" "gsk_..."
dotnet user-secrets set "BackendOptions:LlmProvider" "Groq"

# Copy embeddings from Preprocessor
cp ../../Preprocessor/Preprocessor/output.json Data/embeddings.json

# Run development server (starts at http://localhost:5000)
dotnet run

# Run tests
dotnet test Backend.Tests/Backend.Tests.csproj

# Build for production
dotnet publish Backend.API/Backend.API.csproj --configuration Release --output ./publish
```

**API Endpoints:**

- `POST /api/ask` - Ask a question (`{ "question": "..." }`)
- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe
- `GET /swagger` - API documentation (development only)

### Frontend

```bash
cd frontend

# Install dependencies
npm install

# Set API URL
cp .env.example .env.local
# Edit: NEXT_PUBLIC_API_URL=http://localhost:5000

# Development server (http://localhost:3000)
npm run dev

# Lint & format
npm run lint
npm run format:check

# Run tests
npm test
npm run test:coverage

# Build for production (static export)
npm run build
```

## Architecture

### Data Flow

```
PDF Files → Preprocessor → embeddings.json → Backend → Frontend
                ↓                              ↓
         Extract text +                  REST API:
         Generate embeddings             1. Embed query (OpenAI)
         (Ollama/OpenAI)                 2. Semantic search (cosine similarity)
                                         3. Context + question → LLM (OpenAI/Groq)
                                         4. Return answer + sources
```

### Key Services

**Backend:**

- `MemoryService` - Loads embeddings.json, performs cosine similarity search
- `QuestionAnsweringService` - Orchestrates embedding generation, retrieval, and LLM response
- Uses Semantic Kernel with OpenAI embeddings (text-embedding-3-small)
- Configurable LLM provider: OpenAI (gpt-4o-mini, default) or Groq (llama-3.3-70b-versatile, optional)

**Frontend:**

- `ChatInterface` - Main orchestrator component managing messages, loading state, error handling
- `lib/api.ts` - Type-safe API client for backend communication

### Configuration

**Backend config** (`appsettings.json` / User Secrets / Environment Variables):

- `BackendOptions:LlmProvider` - LLM provider selection ("OpenAI" or "Groq", default: "OpenAI")
- `BackendOptions:OpenAIApiKey` - OpenAI API key for embeddings and chat (required)
- `BackendOptions:OpenAIChatModel` - OpenAI chat model (default: "gpt-4o-mini")
- `BackendOptions:GroqApiKey` - Groq API key for LLM (only if using Groq provider)
- `BackendOptions:EmbeddingsFilePath` - Path to embeddings.json (default: `Data/embeddings.json`)

**Frontend config** (`.env.local`):

- `NEXT_PUBLIC_API_URL` - Backend API URL

## Important Notes

1. **Embedding Compatibility:** Production backend uses OpenAI embeddings. Preprocessor must use `--provider openai` when generating embeddings for production deployment.

2. **Volatile Memory:** Embeddings are loaded in-memory on startup. Server restart reloads from embeddings.json (no persistence by design for MVP).

3. **CI/CD:** Three GitHub workflows in `.github/workflows/`:
   - `pr-checks.yml` - Lint, test, build validation on PRs
   - `deploy-backend.yml` - Deploy to Azure App Service
   - `deploy-frontend.yml` - Deploy to Azure Static Web Apps

4. **CORS:** Backend allows localhost:3000, localhost:3001 for development.

## Testing Guidelines

### Framework & Libraries

**Backend (.NET/C#):**

- **Test Framework:** NUnit
- **Mocking:** AutoFixture.AutoMoq (automatic mock creation)
- **Test Data:** AutoFixture (generates realistic test data)

**Required NuGet Packages:**

```bash
dotnet add package NUnit
dotnet add package NUnit3TestAdapter
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package AutoFixture
dotnet add package AutoFixture.AutoMoq
dotnet add package Moq
```

### Test Structure (AAA Pattern)

Always structure tests using the **Arrange, Act, Assert** pattern:

```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var fixture = new Fixture().Customize(new AutoMoqCustomization());
    var dependency = fixture.Freeze<Mock<IDependency>>();
    dependency.Setup(x => x.Method()).Returns("expected");

    var sut = fixture.Create<ServiceUnderTest>(); // System Under Test

    // Act - Execute the method being tested
    var result = sut.MethodToTest();

    // Assert - Verify expected outcomes
    Assert.That(result, Is.EqualTo("expected"));
    dependency.Verify(x => x.Method(), Times.Once);
}
```

### AutoFixture Best Practices

#### 1. Always Resolve SUT from AutoFixture

**Do this:**

```csharp
private IFixture _fixture;
private ServiceUnderTest _sut;

[SetUp]
public void SetUp()
{
    _fixture = new Fixture().Customize(new AutoMoqCustomization());

    // Configure dependencies
    _fixture.Freeze<Mock<IDependency>>();

    // Resolve SUT from fixture
    _sut = _fixture.Create<ServiceUnderTest>();
}
```

**Don't do this:**

```csharp
// ❌ Manually constructing SUT
_sut = new ServiceUnderTest(dependency1, dependency2);
```

#### 2. Create Custom ISpecimenBuilder for Complex Objects

When you need to reuse complex object creation logic:

```csharp
public class EmbeddingRecordBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(EmbeddingRecord))
        {
            return new EmbeddingRecord
            {
                Id = context.Create<string>(),
                Content = context.Create<string>(),
                Embedding = context.CreateMany<float>(1536).ToArray(),
                SourceFile = context.Create<string>(),
                PageNumber = context.Create<int>()
            };
        }

        return new NoSpecimen();
    }
}

// Usage in test setup
_fixture.Customizations.Add(new EmbeddingRecordBuilder());
var record = _fixture.Create<EmbeddingRecord>();
```

#### 3. Create Custom ICustomization for Reusable Configurations

Group related customizations into a single reusable class:

```csharp
public class BackendDomainCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Add custom specimen builders
        fixture.Customizations.Add(new EmbeddingRecordBuilder());
        fixture.Customizations.Add(new AskRequestBuilder());

        // Configure specific behaviors
        fixture.Register<IEmbeddingRepository>(() =>
            fixture.Freeze<Mock<IEmbeddingRepository>>().Object);

        // Set default values
        fixture.Inject(new BackendOptions
        {
            OpenAIApiKey = "test-key",
            LlmProvider = LlmProvider.OpenAI
        });
    }
}

// Usage in test class
[SetUp]
public void SetUp()
{
    _fixture = new Fixture()
        .Customize(new AutoMoqCustomization())
        .Customize(new BackendDomainCustomization());
}
```

#### 4. Use Freeze for Shared Dependencies

When multiple tests need the same mock instance:

```csharp
[SetUp]
public void SetUp()
{
    _fixture = new Fixture().Customize(new AutoMoqCustomization());

    // Freeze creates a singleton instance that all Create<T>() calls will reuse
    _loggerMock = _fixture.Freeze<Mock<ILogger<MyService>>>();
    _repositoryMock = _fixture.Freeze<Mock<IRepository>>();

    _sut = _fixture.Create<MyService>(); // Uses frozen mocks
}

[Test]
public void Test_UsesSharedMocks()
{
    // Arrange
    _repositoryMock.Setup(x => x.GetData()).Returns("data");

    // Act & Assert
    var result = _sut.ProcessData();
    _loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.AtLeastOnce);
}
```

### Example Test Class Template

```csharp
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;

namespace Backend.Tests.ApplicationCore.Services
{
    [TestFixture]
    public class QuestionAnsweringServiceTests
    {
        private IFixture _fixture;
        private Mock<IMemoryService> _memoryServiceMock;
        private Mock<ILogger<QuestionAnsweringService>> _loggerMock;
        private QuestionAnsweringService _sut;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture()
                .Customize(new AutoMoqCustomization())
                .Customize(new BackendDomainCustomization());

            // Freeze dependencies for reuse across tests
            _memoryServiceMock = _fixture.Freeze<Mock<IMemoryService>>();
            _loggerMock = _fixture.Freeze<Mock<ILogger<QuestionAnsweringService>>>();

            // Resolve SUT from fixture
            _sut = _fixture.Create<QuestionAnsweringService>();
        }

        [Test]
        public async Task AskQuestionAsync_ValidQuestion_ReturnsAnswer()
        {
            // Arrange
            var question = _fixture.Create<string>();
            var searchResults = _fixture.CreateMany<EmbeddingRecord>(3).ToList();

            _memoryServiceMock
                .Setup(x => x.SearchAsync(question, It.IsAny<int>()))
                .ReturnsAsync(searchResults);

            // Act
            var result = await _sut.AskQuestionAsync(question);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Answer, Is.Not.Empty);
            _memoryServiceMock.Verify(x => x.SearchAsync(question, 5), Times.Once);
        }

        [Test]
        public async Task AskQuestionAsync_EmptyQuestion_ThrowsArgumentException()
        {
            // Arrange
            var emptyQuestion = string.Empty;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(
                async () => await _sut.AskQuestionAsync(emptyQuestion));

            Assert.That(ex.ParamName, Is.EqualTo("question"));
        }
    }
}
```

### Naming Conventions

Test method names should follow: `MethodName_Scenario_ExpectedBehavior`

Examples:

- `AskQuestionAsync_ValidQuestion_ReturnsAnswer`
- `SearchAsync_EmptyQuery_ThrowsArgumentException`
- `LoadEmbeddings_InvalidFile_LogsWarningAndReturnsEmpty`
