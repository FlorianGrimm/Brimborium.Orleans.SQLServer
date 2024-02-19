namespace Orleans.Runtime.MembershipService;

public class SqlServerClusteringTable : IMembershipTable {
    private readonly string _ClusterId;
    private readonly IServiceProvider _ServiceProvider;
    private readonly ILogger _Logger;
    private RelationalOrleansQueriesClustering _OrleansQueries;
    private readonly SqlServerClusteringSiloOptions _ClusteringTableOptions;

    public SqlServerClusteringTable(
        IServiceProvider serviceProvider,
        IOptions<ClusterOptions> clusterOptions,
        IOptions<SqlServerClusteringSiloOptions> clusteringOptions,
        ILogger<SqlServerClusteringTable> logger) {
        this._ServiceProvider = serviceProvider;
        this._Logger = logger;
        this._ClusteringTableOptions = clusteringOptions.Value;
        this._ClusterId = clusterOptions.Value.ClusterId;
    }

    public async Task InitializeMembershipTable(bool tryInitTableVersion) {
        if (this._Logger.IsEnabled(LogLevel.Trace)) {
            this._Logger.LogTrace("SqlServerClusteringTable.InitializeMembershipTable called.");
        }

        //This initializes all of Orleans operational queries from the database using a well known view
        //and assumes the database with appropriate definitions exists already.
        this._OrleansQueries = RelationalOrleansQueriesClustering.CreateInstance(
            this._ClusteringTableOptions.ConnectionString);

        // even if I am not the one who created the table, 
        // try to insert an initial table version if it is not already there,
        // so we always have a first table version row, before this silo starts working.
        if (tryInitTableVersion) {
            var wasCreated = await this.InitTableAsync();
            if (wasCreated) {
                this._Logger.LogInformation("Created new table version row.");
            }
        }
    }


    public async Task<MembershipTableData> ReadRow(SiloAddress key) {
        if (this._Logger.IsEnabled(LogLevel.Trace)) {
            this._Logger.LogTrace("SqlServerClusteringTable.ReadRow called with key: {Key}.", key);
        }

        try {
            return await this._OrleansQueries.MembershipReadRowAsync(this._ClusterId, key);
        } catch (Exception ex) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug(ex, "SqlServerClusteringTable.ReadRow failed");
            }

            throw;
        }
    }


    public async Task<MembershipTableData> ReadAll() {
        if (this._Logger.IsEnabled(LogLevel.Trace)) {
            this._Logger.LogTrace("SqlServerClusteringTable.ReadAll called.");
        }

        try {
            return await this._OrleansQueries.MembershipReadAllAsync(this._ClusterId);
        } catch (Exception ex) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug(ex, "SqlServerClusteringTable.ReadAll failed");
            }

            throw;
        }
    }


    public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion) {
        if (this._Logger.IsEnabled(LogLevel.Trace)) {
            this._Logger.LogTrace(
                "SqlServerClusteringTable.InsertRow called with entry {Entry} and tableVersion {TableVersion}.",
                entry,
                tableVersion);
        }

        //The "tableVersion" parameter should always exist when inserting a row as Init should
        //have been called and membership version created and read. This is an optimization to
        //not to go through all the way to database to fail a conditional check on etag (which does
        //exist for the sake of robustness) as mandated by Orleans membership protocol.
        //Likewise, no update can be done without membership entry.
        if (entry == null) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug("SqlServerClusteringTable.InsertRow aborted due to null check. MembershipEntry is null.");
            }

            throw new ArgumentNullException(nameof(entry));
        }
        if (tableVersion is null) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug("SqlServerClusteringTable.InsertRow aborted due to null check. TableVersion is null ");
            }

            throw new ArgumentNullException(nameof(tableVersion));
        }

        try {
            return await this._OrleansQueries.InsertMembershipRowAsync(this._ClusterId, entry, tableVersion.VersionEtag);
        } catch (Exception ex) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug(ex, "SqlServerClusteringTable.InsertRow failed");
            }

            throw;
        }
    }


    public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion) {
        if (this._Logger.IsEnabled(LogLevel.Trace)) {
            this._Logger.LogTrace("IMembershipTable.UpdateRow called with entry {Entry}, etag {ETag} and tableVersion {TableVersion}.", entry, etag, tableVersion);
        }

        //The "tableVersion" parameter should always exist when updating a row as Init should
        //have been called and membership version created and read. This is an optimization to
        //not to go through all the way to database to fail a conditional check (which does
        //exist for the sake of robustness) as mandated by Orleans membership protocol.
        //Likewise, no update can be done without membership entry or an etag.
        if (entry == null) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug("SqlServerClusteringTable.UpdateRow aborted due to null check. MembershipEntry is null.");
            }

            throw new ArgumentNullException(nameof(entry));
        }
        if (tableVersion is null) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug("SqlServerClusteringTable.UpdateRow aborted due to null check. TableVersion is null");
            }

            throw new ArgumentNullException(nameof(tableVersion));
        }

        try {
            return await this._OrleansQueries.UpdateMembershipRowAsync(this._ClusterId, entry, tableVersion.VersionEtag);
        } catch (Exception ex) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug(ex, "SqlServerClusteringTable.UpdateRow failed");
            }

            throw;
        }
    }


    public async Task UpdateIAmAlive(MembershipEntry entry) {
        if (this._Logger.IsEnabled(LogLevel.Trace)) {
            this._Logger.LogTrace("IMembershipTable.UpdateIAmAlive called with entry {Entry}.", entry);
        }

        if (entry == null) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug("SqlServerClusteringTable.UpdateIAmAlive aborted due to null check. MembershipEntry is null.");
            }

            throw new ArgumentNullException(nameof(entry));
        }
        try {
            await this._OrleansQueries.UpdateIAmAliveTimeAsync(this._ClusterId, entry.SiloAddress, entry.IAmAliveTime);
        } catch (Exception ex) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug(ex, "SqlServerClusteringTable.UpdateIAmAlive failed");
            }

            throw;
        }
    }


    public async Task DeleteMembershipTableEntries(string clusterId) {
        if (this._Logger.IsEnabled(LogLevel.Trace)) {
            this._Logger.LogTrace("IMembershipTable.DeleteMembershipTableEntries called with clusterId {ClusterId}.", clusterId);
        }

        try {
            await this._OrleansQueries.DeleteMembershipTableEntriesAsync(clusterId);
        } catch (Exception ex) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug(ex, "SqlServerClusteringTable.DeleteMembershipTableEntries failed");
            }

            throw;
        }
    }

    public async Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate) {
        if (this._Logger.IsEnabled(LogLevel.Trace)) {
            this._Logger.LogTrace("IMembershipTable.CleanupDefunctSiloEntries called with beforeDate {beforeDate} and clusterId {ClusterId}.", beforeDate, this._ClusterId);
        }

        try {
            await this._OrleansQueries.CleanupDefunctSiloEntriesAsync(beforeDate, this._ClusterId);
        } catch (Exception ex) {
            if (this._Logger.IsEnabled(LogLevel.Debug)) {
                this._Logger.LogDebug(ex, "SqlServerClusteringTable.CleanupDefunctSiloEntries failed");
            }

            throw;
        }
    }

    private async Task<bool> InitTableAsync() {
        try {
            return await this._OrleansQueries.InsertMembershipVersionRowAsync(this._ClusterId);
        } catch (Exception ex) {
            if (this._Logger.IsEnabled(LogLevel.Trace)) {
                this._Logger.LogTrace(ex, "Insert silo membership version failed");
            }

            throw;
        }
    }
}
