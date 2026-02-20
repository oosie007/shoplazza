using Microsoft.EntityFrameworkCore;
using ShoplazzaAddonFunctions.Models;
using System.Text.Json;

namespace ShoplazzaAddonFunctions.Data;

/// <summary>
/// Entity Framework database context for the Azure Functions app
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly DatabaseConfiguration.DatabaseProvider _databaseProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        // Detect database provider from connection string
        var connectionString = Database.GetConnectionString();
        _databaseProvider = !string.IsNullOrEmpty(connectionString) 
            ? DatabaseConfiguration.DetectProvider(connectionString)
            : DatabaseConfiguration.DatabaseProvider.SqlServer;
    }

    /// <summary>
    /// Merchants table
    /// </summary>
    public DbSet<Merchant> Merchants { get; set; }

    /// <summary>
    /// Orders table
    /// </summary>
    public DbSet<Order> Orders { get; set; }

    /// <summary>
    /// Order line items table
    /// </summary>
    public DbSet<OrderLineItem> OrderLineItems { get; set; }

    /// <summary>
    /// Sync states table
    /// </summary>
    public DbSet<SyncState> SyncStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Get database-specific default datetime SQL
        var defaultDateTimeSql = DatabaseConfiguration.GetDefaultDateTimeSql(_databaseProvider);

        // Configure Merchant entity
        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Shop).IsUnique();
            entity.HasIndex(e => e.IsActive);
            
            entity.Property(e => e.Shop).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AccessToken).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(defaultDateTimeSql);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(defaultDateTimeSql);

            // Configure relationships
            entity.HasMany(e => e.Orders)
                  .WithOne(e => e.Merchant)
                  .HasForeignKey(e => e.MerchantId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SyncState)
                  .WithOne(e => e.Merchant)
                  .HasForeignKey<SyncState>(e => e.MerchantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShoplazzaOrderId).IsUnique();
            entity.HasIndex(e => e.MerchantId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.FinancialStatus);
            entity.HasIndex(e => e.Status);
            
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AddOnRevenue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.FinancialStatus).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Source).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(defaultDateTimeSql);
            entity.Property(e => e.LastSyncedAt).HasDefaultValueSql(defaultDateTimeSql);

            // Foreign key relationship
            entity.HasOne(e => e.Merchant)
                .WithMany(m => m.Orders)
                .HasForeignKey(e => e.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure OrderLineItem entity
        modelBuilder.Entity<OrderLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.HasAddOn);
            
            entity.Property(e => e.ProductTitle).IsRequired().HasMaxLength(255);
            entity.Property(e => e.VariantTitle).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AddOnPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AddOnTitle).HasMaxLength(255);
            entity.Property(e => e.AddOnSku).HasMaxLength(100);
            entity.Property(e => e.Properties).HasColumnType("text");

            // Foreign key relationship
            entity.HasOne(e => e.Order)
                .WithMany(o => o.LineItems)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SyncState entity
        modelBuilder.Entity<SyncState>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MerchantId).IsUnique();
            entity.HasIndex(e => e.LastSyncAt);
            entity.HasIndex(e => e.SyncStatus);
            
            entity.Property(e => e.SyncStatus).IsRequired().HasMaxLength(20);
            entity.Property(e => e.LastError).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(defaultDateTimeSql);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(defaultDateTimeSql);

            // Foreign key relationship
            entity.HasOne(e => e.Merchant)
                .WithOne(m => m.SyncState)
                .HasForeignKey<SyncState>(e => e.MerchantId)
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
            else if (entry.Entity is Order order)
            {
                if (entry.State == EntityState.Added)
                {
                    order.CreatedAt = DateTime.UtcNow;
                }
                order.LastSyncedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is SyncState syncState)
            {
                if (entry.State == EntityState.Added)
                {
                    syncState.CreatedAt = DateTime.UtcNow;
                }
                syncState.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
} 