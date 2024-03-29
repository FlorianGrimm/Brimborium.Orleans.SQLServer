namespace Orleans.Reminders.SqlServer.Storage;

/// <summary>
/// A class for all relational storages that support all systems stores : membership, reminders and statistics
/// </summary>    
internal class RelationalOrleansQueriesReminder {
    /// <summary>
    /// the underlying storage
    /// </summary>
    private readonly IRelationalStorage storage;

    /// <summary>
    /// When inserting statistics and generating a batch insert clause, these are the columns in the statistics
    /// table that will be updated with multiple values. The other ones are updated with one value only.
    /// </summary>
    private static readonly string[] InsertStatisticsMultiupdateColumns = {
        DbStoredQueriesReminder.Columns.IsValueDelta,
        DbStoredQueriesReminder.Columns.StatValue,
        DbStoredQueriesReminder.Columns.Statistic
    };

    /// <summary>
    /// the orleans functional queries
    /// </summary>
    private readonly DbStoredQueriesReminder _DBStoredQueries;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="storage">the underlying relational storage</param>
    /// <param name="dbStoredQueries">Orleans functional queries</param>
    private RelationalOrleansQueriesReminder(IRelationalStorage storage, DbStoredQueriesReminder dbStoredQueries) {
        this.storage = storage;
        this._DBStoredQueries = dbStoredQueries;
    }

    /// <summary>
    /// Creates an instance of a database of type <see cref="RelationalOrleansQueriesReminder"/> and Initializes Orleans queries from the database. 
    /// Orleans uses only these queries and the variables therein, nothing more.
    /// </summary>
    /// <param name="connectionString">The connection string this database should use for database operations.</param>
    internal static async Task<RelationalOrleansQueriesReminder> CreateInstance(string connectionString) {
#if false
        var storage = RelationalStorage.CreateInstance( connectionString);
        var queries = await storage.ReadAsync(DbStoredQueries.GetQueriesKey, DbStoredQueries.Converters.GetQueryKeyAndValue, null);
        return new RelationalOrleansQueries(storage, new DbStoredQueries(queries.ToDictionary(q => q.Key, q => q.Value)));
#endif

#if false
    internal string GatewaysQueryKey => queries[nameof(GatewaysQueryKey)];
    internal string MembershipReadRowKey => queries[nameof(MembershipReadRowKey)];
    internal string MembershipReadAllKey => queries[nameof(MembershipReadAllKey)];
    internal string InsertMembershipVersionKey => queries[nameof(InsertMembershipVersionKey)];
    internal string UpdateIAmAlivetimeKey => queries[nameof(UpdateIAmAlivetimeKey)];
    internal string InsertMembershipKey => queries[nameof(InsertMembershipKey)];
    internal string UpdateMembershipKey => queries[nameof(UpdateMembershipKey)];
    internal string DeleteMembershipTableEntriesKey => queries[nameof(DeleteMembershipTableEntriesKey)];
    internal string CleanupDefunctSiloEntriesKey => queries[nameof(CleanupDefunctSiloEntriesKey)];

#endif
        var storage = RelationalStorage.CreateInstance(connectionString);
        Dictionary<string, string> queries = new Dictionary<string, string>();
        queries.Add("GatewaysQueryKey", "dbo.GatewaysQuery");
        queries.Add("MembershipReadRowKey", "dbo.MembershipReadRow");
        queries.Add("MembershipReadAllKey", "dbo.MembershipReadAll");
        queries.Add("InsertMembershipVersionKey", "dbo.InsertMembershipVersion");
        queries.Add("UpdateIAmAlivetimeKey", "dbo.UpdateIAmAlivetime");
        queries.Add("InsertMembershipKey", "dbo.InsertMembership");
        queries.Add("UpdateMembershipKey", "dbo.UpdateMembership");
        queries.Add("DeleteMembershipTableEntriesKey", "dbo.DeleteMembershipTableEntries");
        queries.Add("CleanupDefunctSiloEntriesKey", "dbo.CleanupDefunctSiloEntries");
        return new RelationalOrleansQueriesReminder(storage, new DbStoredQueriesReminder(queries.ToDictionary(q => q.Key, q => q.Value)));
    }

