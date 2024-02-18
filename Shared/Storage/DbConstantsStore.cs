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

internal static class DbConstantsStore {
    private static readonly Dictionary<string, DbConstants> invariantNameToConsts =
        new Dictionary<string, DbConstants>
        {
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
        };

    public static DbConstants GetDbConstants(string invariantName) {
        return invariantNameToConsts[invariantName];
    }

}

internal class DbConstants {
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
                       bool isSynchronousSqlServerImplementation, bool supportsStreamNatively, bool supportsCommandCancellation, ICommandInterceptor commandInterceptor) {
        StartEscapeIndicator = startEscapeIndicator;
        EndEscapeIndicator = endEscapeIndicator;
        UnionAllSelectTemplate = unionAllSelectTemplate;
        IsSynchronousSqlServerImplementation = isSynchronousSqlServerImplementation;
        SupportsStreamNatively = supportsStreamNatively;
        SupportsCommandCancellation = supportsCommandCancellation;
        DatabaseCommandInterceptor = commandInterceptor;
    }
}
