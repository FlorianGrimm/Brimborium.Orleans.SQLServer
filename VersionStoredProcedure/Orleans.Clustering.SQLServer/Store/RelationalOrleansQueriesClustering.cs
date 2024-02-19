#nullable enable

namespace Orleans.Clustering.SqlServer.Storage;

/// <summary>
/// A class for all relational storages that support all systems stores : membership, reminders and statistics
/// </summary>    
internal class RelationalOrleansQueriesClustering {
    /// <summary>
    /// the underlying storage
    /// </summary>
    private readonly IRelationalStorage _Storage;
    private readonly DbStoredQueriesClustering _DBStoredQueries;

    /// <summary>
    /// When inserting statistics and generating a batch insert clause, these are the columns in the statistics
    /// table that will be updated with multiple values. The other ones are updated with one value only.
    /// </summary>
    private static readonly string[] InsertStatisticsMultiupdateColumns = {
        DbStoredQueriesClustering.Columns.IsValueDelta,
        DbStoredQueriesClustering.Columns.StatValue,
        DbStoredQueriesClustering.Columns.Statistic
    };

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="storage">the underlying relational storage</param>
    /// <param name="dbStoredQueries">Orleans functional queries</param>
    private RelationalOrleansQueriesClustering(
        IRelationalStorage storage,
        DbStoredQueriesClustering dbStoredQueries) {
        this._Storage = storage;
        this._DBStoredQueries = dbStoredQueries;
    }

    /// <summary>
    /// Creates an instance of a database of type <see cref="RelationalOrleansQueriesClustering"/> and Initializes Orleans queries from the database. 
    /// Orleans uses only these queries and the variables therein, nothing more.
    /// </summary>
    /// <param name="connectionString">The connection string this database should use for database operations.</param>
    internal static RelationalOrleansQueriesClustering CreateInstance(
        string connectionString,
        DbStoredQueriesClustering? dbStoredQueriesClustering = default
        ) {
        var storage = RelationalStorage.CreateInstance(connectionString);
        var result = new RelationalOrleansQueriesClustering(
            storage,
            dbStoredQueriesClustering ?? new DbStoredQueriesClustering());
        return result;
    }

    private Task ExecuteAsync(string query, Func<IDbCommand, DbStoredQueriesClustering.Columns> parameterProvider) {
        return this._Storage.ExecuteAsync(query, command => parameterProvider(command));
    }

    private async Task<TAggregate> ReadAsync<TResult, TAggregate>(string query,
        Func<IDataRecord, TResult> selector,
        Func<IDbCommand, DbStoredQueriesClustering.Columns> parameterProvider,
        Func<IEnumerable<TResult>, TAggregate> aggregator) {
        var ret = await this._Storage.ReadAsync(query, selector, command => parameterProvider(command));
        return aggregator(ret);
    }

    /// <summary>
    /// Lists active gateways. Used mainly by Orleans clients.
    /// </summary>
    /// <param name="deploymentId">The deployment for which to query the gateways.</param>
    /// <returns>The gateways for the silo.</returns>
    internal Task<List<Uri>> ActiveGatewaysAsync(
        string deploymentId) {
        return this.ReadAsync(
            this._DBStoredQueries.GatewaysQueryKey,
            DbStoredQueriesClustering.Converters.GetGatewayUri,
            command => new DbStoredQueriesClustering.Columns(command) { DeploymentId = deploymentId, Status = SiloStatus.Active },
            ret => ret.ToList());
    }

    /// <summary>
    /// Queries Orleans membership data.
    /// </summary>
    /// <param name="deploymentId">The deployment for which to query data.</param>
    /// <param name="siloAddress">Silo data used as parameters in the query.</param>
    /// <returns>Membership table data.</returns>
    internal Task<MembershipTableData> MembershipReadRowAsync(
        string deploymentId,
        SiloAddress siloAddress) {
        return this.ReadAsync(
            this._DBStoredQueries.MembershipReadRowKey,
            DbStoredQueriesClustering.Converters.GetMembershipEntry,
            command => new DbStoredQueriesClustering.Columns(command) { DeploymentId = deploymentId, SiloAddress = siloAddress },
            ConvertToMembershipTableData);
    }

    /// <summary>
    /// returns all membership data for a deployment id
    /// </summary>
    /// <param name="deploymentId"></param>
    /// <returns></returns>
    internal Task<MembershipTableData> MembershipReadAllAsync(
        string deploymentId) {
        return this.ReadAsync(
            this._DBStoredQueries.MembershipReadAllKey,
            DbStoredQueriesClustering.Converters.GetMembershipEntry,
            command => new DbStoredQueriesClustering.Columns(command) { DeploymentId = deploymentId },
            ConvertToMembershipTableData);
    }

    /// <summary>
    /// deletes all membership entries for a deployment id
    /// </summary>
    /// <param name="deploymentId"></param>
    /// <returns></returns>
    internal Task DeleteMembershipTableEntriesAsync(
        string deploymentId) {
        return this.ExecuteAsync(this._DBStoredQueries.DeleteMembershipTableEntriesKey,
            command => new DbStoredQueriesClustering.Columns(command) { DeploymentId = deploymentId });
    }

