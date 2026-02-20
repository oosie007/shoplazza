# 🚀 Azure App Service Deployment Guide (SQLite Edition)

## Overview
This guide provides step-by-step instructions for deploying the Shoplazza Add-On app to Azure App Service using **SQLite** instead of Azure SQL Database. This approach is **cost-effective** and perfect for MVP validation before scaling to SQL Server.

**Cost Savings**: ~$30-50/month (no Azure SQL Database required)

---

## 📋 **Prerequisites**

### 🔧 **Required Tools**
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (latest version)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/downloads)

### 🏗️ **Azure Resources (Minimal)**
- Azure Subscription with billing enabled
- Resource Group for the application
- Azure App Service Plan (Basic tier)
- Azure Key Vault (for secrets management)

---

## 🏗️ **Step 1: Azure Infrastructure Setup (Cost-Optimized)**

### 1.1 **Create Resource Group**
```bash
# Login to Azure
az login

# Create resource group
az group create \
  --name "shoplazza-addon-rg" \
  --location "East US" \
  --tags "Environment=Production" "Project=ShoplazzaAddon" "Database=SQLite"
```

### 1.2 **Create App Service Plan (Basic Tier)**
```bash
# Create Basic App Service Plan (cost-effective for MVP)
az appservice plan create \
  --name "shoplazza-addon-plan" \
  --resource-group "shoplazza-addon-rg" \
  --sku "B1" \
  --is-linux \
  --tags "Environment=Production" "Tier=Basic"
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
  --tags "Environment=Production" "Database=SQLite"
```

### 1.4 **Create Key Vault (Minimal)**
```bash
# Create Key Vault for secrets
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
```

### 2.2 **Configure App Service Environment Variables**
```bash
# Set environment variables for SQLite
az webapp config appsettings set \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app" \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    SHOPLAZZA_REDIRECT_URI="https://shoplazza-addon-app.azurewebsites.net/api/auth/callback" \
    SHOPLAZZA_APP_URL="https://shoplazza-addon-app.azurewebsites.net/api/auth" \
    LOGGING_LEVEL="Information" \
    SESSION_TIMEOUT_MINUTES="30" \
    DATABASE_PROVIDER="SQLite" \
    ConnectionStrings__DefaultConnection="Data Source=/home/site/wwwroot/Data/shoplazza_addon.db"
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
    @Microsoft.KeyVault(SecretUri=https://shoplazza-addon-kv.vault.azure.net/secrets/EncryptionKey/)
```

---

## 🌐 **Step 3: Configure Custom Domain & SSL (Optional)**

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

---

## 📦 **Step 4: Database Setup (SQLite)**

### 4.1 **Update appsettings.Production.json**
Create or update `appsettings.Production.json` in your project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/home/site/wwwroot/Data/shoplazza_addon.db"
  },
  "DatabaseProvider": "SQLite",
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### 4.2 **Ensure SQLite Package is Installed**
```bash
# Navigate to the app directory
cd shoplazza-addon-app

# Ensure SQLite package is installed
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

### 4.3 **Create Data Directory Structure**
```bash
# Create the data directory structure
mkdir -p Data
```

### 4.4 **Update Program.cs for SQLite**
Ensure your `Program.cs` has SQLite configuration:

```csharp
// In Program.cs, ensure this configuration exists:
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "SQLite";
    
    if (databaseProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});
```

---

## 🚀 **Step 5: Deploy Application**

### 5.1 **Create Production Branch**
```bash
# Create production branch
git checkout -b production-sqlite

# Ensure all changes are committed
git add .
git commit -m "Configure for SQLite production deployment"
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
git push azure production-sqlite:master
```

### 5.4 **Alternative: Deploy via Azure CLI**
```bash
# Build the application
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip .

# Deploy using Azure CLI
az webapp deployment source config-zip \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app" \
  --src "../deploy.zip"
