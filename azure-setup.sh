#!/bin/bash
set -e

# ========================================
# Azure Resources Setup Script
# ========================================
# This script creates all required Azure resources for the full-stack application:
# - Backend: App Service, Application Insights, Key Vault
# - Frontend: Static Web App

# Configuration
RESOURCE_GROUP="rg-funddocs-backend"
LOCATION="eastus"
APP_SERVICE_PLAN="plan-funddocs"
APP_SERVICE_NAME="funddocs-backend-api"
KEY_VAULT_NAME="kv-funddocs-$(openssl rand -hex 4)"
APP_INSIGHTS_NAME="ai-funddocs"
STATIC_WEB_APP_NAME="funddocs-frontend"

echo "========================================="
echo "Azure Resources Setup for Full-Stack App"
echo "========================================="
echo ""
echo "This script will create:"
echo "- Resource Group: $RESOURCE_GROUP"
echo ""
echo "Backend Resources:"
echo "- App Service Plan: $APP_SERVICE_PLAN (F1 Free tier)"
echo "- App Service: $APP_SERVICE_NAME"
echo "- Application Insights: $APP_INSIGHTS_NAME"
echo "- Key Vault: $KEY_VAULT_NAME"
echo ""
echo "Frontend Resources:"
echo "- Static Web App: $STATIC_WEB_APP_NAME (Free tier)"
echo ""
read -p "Continue? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]
then
    exit 1
fi

# Create Resource Group
echo ""
echo "[1/10] Creating Resource Group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create App Service Plan (F1 Free tier)
echo ""
echo "[2/10] Creating App Service Plan (F1 Free tier)..."
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku F1 \
  --is-linux

# Create App Service
echo ""
echo "[3/10] Creating App Service..."
az webapp create \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:9.0"

# Enable System-Assigned Managed Identity
echo ""
echo "[4/10] Enabling Managed Identity..."
az webapp identity assign \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP

# Get the Managed Identity Principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

echo "Managed Identity Principal ID: $PRINCIPAL_ID"

# Create Application Insights
echo ""
echo "[5/10] Creating Application Insights..."
az monitor app-insights component create \
  --app $APP_INSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web \
  --retention-time 30

# Get Application Insights Connection String
AI_CONNECTION_STRING=$(az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query connectionString -o tsv)

echo "Application Insights created"

# Create Key Vault
echo ""
echo "[6/10] Creating Key Vault..."
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Grant Managed Identity access to Key Vault
echo ""
echo "[7/10] Granting Managed Identity access to Key Vault..."
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Add secrets to Key Vault
echo ""
echo "[8/10] Adding secrets to Key Vault..."
echo ""
echo "Enter your Groq API Key:"
read -s GROQ_KEY
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "BackendOptions--GroqApiKey" \
  --value "$GROQ_KEY" \
  > /dev/null

echo ""
echo "Enter your OpenAI API Key:"
read -s OPENAI_KEY
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "BackendOptions--OpenAIApiKey" \
  --value "$OPENAI_KEY" \
  > /dev/null

# Configure App Service Settings
echo ""
echo "[9/10] Configuring App Service settings..."
az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "EMBEDDINGS_PATH=/home/site/wwwroot/Data/embeddings.json" \
    "KEY_VAULT_NAME=$KEY_VAULT_NAME" \
    "APPLICATIONINSIGHTS_CONNECTION_STRING=$AI_CONNECTION_STRING" \
  > /dev/null

# Configure health check
echo "Configuring health check probe..."
az webapp config set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --health-check-path "/health/ready" \
  > /dev/null

# Create Static Web App for Frontend
echo ""
echo "[10/10] Creating Static Web App for Frontend..."
az staticwebapp create \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Free

# Get the deployment token for GitHub Actions
echo ""
echo "Retrieving Static Web App deployment token..."
SWA_TOKEN=$(az staticwebapp secrets list \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "properties.apiKey" -o tsv)

# Get Static Web App URL
SWA_URL=$(az staticwebapp show \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "defaultHostname" -o tsv)

echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Backend Resources:"
echo "- App Service URL: https://$APP_SERVICE_NAME.azurewebsites.net"
echo "- Key Vault: $KEY_VAULT_NAME"
echo "- Application Insights: $APP_INSIGHTS_NAME"
echo ""
echo "Frontend Resources:"
echo "- Static Web App URL: https://$SWA_URL"
echo ""
echo "========================================="
echo "Next steps:"
echo "========================================="
echo ""
echo "1. Add the following SECRETS to your GitHub repository:"
echo "   (Go to: GitHub Repository → Settings → Secrets and variables → Actions → Secrets)"
echo ""
echo "   AZURE_WEBAPP_PUBLISH_PROFILE:"
echo "   - Download from: Azure Portal → App Services → $APP_SERVICE_NAME → Get publish profile"
echo "   - Paste the entire contents of the downloaded file"
echo ""
echo "   AZURE_STATIC_WEB_APPS_API_TOKEN:"
echo "   - Value: $SWA_TOKEN"
echo ""
echo "2. Add the following VARIABLE to your GitHub repository:"
echo "   (Go to: GitHub Repository → Settings → Secrets and variables → Actions → Variables)"
echo ""
echo "   NEXT_PUBLIC_API_URL:"
echo "   - Value: https://$APP_SERVICE_NAME.azurewebsites.net"
echo ""
echo "3. Commit your code and push to the main branch to trigger deployment"
echo ""
echo "4. After deployment, test the endpoints:"
echo "   Backend:  curl https://$APP_SERVICE_NAME.azurewebsites.net/health/live"
echo "   Frontend: https://$SWA_URL"
echo ""
echo "========================================="
