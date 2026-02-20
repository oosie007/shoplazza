# 🚀 Azure App Service Deployment Guide

## Overview
This guide provides step-by-step instructions for deploying the Shoplazza Add-On app to Azure App Service for production use.

---

## 📋 **Prerequisites**

### 🔧 **Required Tools**
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (latest version)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/downloads)
- [Azure PowerShell](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps) (optional)

### 🏗️ **Azure Resources**
- Azure Subscription with billing enabled
- Resource Group for the application
- Azure App Service Plan
- Azure SQL Database (for production)
- Azure Key Vault (for secrets management)

---

## 🏗️ **Step 1: Azure Infrastructure Setup**

### 1.1 **Create Resource Group**
```bash
# Login to Azure
az login

# Create resource group
az group create \
  --name "shoplazza-addon-rg" \
  --location "East US" \
  --tags "Environment=Production" "Project=ShoplazzaAddon"
```

### 1.2 **Create App Service Plan**
```bash
# Create App Service Plan (B1 for production, F1 for testing)
az appservice plan create \
  --name "shoplazza-addon-plan" \
  --resource-group "shoplazza-addon-rg" \
  --sku "B1" \
  --is-linux \
  --tags "Environment=Production"
```

### 1.3 **Create Web App**
```bash
# Create the web app
az webapp create \
  --name "shoplazza-addon-app" \
  --resource-group "shoplazza-addon-rg" \
  --plan "shoplazza-addon-plan" \
  --runtime "DOTNETCORE:8.0" \
  --deployment-local-git \
  --tags "Environment=Production"
```

### 1.4 **Create SQL Database**
```bash
# Create SQL Server
az sql server create \
  --name "shoplazza-addon-sql" \
  --resource-group "shoplazza-addon-rg" \
  --location "East US" \
  --admin-user "sqladmin" \
  --admin-password "YourStrongPassword123!" \
  --tags "Environment=Production"

# Create database
az sql db create \
  --resource-group "shoplazza-addon-rg" \
  --server "shoplazza-addon-sql" \
  --name "ShoplazzaAddonDB" \
  --edition "Basic" \
  --capacity 5 \
  --tags "Environment=Production"
```

### 1.5 **Create Key Vault**
```bash
# Create Key Vault
az keyvault create \
  --name "shoplazza-addon-kv" \
  --resource-group "shoplazza-addon-rg" \
  --location "East US" \
  --enabled-for-deployment \
  --enabled-for-disk-encryption \
  --enabled-for-template-deployment \
  --tags "Environment=Production"
```

---

## 🔐 **Step 2: Configure Secrets & Environment Variables**

### 2.1 **Store Secrets in Key Vault**
```bash
# Store Shoplazza credentials
az keyvault secret set \
  --vault-name "shoplazza-addon-kv" \
  --name "ShoplazzaClientId" \
  --value "your_shoplazza_client_id"

az keyvault secret set \
  --vault-name "shoplazza-addon-kv" \
  --name "ShoplazzaClientSecret" \
  --value "your_shoplazza_client_secret"

az keyvault secret set \
  --vault-name "shoplazza-addon-kv" \
  --name "ShoplazzaWebhookSecret" \
  --value "your_shoplazza_webhook_secret"

# Store encryption key
az keyvault secret set \
  --vault-name "shoplazza-addon-kv" \
  --name "EncryptionKey" \
  --value "your_32_character_encryption_key_here_12345"

# Store database connection string
az keyvault secret set \
  --vault-name "shoplazza-addon-kv" \
  --name "ConnectionStrings--DefaultConnection" \
  --value "Server=shoplazza-addon-sql.database.windows.net;Database=ShoplazzaAddonDB;User Id=sqladmin;Password=YourStrongPassword123!;Encrypt=true;TrustServerCertificate=false;MultipleActiveResultSets=true"
```

### 2.2 **Configure App Service Environment Variables**
```bash
# Set environment variables
az webapp config appsettings set \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app" \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    SHOPLAZZA_REDIRECT_URI="https://shoplazza-addon-app.azurewebsites.net/api/auth/callback" \
    SHOPLAZZA_APP_URL="https://shoplazza-addon-app.azurewebsites.net/api/auth" \
    LOGGING_LEVEL="Information" \
    SESSION_TIMEOUT_MINUTES="30"
```

### 2.3 **Configure Key Vault Integration**
```bash
# Enable Key Vault integration
az webapp config appsettings set \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app" \
  --settings \
    @Microsoft.KeyVault(SecretUri=https://shoplazza-addon-kv.vault.azure.net/secrets/ShoplazzaClientId/) \
    @Microsoft.KeyVault(SecretUri=https://shoplazza-addon-kv.vault.azure.net/secrets/ShoplazzaClientSecret/) \
    @Microsoft.KeyVault(SecretUri=https://shoplazza-addon-kv.vault.azure.net/secrets/ShoplazzaWebhookSecret/) \
    @Microsoft.KeyVault(SecretUri=https://shoplazza-addon-kv.vault.azure.net/secrets/EncryptionKey/) \
    @Microsoft.KeyVault(SecretUri=https://shoplazza-addon-kv.vault.azure.net/secrets/ConnectionStrings--DefaultConnection/)
```

