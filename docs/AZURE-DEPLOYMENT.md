# Azure Deployment Guide

Complete guide for deploying the full-stack application (Backend API + Frontend) to Azure with zero-cost hosting.

## Overview

This deployment uses:

**Backend:**

- **Azure App Service (F1 Free tier)** - ~$0/month
- **Application Insights (Free tier)** - ~$0/month (5GB data/month)
- **Azure Key Vault** - ~$0.03/month

**Frontend:**

- **Azure Static Web Apps (Free tier)** - $0/month

**External Services:**

**With OpenAI Chat (Default):**

- **OpenAI Embeddings** - ~$0.003/month (text-embedding-3-small)
- **OpenAI Chat** - ~$0.50/month (gpt-4o-mini)

**Total Cost: ~$0.53/month**

**With Groq Chat (Optional, Free Tier):**

- **OpenAI Embeddings** - ~$0.003/month (text-embedding-3-small)
- **Groq LLM** - $0/month (free tier)

**Total Cost: ~$0.03/month**

## ✅ Production Deployment Prerequisites

**Before deploying to production:**

1. ✅ Update Preprocessor to use OpenAI embeddings API (COMPLETED - 2025-12-28)
2. ⚠️ Regenerate all embeddings in `embeddings.json` using `text-embedding-3-small`
3. ⚠️ Copy updated `embeddings.json` to `backend/Backend.API/Data/`
4. ⚠️ Deploy to Azure with updated embeddings

**Command to regenerate embeddings:**
```bash
# Set OpenAI API key
$env:OPENAI_API_KEY = "sk-..."

# Generate embeddings with OpenAI
cd Preprocessor
dotnet run -- --provider openai --embedding-model text-embedding-3-small -i ./pdfs -o ./embeddings.json

# Copy to backend
cp ./embeddings.json ../backend/Backend.API/Data/embeddings.json
```

## Prerequisites

### Required Tools

