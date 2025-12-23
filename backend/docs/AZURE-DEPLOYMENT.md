# Azure Deployment Guide

Complete guide for deploying the Backend API to Azure App Service with zero-cost hosting using the F1 free tier.

## Overview

This deployment uses:
- **Azure App Service (F1 Free tier)** - ~$0/month
- **Application Insights (Free tier)** - ~$0/month (5GB data/month)
- **Azure Key Vault** - ~$0.03/month
- **OpenAI Embeddings** - ~$0.003/month (text-embedding-3-small)
- **Groq LLM** - $0/month (free tier)

**Total Cost: ~$0.03/month**

## Prerequisites

### Required Tools

1. **Azure Subscription** - [Create free account](https://azure.microsoft.com/free/)
2. **Azure CLI** - [Install](https://docs.microsoft.com/cli/azure/install-azure-cli)
3. **Git** - For pushing code to GitHub
4. **GitHub Account** - For CI/CD
5. **.NET 9 SDK** - For local testing

### Required API Keys

1. **Groq API Key** - [Get free key](https://console.groq.com)
2. **OpenAI API Key** - [Sign up](https://platform.openai.com/signup)

## Deployment Steps

### Step 1: Azure Login

```bash
az login
az account list --output table
az account set --subscription "Your Subscription Name"
```

### Step 2: Run Azure Setup Script

```bash
cd backend
chmod +x azure-setup.sh
./azure-setup.sh
```

The script will create:
- Resource Group
- App Service Plan (F1 Free tier)
- App Service (Linux, .NET 9)
- Managed Identity
- Application Insights
- Key Vault with secrets

**Important:** Save the App Service name and Key Vault name from the output!

### Step 3: Configure GitHub Secrets

1. **Download Publish Profile:**
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to App Services → Your App → Get publish profile
   - Save the downloaded `.PublishSettings` file

2. **Add GitHub Secret:**
   - Go to your GitHub repository
   - Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Value: Paste contents of the publish profile file

### Step 4: Prepare Embeddings

**Critical:** Regenerate embeddings using OpenAI (not Ollama or LM Studio) to ensure vector space compatibility.

```bash
# In Preprocessor directory
cd ../Preprocessor/Preprocessor

# Update to use OpenAI embeddings, then run:
dotnet run -- -m pdfpig -i ./pdfs -o ./bin/Debug/net9.0/output.json

# Copy to backend
cp ./bin/Debug/net9.0/output.json ../../backend/Backend.API/Data/embeddings.json
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

Test the health endpoints:

```bash
# Replace with your actual app name
APP_NAME="funddocs-backend-api"

# Liveness probe (should return 200)
curl https://$APP_NAME.azurewebsites.net/health/live

# Readiness probe (should return 200 if embeddings loaded)
curl https://$APP_NAME.azurewebsites.net/health/ready

# Test Q&A endpoint
curl -X POST https://$APP_NAME.azurewebsites.net/api/ask \
  -H "Content-Type: application/json" \
  -d '{"question":"What is this about?"}'
```

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

- **Path:** `/health/ready`
- **Interval:** 30 seconds
- **Unhealthy threshold:** 3 failures

This is automatically configured by the setup script.

## Monitoring with Application Insights

### Accessing Logs

1. Azure Portal → Application Insights → Your instance
2. View:
   - **Live Metrics** - Real-time performance
   - **Failures** - Exceptions and failed requests
   - **Performance** - Response times
   - **Logs** - Query logs using KQL

### Sample Queries

```kql
// Recent exceptions
exceptions
| where timestamp > ago(1h)
| order by timestamp desc

// Slow requests
requests
| where duration > 5000
| order by timestamp desc

// Embedding service calls
dependencies
| where name contains "OpenAI"
| summarize count(), avg(duration) by resultCode
```

### Staying Within Free Tier

The free tier includes 5GB data/month. To stay within limits:
- Log level set to `Warning` in production
- Sampling enabled for high-volume telemetry
- Monitor usage: Application Insights → Usage and estimated costs

## Updating Secrets

### Using Azure CLI

```bash
# Update Groq API Key
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--GroqApiKey" \
  --value "new-groq-api-key"

# Update OpenAI API Key
az keyvault secret set \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--OpenAIApiKey" \
  --value "new-openai-api-key"
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

## Cost Optimization

### Current Setup (Free/Low Cost)

| Resource | Tier | Monthly Cost |
|----------|------|--------------|
| App Service F1 | Free | $0 |
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

### Automatic Deployments

GitHub Actions automatically deploys on:
- Push to `main` branch
- Changes in `backend/**` directory

### Manual Deployment

Trigger manually from GitHub:
1. Go to Actions tab
2. Select "Deploy Backend to Azure"
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

4. **Update Preprocessor:**
   - Ensure it uses OpenAI embeddings
   - Regenerate all embeddings before production use

## Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Azure Key Vault Quickstart](https://docs.microsoft.com/azure/key-vault/general/quick-create-cli)
- [GitHub Actions for Azure](https://docs.microsoft.com/azure/developer/github/github-actions)