---

## 🌐 **Step 3: Configure Custom Domain & SSL**

### 3.1 **Add Custom Domain**
```bash
# Add custom domain (replace with your domain)
az webapp config hostname add \
  --webapp-name "shoplazza-addon-app" \
  --resource-group "shoplazza-addon-rg" \
  --hostname "api.yourdomain.com"
```

### 3.2 **Configure SSL Certificate**
```bash
# Upload SSL certificate (if you have one)
az webapp config ssl upload \
  --certificate-file "path/to/your/certificate.pfx" \
  --certificate-password "your_certificate_password" \
  --name "shoplazza-addon-app" \
  --resource-group "shoplazza-addon-rg"
```

### 3.3 **Bind SSL Certificate**
```bash
# Bind SSL certificate to custom domain
az webapp config ssl bind \
  --certificate-thumbprint "your_certificate_thumbprint" \
  --ssl-type "SNI" \
  --name "shoplazza-addon-app" \
  --resource-group "shoplazza-addon-rg"
```

---

## 📦 **Step 4: Database Migration**

### 4.1 **Update Connection String**
Update the connection string in Azure App Service to use the production SQL Server:

```bash
# Update connection string
az webapp config appsettings set \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app" \
  --settings \
    ConnectionStrings__DefaultConnection="Server=shoplazza-addon-sql.database.windows.net;Database=ShoplazzaAddonDB;User Id=sqladmin;Password=YourStrongPassword123!;Encrypt=true;TrustServerCertificate=false;MultipleActiveResultSets=true"
```

### 4.2 **Run Database Migrations**
```bash
# Navigate to the app directory
cd shoplazza-addon-app

# Run migrations
dotnet ef database update --connection "Server=shoplazza-addon-sql.database.windows.net;Database=ShoplazzaAddonDB;User Id=sqladmin;Password=YourStrongPassword123!;Encrypt=true;TrustServerCertificate=false;MultipleActiveResultSets=true"
```

---

## 🚀 **Step 5: Deploy Application**

### 5.1 **Create Production Branch**
```bash
# Create production branch
git checkout -b production

# Ensure all changes are committed
git add .
git commit -m "Prepare for production deployment"
```

### 5.2 **Configure Deployment Credentials**
```bash
# Get deployment credentials
az webapp deployment list-publishing-credentials \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app"
```

### 5.3 **Deploy via Git**
```bash
# Add Azure remote
git remote add azure https://shoplazza-addon-app.scm.azurewebsites.net:443/shoplazza-addon-app.git

# Deploy to Azure
git push azure production:master
```

### 5.4 **Alternative: Deploy via Azure CLI**
```bash
# Build the application
dotnet publish -c Release -o ./publish

# Deploy using Azure CLI
az webapp deployment source config-zip \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app" \
  --src "./publish.zip"
```

---

## 🔧 **Step 6: Post-Deployment Configuration**

### 6.1 **Configure Application Insights**
```bash
# Create Application Insights
az monitor app-insights component create \
  --app "shoplazza-addon-insights" \
  --location "East US" \
  --resource-group "shoplazza-addon-rg" \
  --application-type "web" \
  --tags "Environment=Production"

# Get instrumentation key
az monitor app-insights component show \
  --app "shoplazza-addon-insights" \
  --resource-group "shoplazza-addon-rg" \
  --query "instrumentationKey" \
  --output tsv
```

### 6.2 **Configure Monitoring**
```bash
# Set Application Insights key
az webapp config appsettings set \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app" \
  --settings \
    APPINSIGHTS_INSTRUMENTATIONKEY="your_instrumentation_key"
```

### 6.3 **Configure CORS (if needed)**
```bash
# Configure CORS for Shoplazza domains
az webapp cors add \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app" \
  --allowed-origins "https://*.myshoplazza.com"
```

---

## 🧪 **Step 7: Testing & Validation**

### 7.1 **Health Check**
```bash
# Test the health endpoint
curl https://shoplazza-addon-app.azurewebsites.net/health
```

### 7.2 **OAuth Flow Test**
1. Navigate to: `https://shoplazza-addon-app.azurewebsites.net/api/auth?shop=your-test-shop.myshoplazza.com`
2. Verify OAuth flow works correctly
3. Check token storage and validation

### 7.3 **Widget Delivery Test**
1. Test widget script delivery: `https://shoplazza-addon-app.azurewebsites.net/api/widget/widget.js?shop=your-test-shop.myshoplazza.com`
2. Verify JSONP configuration: `https://shoplazza-addon-app.azurewebsites.net/api/widget/config?shop=your-test-shop.myshoplazza.com&productId=123&callback=test`