1. **Azure Subscription** - [Create free account](https://azure.microsoft.com/free/)
2. **Azure CLI** - [Install](https://docs.microsoft.com/cli/azure/install-azure-cli)
3. **Git** - For pushing code to GitHub
4. **GitHub Account** - For CI/CD
5. **.NET 9 SDK** - For local testing

### Required API Keys

1. **OpenAI API Key** - [Sign up](https://platform.openai.com/signup) (required for embeddings and chat, default provider)
2. **Groq API Key** - [Get free key](https://console.groq.com) (optional, only if using Groq as LLM provider)

## Deployment Steps

### Step 1: Azure Login

```bash
az login
az account list --output table
az account set --subscription "Your Subscription Name"
```

### Step 2: Run Azure Setup Script

```bash
chmod +x azure-setup.sh
./azure-setup.sh
```

The script will create:

**Backend Resources:**

- Resource Group
- App Service Plan (F1 Free tier)
- App Service (Linux, .NET 9)
- Managed Identity
- Application Insights
- Key Vault with secrets

**Frontend Resources:**

- Static Web App (Free tier)

**Important:** The script outputs the deployment token and URLs needed for GitHub configuration!

### Step 3: Configure GitHub Secrets and Variables

#### GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions → **Secrets tab**

| Secret Name | How to Get |
|-------------|------------|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Azure Portal → App Services → Your App → Get publish profile → Paste entire file contents |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Copied from `azure-setup.sh` output |

#### GitHub Variables

Go to your GitHub repository → Settings → Secrets and variables → Actions → **Variables tab**

| Variable Name                | Value                                                      |
|------------------------------|--------------------------------------------------------|
| `NEXT_PUBLIC_API_URL`        | `https://<your-backend-app-service-name>.azurewebsites.net` |

### Step 4: Prepare Embeddings

**Critical:** Regenerate embeddings using OpenAI (not Ollama or LM Studio) to ensure vector space compatibility.

```bash
# In Preprocessor directory
cd Preprocessor/Preprocessor

# Update to use OpenAI embeddings, then run:
dotnet run -- -m pdfpig -i ./pdfs -o ./bin/Debug/net9.0/output.json

# Copy to backend
cp ./bin/Debug/net9.0/output.json ../backend/Backend.API/Data/embeddings.json
```

### Step 5: Deploy

Push code to the `main` branch to trigger GitHub Actions deployment:

```bash
git add .
git commit -m "Add Azure deployment configuration"
git push origin main
```

Monitor the deployment:

- GitHub → Actions tab → Watch the workflow

### Step 6: Verify Deployment

**Backend Health Checks:**

```bash
# Liveness probe (should return 200)
curl https://<your-backend-app-service-name>.azurewebsites.net/health/live

# Readiness probe (should return 200 if embeddings loaded)
curl https://<your-backend-app-service-name>.azurewebsites.net/health/ready

# Test Q&A endpoint
curl -X POST https://<your-backend-app-service-name>.azurewebsites.net/api/ask \
  -H "Content-Type: application/json" \
  -d '{"question":"What is this about?"}'
```

**Frontend:**

Open your Static Web App URL in a browser: `https://<your-static-web-app-name>.azurestaticapps.net`

## Optional: Cosmos DB Setup for Persistent Vector Storage

By default, the backend uses in-memory vector storage (`VectorStorageType = InMemory`) with embeddings loaded from `Data/embeddings.json`. This is suitable for development and small deployments.

For production deployments requiring persistent, scalable vector storage, you can optionally configure Azure Cosmos DB. This enables:

- **Persistent storage** - Embeddings survive app restarts
- **Dynamic updates** - Add/update embeddings via API without redeployment
- **Multi-instance deployments** - Shared vector store across multiple backend instances
- **Native vector search** - Cosmos DB's built-in vector indexing for efficient similarity search

### Cosmos DB Prerequisites

- Existing Azure subscription and resource group
- Backend API already deployed to Azure App Service
- Azure CLI installed and authenticated

### Step 1: Create Cosmos DB Account

Create a Cosmos DB account with NoSQL API and vector search enabled:

```bash
# Set variables
RESOURCE_GROUP="<your-resource-group>"
COSMOS_ACCOUNT="<your-cosmos-account-name>"  # Must be globally unique
LOCATION="<your-location>"  # e.g., eastus

# Create Cosmos DB account with free tier and vector search
az cosmosdb create \
  --name "$COSMOS_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --enable-free-tier true \
  --capabilities EnableNoSQLVectorSearch \
  --default-consistency-level Session
```

**Notes:**

- **Free tier:** Each subscription gets ONE free Cosmos DB account (1000 RU/s, 25GB storage)
- **Vector search:** `EnableNoSQLVectorSearch` capability enables native vector indexing
- **Account name:** Must be globally unique (3-44 characters, lowercase alphanumeric and hyphens)

### Step 2: Create Database and Container

Create a database and container optimized for vector embeddings:

```bash
# Set variables
DATABASE_NAME="<your-database-name>"  # e.g., fund-docs-db
CONTAINER_NAME="embeddings"

# Create database (serverless or provisioned)
az cosmosdb sql database create \
  --account-name "$COSMOS_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DATABASE_NAME" \
  --throughput 400  # Minimum for free tier

# Create container with partition key and vector indexing
az cosmosdb sql container create \
  --account-name "$COSMOS_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --database-name "$DATABASE_NAME" \
  --name "$CONTAINER_NAME" \
  --partition-key-path "/sourceFile" \
  --throughput 400 \
  --idx @cosmos-index-policy.json
```

**Create `cosmos-index-policy.json` file:**

```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    {
      "path": "/*"
    }
  ],
  "excludedPaths": [
    {
      "path": "/\"_etag\"/?"
    }
  ],
  "vectorIndexes": [
    {
      "path": "/embedding",
      "type": "quantizedFlat"
    }
  ],
  "vectorEmbeddingPolicy": {
    "vectorEmbeddings": [
      {
        "path": "/embedding",
        "dataType": "float32",
        "distanceFunction": "cosine",
        "dimensions": 1536
      }
    ]
  }
}
```

**Index Policy Explained:**

- **Partition key:** `/sourceFile` enables efficient per-file operations (delete/update by source)
- **Vector index type:** `quantizedFlat` is cost-optimized (lower RU consumption vs. other index types)
- **Distance function:** `cosine` for semantic similarity (same as in-memory implementation)
- **Dimensions:** 1536 matches `text-embedding-3-small` model

### Step 3: Enable Managed Identity on App Service

Configure the backend App Service to authenticate to Cosmos DB using Managed Identity (no secrets required):

```bash
# Set variables
APP_SERVICE_NAME="<your-app-service-name>"

# Enable system-assigned managed identity
az webapp identity assign \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP"

# Get the principal ID (saved to variable for next step)
PRINCIPAL_ID=$(az webapp identity show \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query principalId -o tsv)

echo "Managed Identity Principal ID: $PRINCIPAL_ID"
```

### Step 4: Grant Cosmos DB RBAC Role

Assign the "Cosmos DB Built-in Data Contributor" role to the App Service's managed identity:

```bash
# Grant Cosmos DB Data Contributor role to the managed identity
az cosmosdb sql role assignment create \
  --account-name "$COSMOS_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --role-definition-name "Cosmos DB Built-in Data Contributor" \
  --principal-id "$PRINCIPAL_ID" \
  --scope "/"
```

**Role Permissions:**

- Read, write, and delete documents
- Query containers
- Read container metadata

**Security:** Only the backend's managed identity can access Cosmos DB data (no connection strings or account keys exposed).

### Step 5: Configure Backend API Settings

Store Cosmos DB configuration in Azure Key Vault:

```bash
# Set variables
KEY_VAULT_NAME="<your-keyvault-name>"

# Get Cosmos DB endpoint
COSMOS_ENDPOINT=$(az cosmosdb show \
  --name "$COSMOS_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --query documentEndpoint -o tsv)

# Enable Cosmos DB storage
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "BackendOptions--VectorStorageType" \
  --value "CosmosDb"

# Set Cosmos DB endpoint (Managed Identity handles authentication)
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "BackendOptions--CosmosDbEndpoint" \
  --value "$COSMOS_ENDPOINT"

# Set database name
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "BackendOptions--CosmosDbDatabaseName" \
  --value "$DATABASE_NAME"

# Set container name (optional - defaults to "embeddings")
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "BackendOptions--CosmosDbContainerName" \
  --value "$CONTAINER_NAME"

# Generate and set API key for Preprocessor authentication
API_KEY=$(openssl rand -base64 32)
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "BackendOptions--EmbeddingApiKey" \
  --value "$API_KEY"

echo "Cosmos DB Configuration Complete!"
echo "API Key (save this for Preprocessor): $API_KEY"
```

**Important:** Save the `API_KEY` output - you'll need it to configure the Preprocessor for uploading embeddings.

### Step 6: Restart Backend API

Restart the App Service to load new configuration:

```bash
az webapp restart \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP"
```

**Verify startup logs:**

```bash
# View logs
az webapp log tail \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP"
```

Expected logs:

```text
Vector Storage Type: CosmosDb
Cosmos DB Configuration:
  Endpoint: https://<your-cosmos-account>.documents.azure.com:443/
  Database: <your-database-name>
  Container: embeddings
Registering CosmosClient with Managed Identity (DefaultAzureCredential)
Registering Cosmos DB vector storage (CosmosDbDocumentRepository + CosmosDbSemanticSearch)
✓ Document repository initialized with 0 chunks
```

### Step 7: Upload Embeddings to Cosmos DB

Use the Preprocessor to upload embeddings to the backend API (which will store them in Cosmos DB):

```bash
cd Preprocessor/Preprocessor

# Set environment variables
$env:OPENAI_API_KEY = "sk-your-openai-api-key"

# Generate and upload embeddings to Cosmos DB
dotnet run -- \
  --provider openai \
  --input-dir ../../pdfs \
  --cosmosdb \
  --url "https://<your-app-service>.azurewebsites.net" \
  --api-key "<api-key-from-step-5>"
```

**Preprocessor Arguments:**

- `--cosmosdb`: Upload to Backend API instead of writing to file
- `--url`: Backend API base URL
- `--api-key`: API key from Step 5 (BackendOptions:EmbeddingApiKey)

**Note:** The Preprocessor will:

1. Extract text from PDFs
2. Generate embeddings using OpenAI API
3. POST embeddings to `/api/embeddings/replace-all` endpoint
4. Backend stores embeddings in Cosmos DB

### Step 8: Verify Deployment

**Health Check:**

```bash
# Check backend health (should include cosmosdb check)
curl https://<your-app-service>.azurewebsites.net/health/ready
```

Expected response:

```json
{
  "status": "Healthy",
  "results": {
    "memory_service": {
      "status": "Healthy",
      "description": "Document chunks loaded: 1234"
    },
    "cosmosdb": {
      "status": "Healthy",
      "description": "Cosmos DB connected: 1234 documents in '<database>/<container>'"
    }
  }
}
```

**Test Q&A:**

```bash
curl -X POST https://<your-app-service>.azurewebsites.net/api/ask \
  -H "Content-Type: application/json" \
  -d '{"question":"What is this about?"}'
```

### Local Development Setup

To connect to Cosmos DB from **localhost** (Visual Studio, VS Code, etc.), you need to configure the firewall to allow your local IP address.

**⚠️ Important:** By default, Cosmos DB blocks all external connections. You must explicitly allow your development machine's IP.

#### Option 1: Allow Specific IP (Recommended)

```bash
# Get your public IP address
curl https://api.ipify.org

# Add your IP to Cosmos DB firewall
az cosmosdb update \
  --name "$COSMOS_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --ip-range-filter "<your-public-ip>"
```

**Note:** If your IP changes (e.g., home vs office), you'll need to update the firewall rule.

#### Option 2: Allow All Networks (Easier, Less Secure)

**Azure Portal:**

1. Navigate to your Cosmos DB account
2. Go to **Settings** → **Networking**
3. Select **Firewall and virtual networks**
4. Under "Allow access from", select **All networks**
5. Click **Save**

**Azure CLI:**

```bash
az cosmosdb update \
  --name "$COSMOS_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --enable-public-network true
```

**⚠️ Security Note:** Only use "All networks" for development. In production, restrict access to specific IPs or virtual networks.

#### Option 3: Allow Azure Services + Specific IP

Best practice for production with local development:

```bash
# Allow Azure services (for App Service)
az cosmosdb update \
  --name "$COSMOS_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --enable-public-network true \
  --ip-range-filter "<your-local-ip>"
```

This allows both your App Service (via Managed Identity) and your local machine (via connection string).

#### Verify Firewall Configuration

After configuring the firewall, test connectivity from localhost:

```bash
cd backend/Backend.API
dotnet run
```

Check console output for:

```text
Registering CosmosClient with Connection String (development mode)
✓ Document repository initialized with X chunks
```

If you see connection errors, verify:

1. Your IP is in the firewall rules: Azure Portal → Cosmos DB → Networking
2. Connection string is set in User Secrets: `dotnet user-secrets list`
3. Your public IP hasn't changed: `curl https://api.ipify.org`

### Rollback to InMemory Storage

If you need to switch back to file-based storage:

```bash
# Set storage type back to InMemory
az keyvault secret set \
  --vault-name "$KEY_VAULT_NAME" \
  --name "BackendOptions--VectorStorageType" \
  --value "InMemory"

# Restart backend
az webapp restart \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP"
```

**Note:** Ensure `Data/embeddings.json` exists in the deployed backend before switching back.

### Cost Analysis

**Cosmos DB Free Tier Limits:**

- **1000 RU/s** throughput (shared across database)
- **25 GB** storage
- **ONE** free account per subscription

**Estimated Usage for This Application:**

| Operation              | RU Cost | Frequency        | Daily RU         |
| ---------------------- | ------- | ---------------- | ---------------- |
| Vector search (top 10) | ~50 RU  | 100 queries/day  | 5,000            |
| Add embedding (single) | ~10 RU  | 10 uploads/day   | 100              |
| Health check           | ~2 RU   | 1440/day (1/min) | 2,880            |
| **Total**              |         |                  | **~8,000 RU/day**|

**RU/s Required:** ~8,000 RU/day ÷ 86,400 seconds = **0.09 RU/s average** (well within free tier)

**Storage:** ~1,500 embeddings × 10 KB/doc = **~15 MB** (well within 25 GB limit)

**Conclusion:** This application should run comfortably within Cosmos DB free tier limits for typical usage patterns.

### Troubleshooting

#### Backend fails to connect to Cosmos DB

1. Check managed identity is enabled: `az webapp identity show --name <app> --resource-group <rg>`
2. Verify RBAC role assignment: Check Azure Portal → Cosmos DB → Access Control (IAM)
3. Check Key Vault secrets are set correctly
4. Review App Service logs: `az webapp log tail --name <app> --resource-group <rg>`

#### 401 Unauthorized when uploading embeddings

1. Verify API key matches Key Vault secret: `az keyvault secret show --vault-name <kv> --name BackendOptions--EmbeddingApiKey`
2. Check Preprocessor is using correct `--api-key` argument
3. Verify `VectorStorageType` is set to `CosmosDb` in Key Vault

#### Health check shows "Container not found"

1. Verify container exists: `az cosmosdb sql container show --account-name <account> --database-name <db> --name <container> --resource-group <rg>`
2. Check database and container names match Key Vault secrets
3. Ensure vector indexing policy was applied correctly

## Azure App Service Configuration

### Environment Variables

Set in Azure Portal → App Service → Configuration → Application settings:

| Setting | Value | Source |
|---------|-------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Manual |
| `EMBEDDINGS_PATH` | `/home/site/wwwroot/Data/embeddings.json` | Manual |
| `KEY_VAULT_NAME` | `kv-funddocs-XXXX` | From setup script |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Auto-set | From setup script |

### Health Check Configuration

> **Note:** Health check probes are **NOT available on F1 Free tier**. The endpoints still work for manual testing:

- `/health/live` - Liveness probe (always returns 200)
- `/health/ready` - Readiness probe (returns 200 if embeddings loaded)

Health check monitoring requires B1 tier or higher.

## Monitoring and F1 Tier Limitations

### App Service F1 Free Tier Limitations

The following App Service features are **NOT available** on F1 Free tier:

| Feature | F1 Free | B1 Basic ($13/mo) |
|---------|---------|-------------------|
| Health check probes | ❌ | ✅ |
| Always On | ❌ | ✅ |
| Custom domains | ❌ | ✅ |
| SSL certificates | ❌ | ✅ |
| Deployment slots | ❌ | ✅ |
| CPU limit | 60 min/day | Unlimited |
| RAM | 1 GB | 1.75 GB |

**What IS available on F1:**
- ✅ Application Insights integration (separate service with own free tier)
- ✅ Log streaming
- ✅ Basic deployment via GitHub Actions
- ✅ Managed Identity for Key Vault access

### Application Insights (Separate Service)

Application Insights is a **separate Azure service** (not part of App Service) with its own free tier (5GB/month). It works with App Service F1.

Access via: Azure Portal → Application Insights → `ai-funddocs`

**Free tier limit:** 5GB data ingestion per month. If exceeded, $2.30/GB after that.

## Updating Secrets

### Using Azure CLI

**Option 1: Use OpenAI (default, recommended):**

```bash
# Set LLM Provider to OpenAI
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--LlmProvider" \
  --value "OpenAI"

# Set OpenAI API Key (required for embeddings and chat)
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--OpenAIApiKey" \
  --value "sk-your-openai-api-key"
```

**Option 2: Use Groq (free tier alternative):**

```bash
# Set LLM Provider to Groq
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--LlmProvider" \
  --value "Groq"

# Set OpenAI API Key (still required for embeddings)
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--OpenAIApiKey" \
  --value "sk-your-openai-api-key"

# Set Groq API Key (required for Groq chat completion)
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--GroqApiKey" \
  --value "gsk-your-groq-api-key"
```

### Restarting the App

After updating secrets, restart the app:

```bash
az webapp restart \
  --name "funddocs-backend-api" \
  --resource-group "rg-funddocs-backend"
```

## Troubleshooting

### Issue: App Won't Start

**Check:**

1. Application Insights → Live Metrics for real-time errors
2. App Service → Log stream for startup logs
3. Ensure embeddings.json was deployed (check `/home/site/wwwroot/Data/`)

**Solution:**

```bash
# Check logs
az webapp log tail \
  --name "funddocs-backend-api" \
  --resource-group "rg-funddocs-backend"
```

### Issue: Health Check Failing

**Check:**

1. `/health/live` - Should always return 200
2. `/health/ready` - Check if embeddings loaded

**Common Causes:**

- Embeddings file missing or corrupt
- OpenAI API key invalid
- Memory exhausted (F1 tier has 1GB RAM limit)

### Issue: Cold Starts (F1 Tier)

**Symptoms:**

- First request after 20min idle takes 10-30 seconds
- App "wakes up" slowly

**Solutions:**

- Accept this limitation (it's the free tier)
- Upgrade to B1 tier (~$13/month) for "Always On" feature
- Use external monitoring to ping the app every 10 minutes

### Issue: CPU Limit Exceeded (F1 Tier)

**Symptoms:**

- App stops responding after heavy use
- Error: "CPU quota exceeded"

**Solution:**

- F1 tier has 60 CPU minutes/day limit
- Wait until next day for quota reset
- Upgrade to B1 tier for higher limits
- Optimize code to use less CPU

### Issue: Application Insights Not Working

**Check:**

1. Connection string is set in App Service configuration
2. Environment is NOT Development (AI only enabled in Production)
3. Check for ingestion errors in Application Insights

## Frontend Deployment (Azure Static Web Apps)

### Overview

The Next.js frontend is deployed to Azure Static Web Apps, which provides:

- Global CDN distribution
- Free SSL certificates
- Automatic deployments from GitHub

### Configuration

The Next.js application uses static export mode for Azure Static Web Apps compatibility:

```typescript
// next.config.ts
const nextConfig: NextConfig = {
  output: "export",
  images: { unoptimized: true },
  trailingSlash: true,
};
```

### Environment Variables

| Variable | Purpose | Where to Set |
|----------|---------|--------------|
| `NEXT_PUBLIC_API_URL` | Backend API endpoint | GitHub Repository Variables |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deployment token | GitHub Repository Secrets |

### Manual Deployment (Optional)

```bash
# Build locally
cd frontend
npm run build

# Deploy using SWA CLI
npm install -g @azure/static-web-apps-cli
swa deploy ./out --deployment-token <YOUR_TOKEN>
```

---

## Cost Optimization

### Current Setup (Free/Low Cost)

| Resource | Tier | Monthly Cost |
|----------|------|--------------|
| App Service F1 | Free | $0 |
| Static Web Apps | Free | $0 |
| Application Insights | Free (5GB) | $0 |
| Key Vault | Standard | ~$0.03 |
| OpenAI Embeddings | Pay-per-use | ~$0.003 |
| Groq LLM | Free tier | $0 |
| **Total** | | **~$0.03** |

### If You Need to Upgrade

**App Service B1 (~$13/month):**

- Always-on (no cold starts)
- Custom domains
- 100 ACU vs 60 CPU min/day
- 1.75GB RAM vs 1GB

**Application Insights Pay-as-you-go:**

- First 5GB free
- $2.30/GB after that
- Set up cost alerts!

## Continuous Deployment

### GitHub Workflows

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| Deploy Backend | `deploy-backend.yml` | Push to main (`backend/**`) | Deploy API to App Service |
| Deploy Frontend | `deploy-frontend.yml` | Push to main (`frontend/**`) | Deploy to Static Web Apps |
| PR Checks | `pr-checks.yml` | Pull requests | Lint, test, build verification |

### Automatic Deployments

GitHub Actions automatically deploys on push to `main` branch:

- **Backend:** Changes in `backend/**` trigger `deploy-backend.yml`
- **Frontend:** Changes in `frontend/**` trigger `deploy-frontend.yml`

### Manual Deployment

Trigger manually from GitHub:

1. Go to Actions tab
2. Select the desired workflow (Backend or Frontend)
3. Click "Run workflow"
4. Select branch and run

### Rollback

To rollback to a previous version:

1. Azure Portal → App Service → Deployment Center
2. Select a previous deployment
3. Click "Redeploy"

## Next Steps

1. **Set up Azure Budgets:**
   - Azure Portal → Cost Management + Billing → Budgets
   - Create alert at $5/month threshold

2. **Monitor Performance:**
   - Review Application Insights daily
   - Set up availability tests
   - Configure alert rules

3. **Scale if Needed:**
   - Monitor F1 tier limitations
   - Upgrade to B1 if cold starts are problematic

## Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure Static Web Apps Documentation](https://docs.microsoft.com/azure/static-web-apps/)
- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Azure Key Vault Quickstart](https://docs.microsoft.com/azure/key-vault/general/quick-create-cli)
- [GitHub Actions for Azure](https://docs.microsoft.com/azure/developer/github/github-actions)
