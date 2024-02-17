namespace Orleans.Configuration;

/// <summary>
/// Options for SqlServer clustering
/// </summary>
public class SqlServerClusteringSiloOptions
{
    /// <summary>
    /// Connection string for SqlServer Storage
    /// </summary>
    [Redact]
    public string ConnectionString { get; set; }

    /// <summary>
    /// The invariant name of the connector for membership's database.
    /// </summary>
    public string Invariant { get; set; }
}