    private Task ExecuteAsync(string query, Func<IDbCommand, DbStoredQueriesReminder.Columns> parameterProvider) {
        return storage.ExecuteAsync(query, command => parameterProvider(command));
    }

    private async Task<TAggregate> ReadAsync<TResult, TAggregate>(string query,
        Func<IDataRecord, TResult> selector,
        Func<IDbCommand, DbStoredQueriesReminder.Columns> parameterProvider,
        Func<IEnumerable<TResult>, TAggregate> aggregator) {
        var ret = await storage.ReadAsync(query, selector, command => parameterProvider(command));
        return aggregator(ret);
    }

#if REMINDERS_SqlServer || TESTER_SQLUTILS

    /// <summary>
    /// Reads Orleans reminder data from the tables.
    /// </summary>
    /// <param name="serviceId">The service ID.</param>
    /// <param name="grainId">The grain reference (ID).</param>
    /// <returns>Reminder table data.</returns>
    internal Task<ReminderTableData> ReadReminderRowsAsync(string serviceId, GrainId grainId) {
        return ReadAsync(_DBStoredQueries.ReadReminderRowsKey, GetReminderEntry, command =>
            new DbStoredQueriesReminder.Columns(command) { ServiceId = serviceId, GrainId = grainId.ToString() },
            ret => new ReminderTableData(ret.ToList()));
    }


    /// <summary>
    /// Reads Orleans reminder data from the tables.
    /// </summary>
    /// <param name="serviceId">The service ID.</param>
    /// <param name="beginHash">The begin hash.</param>
    /// <param name="endHash">The end hash.</param>
    /// <returns>Reminder table data.</returns>
    internal Task<ReminderTableData> ReadReminderRowsAsync(string serviceId, uint beginHash, uint endHash) {
        var query = (int)beginHash < (int)endHash ? _DBStoredQueries.ReadRangeRows1Key : _DBStoredQueries.ReadRangeRows2Key;

        return ReadAsync(query, GetReminderEntry, command =>
            new DbStoredQueriesReminder.Columns(command) { ServiceId = serviceId, BeginHash = beginHash, EndHash = endHash },
            ret => new ReminderTableData(ret.ToList()));
    }


    internal static KeyValuePair<string, string> GetQueryKeyAndValue(IDataRecord record) {
        return new KeyValuePair<string, string>(record.GetValue<string>("QueryKey"),
            record.GetValue<string>("QueryText"));
    }

    internal static ReminderEntry GetReminderEntry(IDataRecord record) {
        //Having non-null field, GrainId, means with the query filter options, an entry was found.
        string grainId = record.GetValueOrDefault<string>(nameof(DbStoredQueriesReminder.Columns.GrainId));
        if (grainId != null) {
            return new ReminderEntry {
                GrainId = GrainId.Parse(grainId),
                ReminderName = record.GetValue<string>(nameof(DbStoredQueriesReminder.Columns.ReminderName)),
                StartAt = record.GetDateTimeValue(nameof(DbStoredQueriesReminder.Columns.StartTime)),

                //Use the GetInt64 method instead of the generic GetValue<TValue> version to retrieve the value from the data record
                //GetValue<int> causes an InvalidCastException with oracle data provider. See https://github.com/dotnet/orleans/issues/3561
                Period = TimeSpan.FromMilliseconds(record.GetInt64(nameof(DbStoredQueriesReminder.Columns.Period))),
                ETag = DbStoredQueriesReminder.Converters.GetVersion(record).ToString()
            };
        }
        return null;
    }
    /// <summary>
    /// Reads one row of reminder data.
    /// </summary>
    /// <param name="serviceId">Service ID.</param>
    /// <param name="grainId">The grain reference (ID).</param>
    /// <param name="reminderName">The reminder name to retrieve.</param>
    /// <returns>A remainder entry.</returns>
    internal Task<ReminderEntry> ReadReminderRowAsync(string serviceId, GrainId grainId,
        string reminderName) {
        return ReadAsync(_DBStoredQueries.ReadReminderRowKey, GetReminderEntry, command =>
            new DbStoredQueriesReminder.Columns(command) {
                ServiceId = serviceId,
                GrainId = grainId.ToString(),
                ReminderName = reminderName
            }, ret => ret.FirstOrDefault());
    }

