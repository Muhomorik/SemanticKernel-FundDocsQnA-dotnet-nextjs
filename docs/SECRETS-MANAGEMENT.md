# Configuration & Secrets Guide

> Complete reference for all environment variables, secrets, and configuration across the PDF Q&A Application.

## Quick Reference

### Secrets (API Keys)

| Key | Environment Variable | Where to Get | Local Dev | Production |
|-----|---------------------|--------------|-----------|------------|
| `BackendOptions:GroqApiKey` | `GROQ_API_KEY` | [console.groq.com](https://console.groq.com) | User Secrets | Azure Key Vault |
| `BackendOptions:OpenAIApiKey` | `OPENAI_API_KEY` | [platform.openai.com](https://platform.openai.com) | User Secrets | Azure Key Vault |

### Environment Variables

| Variable | Component | Purpose | Default |
|----------|-----------|---------|---------|
| `NEXT_PUBLIC_API_URL` | Frontend | Backend API URL | `http://localhost:5000` |
| `ASPNETCORE_ENVIRONMENT` | Backend | Runtime environment | `Development` |
| `EMBEDDINGS_PATH` | Backend | Path to embeddings file | `Data/embeddings.json` |
| `KEY_VAULT_NAME` | Backend (Prod) | Azure Key Vault name | - |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Backend (Prod) | Application Insights | - |

### GitHub Secrets (CI/CD)

| Secret | Purpose | Required |
|--------|---------|----------|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Azure App Service deployment credentials | Yes |

---

## API Keys

### Groq API Key

**Purpose:** LLM chat completion for answering questions (free tier available)

**Where to get:**

1. Sign up at [console.groq.com](https://console.groq.com)
2. Navigate to API Keys section
3. Create a new API key

**Local Development:**

```bash
cd backend/Backend.API
dotnet user-secrets set "BackendOptions:GroqApiKey" "gsk_your_key_here"
```

**Production (Azure Key Vault):**

```bash
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--GroqApiKey" \
  --value "gsk_your_key_here"
```

### OpenAI API Key

**Purpose:** Generate embeddings for semantic search (pay-per-use)

**Where to get:**

1. Sign up at [platform.openai.com](https://platform.openai.com)
2. Navigate to API Keys in your account settings
3. Create a new secret key

**Important:** Use the same embedding model (`text-embedding-3-small`) as used during preprocessing for vector space compatibility.

**Local Development:**

```bash
cd backend/Backend.API
dotnet user-secrets set "BackendOptions:OpenAIApiKey" "sk-proj-your_key_here"
```

**Production (Azure Key Vault):**

```bash
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--OpenAIApiKey" \
  --value "sk-proj-your_key_here"
```

---

## Environment Variables

### Backend

| Variable | Purpose | Default | Required |
|----------|---------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | Runtime mode (`Development`/`Production`) | `Development` | Yes |
| `EMBEDDINGS_PATH` | Override path to embeddings JSON file | `Data/embeddings.json` | No |
| `KEY_VAULT_NAME` | Azure Key Vault name for loading secrets | - | Production only |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights telemetry | - | Production only |

**Setting environment variables:**

```bash
# Windows (PowerShell)
$env:EMBEDDINGS_PATH = "C:\path\to\embeddings.json"

# Linux/macOS
export EMBEDDINGS_PATH="/path/to/embeddings.json"
```

### Frontend

| Variable | Purpose | Default | Public |
|----------|---------|---------|--------|
| `NEXT_PUBLIC_API_URL` | Backend API endpoint URL | `http://localhost:5000` | Yes (browser-accessible) |

**Setting frontend environment:**

```bash
# Copy example file
cp frontend/.env.example frontend/.env.local

# Edit .env.local
NEXT_PUBLIC_API_URL=http://localhost:5000
```

**Note:** Variables prefixed with `NEXT_PUBLIC_` are exposed to the browser. Never put secrets in `NEXT_PUBLIC_` variables.

### Azure App Service Settings

These are set automatically by `azure-setup.sh` or can be configured in Azure Portal:

| Setting | Value | Purpose |
|---------|-------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Enable production mode |
| `EMBEDDINGS_PATH` | `/home/site/wwwroot/Data/embeddings.json` | Azure file path |
| `KEY_VAULT_NAME` | `kv-funddocs-{id}` | Key Vault reference |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Auto-configured | Telemetry connection |

---

## GitHub Secrets

Required for CI/CD deployment via GitHub Actions.

### AZURE_WEBAPP_PUBLISH_PROFILE

**Purpose:** Deployment credentials for Azure App Service

**How to get:**

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your App Service
3. Click **Get publish profile** in the Overview section
4. Download the `.PublishSettings` file

**How to set:**

1. Go to your GitHub repository
2. Settings → Secrets and variables → Actions
3. Click **New repository secret**
4. Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
5. Value: Paste the entire contents of the `.PublishSettings` file

---

## Preprocessor CLI Options

The Preprocessor has no secrets - all configuration is via command-line arguments.

| Argument | Short | Default | Description |
|----------|-------|---------|-------------|
| `--method` | `-m` | `pdfpig` | PDF extraction method |
| `--input` | `-i` | `pdfs` | Input directory with PDF files |
| `--output` | `-o` | `output.json` | Output JSON file path |
| `--append` | `-a` | `false` | Append to existing output file |
| `--provider` | `-p` | `lmstudio` | Embedding provider (`ollama` or `lmstudio`) |
| `--embedding-model` | - | `nomic-embed-text` | Embedding model name |
| `--ollama-url` | - | Auto-detect | Provider endpoint URL override |

### Provider Defaults

| Provider | Default URL | API Endpoint |
|----------|-------------|--------------|
| LM Studio | `http://localhost:1234` | `/v1/embeddings` (OpenAI-compatible) |
| Ollama | `http://localhost:11434` | `/api/embed` (native) |

### Example Usage

```bash
# Using LM Studio (default)
dotnet run --project Preprocessor -- -i ./pdfs -o ./output.json

# Using Ollama
dotnet run --project Preprocessor -- --provider ollama -i ./pdfs -o ./output.json

# Custom provider URL
dotnet run --project Preprocessor -- --ollama-url http://localhost:8080 -i ./pdfs -o ./output.json
```

---

## Backend Configuration (appsettings.json)

Non-secret configuration options in `backend/Backend.API/appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `BackendOptions:EmbeddingsFilePath` | `Data/embeddings.json` | Path to embeddings file |
| `BackendOptions:GroqModel` | `llama-3.3-70b-versatile` | Groq LLM model for chat |
| `BackendOptions:GroqApiUrl` | `https://api.groq.com/openai/v1` | Groq API endpoint |
| `BackendOptions:OpenAIEmbeddingModel` | `text-embedding-3-small` | OpenAI embedding model |
| `BackendOptions:MaxSearchResults` | `10` | Number of chunks to retrieve |
| `BackendOptions:MemoryCollectionName` | `fund-documents` | Memory store collection name |

---

## Setup by Environment

### Local Development

1. **Set API keys using User Secrets:**

   ```bash
   cd backend/Backend.API
   dotnet user-secrets set "BackendOptions:GroqApiKey" "your-groq-key"
   dotnet user-secrets set "BackendOptions:OpenAIApiKey" "your-openai-key"
   ```

2. **Configure frontend:**

   ```bash
   cd frontend
   cp .env.example .env.local
   ```

3. **Verify secrets are set:**

   ```bash
   dotnet user-secrets list
   ```

**User Secrets location:**

- Windows: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

### Production (Azure)

Secrets are stored in Azure Key Vault and accessed via Managed Identity:

1. App Service has a System-Assigned Managed Identity
2. Managed Identity is granted "Key Vault Secrets User" role
3. App loads secrets at startup via `DefaultAzureCredential`

**Key Vault secret naming:** Use double dashes (`--`) instead of colons (`:`)

| Configuration Path | Key Vault Secret Name |
|-------------------|-----------------------|
| `BackendOptions:GroqApiKey` | `BackendOptions--GroqApiKey` |
| `BackendOptions:OpenAIApiKey` | `BackendOptions--OpenAIApiKey` |

**Update production secrets:**

```bash
# Update secret
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--GroqApiKey" \
  --value "new-key-value"

# Restart app to load new secrets
az webapp restart \
  --name "funddocs-backend-api" \
  --resource-group "rg-funddocs-backend"
```

### CI/CD (GitHub Actions)

1. Set `AZURE_WEBAPP_PUBLISH_PROFILE` in GitHub repository secrets
2. Push to `main` branch triggers deployment
3. Workflow builds and deploys to Azure App Service

---

## Configuration Priority

Backend configuration is loaded in this order (later values override earlier):

1. `appsettings.json` (defaults)
2. `appsettings.{Environment}.json` (environment-specific)
3. Azure Key Vault (production only, if `KEY_VAULT_NAME` is set)
4. User Secrets (development only)
5. Environment Variables
6. Command-line arguments

---

## Troubleshooting

### "GroqApiKey is not set" warning

**Cause:** User Secrets not configured or API key not set.

**Solution:**

```bash
cd backend/Backend.API
dotnet user-secrets set "BackendOptions:GroqApiKey" "your-key"
```

### "OpenAIApiKey is not set" warning

**Cause:** User Secrets not configured or API key not set.

**Solution:**

```bash
cd backend/Backend.API
dotnet user-secrets set "BackendOptions:OpenAIApiKey" "your-key"
```

### Production app can't access Key Vault

**Causes:**

1. Managed Identity not enabled on App Service
2. Managed Identity not granted access to Key Vault
3. `KEY_VAULT_NAME` environment variable not set

**Solution:**

```bash
# Check Managed Identity is enabled
az webapp identity show \
  --name "funddocs-backend-api" \
  --resource-group "rg-funddocs-backend"

# Grant Key Vault access if missing
PRINCIPAL_ID=$(az webapp identity show \
  --name "funddocs-backend-api" \
  --resource-group "rg-funddocs-backend" \
  --query principalId -o tsv)

az keyvault set-policy \
  --name "your-keyvault-name" \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### Frontend can't connect to backend

**Cause:** `NEXT_PUBLIC_API_URL` not set or incorrect.

**Solution:**

1. Check `.env.local` exists and contains correct URL
2. Restart the Next.js dev server after changing `.env.local`
3. Verify backend is running at the specified URL

### User Secrets not loading

**Check:**

1. `UserSecretsId` is set in `Backend.API.csproj`
2. Running in Development environment (`ASPNETCORE_ENVIRONMENT=Development`)
3. Secrets file exists and is valid JSON

**Verify:**

```bash
cd backend/Backend.API
dotnet user-secrets list
```

---

## Security Best Practices

### Do

- Use User Secrets for local development (never commit API keys)
- Use Azure Key Vault for production (secure, centralized)
- Rotate API keys regularly (every 90 days recommended)
- Use Managed Identity (no credentials in code)
- Grant least privilege access to secrets

### Don't

- Commit secrets to Git (check `.gitignore`)
- Hardcode API keys in source code
- Share User Secrets files between developers
- Use production secrets in local development
- Log secret values (ensure logging doesn't expose sensitive data)
- Put secrets in `NEXT_PUBLIC_` environment variables

---

## Additional Resources

- [.NET User Secrets Documentation](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [Azure Key Vault Overview](https://docs.microsoft.com/azure/key-vault/general/overview)
- [Managed Identity Best Practices](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/best-practice-recommendations)
- [GitHub Secrets Documentation](https://docs.github.com/actions/security-guides/encrypted-secrets)
- [Next.js Environment Variables](https://nextjs.org/docs/basic-features/environment-variables)
