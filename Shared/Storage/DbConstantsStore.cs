using System.Collections.Generic;

#if CLUSTERING_SqlServer
namespace Orleans.Clustering.SqlServer.Storage;
#elif PERSISTENCE_SqlServer
namespace Orleans.Persistence.SqlServer.Storage;
#elif REMINDERS_SqlServer
namespace Orleans.Reminders.SqlServer.Storage;
#elif TESTER_SQLUTILS
namespace Orleans.Tests.SqlUtils
#else
// No default namespace intentionally to cause compile errors if something is not defined
#endif

internal static class DbConstantsStore
{
    private static readonly Dictionary<string, DbConstants> invariantNameToConsts =
        new Dictionary<string, DbConstants>
        {
            {
                SqlServerInvariants.InvariantNameSqlServer,
                new DbConstants(startEscapeIndicator: '[',
                                endEscapeIndicator: ']',
                                unionAllSelectTemplate: " UNION ALL SELECT ",
                                isSynchronousSqlServerImplementation: false,
                                supportsStreamNatively: true,
                                supportsCommandCancellation: true,
                                commandInterceptor: NoOpCommandInterceptor.Instance)
            },
            {SqlServerInvariants.InvariantNameMySql, new DbConstants(
                                startEscapeIndicator: '`',
                                endEscapeIndicator: '`',
                                unionAllSelectTemplate: " UNION ALL SELECT ",
                                isSynchronousSqlServerImplementation: true,
                                supportsStreamNatively: false,
                                supportsCommandCancellation: false,
                                commandInterceptor: NoOpCommandInterceptor.Instance)
            },
            {SqlServerInvariants.InvariantNamePostgreSql, new DbConstants(
                                startEscapeIndicator: '"',
                                endEscapeIndicator: '"',
                                unionAllSelectTemplate: " UNION ALL SELECT ",
                                isSynchronousSqlServerImplementation: true, //there are some intermittent PostgreSQL problems too, see more discussion at https://github.com/dotnet/orleans/pull/2949.
                                supportsStreamNatively: true,
                                supportsCommandCancellation: true, // See https://dev.mysql.com/doc/connector-net/en/connector-net-ref-mysqlclient-mysqlcommandmembers.html.
                                commandInterceptor: NoOpCommandInterceptor.Instance)

            },
            {SqlServerInvariants.InvariantNameOracleDatabase, new DbConstants(
                                startEscapeIndicator: '\"',
                                endEscapeIndicator: '\"',
                                unionAllSelectTemplate: " FROM DUAL UNION ALL SELECT ",
                                isSynchronousSqlServerImplementation: true,
                                supportsStreamNatively: false,
                                supportsCommandCancellation: false, // Is supported but the remarks sound scary: https://docs.oracle.com/cd/E11882_01/win.112/e23174/OracleCommandClass.htm#DAFIEHHG.
                                commandInterceptor: OracleCommandInterceptor.Instance)

            },
            {
                SqlServerInvariants.InvariantNameSqlServerDotnetCore,
                new DbConstants(startEscapeIndicator: '[',
                                endEscapeIndicator: ']',
                                unionAllSelectTemplate: " UNION ALL SELECT ",
                                isSynchronousSqlServerImplementation: false,
                                supportsStreamNatively: true,
                                supportsCommandCancellation: true,
                                commandInterceptor: NoOpCommandInterceptor.Instance)
            },
            {
                SqlServerInvariants.InvariantNameMySqlConnector,
                new DbConstants(startEscapeIndicator: '[',
                                endEscapeIndicator: ']',
                                unionAllSelectTemplate: " UNION ALL SELECT ",
                                isSynchronousSqlServerImplementation: false,
                                supportsStreamNatively: true,
                                supportsCommandCancellation: true,
                                commandInterceptor: NoOpCommandInterceptor.Instance)
            }
        };

    public static DbConstants GetDbConstants(string invariantName)
    {
        return invariantNameToConsts[invariantName];
    }

    /// <summary>
    /// If the underlying storage supports cancellation or not.
    /// </summary>
    /// <param name="storage">The storage used.</param>
    /// <returns><em>TRUE</em> if cancellation is supported. <em>FALSE</em> otherwise.</returns>
    public static bool SupportsCommandCancellation(this IRelationalStorage storage)
    {
        return SupportsCommandCancellation(storage.InvariantName);
    }


