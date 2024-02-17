using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime.ReminderService;

namespace Orleans.Hosting;

/// <summary>
/// Silo host builder extensions.
/// </summary>
public static class SiloBuilderReminderExtensions
{
    /// <summary>Adds reminder storage using SqlServer. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">Configuration delegate.</param>
    /// <returns>The provided <see cref="ISiloBuilder"/>, for chaining.</returns>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static ISiloBuilder UseSqlServerReminderService(
        this ISiloBuilder builder,
        Action<SqlServerReminderTableOptions> configureOptions)
    {
        return builder.UseSqlServerReminderService(ob => ob.Configure(configureOptions));
    }

    /// <summary>Adds reminder storage using SqlServer. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">Configuration delegate.</param>
    /// <returns>The provided <see cref="ISiloBuilder"/>, for chaining.</returns>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static ISiloBuilder UseSqlServerReminderService(
        this ISiloBuilder builder,
        Action<OptionsBuilder<SqlServerReminderTableOptions>> configureOptions)
    {
        return builder.ConfigureServices(services => services.UseSqlServerReminderService(configureOptions));
    }

    /// <summary>Adds reminder storage using SqlServer. Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Configuration delegate.</param>
    /// <returns>The provided <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <remarks>
    /// Instructions on configuring your database are available at <see href="http://aka.ms/orleans-sql-scripts"/>.
    /// </remarks>
    public static IServiceCollection UseSqlServerReminderService(this IServiceCollection services, Action<OptionsBuilder<SqlServerReminderTableOptions>> configureOptions)
    {
        services.AddReminders();
        services.AddSingleton<IReminderTable, SqlServerReminderTable>();
        services.ConfigureFormatter<SqlServerReminderTableOptions>();
        services.AddSingleton<IConfigurationValidator, SqlServerReminderTableOptionsValidator>();
        configureOptions(services.AddOptions<SqlServerReminderTableOptions>());
        return services;
    }
}