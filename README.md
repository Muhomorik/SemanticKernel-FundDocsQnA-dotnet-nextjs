# PDF Q&A Application

A zero-cost hobby project for asking questions about PDF documents using AI. Built with .NET, Next.js, Semantic Kernel, and cloud AI services.

## Architecture

```plaintext
PDFs ──► Preprocessor ──► embeddings.json ──► Backend API ◄── Frontend
              │                                    │
              │                                    ▼
         LM Studio /                        OpenAI Embeddings
         Ollama                             + Groq LLM
```

| Component | Tech Stack | Description |
|-----------|------------|-------------|
| [Preprocessor](Preprocessor/README.md) | .NET 9, PdfPig, Semantic Kernel | Extract text from PDFs, generate embeddings |
| [Backend](backend/README.md) | ASP.NET Core 9, Semantic Kernel | Semantic search + Q&A API |
| [Frontend](frontend/README.md) | Next.js 16, TypeScript, Tailwind, shadcn/ui | Chat interface |

## Quick Start

### Prerequisites

- .NET 9 SDK
- Node.js 18+
- LM Studio or Ollama (for preprocessing)
- API keys: [Groq](https://console.groq.com) (free), [OpenAI](https://platform.openai.com)

### 1. Generate Embeddings

```bash
cd Preprocessor/Preprocessor
# Add PDFs to ./pdfs folder
dotnet run
```

See [Preprocessor README](Preprocessor/README.md) for details.

### 2. Start Backend

```bash
cd backend/Backend.API
dotnet user-secrets set "BackendOptions:GroqApiKey" "your-key"
dotnet user-secrets set "BackendOptions:OpenAIApiKey" "your-key"
cp ../../Preprocessor/Preprocessor/bin/Debug/net9.0/output.json Data/embeddings.json
dotnet run
```

API available at `http://localhost:5000`. See [Backend README](backend/README.md) for details.

### 3. Start Frontend

```bash
cd frontend
npm install
cp .env.example .env.local
npm run dev
```

Visit `http://localhost:3000`. See [Frontend README](frontend/README.md) for details.

## Project Structure

```plaintext
.
├── Preprocessor/          # PDF text extraction & embeddings
├── backend/               # ASP.NET Core API
│   ├── Backend.API/       # Main API project
│   └── Backend.Tests/     # Unit tests
├── frontend/              # Next.js web app
├── docs/                  # Deployment & secrets guides
│   ├── AZURE-DEPLOYMENT.md
│   └── SECRETS-MANAGEMENT.md
├── azure-setup.sh         # Azure deployment script
└── Status.md              # Project status tracker
```

## Deployment

Deploy to Azure with near-zero cost (~$0.03/month):

| Resource | Tier | Cost |
|----------|------|------|
| Azure App Service | F1 Free | $0 |
| Application Insights | Free (5GB) | $0 |
| Azure Key Vault | Standard | ~$0.03 |
| OpenAI Embeddings | Pay-per-use | ~$0.003 |
| Groq LLM | Free tier | $0 |

**[Full Deployment Guide](docs/AZURE-DEPLOYMENT.md)**

Quick deploy:

```bash
./azure-setup.sh  # Creates Azure resources
# Configure GitHub secret: AZURE_WEBAPP_PUBLISH_PROFILE
git push origin main  # Triggers deployment
```

## Secrets Management

Three-tier strategy for secure secret handling:

| Environment | Method | Details |
|-------------|--------|---------|
| Local Dev | .NET User Secrets | `dotnet user-secrets set "key" "value"` |
| Production | Azure Key Vault | Accessed via Managed Identity |
| CI/CD | GitHub Secrets | For deployment credentials |

**[Full Secrets Guide](docs/SECRETS-MANAGEMENT.md)**

## Documentation

- [Project Status](Status.md) - Implementation progress and known issues
- [Azure Deployment](docs/AZURE-DEPLOYMENT.md) - Complete Azure setup guide
- [Secrets Management](docs/SECRETS-MANAGEMENT.md) - API key handling
- [Backend API](backend/README.md) - API endpoints and configuration
- [Frontend](frontend/README.md) - Development and testing
- [Preprocessor](Preprocessor/README.md) - PDF processing options

## Tech Stack

- **Preprocessor**: .NET 9, PdfPig, Semantic Kernel, Ollama/LM Studio
- **Backend**: ASP.NET Core 9, Semantic Kernel, OpenAI, Groq, Application Insights
- **Frontend**: Next.js 16, TypeScript, Tailwind CSS, shadcn/ui, Jest

## License

This is a hobby project for learning Semantic Kernel and AI integration.
