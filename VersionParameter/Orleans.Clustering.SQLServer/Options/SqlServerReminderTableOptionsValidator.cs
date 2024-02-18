namespace Orleans.Configuration;

/// <summary>
/// Validates <see cref="SqlServerClusteringSiloOptions"/> configuration.
/// </summary>
public class SqlServerClusteringSiloOptionsValidator : IConfigurationValidator {
    private readonly SqlServerClusteringSiloOptions options;

    public SqlServerClusteringSiloOptionsValidator(IOptions<SqlServerClusteringSiloOptions> options) {
        this.options = options.Value;
    }

    /// <inheritdoc />
    public void ValidateConfiguration() {
        if (string.IsNullOrWhiteSpace(this.options.ConnectionString)) {
            throw new OrleansConfigurationException($"Invalid {nameof(SqlServerClusteringSiloOptions)} values for {nameof(SqlServerClusteringTable)}. {nameof(options.ConnectionString)} is required.");
        }
    }
}