    /// <summary>
    /// If the provider supports cancellation or not.
    /// </summary>
    /// <param name="sqlServerProvider">The SqlServer provider invariant string.</param>
    /// <returns><em>TRUE</em> if cancellation is supported. <em>FALSE</em> otherwise.</returns>
    public static bool SupportsCommandCancellation(string sqlServerProvider)
    {
        return GetDbConstants(sqlServerProvider).SupportsCommandCancellation;
    }


    /// <summary>
    /// If the underlying storage supports streaming natively.
    /// </summary>
    /// <param name="storage">The storage used.</param>
    /// <returns><em>TRUE</em> if streaming is supported natively. <em>FALSE</em> otherwise.</returns>
    public static bool SupportsStreamNatively(this IRelationalStorage storage)
    {
        return SupportsStreamNatively(storage.InvariantName);
    }


    /// <summary>
    /// If the provider supports streaming natively.
    /// </summary>
    /// <param name="sqlServerProvider">The SqlServer provider invariant string.</param>
    /// <returns><em>TRUE</em> if streaming is supported natively. <em>FALSE</em> otherwise.</returns>
    public static bool SupportsStreamNatively(string sqlServerProvider)
    {
        return GetDbConstants(sqlServerProvider).SupportsStreamNatively;
    }


    /// <summary>
    /// If the underlying SqlServer implementation is known to be synchronous.
    /// </summary>
    /// <param name="storage">The storage used.</param>
    /// <returns></returns>
    public static bool IsSynchronousSqlServerImplementation(this IRelationalStorage storage)
    {
        //Currently the assumption is all but MySQL are asynchronous.
        return IsSynchronousSqlServerImplementation(storage.InvariantName);
    }


    /// <summary>
    /// If the provider supports cancellation or not.
    /// </summary>
    /// <param name="sqlServerProvider">The SqlServer provider invariant string.</param>
    /// <returns></returns>
    public static bool IsSynchronousSqlServerImplementation(string sqlServerProvider)
    {
        return GetDbConstants(sqlServerProvider).IsSynchronousSqlServerImplementation;
    }

    public static ICommandInterceptor GetDatabaseCommandInterceptor(string invariantName)
    {
        return GetDbConstants(invariantName).DatabaseCommandInterceptor;
    }
}

internal class DbConstants
{
    /// <summary>
    /// A query template for union all select
    /// </summary>
    public readonly string UnionAllSelectTemplate;

    /// <summary>
    /// Indicates whether the SqlServer provider does only support synchronous operations.
    /// </summary>
    public readonly bool IsSynchronousSqlServerImplementation;

    /// <summary>
    /// Indicates whether the SqlServer provider does streaming operations natively.
    /// </summary>
    public readonly bool SupportsStreamNatively;

    /// <summary>
    /// Indicates whether the SqlServer provider supports cancellation of commands.
    /// </summary>
    public readonly bool SupportsCommandCancellation;

    /// <summary>
    /// The character that indicates a start escape key for columns and tables that are reserved words.
    /// </summary>
    public readonly char StartEscapeIndicator;

    /// <summary>
    /// The character that indicates an end escape key for columns and tables that are reserved words.
    /// </summary>
    public readonly char EndEscapeIndicator;

    public readonly ICommandInterceptor DatabaseCommandInterceptor;


    public DbConstants(char startEscapeIndicator, char endEscapeIndicator, string unionAllSelectTemplate,
                       bool isSynchronousSqlServerImplementation, bool supportsStreamNatively, bool supportsCommandCancellation, ICommandInterceptor commandInterceptor)
    {
        StartEscapeIndicator = startEscapeIndicator;
        EndEscapeIndicator = endEscapeIndicator;
        UnionAllSelectTemplate = unionAllSelectTemplate;
        IsSynchronousSqlServerImplementation = isSynchronousSqlServerImplementation;
        SupportsStreamNatively = supportsStreamNatively;
        SupportsCommandCancellation = supportsCommandCancellation;
        DatabaseCommandInterceptor = commandInterceptor;
    }
}
