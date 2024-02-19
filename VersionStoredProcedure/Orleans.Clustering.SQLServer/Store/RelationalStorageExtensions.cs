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

/// <summary>
/// Convenience functions to work with objects of type <see cref="IRelationalStorage"/>.
/// </summary>
internal static class RelationalStorageExtensions {
    /// <summary>
    /// Used to format .NET objects suitable to relational database format.
    /// </summary>
    private static readonly SqlServerFormatProvider sqlServerFormatProvider = new SqlServerFormatProvider();

    /// <summary>
    /// This is a template to produce query parameters that are indexed.
    /// </summary>
    private const string indexedParameterTemplate = "@p{0}";

    /// <summary>
    /// A simplified version of <see cref="IRelationalStorage.ReadAsync{TResult}"/>
    /// </summary>
    /// <param name="storage"></param>
    /// <param name="query"></param>
    /// <param name="selector"></param>
    /// <param name="parameterProvider"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static Task<IEnumerable<TResult>> ReadAsync<TResult>(this IRelationalStorage storage, string query, Func<IDataRecord, TResult> selector, Action<IDbCommand> parameterProvider) {
        return storage.ReadAsync(query, parameterProvider, (record, i, cancellationToken) => Task.FromResult(selector(record)));
    }

    /// <summary>
    /// Uses <see cref="IRelationalStorage"/> with <see cref="DbExtensions.ReflectionSelector{TResult}(System.Data.IDataRecord)"/>.
    /// </summary>
    /// <param name="storage">The storage to use.</param>
    /// <param name="query">Executes a given statement. Especially intended to use with <em>INSERT</em>, <em>UPDATE</em>, <em>DELETE</em> or <em>DDL</em> queries.</param>
    /// <param name="parameters">Adds parameters to the query. Parameter names must match those defined in the query.</param>
    /// <param name="cancellationToken">The cancellation token. Defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>Affected rows count.</returns>
    /// <example>This uses reflection to provide parameters to an execute
    /// query that reads only affected rows count if available.
    /// <code>
    /// //Here reflection (<seealso cref="DbExtensions.ReflectionParameterProvider{T}(IDbCommand, T, IReadOnlyDictionary{string, string})"/>)
    /// is used to match parameter names as well as to read back the results (<seealso cref="DbExtensions.ReflectionSelector{TResult}(IDataRecord)"/>).
    /// var query = "IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tname) CREATE TABLE Test(Id INT PRIMARY KEY IDENTITY(1, 1) NOT NULL);"
    /// await db.ExecuteAsync(query, new { tname = "test_table" });
    /// </code>
    /// </example>
    public static Task<int> ExecuteAsync(this IRelationalStorage storage, string query, object parameters, CancellationToken cancellationToken = default) {
        return storage.ExecuteAsync(query, command => {
            if (parameters != null) {
                command.ReflectionParameterProvider(parameters);
            }
        }, cancellationToken: cancellationToken);
    }


    /// <summary>
    /// Uses <see cref="IRelationalStorage"/> with <see cref="DbExtensions.ReflectionSelector{TResult}(System.Data.IDataRecord)"/>.
    /// </summary>
    /// <param name="storage">The storage to use.</param>
    /// <param name="query">Executes a given statement. Especially intended to use with <em>INSERT</em>, <em>UPDATE</em>, <em>DELETE</em> or <em>DDL</em> queries.</param>
    /// <param name="cancellationToken">The cancellation token. Defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>Affected rows count.</returns>
    public static Task<int> ExecuteAsync(this IRelationalStorage storage, string query, CancellationToken cancellationToken = default) {
        return ExecuteAsync(storage, query, null, cancellationToken);
    }


    /// <summary>
    /// Returns a native implementation of <see cref="DbDataReader.GetStream(int)"/> for those providers
    /// which support it. Otherwise returns a chunked read using <see cref="DbDataReader.GetBytes(int, long, byte[], int, int)"/>.
    /// </summary>
    /// <param name="reader">The reader from which to return the stream.</param>
    /// <param name="ordinal">The ordinal column for which to return the stream.</param>
    /// <param name="storage">The storage that gives the invariant.</param>
    /// <returns></returns>
    public static Stream GetStream(this DbDataReader reader, int ordinal, IRelationalStorage storage) {
        return reader.GetStream(ordinal);
    }
}
