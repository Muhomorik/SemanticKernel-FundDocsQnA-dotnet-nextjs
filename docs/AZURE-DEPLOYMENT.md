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

| Variable Name | Value |
|---------------|-------|
| `NEXT_PUBLIC_API_URL` | `https://funddocs-backend-api.azurewebsites.net` |

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
curl https://funddocs-backend-api.azurewebsites.net/health/live

# Readiness probe (should return 200 if embeddings loaded)
curl https://funddocs-backend-api.azurewebsites.net/health/ready

# Test Q&A endpoint
curl -X POST https://funddocs-backend-api.azurewebsites.net/api/ask \
  -H "Content-Type: application/json" \
  -d '{"question":"What is this about?"}'
```

**Frontend:**

Open your Static Web App URL in a browser: `https://funddocs-frontend.azurestaticapps.net`

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
