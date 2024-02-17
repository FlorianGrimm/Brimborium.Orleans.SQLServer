﻿namespace Orleans.Configuration;

public class SqlServerClusteringClientOptions
{
    /// <summary>
    /// Connection string for Sql
    /// </summary>
    [Redact]
    public string ConnectionString { get; set; }

    /// <summary>
    /// The invariant name of the connector for gatewayProvider's database.
    /// </summary>
    public string Invariant { get; set; }
}
