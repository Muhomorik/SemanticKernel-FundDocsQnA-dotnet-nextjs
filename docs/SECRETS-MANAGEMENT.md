# Secrets Management Guide

This guide explains how secrets are managed across different environments to ensure security while allowing developer flexibility.

## Overview

Three-tier secrets strategy:

- **Local Development:** .NET User Secrets (developer-specific, not committed)
- **Production:** Azure Key Vault (secure cloud storage, read-only for developers)
- **CI/CD:** GitHub Secrets (for automated deployment)

## Local Development

### Setup User Secrets

User Secrets store sensitive data outside your project directory and are never committed to source control.

**Initialize User Secrets:**

```bash
cd backend/Backend.API
dotnet user-secrets init
```

This adds a `<UserSecretsId>` to your `.csproj` file (already configured).

**Set Your API Keys:**

```bash
# Set Groq API Key
dotnet user-secrets set "BackendOptions:GroqApiKey" "your-groq-api-key"

# Set OpenAI API Key
dotnet user-secrets set "BackendOptions:OpenAIApiKey" "your-openai-api-key"
```

**List Current Secrets:**

```bash
dotnet user-secrets list
```

**Remove a Secret:**

```bash
dotnet user-secrets remove "BackendOptions:GroqApiKey"
```

**Clear All Secrets:**

```bash
dotnet user-secrets clear
```

### Where Are User Secrets Stored?

**Windows:**

```
%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
```

**Linux/macOS:**

```
~/.microsoft/usersecrets/<UserSecretsId>/secrets.json
```

### Configuration Priority

When the app runs, configuration values are loaded in this order (later values override earlier ones):

1. `appsettings.json`
2. `appsettings.Development.json` (in Development environment)
3. User Secrets (in Development environment)
4. Environment Variables
5. Command-line arguments

**Example:** If `GroqApiKey` is set in both `appsettings.json` and User Secrets, the User Secrets value will be used in development.

## Production (Azure Key Vault)

### How It Works

In production, the backend automatically loads secrets from Azure Key Vault using Managed Identity:

1. App Service has a **System-Assigned Managed Identity**
2. Managed Identity is granted **"Key Vault Secrets User"** role
3. App loads secrets at startup via `DefaultAzureCredential`
4. No API keys or passwords in code or configuration files

### Viewing Production Secrets

**Using Azure Portal:**

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to Key Vaults → Your Key Vault
3. Secrets → View secret values

**Using Azure CLI:**

```bash
# List all secrets
az keyvault secret list \
  --vault-name "your-keyvault-name" \
  --query "[].name"

# Show a secret value (requires appropriate permissions)
az keyvault secret show \
  --vault-name "your-keyvault-name" \
  --name "BackendOptions--GroqApiKey" \
  --query "value" \
  --output tsv
```

### Updating Production Secrets

**Only administrators with Key Vault access can update production secrets.**

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

# Restart the app to load new secrets
az webapp restart \
  --name "funddocs-backend-api" \
  --resource-group "rg-funddocs-backend"
