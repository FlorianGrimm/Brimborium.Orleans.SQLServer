namespace Orleans.Configuration;

public class SqlServerClusteringClientOptions
{
    /// <summary>
    /// Connection string for Sql
    /// </summary>
    [Redact]
    public string ConnectionString { get; set; }
}
