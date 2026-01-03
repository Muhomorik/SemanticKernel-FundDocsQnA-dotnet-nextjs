# Feature Plan: Azure Cosmos DB Vector Database Integration

## Overview

Replace the current file-based embeddings storage (`embeddings.json`) with Azure Cosmos DB for NoSQL with vector search capabilities. This enables persistent, scalable vector storage while maintaining the low-cost approach using Cosmos DB free tier.

**Scope:**
- Backend: Add Cosmos DB integration for vector storage and search
- Preprocessor: Add option to upload embeddings via Backend API
- Frontend: No changes required

## Architecture

### Current Architecture

```
Preprocessor → embeddings.json → Backend (in-memory) → Frontend
```

### New Architecture

```
                                    ┌─────────────────┐
                                    │   Cosmos DB     │
                                    │  (Vector Store) │
                                    └────────┬────────┘
                                             │
Preprocessor ──(API)──► Backend ─────────────┘
                           │
                        Frontend
```

### Data Flow

**Embedding Upload (Preprocessor → Backend → Cosmos DB):**
```
1. Preprocessor extracts text from PDFs
2. Preprocessor generates embeddings (OpenAI/Ollama)
3. Preprocessor sends embeddings to Backend API (authenticated)
4. Backend validates API key
5. Backend stores embeddings in Cosmos DB
```

**Query Flow (Frontend → Backend → Cosmos DB):**
```
1. Frontend sends question to Backend
2. Backend generates query embedding (OpenAI)
3. Backend performs vector search in Cosmos DB
4. Backend sends context + question to LLM
5. Backend returns answer to Frontend
```

## Azure Portal Setup Guide

### Step 1: Create Cosmos DB Account