    /// <summary>
    /// deletes all membership entries for inactive silos where the IAmAliveTime is before the beforeDate parameter
    /// and the silo status is <seealso cref="SiloStatus.Dead"/>.
    /// </summary>
    /// <param name="beforeDate"></param>
    /// <param name="deploymentId"></param>
    /// <returns></returns>
    internal Task CleanupDefunctSiloEntriesAsync(
        DateTimeOffset beforeDate,
        string deploymentId) {
        return this.ExecuteAsync(this._DBStoredQueries.CleanupDefunctSiloEntriesKey, command =>
            new DbStoredQueriesClustering.Columns(command) {
                DeploymentId = deploymentId,
                IAmAliveTime = beforeDate.UtcDateTime
            });
    }

    /// <summary>
    /// Updates IAmAlive for a silo
    /// </summary>
    /// <param name="deploymentId"></param>
    /// <param name="siloAddress"></param>
    /// <param name="iAmAliveTime"></param>
    /// <returns></returns>
    internal Task UpdateIAmAliveTimeAsync(
        string deploymentId, SiloAddress siloAddress, DateTime iAmAliveTime) {
        return this.ExecuteAsync(
            this._DBStoredQueries.UpdateIAmAlivetimeKey,
            command =>
            new DbStoredQueriesClustering.Columns(command) {
                DeploymentId = deploymentId,
                SiloAddress = siloAddress,
                IAmAliveTime = iAmAliveTime
            });
    }

    /// <summary>
    /// Inserts a version row if one does not already exist.
    /// </summary>
    /// <param name="deploymentId">The deployment for which to query data.</param>
    /// <returns><em>TRUE</em> if a row was inserted. <em>FALSE</em> otherwise.</returns>
    internal Task<bool> InsertMembershipVersionRowAsync(string deploymentId) {
        return this.ReadAsync(
            this._DBStoredQueries.InsertMembershipVersionKey,
            DbStoredQueriesClustering.Converters.GetSingleBooleanValue,
            command => new DbStoredQueriesClustering.Columns(command) {
                DeploymentId = deploymentId
            },
            ret => ret.First());
    }

    /// <summary>
    /// Inserts a membership row if one does not already exist.
    /// </summary>
    /// <param name="deploymentId">The deployment with which to insert row.</param>
    /// <param name="membershipEntry">The membership entry data to insert.</param>
    /// <param name="etag">The table expected version etag.</param>
    /// <returns><em>TRUE</em> if insert succeeds. <em>FALSE</em> otherwise.</returns>
    internal Task<bool> InsertMembershipRowAsync(
        string deploymentId,
        MembershipEntry membershipEntry,
        string etag) {
        return this.ReadAsync(this._DBStoredQueries.InsertMembershipKey, DbStoredQueriesClustering.Converters.GetSingleBooleanValue, command =>
            new DbStoredQueriesClustering.Columns(command) {
                DeploymentId = deploymentId,
                IAmAliveTime = membershipEntry.IAmAliveTime,
                SiloName = membershipEntry.SiloName,
                HostName = membershipEntry.HostName,
                SiloAddress = membershipEntry.SiloAddress,
                StartTime = membershipEntry.StartTime,
                Status = membershipEntry.Status,
                ProxyPort = membershipEntry.ProxyPort,
                Version = etag
            }, ret => ret.First());
    }

    /// <summary>
    /// Updates membership row data.
    /// </summary>
    /// <param name="deploymentId">The deployment with which to insert row.</param>
    /// <param name="membershipEntry">The membership data to used to update database.</param>
    /// <param name="etag">The table expected version etag.</param>
    /// <returns><em>TRUE</em> if update SUCCEEDS. <em>FALSE</em> ot</returns>
    internal Task<bool> UpdateMembershipRowAsync(
        string deploymentId,
        MembershipEntry membershipEntry,
        string etag) {
        return this.ReadAsync(this._DBStoredQueries.UpdateMembershipKey, DbStoredQueriesClustering.Converters.GetSingleBooleanValue, command =>
            new DbStoredQueriesClustering.Columns(command) {
                DeploymentId = deploymentId,
                SiloAddress = membershipEntry.SiloAddress,
                IAmAliveTime = membershipEntry.IAmAliveTime,
                Status = membershipEntry.Status,
                SuspectTimes = membershipEntry.SuspectTimes,
                Version = etag
            }, ret => ret.First());
    }

    private static MembershipTableData ConvertToMembershipTableData(IEnumerable<Tuple<MembershipEntry, int>> ret) {
        var retList = ret.ToList();
        var tableVersionEtag = retList[0].Item2;
        var membershipEntries = new List<Tuple<MembershipEntry, string>>();
        if (retList[0].Item1 != null) {
            membershipEntries.AddRange(
                retList.Select(
                    i => new Tuple<MembershipEntry, string>(i.Item1, string.Empty)));
        }
        return new MembershipTableData(
            membershipEntries,
            new TableVersion(tableVersionEtag, tableVersionEtag.ToString()));
    }

}
