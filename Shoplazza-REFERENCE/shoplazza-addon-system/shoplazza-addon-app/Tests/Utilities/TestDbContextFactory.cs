using Microsoft.EntityFrameworkCore;
using ShoplazzaAddonApp.Data;

namespace ShoplazzaAddonApp.Tests.Utilities;

/// <summary>
/// Factory for creating test database contexts
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates an in-memory database context for testing
    /// </summary>
    /// <param name="databaseName">Unique name for the test database</param>
    /// <returns>Configured ApplicationDbContext for testing</returns>
    public static ApplicationDbContext CreateTestContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new TestApplicationDbContext(options);
    }

    /// <summary>
    /// Test-specific ApplicationDbContext that overrides problematic methods
    /// </summary>
    private class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Skip the problematic database provider detection for tests
            base.OnModelCreating(modelBuilder);
        }
    }
}