1. **Navigate to Azure Portal**: Go to [portal.azure.com](https://portal.azure.com)

2. **Create Resource**: Click "Create a resource" → Search "Azure Cosmos DB" → Click "Create"

3. **Select API**: Choose **"Azure Cosmos DB for NoSQL"** (required for vector search)

4. **Configure Basics Tab**:

   | Setting                        | Value                                           |
   | ------------------------------ | ----------------------------------------------- |
   | Subscription                   | Your subscription                               |
   | Resource Group                 | `<your-resource-group>` (existing or create new)|
   | Account Name                   | `<your-cosmos-account>` (globally unique)       |
   | Location                       | Same as your App Service (e.g., `North Europe`) |
   | Capacity mode                  | **Provisioned throughput**                      |
   | Apply Free Tier Discount       | **Apply**                                       |
   | Limit total account throughput | Check this box (recommended for cost control)   |

5. **Configure Global Distribution Tab**:
   - Geo-Redundancy: **Disable** (not needed for hobby project, saves cost)
   - Multi-region Writes: **Disable**

6. **Configure Networking Tab**:
   - Connectivity method: **All networks** (simplest for development)
   - For production: Consider "Selected networks" with your App Service VNet

7. **Configure Backup Policy Tab**:
   - Backup policy: **Periodic** (included free)
   - Backup interval: 240 minutes (default)
   - Backup retention: 8 hours (default)

8. **Review + Create**: Review settings and click "Create"

   ⏱️ **Wait time**: 5-10 minutes for deployment

### Step 2: Create Database and Container

1. **Open your Cosmos DB account** in Azure Portal

2. **Create Database**:
   - Go to "Data Explorer" in left menu
   - Click "New Database"
   - Database id: `<your-database-name>` (e.g., `funddocs`)
   - Provision throughput: **Check** ✅
   - Database throughput (autoscale): **Manual**
   - Database RU/s: `400` (minimum, saves cost)
   - Click "OK"

3. **Create Container**:
   - Click "..." next to your database → "New Container"
   - Container id: `embeddings`
   - Partition key: `/sourceFile`
   - Container throughput: **Use database throughput** (shares 400 RU/s)
   - Click "OK"

### Step 3: Configure Vector Search Policy

**Important**: Vector indexing policy must be set via Azure CLI or SDK (not Portal UI).

```bash
# Get your Cosmos DB account name
COSMOS_ACCOUNT="<your-cosmos-account>"
RESOURCE_GROUP="<your-resource-group>"

# Create container with vector index policy (replace existing if needed)
az cosmosdb sql container create \
  --account-name "$COSMOS_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --database-name "<your-database-name>" \
  --name "embeddings" \
  --partition-key-path "/sourceFile" \
  --idx '{
    "indexingMode": "consistent",
    "automatic": true,
    "includedPaths": [{ "path": "/*" }],
    "excludedPaths": [{ "path": "/embedding/*" }],
    "vectorIndexes": [{ "path": "/embedding", "type": "quantizedFlat" }]
  }' \
  --vector-embedding-policy '{
    "vectorEmbeddings": [{
      "path": "/embedding",
      "dataType": "float32",
      "dimensions": 1536,
      "distanceFunction": "cosine"
    }]
  }'
```

### Step 4: Configure Access for Backend

#### Option A: Managed Identity (Recommended for Production)

1. **Enable Managed Identity on App Service**:

   ```bash
   az webapp identity assign \
     --name "<your-backend-app>" \
     --resource-group "<your-resource-group>"
   ```

2. **Get the Principal ID**:

   ```bash
   PRINCIPAL_ID=$(az webapp identity show \
     --name "<your-backend-app>" \
     --resource-group "<your-resource-group>" \
     --query principalId -o tsv)
   ```

3. **Grant Cosmos DB Data Contributor Role**:

   ```bash
   COSMOS_ACCOUNT_ID=$(az cosmosdb show \
     --name "$COSMOS_ACCOUNT" \
     --resource-group "$RESOURCE_GROUP" \
     --query id -o tsv)

   az role assignment create \
     --assignee "$PRINCIPAL_ID" \
     --role "Cosmos DB Built-in Data Contributor" \
     --scope "$COSMOS_ACCOUNT_ID"
   ```

#### Option B: Connection String (Development Only)

1. **Get Connection String from Portal**:
   - Go to Cosmos DB account → "Keys" in left menu
   - Copy "PRIMARY CONNECTION STRING"

2. **Store in User Secrets (local dev)**:

   ```bash
   cd backend/Backend.API
   dotnet user-secrets set "BackendOptions:CosmosDbConnectionString" "<your-connection-string>"
   ```

### Step 5: Configure App Service Environment Variables

In Azure Portal → App Service → Configuration → Application settings:

| Setting | Value | Notes |
|---------|-------|-------|
| `BackendOptions__VectorStoreProvider` | `CosmosDb` | Enable Cosmos DB |
| `BackendOptions__CosmosDbEndpoint` | `https://<your-cosmos-account>.documents.azure.com:443/` | From Cosmos DB "Keys" page |
| `BackendOptions__CosmosDbDatabaseName` | `<your-database-name>` | Database name |
| `BackendOptions__CosmosDbContainerName` | `embeddings` | Container name |
| `BackendOptions__EmbeddingApiKey` | `<generated-api-key>` | For preprocessor auth |

**Note**: When using Managed Identity, you don't need `CosmosDbKey` - authentication is automatic.

### Step 6: Generate Embedding API Key

Generate a secure API key for preprocessor authentication:

```powershell
# PowerShell - Generate 32-character random key
$apiKey = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object {[char]$_})
Write-Host "Generated API Key: $apiKey"
```

```bash
# Bash/Linux - Generate 32-character random key
openssl rand -base64 32 | tr -dc 'a-zA-Z0-9' | head -c 32
```

**Store the key in**:

1. **Azure Key Vault** (production):

   ```bash
   az keyvault secret set \
     --vault-name "<your-keyvault-name>" \
     --name "BackendOptions--EmbeddingApiKey" \
     --value "<your-generated-key>"
   ```

2. **User Secrets** (local development):

   ```bash
   dotnet user-secrets set "BackendOptions:EmbeddingApiKey" "<your-generated-key>"
   ```

3. **Preprocessor** (when running):

   ```bash
   # Environment variable
   $env:FUNDDOCS_API_KEY = "<your-generated-key>"

   # Or CLI argument
   dotnet run -- --api-key "<your-generated-key>"
   ```

---

## Pricing Tiers Comparison

### Azure Cosmos DB Pricing Options

| Tier | Throughput | Storage | Monthly Cost | Best For |
|------|------------|---------|--------------|----------|
| **Free Tier** | 1000 RU/s | 25 GB | **$0** | Hobby projects, development |
| Serverless | Pay per RU | 1 TB max | ~$0.25 per 1M RU | Sporadic, unpredictable workloads |
| Provisioned (400 RU/s) | 400 RU/s | Pay per GB | ~$23/month | Consistent low traffic |
| Provisioned (1000 RU/s) | 1000 RU/s | Pay per GB | ~$58/month | Production workloads |
| Autoscale (1000-10000 RU/s) | 1000-10000 RU/s | Pay per GB | ~$87/month min | Variable traffic |

### Free Tier Details

**Eligibility**: One free tier account per Azure subscription

| Resource | Free Allowance | Overage Cost |
|----------|---------------|--------------|
| Throughput | 1000 RU/s | $0.008 per 100 RU/s/hour |
| Storage | 25 GB | $0.25 per GB/month |
| Backup Storage | 2x data size | Included |

**Limitations**:

- Only one free tier account per subscription
- Cannot be converted to/from other capacity modes
- Free tier discount applied automatically to first 1000 RU/s and 25 GB

### Cost Scenarios for This Project

| Scenario | RU/s Needed | Storage | Monthly Cost |
|----------|-------------|---------|--------------|
| **Development** (local) | 0 | 0 | $0 |
| **Hobby** (10 users/day) | <100 RU/s avg | <1 GB | **$0** (free tier) |
| **Light Production** (100 users/day) | ~200 RU/s avg | <5 GB | **$0** (free tier) |
| **Medium Production** (1000 users/day) | ~500 RU/s avg | <10 GB | **$0** (free tier) |
| **Beyond Free Tier** | >1000 RU/s | >25 GB | ~$23-58/month |

### Serverless vs Provisioned

| Factor | Serverless | Provisioned (Free Tier) |
|--------|------------|------------------------|
| Best for | Sporadic use, dev/test | Consistent traffic |
| Minimum cost | $0 (pay per use) | $0 (free tier) |
| Max throughput | 5000 RU/s per container | Unlimited (with scaling) |
| Vector search | ✅ Supported | ✅ Supported |
| Free tier | ❌ Not available | ✅ Available |
| Cold start | May have latency | No cold start |

**Recommendation**: Use **Free Tier with Provisioned throughput** for this project. Serverless doesn't offer free tier and has cold start issues.

### Request Unit (RU) Cost Estimates

| Operation | Estimated RUs | With 1000 queries/day |
|-----------|---------------|----------------------|
| Vector search (top 10) | 10-50 RU | 10,000-50,000 RU/day |
| Point read (by ID) | 1 RU | Minimal |
| Insert document | 5-10 RU | Batch uploads only |
| Query by partition | 3-10 RU | Minimal |

**Daily RU Budget**: 1000 RU/s × 86400 seconds = **86.4 million RU/day** available in free tier

---

## Complete Environment Variables Reference

### Backend (Production - Azure App Service)

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Yes | - | Set to `Production` |
| `BackendOptions__VectorStoreProvider` | No | `InMemory` | `InMemory` or `CosmosDb` |
| `BackendOptions__CosmosDbEndpoint` | When CosmosDb | - | Cosmos DB URI |
| `BackendOptions__CosmosDbDatabaseName` | When CosmosDb | `<your-database-name>` | Database name |
| `BackendOptions__CosmosDbContainerName` | When CosmosDb | `embeddings` | Container name |
| `BackendOptions__EmbeddingApiKey` | When using API | - | API key for preprocessor |
| `BackendOptions__OpenAIApiKey` | Yes | - | OpenAI API key (via Key Vault) |
| `BackendOptions__LlmProvider` | No | `OpenAI` | `OpenAI` or `Groq` |
| `KEY_VAULT_NAME` | Yes | - | Key Vault name for secrets |

### Backend (Development - User Secrets)

```bash
cd backend/Backend.API

# Required
dotnet user-secrets set "BackendOptions:OpenAIApiKey" "sk-..."

# Cosmos DB (when using CosmosDb provider)
dotnet user-secrets set "BackendOptions:VectorStoreProvider" "CosmosDb"
dotnet user-secrets set "BackendOptions:CosmosDbConnectionString" "<connection-string>"
# OR
dotnet user-secrets set "BackendOptions:CosmosDbEndpoint" "https://xxx.documents.azure.com:443/"
dotnet user-secrets set "BackendOptions:CosmosDbKey" "<primary-key>"

# Embedding API (when preprocessor uses API mode)
dotnet user-secrets set "BackendOptions:EmbeddingApiKey" "<your-api-key>"

# Optional
dotnet user-secrets set "BackendOptions:LlmProvider" "OpenAI"
dotnet user-secrets set "BackendOptions:GroqApiKey" "gsk-..."  # If using Groq
```

### Azure Key Vault Secrets

| Secret Name | Purpose | Required |
|-------------|---------|----------|
| `BackendOptions--OpenAIApiKey` | OpenAI API key | Yes |
| `BackendOptions--GroqApiKey` | Groq API key | When using Groq |
| `BackendOptions--EmbeddingApiKey` | Preprocessor auth | When using API |
| `BackendOptions--CosmosDbKey` | Cosmos DB key | Dev only (use MI in prod) |

**Set secrets via CLI**:

```bash
KV_NAME="<your-keyvault-name>"

az keyvault secret set --vault-name "$KV_NAME" \
  --name "BackendOptions--EmbeddingApiKey" \
  --value "<generated-key>"
```

### Preprocessor Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `OPENAI_API_KEY` | When using OpenAI | - | OpenAI API key for embeddings |
| `FUNDDOCS_API_KEY` | When using API mode | - | Backend API key |
| `FUNDDOCS_API_URL` | When using API mode | `http://localhost:5000` | Backend URL |

**Set in PowerShell**:

```powershell
$env:OPENAI_API_KEY = "sk-..."
$env:FUNDDOCS_API_KEY = "<your-api-key>"
$env:FUNDDOCS_API_URL = "https://<your-backend-app>.azurewebsites.net"
```

**Set in Bash**:

```bash
export OPENAI_API_KEY="sk-..."
export FUNDDOCS_API_KEY="<your-api-key>"
export FUNDDOCS_API_URL="https://<your-backend-app>.azurewebsites.net"
```

---

## Azure Cosmos DB Configuration

### Free Tier Limits

| Resource | Free Tier Limit | Notes |
|----------|-----------------|-------|
| Throughput | 1000 RU/s | Shared across all containers |
| Storage | 25 GB | Sufficient for thousands of documents |
| Vector Dimensions | 1536 | Matches OpenAI text-embedding-3-small |
| Vector Index Type | quantizedFlat / flat | quantizedFlat recommended for cost |

### Container Schema

**Container Name:** `embeddings`

**Partition Key:** `/sourceFile` (enables efficient queries by document)

**Document Structure:**
```json
{
  "id": "unique-chunk-id",
  "sourceFile": "document.pdf",
  "pageNumber": 1,
  "chunkIndex": 0,
  "content": "Text content of the chunk...",
  "embedding": [0.123, -0.456, ...],  // 1536 dimensions
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

**Vector Index Policy:**
```json
{
  "vectorIndexes": [
    {
      "path": "/embedding",
      "type": "quantizedFlat"
    }
  ]
}
```

### Cosmos DB Setup Script

Add to `azure-setup.sh`:
```bash
# Create Cosmos DB account (free tier)
az cosmosdb create \
  --name "<your-cosmos-account>" \
  --resource-group "$RESOURCE_GROUP" \
  --kind "GlobalDocumentDB" \
  --enable-free-tier true \
  --default-consistency-level "Session" \
  --locations regionName="$LOCATION" failoverPriority=0

# Create database
az cosmosdb sql database create \
  --account-name "<your-cosmos-account>" \
  --resource-group "$RESOURCE_GROUP" \
  --name "<your-database-name>"

# Create container with vector index
az cosmosdb sql container create \
  --account-name "<your-cosmos-account>" \
  --resource-group "$RESOURCE_GROUP" \
  --database-name "<your-database-name>" \
  --name "embeddings" \
  --partition-key-path "/sourceFile" \
  --throughput 400
```

## Authentication Strategy

### Preprocessor → Backend Authentication

**Recommended: API Key Authentication**

The preprocessor authenticates with the backend using a shared API key. This is simple, secure for internal tools, and doesn't require Azure AD setup.

**API Key Flow:**
```
1. Generate a secure API key (e.g., 32-character random string)
2. Store in Backend as environment variable / Key Vault
3. Store in Preprocessor as CLI argument or environment variable
4. Preprocessor sends key in Authorization header
5. Backend validates key before processing request
```

**Header Format:**
```
Authorization: ApiKey <your-api-key>
```

**Why API Key over other options:**

| Option     | Pros                             | Cons                                 | Recommendation      |
| ---------- | -------------------------------- | ------------------------------------ | ------------------- |
| API Key    | Simple, no external dependencies | Must rotate manually                 | ✅ Recommended      |
| Azure AD   | Enterprise-grade, auto-rotation  | Complex setup, overkill for CLI tool | For enterprise only |
| JWT Token  | Stateless, can include claims    | Requires token generation service    | Overkill            |
| Basic Auth | Simple                           | Less secure, credentials in header   | Not recommended     |

### Backend → Cosmos DB Authentication

**Recommended: Managed Identity (Production) / Connection String (Development)**

| Environment | Authentication Method |
|-------------|----------------------|
| Production (Azure) | Managed Identity (DefaultAzureCredential) |
| Local Development | Connection String via User Secrets |

## Backend Changes

### New Configuration Options

Add to `BackendOptions`:
```csharp
public class BackendOptions
{
    // Existing options...

    // Vector Store Configuration
    public string VectorStoreProvider { get; set; } = "InMemory"; // "InMemory" | "CosmosDb"

    // Cosmos DB Configuration (when VectorStoreProvider = "CosmosDb")
    public string CosmosDbEndpoint { get; set; }
    public string CosmosDbKey { get; set; }  // For local dev only, use Managed Identity in prod
    public string CosmosDbDatabaseName { get; set; } = "your-database-name";
    public string CosmosDbContainerName { get; set; } = "embeddings";

    // Embedding Management API
    public string EmbeddingApiKey { get; set; }  // API key for preprocessor authentication
}
```

### New API Endpoints

| Endpoint | Method | Purpose | Authentication |
|----------|--------|---------|----------------|
| `/api/embeddings` | POST | Add new embeddings | API Key |
| `/api/embeddings/{sourceFile}` | PUT | Update embeddings for a file | API Key |
| `/api/embeddings/{sourceFile}` | DELETE | Delete embeddings for a file | API Key |
| `/api/embeddings/replace-all` | POST | Replace all embeddings | API Key |

**POST /api/embeddings**
```json
// Request
{
  "embeddings": [
    {
      "id": "doc1_page1_chunk0",
      "sourceFile": "document.pdf",
      "pageNumber": 1,
      "chunkIndex": 0,
      "content": "Text content...",
      "embedding": [0.123, -0.456, ...]
    }
  ]
}

// Response
{
  "success": true,
  "added": 15,
  "message": "Added 15 embeddings for 1 file(s)"
}
```

**PUT /api/embeddings/{sourceFile}**
```json
// Request - replaces all embeddings for the specified file
{
  "embeddings": [...]
}

// Response
{
  "success": true,
  "replaced": 12,
  "message": "Replaced 12 embeddings for document.pdf"
}
```

**DELETE /api/embeddings/{sourceFile}**
```json
// Response
{
  "success": true,
  "deleted": 12,
  "message": "Deleted 12 embeddings for document.pdf"
}
```

**POST /api/embeddings/replace-all**
```json
// Request - deletes all existing and adds new
{
  "embeddings": [...]
}

// Response
{
  "success": true,
  "deleted": 100,
  "added": 150,
  "message": "Replaced all embeddings: deleted 100, added 150"
}
```

### New Services

**IVectorStore Interface (Domain Layer):**
```csharp
public interface IVectorStore
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        int topK,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        IEnumerable<EmbeddingRecord> records,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        string sourceFile,
        IEnumerable<EmbeddingRecord> records,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string sourceFile,
        CancellationToken cancellationToken = default);

    Task ReplaceAllAsync(
        IEnumerable<EmbeddingRecord> records,
        CancellationToken cancellationToken = default);
}
```

**Implementations (Infrastructure Layer):**
- `InMemoryVectorStore` - Existing behavior (loads from embeddings.json)
- `CosmosDbVectorStore` - New Cosmos DB implementation

### Service Registration

```csharp
// Program.cs
if (options.VectorStoreProvider == "CosmosDb")
{
    services.AddSingleton<IVectorStore, CosmosDbVectorStore>();
}
else
{
    services.AddSingleton<IVectorStore, InMemoryVectorStore>();
}
```

### Authentication Middleware

```csharp
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));

        var header = authHeader.ToString();
        if (!header.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization scheme"));

        var apiKey = header.Substring("ApiKey ".Length).Trim();
        if (apiKey != _options.EmbeddingApiKey)
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));

        var claims = new[] { new Claim(ClaimTypes.Name, "Preprocessor") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

## Preprocessor Changes

### CLI Design with CommandLineParser Verbs

The Preprocessor uses [CommandLineParser](https://github.com/commandlineparser/commandline) with **verbs** for clear, intuitive commands:

```text
dotnet run -- <verb> [options]

Verbs:
  file      Output embeddings to JSON file (default behavior)
  upload    Upload embeddings to Backend API
  help      Display help
```

### Verb: `file` (JSON Output)

Existing behavior - generates embeddings and saves to JSON file.

**Options:**

| Option              | Short | Default                  | Description                                 |
| ------------------- | ----- | ------------------------ | ------------------------------------------- |
| `--input`           | `-i`  | `pdfs`                   | Input folder with PDF files                 |
| `--output`          | `-o`  | `./embeddings.json`      | Output JSON file path                       |
| `--append`          | `-a`  | `false`                  | Append to existing JSON file                |
| `--provider`        | `-p`  | `openai`                 | Embedding provider (openai/ollama/lmstudio) |
| `--embedding-model` | `-e`  | `text-embedding-3-small` | Embedding model name                        |

**Usage:**

```bash
# Default - process PDFs and output to JSON
dotnet run -- file -i ./pdfs -o ./embeddings.json

# Append new PDFs to existing embeddings
dotnet run -- file -i ./new-pdfs -o ./embeddings.json --append

# Use local Ollama for embeddings
dotnet run -- file -i ./pdfs -o ./embeddings.json --provider ollama --embedding-model nomic-embed-text
```

### Verb: `upload` (API Output)

New behavior - generates embeddings and uploads to Backend API.

**Options:**

| Option             | Short | Default                  | Required | Description                                 |
| ------------------ | ----- | ------------------------ | -------- | ------------------------------------------- |
| `--input`          | `-i`  | `pdfs`                   | No       | Input folder with PDF files                 |
| `--url`            | `-u`  | `http://localhost:5000`  | No       | Backend API URL                             |
| `--key`            | `-k`  | (env: `FUNDDOCS_API_KEY`)| Yes      | API key for authentication                  |
| `--operation`      | `-o`  | `add`                    | No       | Operation: `add`, `update`, `replace-all`   |
| `--provider`       | `-p`  | `openai`                 | No       | Embedding provider (openai/ollama/lmstudio) |
| `--embedding-model`| `-e`  | `text-embedding-3-small` | No       | Embedding model name                        |
| `--batch-size`     | `-b`  | `100`                    | No       | Embeddings per API request                  |

**Operations (Mutually Exclusive via `--operation`):**

| Operation     | Description                                              |
| ------------- | -------------------------------------------------------- |
| `add`         | Add new embeddings (keeps existing, skips duplicates)    |
| `update`      | Update embeddings for processed files (replaces by file) |
| `replace-all` | Delete ALL existing embeddings, then add new             |

**Usage:**

```bash
# Add new PDFs to database (doesn't affect existing)
dotnet run -- upload -i ./pdfs --key "your-api-key"

# Update specific PDFs (replaces embeddings for those files only)
dotnet run -- upload -i ./updated-pdfs --key "your-api-key" --operation update

# Replace all embeddings (full refresh)
dotnet run -- upload -i ./pdfs --key "your-api-key" --operation replace-all

# Upload to production
dotnet run -- upload -i ./pdfs \
  --url "https://<your-backend-app>.azurewebsites.net" \
  --key "your-api-key" \
  --operation replace-all
```

### CommandLineParser Implementation

```csharp
// Verb definitions
[Verb("file", isDefault: true, HelpText = "Output embeddings to JSON file")]
public class FileOptions : CommonOptions
{
    [Option('o', "output", Default = "./embeddings.json", HelpText = "Output JSON file path")]
    public string Output { get; set; } = "./embeddings.json";

    [Option('a', "append", Default = false, HelpText = "Append to existing file")]
    public bool Append { get; set; }
}

[Verb("upload", HelpText = "Upload embeddings to Backend API")]
public class UploadOptions : CommonOptions
{
    [Option('u', "url", Default = "http://localhost:5000", HelpText = "Backend API URL")]
    public string Url { get; set; } = "http://localhost:5000";

    [Option('k', "key", Required = true, HelpText = "API key (or set FUNDDOCS_API_KEY env var)")]
    public string ApiKey { get; set; } = string.Empty;

    [Option('o', "operation", Default = UploadOperation.Add, HelpText = "Operation: add, update, replace-all")]
    public UploadOperation Operation { get; set; }

    [Option('b', "batch-size", Default = 100, HelpText = "Embeddings per API request")]
    public int BatchSize { get; set; }
}

// Shared options (inherited by both verbs)
public abstract class CommonOptions
{
    [Option('i', "input", Default = "pdfs", HelpText = "Input folder with PDF files")]
    public string Input { get; set; } = "pdfs";

    [Option('p', "provider", Default = "openai", HelpText = "Embedding provider")]
    public string Provider { get; set; } = "openai";

    [Option('e', "embedding-model", Default = "text-embedding-3-small", HelpText = "Embedding model")]
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    [Option('m', "method", Default = "pdfpig", HelpText = "PDF extraction method")]
    public string Method { get; set; } = "pdfpig";
}

public enum UploadOperation
{
    Add,
    Update,
    ReplaceAll
}

// Program.cs entry point
Parser.Default.ParseArguments<FileOptions, UploadOptions>(args)
    .WithParsed<FileOptions>(opts => RunFileMode(opts))
    .WithParsed<UploadOptions>(opts => RunUploadMode(opts))
    .WithNotParsed(errors => HandleErrors(errors));
```

### Preprocessor Output Services

**IEmbeddingOutput Interface:**

```csharp
public interface IEmbeddingOutput
{
    Task WriteAsync(IEnumerable<EmbeddingRecord> records, CancellationToken cancellationToken);
}
```

**Implementations:**

- `FileEmbeddingOutput` - Writes to JSON file (existing behavior)
- `ApiEmbeddingOutput` - Sends to Backend API (new)

## Configuration Reference (Summary)

### Backend Environment Variables (Summary)

| Variable                              | Purpose                    | Required                 |
| ------------------------------------- | -------------------------- | ------------------------ |
| `BackendOptions__VectorStoreProvider` | `InMemory` or `CosmosDb`   | No (default: InMemory)   |
| `BackendOptions__CosmosDbEndpoint`    | Cosmos DB endpoint URL     | When using CosmosDb      |
| `BackendOptions__CosmosDbKey`         | Cosmos DB key (dev only)   | Local dev only           |
| `BackendOptions__EmbeddingApiKey`     | API key for preprocessor   | When using API           |

### Key Vault Secrets (Summary)

| Secret Name                        | Purpose                          |
| ---------------------------------- | -------------------------------- |
| `BackendOptions--CosmosDbEndpoint` | Cosmos DB endpoint               |
| `BackendOptions--EmbeddingApiKey`  | API key for preprocessor auth    |

**Note:** In production, use Managed Identity for Cosmos DB authentication (no key needed).

### Preprocessor Environment Variables (Summary)

| Variable           | Purpose                            |
| ------------------ | ---------------------------------- |
| `FUNDDOCS_API_KEY` | API key for backend authentication |
| `FUNDDOCS_API_URL` | Backend API URL                    |

## Cost Analysis

### Cosmos DB Free Tier

| Resource | Free Tier | Expected Usage | Status |
|----------|-----------|----------------|--------|
| Throughput | 1000 RU/s | ~100 RU/s average | ✅ Well within limits |
| Storage | 25 GB | ~100 MB (thousands of docs) | ✅ Well within limits |
| Egress | 5 GB/month | ~1 GB/month | ✅ Well within limits |

**Estimated Monthly Cost: $0** (within free tier)

### RU Consumption Estimates

| Operation | Estimated RUs | Frequency |
|-----------|---------------|-----------|
| Vector search (top 10) | ~10-50 RU | Per query |
| Insert embedding | ~5-10 RU | Per document chunk |
| Delete by partition | ~10 RU | Rare |

## Implementation Phases

### Phase 1: Backend Infrastructure
- [ ] Add Cosmos DB NuGet packages
- [ ] Create `IVectorStore` interface
- [ ] Implement `CosmosDbVectorStore`
- [ ] Add configuration options
- [ ] Create API key authentication middleware
- [ ] Implement embedding management endpoints

### Phase 2: Backend Integration
- [ ] Refactor `QuestionAnsweringService` to use `IVectorStore`
- [ ] Add health check for Cosmos DB connection
- [ ] Update DI configuration for provider selection
- [ ] Add migration path from embeddings.json to Cosmos DB

### Phase 3: Preprocessor Updates
- [ ] Add new CLI options
- [ ] Implement `ApiEmbeddingOutput` service
- [ ] Add HTTP client for backend communication
- [ ] Support for add/update/replace-all operations

### Phase 4: Azure Infrastructure
- [ ] Update `azure-setup.sh` with Cosmos DB creation
- [ ] Configure Managed Identity for Cosmos DB access
- [ ] Add Key Vault secrets for API key
- [ ] Update deployment documentation

### Phase 5: Testing & Documentation
- [ ] Unit tests for new services
- [ ] Integration tests for Cosmos DB operations
- [ ] Update README files
- [ ] Update SECRETS-MANAGEMENT.md
- [ ] Update Status.md

## Backward Compatibility

The implementation maintains full backward compatibility:

1. **Default behavior unchanged**: `VectorStoreProvider = "InMemory"` uses existing embeddings.json flow
2. **Preprocessor default**: `--output-mode file` writes JSON as before
3. **No frontend changes**: API contract remains identical
4. **Gradual migration**: Can switch between InMemory and CosmosDb via configuration

## Security Considerations

1. **API Key Storage**:
   - Never commit API keys to version control
   - Use environment variables or Key Vault
   - Rotate keys periodically

2. **Cosmos DB Access**:
   - Use Managed Identity in production (no keys in code)
   - Connection string only for local development
   - Network isolation via private endpoints (optional, adds cost)

3. **Endpoint Protection**:
   - Embedding management endpoints require API key authentication
   - Rate limiting recommended for production
   - Input validation on all endpoints

## NuGet Packages Required

**Backend:**
```xml
<PackageReference Include="Microsoft.Azure.Cosmos" Version="3.x" />
<PackageReference Include="Azure.Identity" Version="1.x" />
```

**Preprocessor:**
```xml
<!-- Already has HTTP client support -->
```

## Open Questions

1. **Vector search algorithm**: Use `quantizedFlat` (lower RU cost) or `flat` (higher accuracy)?
   - Recommendation: Start with `quantizedFlat`, upgrade if accuracy issues arise

2. **Batch size for uploads**: How many embeddings per API call?
   - Recommendation: 100 embeddings per batch to stay under request size limits

3. **Retry policy**: How to handle transient failures?
   - Recommendation: Exponential backoff with 3 retries

## References

- [Azure Cosmos DB Vector Search](https://learn.microsoft.com/azure/cosmos-db/nosql/vector-search)
- [Cosmos DB Free Tier](https://learn.microsoft.com/azure/cosmos-db/free-tier)
- [Managed Identity for Cosmos DB](https://learn.microsoft.com/azure/cosmos-db/managed-identity-based-authentication)
- [Semantic Kernel Cosmos DB Connector](https://learn.microsoft.com/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/azure-cosmosdb-nosql-connector)
