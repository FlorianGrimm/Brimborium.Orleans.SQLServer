using Microsoft.Extensions.Options;

using Orleans.Runtime;
using Orleans.Runtime.MembershipService;

namespace Orleans.Configuration;

/// <summary>
/// Validates <see cref="SqlServerClusteringClientOptions"/> configuration.
/// </summary>
public class SqlServerClusteringClientOptionsValidator : IConfigurationValidator {
    private readonly SqlServerClusteringClientOptions options;

    public SqlServerClusteringClientOptionsValidator(IOptions<SqlServerClusteringClientOptions> options) {
        this.options = options.Value;
    }

    /// <inheritdoc />
    public void ValidateConfiguration() {
        if (string.IsNullOrWhiteSpace(this.options.ConnectionString)) {
            throw new OrleansConfigurationException($"Invalid {nameof(SqlServerClusteringClientOptions)} values for {nameof(SqlServerClusteringTable)}. {nameof(this.options.ConnectionString)} is required.");
        }
    }
}