#!/bin/bash
set -e

# ========================================
# Azure Resources Setup Script
# ========================================
# This script creates all required Azure resources for the Backend API
# including App Service, Application Insights, and Key Vault

# Configuration
RESOURCE_GROUP="rg-funddocs-backend"
LOCATION="eastus"
APP_SERVICE_PLAN="plan-funddocs"
APP_SERVICE_NAME="funddocs-backend-api"
KEY_VAULT_NAME="kv-funddocs-$(openssl rand -hex 4)"
APP_INSIGHTS_NAME="ai-funddocs"

echo "========================================="
echo "Azure Resources Setup for Backend API"
echo "========================================="
echo ""
echo "This script will create:"
echo "- Resource Group: $RESOURCE_GROUP"
echo "- App Service Plan: $APP_SERVICE_PLAN (F1 Free tier)"
echo "- App Service: $APP_SERVICE_NAME"
echo "- Application Insights: $APP_INSIGHTS_NAME"
echo "- Key Vault: $KEY_VAULT_NAME"
echo ""
read -p "Continue? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]
then
    exit 1
fi

# Create Resource Group
echo ""
echo "[1/8] Creating Resource Group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create App Service Plan (F1 Free tier)
echo ""
echo "[2/8] Creating App Service Plan (F1 Free tier)..."
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku F1 \
  --is-linux

# Create App Service
echo ""
echo "[3/8] Creating App Service..."
az webapp create \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:9.0"

# Enable System-Assigned Managed Identity
echo ""
echo "[4/8] Enabling Managed Identity..."
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
echo "[5/8] Creating Application Insights..."
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
echo "[6/8] Creating Key Vault..."
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Grant Managed Identity access to Key Vault
echo ""
echo "[7/8] Granting Managed Identity access to Key Vault..."
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Add secrets to Key Vault
echo ""
echo "[8/8] Adding secrets to Key Vault..."
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
echo "Configuring App Service settings..."
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

echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Resources created:"
echo "- App Service URL: https://$APP_SERVICE_NAME.azurewebsites.net"
echo "- Key Vault: $KEY_VAULT_NAME"
echo "- Application Insights: $APP_INSIGHTS_NAME"
echo ""
echo "Next steps:"
echo "1. Download publish profile from Azure Portal:"
echo "   Go to: https://portal.azure.com → App Services → $APP_SERVICE_NAME → Get publish profile"
echo ""
echo "2. Add the following secret to your GitHub repository:"
echo "   Secret name: AZURE_WEBAPP_PUBLISH_PROFILE"
echo "   Secret value: (contents of the downloaded publish profile)"
echo ""
echo "3. Commit your code and push to the main branch to trigger deployment"
echo ""
echo "4. After deployment, test the health endpoints:"
echo "   curl https://$APP_SERVICE_NAME.azurewebsites.net/health/live"
echo "   curl https://$APP_SERVICE_NAME.azurewebsites.net/health/ready"
echo ""
echo "========================================="
