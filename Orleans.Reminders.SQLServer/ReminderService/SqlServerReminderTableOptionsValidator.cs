using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Orleans.Runtime.ReminderService;

namespace Orleans.Configuration;

/// <summary>
/// Validates <see cref="SqlServerReminderTableOptions"/> configuration.
/// </summary>
public class SqlServerReminderTableOptionsValidator : IConfigurationValidator
{
    private readonly SqlServerReminderTableOptions options;
    
    public SqlServerReminderTableOptionsValidator(IOptions<SqlServerReminderTableOptions> options)
    {
        this.options = options.Value;
    }

    /// <inheritdoc />
    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(this.options.Invariant))
        {
            throw new OrleansConfigurationException($"Invalid {nameof(SqlServerReminderTableOptions)} values for {nameof(SqlServerReminderTable)}. {nameof(options.Invariant)} is required.");
        }

        if (string.IsNullOrWhiteSpace(this.options.ConnectionString))
        {
            throw new OrleansConfigurationException($"Invalid {nameof(SqlServerReminderTableOptions)} values for {nameof(SqlServerReminderTable)}. {nameof(options.ConnectionString)} is required.");
        }
    }
}