using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShoplazzaAddonFunctions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Add Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configure Entity Framework with database provider selection
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection") ??
            DatabaseConfiguration.DefaultConnectionStrings.GetDefault("Development");

        var databaseProvider = DatabaseConfiguration.DetectProvider(connectionString);

        // Ensure SQLite directory exists if using SQLite
        if (databaseProvider == DatabaseConfiguration.DatabaseProvider.Sqlite)
        {
            DatabaseConfiguration.EnsureSqliteDirectoryExists(connectionString);
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            DatabaseConfiguration.ConfigureDatabase(options, connectionString, databaseProvider));

        // Add HTTP client
        services.AddHttpClient();

        // Add memory cache
        services.AddMemoryCache();
    })
    .Build();

host.Run(); 