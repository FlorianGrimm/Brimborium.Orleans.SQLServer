namespace Orleans.Reminders.SqlServer.Storage;

/// <summary>
/// This class implements the expected contract between Orleans and the underlying relational storage.
/// It makes sure all the stored queries are present and 
/// </summary>
internal class DbStoredQueriesReminder {
    private readonly Dictionary<string, string> queries;

    internal DbStoredQueriesReminder(Dictionary<string, string> queries) {
        var fields = typeof(DbStoredQueriesReminder).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(p => p.Name);
        var missingQueryKeys = fields.Except(queries.Keys).ToArray();
        if (missingQueryKeys.Length > 0) {
            throw new ArgumentException(
                $"Not all required queries found. Missing are: {string.Join(",", missingQueryKeys)}");
        }
        this.queries = queries;
    }

    /// <summary>
    /// The query that's used to get all the stored queries.
    /// this will probably be the same for all relational dbs.
    /// </summary>
    internal const string GetQueriesKey = "SELECT QueryKey, QueryText FROM OrleansQuery";

    /// <summary>
    /// A query template to read reminder entries.
    /// </summary>
    internal string ReadReminderRowsKey => queries[nameof(ReadReminderRowsKey)];

    /// <summary>
    /// A query template to read reminder entries with ranges.
    /// </summary>
    internal string ReadRangeRows1Key => queries[nameof(ReadRangeRows1Key)];

    /// <summary>
    /// A query template to read reminder entries with ranges.
    /// </summary>
    internal string ReadRangeRows2Key => queries[nameof(ReadRangeRows2Key)];

    /// <summary>
    /// A query template to read a reminder entry with ranges.
    /// </summary>
    internal string ReadReminderRowKey => queries[nameof(ReadReminderRowKey)];

    /// <summary>
    /// A query template to upsert a reminder row.
    /// </summary>
    internal string UpsertReminderRowKey => queries[nameof(UpsertReminderRowKey)];

    /// <summary>
    /// A query template to delete a reminder row.
    /// </summary>
    internal string DeleteReminderRowKey => queries[nameof(DeleteReminderRowKey)];

    /// <summary>
    /// A query template to delete all reminder rows.
    /// </summary>
    internal string DeleteReminderRowsKey => queries[nameof(DeleteReminderRowsKey)];

    internal static class Converters {
        internal static KeyValuePair<string, string> GetQueryKeyAndValue(IDataRecord record) {
            return new KeyValuePair<string, string>(
                record.GetValue<string>("QueryKey"),
                record.GetValue<string>("QueryText"));
        }


        internal static Tuple<MembershipEntry, int> GetMembershipEntry(IDataRecord record) {
            //TODO: This is a bit of hack way to check in the current version if there's membership data or not, but if there's a start time, there's member.            
            DateTime? startTime = record.GetDateTimeValueOrDefault(nameof(Columns.StartTime));
            MembershipEntry entry = null;
            if (startTime.HasValue) {
                entry = new MembershipEntry {
                    SiloAddress = GetSiloAddress(record, nameof(Columns.Port)),
                    SiloName = TryGetSiloName(record),
                    HostName = record.GetValue<string>(nameof(Columns.HostName)),
                    Status = (SiloStatus)Enum.Parse(typeof(SiloStatus), record.GetInt32(nameof(Columns.Status)).ToString()),
                    ProxyPort = record.GetInt32(nameof(Columns.ProxyPort)),
                    StartTime = startTime.Value,
                    IAmAliveTime = record.GetDateTimeValue(nameof(Columns.IAmAliveTime))
                };

                string suspectingSilos = record.GetValueOrDefault<string>(nameof(Columns.SuspectTimes));
                if (!string.IsNullOrWhiteSpace(suspectingSilos)) {
                    entry.SuspectTimes = new List<Tuple<SiloAddress, DateTime>>();
                    entry.SuspectTimes.AddRange(suspectingSilos.Split('|').Select(s => {
                        var split = s.Split(',');
                        return new Tuple<SiloAddress, DateTime>(SiloAddress.FromParsableString(split[0]),
                            LogFormatter.ParseDate(split[1]));
                    }));
                }
            }

            return Tuple.Create(entry, GetVersion(record));
        }

        /// <summary>
        /// This method is for compatibility with membership tables that
        /// do not contain a SiloName field
        /// </summary>
        private static string TryGetSiloName(IDataRecord record) {
            int pos;
            try {
                pos = record.GetOrdinal(nameof(Columns.SiloName));
            } catch (IndexOutOfRangeException) {
                return null;
            }

            return (string)record.GetValue(pos);

        }

