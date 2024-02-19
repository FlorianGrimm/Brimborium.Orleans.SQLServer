namespace Orleans.Hosting;

/// <summary>
/// Extensions for configuring SqlServer for clustering.
/// </summary>
public static class SqlServerHostingExtensions
{
    /// <summary>
    /// Configures this silo to use SqlServer for clustering. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <param name="builder">
    /// The builder.
    /// </param>
    /// <param name="configureOptions">
    /// The configuration delegate.
    /// </param>
    /// <returns>
    /// The provided <see cref="ISiloBuilder"/>.
    /// </returns>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static ISiloBuilder UseSqlServerClustering(
        this ISiloBuilder builder,
        Action<SqlServerClusteringSiloOptions> configureOptions)
    {
        return builder.ConfigureServices(
            services =>
            {
                if (configureOptions != null)
                {
                    services.Configure(configureOptions);
                }

                services.AddSingleton<IMembershipTable, SqlServerClusteringTable>();
                services.AddSingleton<IConfigurationValidator, SqlServerClusteringSiloOptionsValidator>();
            });
    }

    /// <summary>
    /// Configures this silo to use SqlServer for clustering. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <param name="builder">
    /// The builder.
    /// </param>
    /// <param name="configureOptions">
    /// The configuration delegate.
    /// </param>
    /// <returns>
    /// The provided <see cref="ISiloBuilder"/>.
    /// </returns>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static ISiloBuilder UseSqlServerClustering(
        this ISiloBuilder builder,
        Action<OptionsBuilder<SqlServerClusteringSiloOptions>> configureOptions)
    {
        return builder.ConfigureServices(
            services =>
            {
                configureOptions?.Invoke(services.AddOptions<SqlServerClusteringSiloOptions>());
                services.AddSingleton<IMembershipTable, SqlServerClusteringTable>();
                services.AddSingleton<IConfigurationValidator, SqlServerClusteringSiloOptionsValidator>();
            });
    }

    /// <summary>
    /// Configures this client to use SqlServer for clustering. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <param name="builder">
    /// The builder.
    /// </param>
    /// <param name="configureOptions">
    /// The configuration delegate.
    /// </param>
    /// <returns>
    /// The provided <see cref="IClientBuilder"/>.
    /// </returns>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static IClientBuilder UseSqlServerClustering(
        this IClientBuilder builder,
        Action<SqlServerClusteringClientOptions> configureOptions)
    {
        return builder.ConfigureServices(
            services =>
            {
                if (configureOptions != null)
                {
                    services.Configure(configureOptions);
                }

                services.AddSingleton<IGatewayListProvider, SqlServerGatewayListProvider>();
                services.AddSingleton<IConfigurationValidator, SqlServerClusteringClientOptionsValidator>();
            });
    }

    /// <summary>
    /// Configures this client to use SqlServer for clustering. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <param name="builder">
    /// The builder.
    /// </param>
    /// <param name="configureOptions">
    /// The configuration delegate.
    /// </param>
    /// <returns>
    /// The provided <see cref="IClientBuilder"/>.
    /// </returns>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static IClientBuilder UseSqlServerClustering(
        this IClientBuilder builder,
        Action<OptionsBuilder<SqlServerClusteringClientOptions>> configureOptions)
    {
        return builder.ConfigureServices(
            services =>
            {
                configureOptions?.Invoke(services.AddOptions<SqlServerClusteringClientOptions>());
                services.AddSingleton<IGatewayListProvider, SqlServerGatewayListProvider>();
                services.AddSingleton<IConfigurationValidator, SqlServerClusteringClientOptionsValidator>();
            });
    }
}
