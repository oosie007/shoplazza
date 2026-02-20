using Microsoft.EntityFrameworkCore;
using ShoplazzaAddonApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using ShoplazzaAddonApp.Middleware;
using ShoplazzaAddonApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/auth";
        options.AccessDeniedPath = "/api/auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Shoplazza Add-On API",
        Version = "v1",
        Description = "API for managing optional product add-ons in Shoplazza stores",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Shoplazza Add-On System",
            Email = "support@your-domain.com"
        }
    });

    // Include XML documentation if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add security definition for OAuth
    c.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2,
        Flows = new Microsoft.OpenApi.Models.OpenApiOAuthFlows
        {
            AuthorizationCode = new Microsoft.OpenApi.Models.OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("https://admin.myshoplaza.com/oauth/authorize"),
                TokenUrl = new Uri("https://admin.myshoplaza.com/oauth/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "read_product", "Read product information" },
                    { "write_product", "Modify product information" },
                    { "read_order", "Read order information" },
                    { "write_order", "Modify order information" },
                    { "read_script_tags", "Read script tags" },
                    { "write_script_tags", "Manage script tags" },
                    { "read_shop", "Read shop information" }
                }
            }
        }
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "read_product", "write_product", "read_order", "write_order", "read_script_tags", "write_script_tags", "read_shop" }
        }
    });
});

// Configure Entity Framework with database provider selection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    DatabaseConfiguration.DefaultConnectionStrings.GetDefault(builder.Environment.EnvironmentName);

var databaseProvider = DatabaseConfiguration.DetectProvider(connectionString);

// Ensure SQLite directory exists if using SQLite
if (databaseProvider == DatabaseConfiguration.DatabaseProvider.Sqlite)
{
    DatabaseConfiguration.EnsureSqliteDirectoryExists(connectionString);
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    DatabaseConfiguration.ConfigureDatabase(options, connectionString, databaseProvider));

// Add HTTP client for external API calls
builder.Services.AddHttpClient<IShoplazzaAuthService, ShoplazzaAuthService>();
builder.Services.AddHttpClient<IMerchantService, MerchantService>();
builder.Services.AddHttpClient<IShoplazzaApiService, ShoplazzaApiService>();

// Register repository pattern
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register custom services
builder.Services.AddScoped<IShoplazzaAuthService, ShoplazzaAuthService>();
builder.Services.AddScoped<IMerchantService, MerchantService>();
builder.Services.AddScoped<IShoplazzaApiService, ShoplazzaApiService>();
builder.Services.AddScoped<IProductAddOnService, ProductAddOnService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IShoplazzaFunctionApiService, ShoplazzaFunctionApiService>();
builder.Services.AddScoped<ICartTransformFunctionService, CartTransformFunctionService>();

// Register global function startup service
builder.Services.AddHostedService<GlobalFunctionStartupService>();

// Add session support for OAuth state management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add CORS if needed for frontend integration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseSession();

// Add custom middleware
app.UseMiddleware<HmacValidationMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
   .WithName("HealthCheck")
   .WithOpenApi();

// Apply EF Core migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Startup");
        logger.LogError(ex, "Failed to apply EF Core migrations on startup");
        // Continue running the app; database might be read-only or already up-to-date
    }
}

app.Run();
