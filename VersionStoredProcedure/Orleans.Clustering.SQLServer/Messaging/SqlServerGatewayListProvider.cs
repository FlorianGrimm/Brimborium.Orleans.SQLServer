using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Clustering.SqlServer.Storage;
using Orleans.Messaging;
using Orleans.Configuration;

namespace Orleans.Runtime.Membership;

public class SqlServerGatewayListProvider : IGatewayListProvider
{
    private readonly ILogger _logger;
    private readonly string _clusterId;
    private readonly SqlServerClusteringClientOptions _options;
    private RelationalOrleansQueriesClustering _orleansQueries;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _maxStaleness;

    public SqlServerGatewayListProvider(
        ILogger<SqlServerGatewayListProvider> logger, 
        IServiceProvider serviceProvider,
        IOptions<SqlServerClusteringClientOptions> options,
        IOptions<GatewayOptions> gatewayOptions,
        IOptions<ClusterOptions> clusterOptions)
    {
        this._logger = logger;
        this._serviceProvider = serviceProvider;
        this._options = options.Value;
        this._clusterId = clusterOptions.Value.ClusterId;
        this._maxStaleness = gatewayOptions.Value.GatewayListRefreshPeriod;
    }

    public TimeSpan MaxStaleness
    {
        get { return this._maxStaleness; }
    }

    public bool IsUpdatable
    {
        get { return true; }
    }

    public Task InitializeGatewayListProvider()
    {
        if (this._logger.IsEnabled(LogLevel.Trace)) {
            this._logger.LogTrace("SqlServerClusteringTable.InitializeGatewayListProvider called.");
        }

        this._orleansQueries = RelationalOrleansQueriesClustering.CreateInstance(this._options.ConnectionString);
        return Task.CompletedTask;
    }

    public async Task<IList<Uri>> GetGateways()
    {
        if (this._logger.IsEnabled(LogLevel.Trace)) {
            this._logger.LogTrace("SqlServerClusteringTable.GetGateways called.");
        }

        try
        {
            return await this._orleansQueries.ActiveGatewaysAsync(this._clusterId);
        }
        catch (Exception ex)
        {
            if (this._logger.IsEnabled(LogLevel.Debug)) {
                this._logger.LogDebug(ex, "SqlServerClusteringTable.Gateways failed");
            }

            throw;
        }
    }
}
