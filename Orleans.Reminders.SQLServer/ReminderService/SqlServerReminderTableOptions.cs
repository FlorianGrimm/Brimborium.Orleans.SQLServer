namespace Orleans.Configuration;

/// <summary>
/// Options for SqlServer reminder storage.
/// </summary>
public class SqlServerReminderTableOptions {
    /// <summary>
    /// Gets or sets the SqlServer invariant.
    /// </summary>
    [Obsolete]
    public string Invariant { get; set; }

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    [Redact]
    public string ConnectionString { get; set; }
}