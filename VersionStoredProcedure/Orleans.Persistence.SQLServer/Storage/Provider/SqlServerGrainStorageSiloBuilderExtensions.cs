using System;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers;

namespace Orleans.Hosting;

public static class SqlServerGrainStorageSiloBuilderExtensions
{
    /// <summary>
    /// Configure silo to use SqlServer grain storage as the default grain storage. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static ISiloBuilder AddSqlServerGrainStorageAsDefault(this ISiloBuilder builder, Action<SqlServerGrainStorageOptions> configureOptions)
    {
        return builder.AddSqlServerGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    /// Configure silo to use  SqlServer grain storage for grain storage. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static ISiloBuilder AddSqlServerGrainStorage(this ISiloBuilder builder, string name, Action<SqlServerGrainStorageOptions> configureOptions)
    {
        return builder.ConfigureServices(services => services.AddSqlServerGrainStorage(name, configureOptions));
    }

    /// <summary>
    /// Configure silo to use  SqlServer grain storage as the default grain storage. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static ISiloBuilder AddSqlServerGrainStorageAsDefault(this ISiloBuilder builder, Action<OptionsBuilder<SqlServerGrainStorageOptions>> configureOptions = null)
    {
        return builder.AddSqlServerGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    /// Configure silo to use SqlServer grain storage for grain storage. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static ISiloBuilder AddSqlServerGrainStorage(this ISiloBuilder builder, string name, Action<OptionsBuilder<SqlServerGrainStorageOptions>> configureOptions = null)
    {
        return builder.ConfigureServices(services => services.AddSqlServerGrainStorage(name, configureOptions));
    }
}
