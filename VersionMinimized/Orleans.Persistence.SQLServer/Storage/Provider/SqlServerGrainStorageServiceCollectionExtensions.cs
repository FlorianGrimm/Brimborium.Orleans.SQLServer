using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Runtime.Hosting;
using Orleans.Storage;

namespace Orleans.Hosting;

/// <summary>
/// <see cref="IServiceCollection"/> extensions.
/// </summary>
public static class SqlServerGrainStorageServiceCollectionExtensions
{
    /// <summary>
    /// Configure silo to use  SqlServer grain storage as the default grain storage. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static IServiceCollection AddSqlServerGrainStorage(this IServiceCollection services, Action<SqlServerGrainStorageOptions> configureOptions)
    {
        return services.AddSqlServerGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, ob => ob.Configure(configureOptions));
    }

    /// <summary>
    /// Configure silo to use SqlServer grain storage for grain storage. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static IServiceCollection AddSqlServerGrainStorage(this IServiceCollection services, string name, Action<SqlServerGrainStorageOptions> configureOptions)
    {
        return services.AddSqlServerGrainStorage(name, ob => ob.Configure(configureOptions));
    }

    /// <summary>
    /// Configure silo to use SqlServer grain storage as the default grain storage. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static IServiceCollection AddSqlServerGrainStorageAsDefault(this IServiceCollection services, Action<OptionsBuilder<SqlServerGrainStorageOptions>> configureOptions = null)
    {
        return services.AddSqlServerGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    /// Configure silo to use SqlServer grain storage for grain storage. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </summary>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static IServiceCollection AddSqlServerGrainStorage(this IServiceCollection services, string name,
        Action<OptionsBuilder<SqlServerGrainStorageOptions>> configureOptions = null)
    {
        configureOptions?.Invoke(services.AddOptions<SqlServerGrainStorageOptions>(name));
        services.ConfigureNamedOptionForLogging<SqlServerGrainStorageOptions>(name);
        services.AddTransient<IPostConfigureOptions<SqlServerGrainStorageOptions>, DefaultStorageProviderSerializerOptionsConfigurator<SqlServerGrainStorageOptions>>();
        services.AddTransient<IConfigurationValidator>(sp => new SqlServerGrainStorageOptionsValidator(sp.GetRequiredService<IOptionsMonitor<SqlServerGrainStorageOptions>>().Get(name), name));
        return services.AddGrainStorage(name, SqlServerGrainStorageFactory.Create);
    }
} 