    /// <summary>
    /// Either inserts or updates a reminder row.
    /// </summary>
    /// <param name="serviceId">The service ID.</param>
    /// <param name="grainId">The grain reference (ID).</param>
    /// <param name="reminderName">The reminder name to retrieve.</param>
    /// <param name="startTime">Start time of the reminder.</param>
    /// <param name="period">Period of the reminder.</param>
    /// <returns>The new etag of the either or updated or inserted reminder row.</returns>
    internal Task<string> UpsertReminderRowAsync(string serviceId, GrainId grainId,
        string reminderName, DateTime startTime, TimeSpan period) {
        return ReadAsync(_DBStoredQueries.UpsertReminderRowKey, DbStoredQueriesReminder.Converters.GetVersion, command =>
            new DbStoredQueriesReminder.Columns(command) {
                ServiceId = serviceId,
                GrainHash = grainId.GetUniformHashCode(),
                GrainId = grainId.ToString(),
                ReminderName = reminderName,
                StartTime = startTime,
                Period = period
            }, ret => ret.First().ToString());
    }

    /// <summary>
    /// Deletes a reminder
    /// </summary>
    /// <param name="serviceId">Service ID.</param>
    /// <param name="grainId"></param>
    /// <param name="reminderName"></param>
    /// <param name="etag"></param>
    /// <returns></returns>
    internal Task<bool> DeleteReminderRowAsync(string serviceId, GrainId grainId, string reminderName,
        string etag) {
        return ReadAsync(_DBStoredQueries.DeleteReminderRowKey, DbStoredQueriesReminder.Converters.GetSingleBooleanValue, command =>
            new DbStoredQueriesReminder.Columns(command) {
                ServiceId = serviceId,
                GrainId = grainId.ToString(),
                ReminderName = reminderName,
                Version = etag
            }, ret => ret.First());
    }

    /// <summary>
    /// Deletes all reminders rows of a service id.
    /// </summary>
    /// <param name="serviceId"></param>
    /// <returns></returns>
    internal Task DeleteReminderRowsAsync(string serviceId) {
        return ExecuteAsync(_DBStoredQueries.DeleteReminderRowsKey, command =>
            new DbStoredQueriesReminder.Columns(command) { ServiceId = serviceId });
    }

#endif

#if CLUSTERING_SqlServer || TESTER_SQLUTILS

    /// <summary>
    /// Lists active gateways. Used mainly by Orleans clients.
    /// </summary>
    /// <param name="deploymentId">The deployment for which to query the gateways.</param>
    /// <returns>The gateways for the silo.</returns>
    internal Task<List<Uri>> ActiveGatewaysAsync(string deploymentId)
    {
        return ReadAsync(dbStoredQueries.GatewaysQueryKey, DbStoredQueries.Converters.GetGatewayUri, command =>
            new DbStoredQueries.Columns(command) { DeploymentId = deploymentId, Status = SiloStatus.Active },
            ret => ret.ToList());
    }

    /// <summary>
    /// Queries Orleans membership data.
    /// </summary>
    /// <param name="deploymentId">The deployment for which to query data.</param>
    /// <param name="siloAddress">Silo data used as parameters in the query.</param>
    /// <returns>Membership table data.</returns>
    internal Task<MembershipTableData> MembershipReadRowAsync(string deploymentId, SiloAddress siloAddress)
    {
        return ReadAsync(dbStoredQueries.MembershipReadRowKey, DbStoredQueries.Converters.GetMembershipEntry, command =>
            new DbStoredQueries.Columns(command) { DeploymentId = deploymentId, SiloAddress = siloAddress },
            ConvertToMembershipTableData);
    }

    /// <summary>
    /// returns all membership data for a deployment id
    /// </summary>
    /// <param name="deploymentId"></param>
    /// <returns></returns>
    internal Task<MembershipTableData> MembershipReadAllAsync(string deploymentId)
    {
        return ReadAsync(dbStoredQueries.MembershipReadAllKey, DbStoredQueries.Converters.GetMembershipEntry, command =>
            new DbStoredQueries.Columns(command) { DeploymentId = deploymentId }, ConvertToMembershipTableData);
    }

