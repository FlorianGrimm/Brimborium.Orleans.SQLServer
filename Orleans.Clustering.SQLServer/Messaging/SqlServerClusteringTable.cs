using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Clustering.SqlServer.Storage;
using Orleans.Configuration;

namespace Orleans.Runtime.MembershipService;

public class SqlServerClusteringTable : IMembershipTable
{
    private readonly string clusterId;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;
    private RelationalOrleansQueries orleansQueries;
    private readonly SqlServerClusteringSiloOptions clusteringTableOptions;

    public SqlServerClusteringTable(
        IServiceProvider serviceProvider,
        IOptions<ClusterOptions> clusterOptions,
        IOptions<SqlServerClusteringSiloOptions> clusteringOptions,
        ILogger<SqlServerClusteringTable> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.clusteringTableOptions = clusteringOptions.Value;
        this.clusterId = clusterOptions.Value.ClusterId;
    }

    public async Task InitializeMembershipTable(bool tryInitTableVersion)
    {
        if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("SqlServerClusteringTable.InitializeMembershipTable called.");

        //This initializes all of Orleans operational queries from the database using a well known view
        //and assumes the database with appropriate definitions exists already.
        orleansQueries = await RelationalOrleansQueries.CreateInstance(
            clusteringTableOptions.Invariant,
            clusteringTableOptions.ConnectionString);

        // even if I am not the one who created the table, 
        // try to insert an initial table version if it is not already there,
        // so we always have a first table version row, before this silo starts working.
        if (tryInitTableVersion)
        {
            var wasCreated = await InitTableAsync();
            if (wasCreated)
            {
                logger.LogInformation("Created new table version row.");
            }
        }
    }


    public async Task<MembershipTableData> ReadRow(SiloAddress key)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("SqlServerClusteringTable.ReadRow called with key: {Key}.", key);
        try
        {
            return await orleansQueries.MembershipReadRowAsync(this.clusterId, key);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug(ex, "SqlServerClusteringTable.ReadRow failed");
            throw;
        }
    }


    public async Task<MembershipTableData> ReadAll()
    {
        if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("SqlServerClusteringTable.ReadAll called.");
        try
        {
            return await orleansQueries.MembershipReadAllAsync(this.clusterId);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug(ex, "SqlServerClusteringTable.ReadAll failed");
            throw;
        }
    }


    public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace(
                "SqlServerClusteringTable.InsertRow called with entry {Entry} and tableVersion {TableVersion}.",
                entry,
                tableVersion);

        //The "tableVersion" parameter should always exist when inserting a row as Init should
        //have been called and membership version created and read. This is an optimization to
        //not to go through all the way to database to fail a conditional check on etag (which does
        //exist for the sake of robustness) as mandated by Orleans membership protocol.
        //Likewise, no update can be done without membership entry.
        if (entry == null)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("SqlServerClusteringTable.InsertRow aborted due to null check. MembershipEntry is null.");
            throw new ArgumentNullException(nameof(entry));
        }
        if (tableVersion is null)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("SqlServerClusteringTable.InsertRow aborted due to null check. TableVersion is null ");
            throw new ArgumentNullException(nameof(tableVersion));
        }

        try
        {
            return await orleansQueries.InsertMembershipRowAsync(this.clusterId, entry, tableVersion.VersionEtag);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug(ex, "SqlServerClusteringTable.InsertRow failed");
            throw;
        }
    }


    public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
    {
        if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace("IMembershipTable.UpdateRow called with entry {Entry}, etag {ETag} and tableVersion {TableVersion}.", entry, etag, tableVersion);

        //The "tableVersion" parameter should always exist when updating a row as Init should
        //have been called and membership version created and read. This is an optimization to
        //not to go through all the way to database to fail a conditional check (which does
        //exist for the sake of robustness) as mandated by Orleans membership protocol.
        //Likewise, no update can be done without membership entry or an etag.
        if (entry == null)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("SqlServerClusteringTable.UpdateRow aborted due to null check. MembershipEntry is null.");
            throw new ArgumentNullException(nameof(entry));
        }
        if (tableVersion is null)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("SqlServerClusteringTable.UpdateRow aborted due to null check. TableVersion is null");
            throw new ArgumentNullException(nameof(tableVersion));
        }

        try
        {
            return await orleansQueries.UpdateMembershipRowAsync(this.clusterId, entry, tableVersion.VersionEtag);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug(ex, "SqlServerClusteringTable.UpdateRow failed");
            throw;
        }
    }


    public async Task UpdateIAmAlive(MembershipEntry entry)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("IMembershipTable.UpdateIAmAlive called with entry {Entry}.", entry);
        if (entry == null)
        {
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("SqlServerClusteringTable.UpdateIAmAlive aborted due to null check. MembershipEntry is null.");
            throw new ArgumentNullException(nameof(entry));
        }
        try
        {
            await orleansQueries.UpdateIAmAliveTimeAsync(this.clusterId, entry.SiloAddress, entry.IAmAliveTime);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(ex, "SqlServerClusteringTable.UpdateIAmAlive failed");
            throw;
        }
    }


    public async Task DeleteMembershipTableEntries(string clusterId)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("IMembershipTable.DeleteMembershipTableEntries called with clusterId {ClusterId}.", clusterId);
        try
        {
            await orleansQueries.DeleteMembershipTableEntriesAsync(clusterId);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(ex, "SqlServerClusteringTable.DeleteMembershipTableEntries failed");
            throw;
        }
    }

    public async Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
    {
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("IMembershipTable.CleanupDefunctSiloEntries called with beforeDate {beforeDate} and clusterId {ClusterId}.", beforeDate, clusterId);
        try
        {
            await orleansQueries.CleanupDefunctSiloEntriesAsync(beforeDate, this.clusterId);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(ex, "SqlServerClusteringTable.CleanupDefunctSiloEntries failed");
            throw;
        }
    }

    private async Task<bool> InitTableAsync()
    {
        try
        {
            return await orleansQueries.InsertMembershipVersionRowAsync(this.clusterId);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Trace)) logger.LogTrace(ex, "Insert silo membership version failed");
            throw;
        }
    }
}