### 7.4 **Webhook Testing**
```bash
# Test webhook endpoints
curl -X POST https://shoplazza-addon-app.azurewebsites.net/api/webhooks/orders/create \
  -H "Content-Type: application/json" \
  -H "X-Shoplazza-Hmac-Sha256: test-signature" \
  -H "X-Shoplazza-Shop-Domain: your-test-shop.myshoplazza.com" \
  -d '{"id": 123, "financial_status": "pending"}'
```

---

## 📊 **Step 8: Monitoring & Maintenance**

### 8.1 **Set Up Alerts**
```bash
# Create availability alert
az monitor metrics alert create \
  --name "shoplazza-addon-availability" \
  --resource-group "shoplazza-addon-rg" \
  --scopes "/subscriptions/your-subscription-id/resourceGroups/shoplazza-addon-rg/providers/Microsoft.Web/sites/shoplazza-addon-app" \
  --condition "avg availability percentage < 99" \
  --description "Alert when app availability drops below 99%"
```

### 8.2 **Configure Log Analytics**
```bash
# Create Log Analytics workspace
az monitor log-analytics workspace create \
  --resource-group "shoplazza-addon-rg" \
  --workspace-name "shoplazza-addon-logs" \
  --location "East US"
```

### 8.3 **Set Up Backup**
```bash
# Configure database backup
az sql db update \
  --resource-group "shoplazza-addon-rg" \
  --server "shoplazza-addon-sql" \
  --name "ShoplazzaAddonDB" \
  --backup-storage-redundancy "Geo"
```

---

## 🔄 **Step 9: CI/CD Pipeline (Optional)**

### 9.1 **GitHub Actions Workflow**
Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ production ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build and publish
      run: |
        dotnet build --configuration Release
        dotnet publish -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'shoplazza-addon-app'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

### 9.2 **Azure DevOps Pipeline**
Create `azure-pipelines.yml`:

```yaml
trigger:
- production

pool:
  vmImage: 'ubuntu-latest'

variables:
  solution: '**/*.sln'
  buildPlatform: 'Any CPU'
  buildConfiguration: 'Release'

steps:
- task: DotNetCoreCLI@2
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--configuration $(buildConfiguration)'

- task: DotNetCoreCLI@2
  inputs:
    command: 'publish'
    publishWebProjects: true
    arguments: '--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)'
    zipAfterPublish: true

- task: AzureWebApp@1
  inputs:
    azureSubscription: 'Your-Azure-Subscription'
    appName: 'shoplazza-addon-app'
    package: '$(Build.ArtifactStagingDirectory)/**/*.zip'
```

---

## 🚨 **Troubleshooting**

### Common Issues

#### **Database Connection Issues**
```bash
# Check database connectivity
az sql db show \
  --resource-group "shoplazza-addon-rg" \
  --server "shoplazza-addon-sql" \
  --name "ShoplazzaAddonDB"
```

#### **App Service Logs**
```bash
# View application logs
az webapp log tail \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app"
```

#### **Environment Variables**
```bash
# List all app settings
az webapp config appsettings list \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app"
```

#### **SSL Certificate Issues**
```bash
# Check SSL binding
az webapp config ssl list \
  --resource-group "shoplazza-addon-rg"
```

---

## 📋 **Deployment Checklist**

### Pre-Deployment
- [ ] Azure resources created
- [ ] Secrets stored in Key Vault
- [ ] Environment variables configured
- [ ] Database created and accessible
- [ ] Custom domain configured (if needed)
- [ ] SSL certificate installed (if needed)

### Deployment
- [ ] Production branch created
- [ ] Application deployed successfully
- [ ] Database migrations applied
- [ ] Health checks passing
- [ ] OAuth flow tested
- [ ] Widget delivery tested
- [ ] Webhooks tested

### Post-Deployment
- [ ] Application Insights configured
- [ ] Monitoring alerts set up
- [ ] Backup strategy implemented
- [ ] CI/CD pipeline configured (optional)
- [ ] Documentation updated
- [ ] Team notified of deployment

---

## 🎯 **Next Steps**

1. **Register with Shoplazza**
   - Submit app for review
   - Configure app URLs in Shoplazza dashboard
   - Test with real merchants

2. **Performance Optimization**
   - Implement caching strategies
   - Optimize database queries
   - Monitor performance metrics

3. **Security Hardening**
   - Regular security audits
   - Dependency updates
   - Penetration testing

4. **Scaling Preparation**
   - Plan for increased load
   - Consider Azure Functions for webhooks
   - Implement CDN for static assets

---

## 📞 **Support Resources**

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure SQL Database Documentation](https://docs.microsoft.com/en-us/azure/sql-database/)
- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Shoplazza API Documentation](https://developers.shoplazza.com/)

**Deployment Status: 🟢 READY FOR PRODUCTION** 