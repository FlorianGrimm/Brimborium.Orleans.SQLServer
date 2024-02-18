
using System.Runtime.CompilerServices;

public class Program {
    public static async Task Main(string[] args) {
        var builder = Host.CreateApplicationBuilder(args);
        var startAsync = new StartCoordination();
        builder.Configuration.AddUserSecrets<Program>();
        backupDatabase(builder.Configuration.GetValue<string>("ConnectionString"));
        builder.Services.AddSingleton(startAsync);
        builder.Services.AddHostedService<Worker>();
        builder
            .UseOrleans(siloBuilder => {
                siloBuilder.Configure<ClusterOptions>(options => {
                    options.ClusterId = "cluster";
                    options.ServiceId = "Orleans";
                });

                siloBuilder.UseSqlServerClustering(
                    (OptionsBuilder<SqlServerClusteringSiloOptions> optionsBuilder) => {
                        // builder.Configuration.Bind("", options);
                        //options.ConnectionString = builder.Configuration.GetSection("ConnectionString").Value;
                        optionsBuilder.Bind(builder.Configuration);
                    });


                siloBuilder.ConfigureEndpoints(siloPort: 11_111, gatewayPort: 30_000);
                siloBuilder.ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Warning).AddConsole());

                siloBuilder.AddSqlServerGrainStorage("sql", (OptionsBuilder<SqlServerGrainStorageOptions> optionsBuilder) => {
                    optionsBuilder.Bind(builder.Configuration);
                });

                siloBuilder.AddStartupTask((sp, ct) => {
                    sp.GetRequiredService<StartCoordination>().Start(ct);
                    return Task.CompletedTask;
                });
            });
        var host = builder.Build();
        var task = host.RunAsync();
        await task;
    }

    private static void backupDatabase(string? connectionString) {
        if (string.IsNullOrEmpty(connectionString)) { return; }
        var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        var databaseName = csb.InitialCatalog;
        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString)) {
            connection.Open();
            using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                $"BACKUP DATABASE [{databaseName}] TO  DISK = N'NUL:' WITH NOFORMAT, NOINIT,  NAME = N'Backup', SKIP, NOREWIND, NOUNLOAD",
                connection
                )) {
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                $"BACKUP LOG [{databaseName}] TO  DISK = N'NUL:' WITH NOFORMAT, NOINIT,  NAME = N'Backup', SKIP, NOREWIND, NOUNLOAD",
                connection
                )) {
                cmd.ExecuteNonQuery();
            }
            connection.Close();
        }
        //csb.InitialCatalog
    }
}

public sealed class StartCoordination {
    private TaskCompletionSource<DateTime> _TaskCompletionSource;
    public StartCoordination() {
        this._TaskCompletionSource = new TaskCompletionSource<DateTime>();
    }
    public void Start(CancellationToken cancellationToken = default) {
        if (cancellationToken.IsCancellationRequested) {
            this._TaskCompletionSource.SetException(new OperationCanceledException());
        }
        this._TaskCompletionSource.SetResult(DateTime.UtcNow);
    }
    public Task Wait(CancellationToken cancellationToken = default) {
        return this._TaskCompletionSource.Task.WaitAsync(cancellationToken);
    }
}

public class Worker(
    IHostApplicationLifetime hostApplicationLifetime,
    IClusterClient clusterClient,
    StartCoordination startCoordination
    ) : BackgroundService {
    private readonly IHostApplicationLifetime _HostApplicationLifetime = hostApplicationLifetime;
    private readonly IClusterClient _ClusterClient = clusterClient;
    private readonly StartCoordination _StartCoordination = startCoordination;

    private const int LoopCount = 5000;
    private const int ThreadCount = 40;
    // 5000 * 40 - 78033.6365ms  138806.5116ms

    //private const int LoopCount = 500;
    //private const int ThreadCount = 40;
    // 500 * 40 - 4931.7045ms

    //private const int LoopCount = 1000;
    //private const int ThreadCount = 20;
    // 1000 * 20 - 15691.6614ms

    //private const int LoopCount = 10000;
    //private const int ThreadCount = 2;
    // 10000 * 2 - 61161.7623ms
    //private const int LoopCount = 20000;
    //private const int ThreadCount = 1;
    // 20000 * 1 - 70112.4094ms

    private readonly List<string> _Values = System.Linq.Enumerable.Range(1, LoopCount).Select(i => i.ToString() + new string('-', i)).ToList();


    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await this._StartCoordination.Wait(stoppingToken);
        await Task.Delay(1000);


        await System.Console.Out.WriteLineAsync($"Start {DateTime.UtcNow:s}");
        var start = DateTime.UtcNow;

        var listTask=System.Linq.Enumerable.Range(1, ThreadCount)
            .Select(i => i.ToString())
            .Select(n=>this.RunLoop(n))
            .ToList();
        await Task.WhenAll(listTask);
        var stop = DateTime.UtcNow;
        await System.Console.Out.WriteLineAsync($"Stop {stop:s} - {(stop - start).TotalMilliseconds}ms");
        this._HostApplicationLifetime.StopApplication();
    }

    private async Task RunLoop(string name) {
        var grain = this._ClusterClient.GetGrain<IStringKeyGrain>(name);
        for (int idxLoop = 0; idxLoop<LoopCount; idxLoop++) {
            var oldValue = await grain.Get();
            var newValue = this._Values[idxLoop];
            await grain.Set(newValue);
        }
    }
}

[Alias("IStringKeyGrain")]
public interface IStringKeyGrain : IGrainWithStringKey {
    [Alias("Get")]
    [Orleans.Concurrency.ReadOnly]
    ValueTask<string> Get();

    [Alias("Set")]
    ValueTask Set(string value);

}

[Orleans.Alias("StringKeyState")]
[Orleans.Immutable]
public record StringKeyState(string Value) {
    public StringKeyState() : this(string.Empty) { }
}

public class StringKeyGrain : Grain, IStringKeyGrain {
    private readonly IPersistentState<StringKeyState> _State;

    public StringKeyGrain(
        [Orleans.Runtime.PersistentState("state", "sql")]
        IPersistentState<StringKeyState> state
        ) {
        this._State = state;
    }
    public async ValueTask<string> Get() {
        await this._State.ReadStateAsync();
        return this._State.State.Value;
        //return ValueTask.FromResult(this._State.State.Value);
    }

    public async ValueTask Set(string value) {
        this._State.State = new StringKeyState(value);
        await this._State.WriteStateAsync();
    }
}