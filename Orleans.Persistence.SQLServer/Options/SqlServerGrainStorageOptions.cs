using Orleans.Persistence.SqlServer.Storage;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Configuration;

/// <summary>
/// Options for AdonetGrainStorage
/// </summary>
public class SqlServerGrainStorageOptions : IStorageProviderSerializerOptions
{
    /// <summary>
    /// Connection string for SqlServer storage.
    /// </summary>
    [Redact]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Stage of silo lifecycle where storage should be initialized.  Storage must be initialized prior to use.
    /// </summary>
    public int InitStage { get; set; } = DEFAULT_INIT_STAGE;
    /// <summary>
    /// Default init stage in silo lifecycle.
    /// </summary>
    public const int DEFAULT_INIT_STAGE = ServiceLifecycleStage.ApplicationServices;

    /// <summary>
    /// The default SqlServer invariant used for storage if none is given. 
    /// </summary>
    public const string DEFAULT_SqlServer_INVARIANT = SqlServerInvariants.InvariantNameSqlServer;
    /// <summary>
    /// The invariant name for storage.
    /// </summary>
    public string Invariant { get; set; } = DEFAULT_SqlServer_INVARIANT;

    /// <inheritdoc/>
    public IGrainStorageSerializer GrainStorageSerializer { get; set; }
}

/// <summary>
/// ConfigurationValidator for SqlServerGrainStorageOptions
/// </summary>
public class SqlServerGrainStorageOptionsValidator : IConfigurationValidator
{
    private readonly SqlServerGrainStorageOptions options;
    private readonly string name;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="configurationOptions">The option to be validated.</param>
    /// <param name="name">The name of the option to be validated.</param>
    public SqlServerGrainStorageOptionsValidator(SqlServerGrainStorageOptions configurationOptions, string name)
    {
        if(configurationOptions == null)
            throw new OrleansConfigurationException($"Invalid SqlServerGrainStorageOptions for SqlServerGrainStorage {name}. Options is required.");
        this.options = configurationOptions;
        this.name = name;
    }
    /// <inheritdoc cref="IConfigurationValidator"/>
    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(this.options.Invariant))
        {
            throw new OrleansConfigurationException($"Invalid {nameof(SqlServerGrainStorageOptions)} values for {nameof(SqlServerGrainStorage)} \"{name}\". {nameof(options.Invariant)} is required.");
        }

        if (string.IsNullOrWhiteSpace(this.options.ConnectionString))
        {
            throw new OrleansConfigurationException($"Invalid {nameof(SqlServerGrainStorageOptions)} values for {nameof(SqlServerGrainStorage)} \"{name}\". {nameof(options.ConnectionString)} is required.");
        }
    }
}