```

### Secret Naming Convention

Key Vault secret names use double dashes (`--`) instead of colons (`:`) because colons are not allowed in secret names.

| Configuration Path | Key Vault Secret Name |
|-------------------|-----------------------|
| `BackendOptions:GroqApiKey` | `BackendOptions--GroqApiKey` |
| `BackendOptions:OpenAIApiKey` | `BackendOptions--OpenAIApiKey` |

The backend automatically translates these at runtime.

## CI/CD (GitHub Secrets)

### Required GitHub Secrets

**Repository Settings → Secrets and variables → Actions:**

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Deployment credentials | Download from Azure Portal: App Service → Get publish profile |

### Optional GitHub Secrets

If you prefer to set API keys via GitHub Actions instead of Key Vault:

| Secret Name | Description |
|-------------|-------------|
| `GROQ_API_KEY` | Groq API key (alternative to Key Vault) |
| `OPENAI_API_KEY` | OpenAI API key (alternative to Key Vault) |

**Note:** Using Key Vault is recommended for production. GitHub Secrets are better for non-production environments or temporary deployments.

### Adding GitHub Secrets

1. Go to your GitHub repository
2. Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Enter name and value
5. Click "Add secret"

## Security Best Practices

### DO

- **Use User Secrets for local development** - Never commit API keys to source control
- **Use Azure Key Vault for production** - Secure, centralized secret management
- **Rotate keys regularly** - Update API keys every 90 days
- **Use Managed Identity** - No credentials in code or configuration
- **Grant least privilege** - Only give key access to those who need it
- **Monitor secret access** - Review Key Vault audit logs

### DON'T

- **Never commit secrets to Git** - Use `.gitignore` for `appsettings.Development.json` if it contains secrets
- **Never hardcode API keys** - Always use configuration
- **Never share User Secrets files** - Each developer should set up their own
- **Never use production secrets locally** - Use test/development API keys
- **Never log secret values** - Ensure logging doesn't expose sensitive data

## Troubleshooting

### Issue: "GroqApiKey is not set" Warning on Startup

**Cause:** User Secrets not configured or API key not set.

**Solution:**

```bash
dotnet user-secrets set "BackendOptions:GroqApiKey" "your-key-here"
dotnet user-secrets set "BackendOptions:OpenAIApiKey" "your-key-here"
```

### Issue: Production App Can't Access Key Vault

**Causes:**

1. Managed Identity not enabled
2. Managed Identity not granted access to Key Vault
3. Key Vault name environment variable not set

**Solution:**

```bash
# Check Managed Identity is enabled
az webapp identity show \
  --name "funddocs-backend-api" \
  --resource-group "rg-funddocs-backend"

# Grant access (if missing)
PRINCIPAL_ID=$(az webapp identity show \
  --name "funddocs-backend-api" \
  --resource-group "rg-funddocs-backend" \
  --query principalId -o tsv)

az keyvault set-policy \
  --name "your-keyvault-name" \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### Issue: Secrets Not Loading from User Secrets

**Check:**

1. `UserSecretsId` is set in `.csproj`
2. Running in Development environment
3. Secrets file exists and is valid JSON
4. Secret keys match the configuration path exactly

**Verify:**

```bash
# List secrets
dotnet user-secrets list

# Check environment
echo $ASPNETCORE_ENVIRONMENT  # Should be empty or "Development"
```

## Configuration Examples

### Development (User Secrets)

```json
{
  "BackendOptions:GroqApiKey": "gsk_dev_xxxxx",
  "BackendOptions:OpenAIApiKey": "sk-dev-xxxxx"
}
```

### Production (Key Vault)

```plaintext
BackendOptions--GroqApiKey: gsk_prod_xxxxx
BackendOptions--OpenAIApiKey: sk-proj-xxxxx
```

### Priority in Development

1. User Secrets: `GroqApiKey = "my-dev-key"`
2. appsettings.Development.json: `GroqApiKey = ""`
3. appsettings.json: `GroqApiKey = ""`

Result: App uses `"my-dev-key"` from User Secrets.

### Priority in Production

1. Key Vault: `BackendOptions--GroqApiKey = "my-prod-key"`
2. Environment Variable: `GROQ_API_KEY = not set`
3. appsettings.Production.json: Not included
4. appsettings.json: `GroqApiKey = ""`

Result: App uses `"my-prod-key"` from Key Vault.

## Developer Permissions

| Action | Local Dev | Production |
|--------|-----------|------------|
| Set own API keys | Yes (User Secrets) | No |
| Read own secrets | Yes | No |
| Update production secrets | No | No (admin only) |
| View production secrets | No | No (admin only) |
| Deploy to production | Yes (via GitHub) | Yes (via GitHub) |

**Key Point:** Developers cannot view or change production secrets, ensuring security separation.

## Additional Resources

- [.NET User Secrets Documentation](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [Azure Key Vault Overview](https://docs.microsoft.com/azure/key-vault/general/overview)
- [Managed Identity Best Practices](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/best-practice-recommendations)
- [GitHub Secrets Documentation](https://docs.github.com/actions/security-guides/encrypted-secrets)
