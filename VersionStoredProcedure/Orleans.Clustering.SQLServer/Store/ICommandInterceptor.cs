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

internal interface ICommandInterceptor {
    void Intercept(IDbCommand command);
}