    /// <summary>
    /// deletes all membership entries for a deployment id
    /// </summary>
    /// <param name="deploymentId"></param>
    /// <returns></returns>
    internal Task DeleteMembershipTableEntriesAsync(string deploymentId)
    {
        return ExecuteAsync(dbStoredQueries.DeleteMembershipTableEntriesKey, command =>
            new DbStoredQueries.Columns(command) { DeploymentId = deploymentId });
    }

    /// <summary>
    /// deletes all membership entries for inactive silos where the IAmAliveTime is before the beforeDate parameter
    /// and the silo status is <seealso cref="SiloStatus.Dead"/>.
    /// </summary>
    /// <param name="beforeDate"></param>
    /// <param name="deploymentId"></param>
    /// <returns></returns>
    internal Task CleanupDefunctSiloEntriesAsync(DateTimeOffset beforeDate, string deploymentId)
    {
        return ExecuteAsync(dbStoredQueries.CleanupDefunctSiloEntriesKey, command =>
            new DbStoredQueries.Columns(command) { DeploymentId = deploymentId, IAmAliveTime = beforeDate.UtcDateTime });
    }

    /// <summary>
    /// Updates IAmAlive for a silo
    /// </summary>
    /// <param name="deploymentId"></param>
    /// <param name="siloAddress"></param>
    /// <param name="iAmAliveTime"></param>
    /// <returns></returns>
    internal Task UpdateIAmAliveTimeAsync(string deploymentId, SiloAddress siloAddress, DateTime iAmAliveTime)
    {
        return ExecuteAsync(dbStoredQueries.UpdateIAmAlivetimeKey, command =>
            new DbStoredQueries.Columns(command)
            {
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
    internal Task<bool> InsertMembershipVersionRowAsync(string deploymentId)
    {
        return ReadAsync(dbStoredQueries.InsertMembershipVersionKey, DbStoredQueries.Converters.GetSingleBooleanValue, command =>
            new DbStoredQueries.Columns(command) { DeploymentId = deploymentId }, ret => ret.First());
    }

    /// <summary>
    /// Inserts a membership row if one does not already exist.
    /// </summary>
    /// <param name="deploymentId">The deployment with which to insert row.</param>
    /// <param name="membershipEntry">The membership entry data to insert.</param>
    /// <param name="etag">The table expected version etag.</param>
    /// <returns><em>TRUE</em> if insert succeeds. <em>FALSE</em> otherwise.</returns>
    internal Task<bool> InsertMembershipRowAsync(string deploymentId, MembershipEntry membershipEntry,
        string etag)
    {
        return ReadAsync(dbStoredQueries.InsertMembershipKey, DbStoredQueries.Converters.GetSingleBooleanValue, command =>
            new DbStoredQueries.Columns(command)
            {
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
    internal Task<bool> UpdateMembershipRowAsync(string deploymentId, MembershipEntry membershipEntry,
        string etag)
    {
        return ReadAsync(dbStoredQueries.UpdateMembershipKey, DbStoredQueries.Converters.GetSingleBooleanValue, command =>
            new DbStoredQueries.Columns(command)
            {
                DeploymentId = deploymentId,
                SiloAddress = membershipEntry.SiloAddress,
                IAmAliveTime = membershipEntry.IAmAliveTime,
                Status = membershipEntry.Status,
                SuspectTimes = membershipEntry.SuspectTimes,
                Version = etag
            }, ret => ret.First());
    }

    private static MembershipTableData ConvertToMembershipTableData(IEnumerable<Tuple<MembershipEntry, int>> ret)
    {
        var retList = ret.ToList();
        var tableVersionEtag = retList[0].Item2;
        var membershipEntries = new List<Tuple<MembershipEntry, string>>();
        if (retList[0].Item1 != null)
        {
            membershipEntries.AddRange(retList.Select(i => new Tuple<MembershipEntry, string>(i.Item1, string.Empty)));
        }
        return new MembershipTableData(membershipEntries, new TableVersion(tableVersionEtag, tableVersionEtag.ToString()));
    }

#endif

}