```

---

## 🔧 **Step 6: Post-Deployment Configuration**

### 6.1 **Create Data Directory on Azure**
```bash
# Connect to Azure App Service via Kudu
# Go to: https://shoplazza-addon-app.scm.azurewebsites.net/webssh/host

# Create data directory
mkdir -p /home/site/wwwroot/Data
```

### 6.2 **Run Database Migrations**
```bash
# Navigate to the app directory
cd shoplazza-addon-app

# Run migrations for SQLite
dotnet ef database update --connection "Data Source=/home/site/wwwroot/Data/shoplazza_addon.db"
```

### 6.3 **Configure Application Insights (Optional)**
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

# Set Application Insights key
az webapp config appsettings set \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app" \
  --settings \
    APPINSIGHTS_INSTRUMENTATIONKEY="your_instrumentation_key"
```

### 6.4 **Configure CORS (if needed)**
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

### 7.2 **Database Connection Test**
```bash
# Test database connectivity
curl https://shoplazza-addon-app.azurewebsites.net/api/products
```

### 7.3 **OAuth Flow Test**
1. Navigate to: `https://shoplazza-addon-app.azurewebsites.net/api/auth?shop=your-test-shop.myshoplazza.com`
2. Verify OAuth flow works correctly
3. Check token storage and validation

### 7.4 **Widget Delivery Test**
1. Test widget script delivery: `https://shoplazza-addon-app.azurewebsites.net/api/widget/widget.js?shop=your-test-shop.myshoplazza.com`
2. Verify JSONP configuration: `https://shoplazza-addon-app.azurewebsites.net/api/widget/config?shop=your-test-shop.myshoplazza.com&productId=123&callback=test`

---

## 📊 **Step 8: Monitoring & Maintenance**

### 8.1 **Set Up Basic Alerts**
```bash
# Create availability alert
az monitor metrics alert create \
  --name "shoplazza-addon-availability" \
  --resource-group "shoplazza-addon-rg" \
  --scopes "/subscriptions/your-subscription-id/resourceGroups/shoplazza-addon-rg/providers/Microsoft.Web/sites/shoplazza-addon-app" \
  --condition "avg availability percentage < 99" \
  --description "Alert when app availability drops below 99%"
```

### 8.2 **Database Backup Strategy**
Since SQLite is file-based, implement a backup strategy:

```bash
# Create a backup script
cat > backup-sqlite.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/home/site/wwwroot/backups"
mkdir -p $BACKUP_DIR
cp /home/site/wwwroot/Data/shoplazza_addon.db $BACKUP_DIR/shoplazza_addon_$DATE.db
# Keep only last 7 days of backups
find $BACKUP_DIR -name "*.db" -mtime +7 -delete
EOF

# Make executable
chmod +x backup-sqlite.sh
```

---

## 💰 **Cost Comparison**

### **SQLite Deployment (This Guide)**
```
Azure App Service (B1): $13.14/month
Azure Key Vault: $0.03/month
Application Insights: $0.00/month (first 5GB free)
Custom Domain SSL: $0.00/month (free with App Service)

TOTAL: ~$13.17/month
```

### **SQL Server Deployment (Original Guide)**
```
Azure App Service (B1): $13.14/month
Azure SQL Database (Basic): $5.00/month
Azure Key Vault: $0.03/month
Application Insights: $0.00/month
Custom Domain SSL: $0.00/month

TOTAL: ~$18.17/month
```

**💰 Monthly Savings: $5.00**
**💰 Annual Savings: $60.00**

---

## ⚠️ **SQLite Limitations & Considerations**

### **Pros**
- ✅ **Cost-effective**: No additional database costs
- ✅ **Simple setup**: File-based, no server management
- ✅ **Perfect for MVP**: Ideal for validation and testing
- ✅ **Easy backup**: Single file backup
- ✅ **No connection limits**: No concurrent user restrictions

