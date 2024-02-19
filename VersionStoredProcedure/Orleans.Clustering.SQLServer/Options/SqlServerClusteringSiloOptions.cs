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
}
