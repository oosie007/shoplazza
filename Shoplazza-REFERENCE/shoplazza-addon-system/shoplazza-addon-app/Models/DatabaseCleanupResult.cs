namespace ShoplazzaAddonApp.Models;

/// <summary>
/// Result of a database cleanup operation
/// </summary>
public class DatabaseCleanupResult
{
    /// <summary>
    /// Whether the cleanup was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if cleanup failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Details about what was cleaned up
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Number of records deleted from each table
    /// </summary>
    public Dictionary<string, int> RecordsDeleted { get; set; } = new();

    /// <summary>
    /// Timestamp when cleanup was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful cleanup result
    /// </summary>
    public static DatabaseCleanupResult SuccessResult(string details, Dictionary<string, int> recordsDeleted)
    {
        return new DatabaseCleanupResult
        {
            Success = true,
            Details = details,
            RecordsDeleted = recordsDeleted
        };
    }

    /// <summary>
    /// Creates a failed cleanup result
    /// </summary>
    public static DatabaseCleanupResult FailureResult(string error)
    {
        return new DatabaseCleanupResult
        {
            Success = false,
            Error = error
        };
    }
}