        internal static int GetVersion(IDataRecord record) {
            return Convert.ToInt32(record.GetValue<object>(nameof(Version)));
        }

        internal static Uri GetGatewayUri(IDataRecord record) {
            return GetSiloAddress(record, nameof(Columns.ProxyPort)).ToGatewayUri();
        }

        private static SiloAddress GetSiloAddress(IDataRecord record, string portName) {
            //Use the GetInt32 method instead of the generic GetValue<TValue> version to retrieve the value from the data record
            //GetValue<int> causes an InvalidCastException with orcale data provider. See https://github.com/dotnet/orleans/issues/3561
            int port = record.GetInt32(portName);
            int generation = record.GetInt32(nameof(Columns.Generation));
            string address = record.GetValue<string>(nameof(Columns.Address));
            var siloAddress = SiloAddress.New(IPAddress.Parse(address), port, generation);
            return siloAddress;
        }

        internal static bool GetSingleBooleanValue(IDataRecord record) {
            if (record.FieldCount != 1) throw new InvalidOperationException("Expected a single column");
            return Convert.ToBoolean(record.GetValue(0));
        }
    }

    internal class Columns {
        private readonly IDbCommand command;

        internal Columns(IDbCommand cmd) {
            command = cmd;

        }

        private void Add<T>(string paramName, T paramValue, DbType? dbType = null) {
            command.AddParameter(paramName, paramValue, dbType: dbType);
        }

        private void AddAddress(string name, IPAddress address) {
            Add(name, address.ToString(), dbType: DbType.AnsiString);
        }

        private void AddGrainHash(string name, uint grainHash) {
            Add(name, (int)grainHash);
        }

        internal string ClientId {
            set { Add(nameof(ClientId), value); }
        }

        internal int GatewayPort {
            set { Add(nameof(GatewayPort), value); }
        }

        internal IPAddress GatewayAddress {
            set { AddAddress(nameof(GatewayAddress), value); }
        }

        internal string SiloId {
            set { Add(nameof(SiloId), value); }
        }

        internal string Id {
            set { Add(nameof(Id), value); }
        }

        internal string Name {
            set { Add(nameof(Name), value); }
        }

        internal const string IsValueDelta = nameof(IsValueDelta);
        internal const string StatValue = nameof(StatValue);
        internal const string Statistic = nameof(Statistic);

        internal SiloAddress SiloAddress {
            set {
                Address = value.Endpoint.Address;
                Port = value.Endpoint.Port;
                Generation = value.Generation;
            }
        }

        internal int Generation {
            set { Add(nameof(Generation), value); }
        }

        internal int Port {
            set { Add(nameof(Port), value); }
        }

        internal uint BeginHash {
            set { AddGrainHash(nameof(BeginHash), value); }
        }

        internal uint EndHash {
            set { AddGrainHash(nameof(EndHash), value); }
        }

        internal uint GrainHash {
            set { AddGrainHash(nameof(GrainHash), value); }
        }

        internal DateTime StartTime {
            set { Add(nameof(StartTime), value); }
        }

        internal IPAddress Address {
            set { AddAddress(nameof(Address), value); }
        }

        internal string ServiceId {
            set { Add(nameof(ServiceId), value); }
        }

        internal string DeploymentId {
            set { Add(nameof(DeploymentId), value); }
        }

        internal string SiloName {
            set { Add(nameof(SiloName), value); }
        }

        internal string HostName {
            set { Add(nameof(HostName), value); }
        }

        internal string Version {
            set { Add(nameof(Version), int.Parse(value)); }
        }

        internal DateTime IAmAliveTime {
            set { Add(nameof(IAmAliveTime), value); }
        }

        internal string GrainId {
            set { Add(nameof(GrainId), value, dbType: DbType.AnsiString); }
        }

        internal string ReminderName {
            set { Add(nameof(ReminderName), value); }
        }

        internal TimeSpan Period {
            set {
                if (value.TotalMilliseconds <= int.MaxValue) {
                    // Original casting when old schema is used.  Here to maintain backwards compatibility
                    Add(nameof(Period), (int)value.TotalMilliseconds);
                } else {
                    Add(nameof(Period), (long)value.TotalMilliseconds);
                }
            }
        }

        internal SiloStatus Status {
            set { Add(nameof(Status), (int)value); }
        }

        internal int ProxyPort {
            set { Add(nameof(ProxyPort), value); }
        }

        internal List<Tuple<SiloAddress, DateTime>> SuspectTimes {
            set {
                Add(nameof(SuspectTimes), value == null
                    ? null
                    : string.Join("|", value.Select(
                        s => $"{s.Item1.ToParsableString()},{LogFormatter.PrintDate(s.Item2)}")));
            }
        }
    }
}