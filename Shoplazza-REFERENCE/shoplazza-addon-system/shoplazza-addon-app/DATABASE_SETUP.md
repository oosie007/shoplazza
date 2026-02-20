# Database Configuration Guide

The Shoplazza Add-On application supports both **SQL Server** and **SQLite** databases, allowing for flexible deployment scenarios.

## 🗂️ Database Provider Support

### SQLite (Default for Development)
- **Best for**: POC, Development, Quick Setup, Local Testing
- **Advantages**: Zero configuration, file-based, no server required
- **File location**: `shoplazza_addon_dev.db` (configurable)

### SQL Server (Recommended for Production)  
- **Best for**: Production, High Performance, Scalability
- **Advantages**: Enterprise features, better performance at scale
- **Requirements**: SQL Server instance (LocalDB, Express, or Full)

---

## 🔧 Configuration

### Environment-Based Configuration

The application automatically detects the database provider from the connection string:

**Development (SQLite - Default):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=shoplazza_addon_dev.db"
  }
}
```

**Production (SQL Server):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=ShoplazzaAddonApp;User Id=user;Password=password;Encrypt=true;TrustServerCertificate=false;MultipleActiveResultSets=true"
  }
}
```

### Available Connection Strings

The `appsettings.json` provides several pre-configured options:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=shoplazza_addon.db",
    "SqlServerConnection": "Server=(localdb)\\mssqllocaldb;Database=ShoplazzaAddonApp;Trusted_Connection=true;MultipleActiveResultSets=true",
    "SqliteConnection": "Data Source=shoplazza_addon.db",
    "SqliteInMemoryConnection": "Data Source=:memory:"
  }
}
```

---

## 🚀 Quick Setup

### For POC/Development (SQLite)

1. **No additional setup required!** SQLite is the default for development.

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Database file created automatically:** `shoplazza_addon_dev.db`

### For Production (SQL Server)

1. **Update connection string** in `appsettings.Production.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your-production-server;Database=ShoplazzaAddonApp;User Id=your-user;Password=your-password;Encrypt=true;TrustServerCertificate=false;MultipleActiveResultSets=true"
     }
   }
   ```

2. **Apply migrations:**
   ```bash
   dotnet ef database update --context ApplicationDbContext
   ```

---

## 🔄 Switching Between Databases

### Method 1: Environment Variables
```bash
export ConnectionStrings__DefaultConnection="Data Source=different.db"
dotnet run
```

### Method 2: Configuration Override
Update the `DefaultConnection` in your environment-specific `appsettings.{Environment}.json`

### Method 3: Command Line
```bash
dotnet run --ConnectionStrings:DefaultConnection="Data Source=test.db"
```

---

## 🛠️ Database Management

### Create New Migration
```bash
dotnet ef migrations add MigrationName --context ApplicationDbContext
```

### Apply Migrations
```bash
dotnet ef database update --context ApplicationDbContext
```

### Reset Database (SQLite)
```bash
rm *.db
dotnet ef database update --context ApplicationDbContext
```

### View Current Database
```bash
dotnet ef database info --context ApplicationDbContext
```

---

## 📊 Database Provider Detection

The application automatically detects the database provider:

```csharp
// SQLite indicators
"data source=" + (".db" || ".sqlite")

// SQL Server indicators  
"server=" || ("data source=" + ("localdb" || "mssqllocaldb"))
```

### Manual Provider Selection

For advanced scenarios, you can force a specific provider:

```csharp
// In Program.cs - example for forcing SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    DatabaseConfiguration.ConfigureDatabase(
        options, 
        connectionString, 
        DatabaseConfiguration.DatabaseProvider.Sqlite
    ));
```

---

## 🔍 Troubleshooting

### SQLite Issues

**Database locked:**
```bash
# Kill any running processes
pkill -f ShoplazzaAddonApp
rm *.db-wal *.db-shm  # Remove SQLite temp files
```

**Permission denied:**
```bash
chmod 664 *.db  # Fix file permissions
```

### SQL Server Issues

**Connection failed:**
- Verify SQL Server is running
- Check connection string credentials
- Ensure database exists
- Check firewall settings

**LocalDB not found:**
```bash
# Install SQL Server LocalDB
# Windows: Download from Microsoft
# macOS: Use Docker or SQL Server in Azure
```

---

## 📈 Performance Considerations

### SQLite Limitations
- Single writer at a time
- No stored procedures
- Limited concurrent connections
- File I/O bound

### When to Use Each

| Scenario | Recommended |
|----------|-------------|
| POC/Demo | SQLite |
| Development | SQLite |
| Local Testing | SQLite |
| CI/CD Pipeline | SQLite (in-memory) |
| Staging | SQL Server |
| Production | SQL Server |
| High Concurrency | SQL Server |
| Enterprise | SQL Server |

---

## 📝 Configuration Examples

### SQLite Development Setup
```bash
# appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=shoplazza_addon_dev.db"
  },
  "Database": {
    "Provider": "Sqlite"
  }
}
```

### SQL Server Production Setup
```bash
# appsettings.Production.json  
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:yourserver.database.windows.net,1433;Database=ShoplazzaAddonApp;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "Database": {
    "Provider": "SqlServer"
  }
}
```

### Docker Setup (SQL Server)
```bash
# Docker Compose example
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong@Passw0rd"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
      
  app:
    build: .
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=ShoplazzaAddonApp;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
    depends_on:
      - sqlserver
```

---

## ✅ Verification

### Test Database Connection
```csharp
// Health check endpoint automatically tests database
curl http://localhost:5128/health
```

### View Database Schema
```bash
# SQLite
sqlite3 shoplazza_addon_dev.db ".schema"

# SQL Server
sqlcmd -S localhost -E -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'"
```

---

## 🔐 Security Notes

### SQLite Security
- Database file permissions (600 or 644)
- No network exposure by default
- Consider encryption at rest for sensitive data

### SQL Server Security  
- Use strong passwords
- Enable encryption in transit (Encrypt=True)
- Use managed identity in Azure
- Regular security updates
- Network security groups

---

This flexible database configuration ensures that the Shoplazza Add-On application can be deployed in various environments while maintaining optimal performance and ease of development.