### **Cons**
- ⚠️ **Limited concurrency**: Single writer at a time
- ⚠️ **No advanced features**: No stored procedures, complex queries
- ⚠️ **File size limits**: May grow large over time
- ⚠️ **No built-in replication**: Manual backup required
- ⚠️ **Limited scalability**: Not suitable for high-traffic production

### **When to Upgrade to SQL Server**
- **User Growth**: When you have 100+ concurrent users
- **Data Volume**: When database exceeds 1GB
- **Complex Queries**: When you need advanced SQL features
- **High Availability**: When you need 99.9%+ uptime
- **Compliance**: When you need enterprise security features

---

## 🔄 **Migration Path to SQL Server**

When you're ready to scale, here's the migration path:

### **1. Prepare Migration**
```bash
# Create SQL Server database
az sql server create \
  --name "shoplazza-addon-sql" \
  --resource-group "shoplazza-addon-rg" \
  --location "East US" \
  --admin-user "sqladmin" \
  --admin-password "YourStrongPassword123!"

az sql db create \
  --resource-group "shoplazza-addon-rg" \
  --server "shoplazza-addon-sql" \
  --name "ShoplazzaAddonDB" \
  --edition "Basic"
```

### **2. Data Migration**
```bash
# Export SQLite data
sqlite3 /home/site/wwwroot/Data/shoplazza_addon.db ".dump" > backup.sql

# Convert and import to SQL Server
# (Use a migration script to convert SQLite syntax to SQL Server)
```

### **3. Update Configuration**
```bash
# Update connection string
az webapp config appsettings set \
  --resource-group "shoplazza-addon-rg" \
  --name "shoplazza-addon-app" \
  --settings \
    DATABASE_PROVIDER="SQLServer" \
    ConnectionStrings__DefaultConnection="Server=shoplazza-addon-sql.database.windows.net;Database=ShoplazzaAddonDB;User Id=sqladmin;Password=YourStrongPassword123!;Encrypt=true;TrustServerCertificate=false;MultipleActiveResultSets=true"
```

---

## 📋 **SQLite Deployment Checklist**

### Pre-Deployment
- [ ] Azure App Service created (B1 tier)
- [ ] Azure Key Vault created
- [ ] Secrets stored in Key Vault
- [ ] Environment variables configured
- [ ] SQLite package installed
- [ ] Data directory structure created
- [ ] Custom domain configured (if needed)

### Deployment
- [ ] Production branch created
- [ ] Application deployed successfully
- [ ] Data directory created on Azure
- [ ] Database migrations applied
- [ ] Health checks passing
- [ ] OAuth flow tested
- [ ] Widget delivery tested

### Post-Deployment
- [ ] Application Insights configured (optional)
- [ ] Basic monitoring alerts set up
- [ ] Backup strategy implemented
- [ ] Performance monitoring active
- [ ] Documentation updated

---

## 🎯 **Next Steps**

1. **Deploy with SQLite** - Use this guide for MVP validation
2. **Test with Real Merchants** - Validate the app functionality
3. **Monitor Performance** - Track usage and performance metrics
4. **Plan Migration** - When ready, migrate to SQL Server
5. **Scale Infrastructure** - Add more resources as needed

---

## 📞 **Support Resources**

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [Entity Framework Core SQLite](https://docs.microsoft.com/en-us/ef/core/providers/sqlite/)
- [Shoplazza API Documentation](https://developers.shoplazza.com/)

**Deployment Status: 🟢 READY FOR MVP VALIDATION**

---

## 💡 **Pro Tips for SQLite Production**

1. **Regular Backups**: Set up automated daily backups
2. **Monitor File Size**: Watch database growth
3. **Performance Tuning**: Use appropriate indexes
4. **Connection Management**: Implement proper connection pooling
5. **Error Handling**: Robust error handling for file operations

**This SQLite deployment is perfect for MVP validation and can easily scale to SQL Server when you're ready!** 🚀 