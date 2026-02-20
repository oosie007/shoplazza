using Microsoft.EntityFrameworkCore;
using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Models.Configuration;
using Newtonsoft.Json;

namespace ShoplazzaAddonApp.Data;

/// <summary>
/// Entity Framework database context for the Shoplazza Add-On application
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly DatabaseConfiguration.DatabaseProvider _databaseProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        // Detect database provider from connection string
        try
        {
            var connectionString = Database.GetConnectionString();
            _databaseProvider = !string.IsNullOrEmpty(connectionString) 
                ? DatabaseConfiguration.DetectProvider(connectionString)
                : DatabaseConfiguration.DatabaseProvider.Sqlite; // Default to SQLite for development
        }
        catch (InvalidOperationException)
        {
            // In-memory database doesn't support GetConnectionString
            // Default to SQLite for development environment
            _databaseProvider = DatabaseConfiguration.DatabaseProvider.Sqlite;
        }
    }

    /// <summary>
    /// Merchants (Shoplazza stores)
    /// </summary>
    public DbSet<Merchant> Merchants { get; set; }

    /// <summary>
    /// Product add-on configurations
    /// </summary>
    public DbSet<ProductAddOn> ProductAddOns { get; set; }

    /// <summary>
    /// Global configuration settings
    /// </summary>
    public DbSet<Configuration> Configurations { get; set; }

    /// <summary>
    /// Function configurations for cart-transform functions
    /// </summary>
    public DbSet<Models.Configuration.FunctionConfiguration> FunctionConfigurations { get; set; }

    /// <summary>
    /// Global function configurations that are created once and reused across all merchants
    /// </summary>
    public DbSet<Models.Configuration.GlobalFunctionConfiguration> GlobalFunctionConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use SQLite defaults for consistency (since we're primarily using SQLite)
        var defaultDateTimeSql = "datetime('now')";

        // Configure Merchant entity
        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Shop).IsUnique();
            entity.Property(e => e.Shop).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StoreName).HasMaxLength(500);
            entity.Property(e => e.StoreEmail).HasMaxLength(255);
            entity.Property(e => e.AccessToken).HasMaxLength(1000);
            entity.Property(e => e.Scopes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(defaultDateTimeSql);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(defaultDateTimeSql);

            // Configure relationships
            entity.HasMany(e => e.ProductAddOns)
                  .WithOne(e => e.Merchant)
                  .HasForeignKey(e => e.MerchantId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Configuration)
                  .WithOne(e => e.Merchant)
                  .HasForeignKey<Configuration>(e => e.MerchantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ProductAddOn entity
        modelBuilder.Entity<ProductAddOn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MerchantId, e.ProductId }).IsUnique();
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.ProductTitle).HasMaxLength(500);
            entity.Property(e => e.ProductHandle).HasMaxLength(255);
            entity.Property(e => e.AddOnTitle).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AddOnDescription).HasMaxLength(1000);
            entity.Property(e => e.AddOnPriceCents).IsRequired();
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.DisplayText).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AddOnSku).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(defaultDateTimeSql);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(defaultDateTimeSql);

            // Ignore computed properties
            entity.Ignore(e => e.FormattedPrice);
            entity.Ignore(e => e.Price);
        });

        // Configure Configuration entity
        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MerchantId).IsUnique();
            entity.Property(e => e.MerchantId).IsRequired();
            entity.Property(e => e.DefaultCurrency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(defaultDateTimeSql);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(defaultDateTimeSql);
        });

        // Configure FunctionConfiguration entity
        modelBuilder.Entity<Models.Configuration.FunctionConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MerchantId);
            entity.Property(e => e.MerchantId).IsRequired();
            entity.Property(e => e.FunctionId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FunctionName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FunctionType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.ConfigurationJson).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(defaultDateTimeSql);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(defaultDateTimeSql);

            // Configure relationship
            entity.HasOne(e => e.Merchant)
                  .WithMany()
                  .HasForeignKey(e => e.MerchantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Override SaveChanges to automatically update UpdatedAt timestamps
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically update UpdatedAt timestamps
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates the UpdatedAt timestamp for modified entities
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Merchant merchant)
            {
                if (entry.State == EntityState.Added)
                {
                    merchant.CreatedAt = DateTime.UtcNow;
                }
                merchant.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is ProductAddOn productAddOn)
            {
                if (entry.State == EntityState.Added)
                {
                    productAddOn.CreatedAt = DateTime.UtcNow;
                }
                productAddOn.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Configuration configuration)
            {
                if (entry.State == EntityState.Added)
                {
                    configuration.CreatedAt = DateTime.UtcNow;
                }
                configuration.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}