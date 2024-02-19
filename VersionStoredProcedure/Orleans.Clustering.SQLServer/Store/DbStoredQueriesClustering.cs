namespace Orleans.Clustering.SqlServer.Storage;

/// <summary>
/// This class implements the expected contract between Orleans and the underlying relational storage.
/// It makes sure all the stored queries are present and 
/// </summary>
internal class DbStoredQueriesClustering(
    string gatewaysQueryKey,
    string membershipReadRowKey,
    string membershipReadAllKey,
    string insertMembershipVersionKey,
    string updateIAmAlivetimeKey,
    string insertMembershipKey,
    string updateMembershipKey,
    string deleteMembershipTableEntriesKey,
    string cleanupDefunctSiloEntriesKey
    ) {

    public DbStoredQueriesClustering()
        : this(
            gatewaysQueryKey: "dbo.GatewaysQueryKey",
            membershipReadRowKey: "dbo.MembershipReadRowKey",
            membershipReadAllKey: "dbo.MembershipReadAllKey",
            insertMembershipVersionKey: "dbo.InsertMembershipVersionKey",
            updateIAmAlivetimeKey: "dbo.UpdateIAmAlivetimeKey",
            insertMembershipKey: "dbo.InsertMembershipKey",
            updateMembershipKey: "dbo.UpdateMembershipKey",
            deleteMembershipTableEntriesKey: "dbo.DeleteMembershipTableEntriesKey",
            cleanupDefunctSiloEntriesKey: "dbo.CleanupDefunctSiloEntriesKey"
        ) {

    }

    /// <summary>
    /// A query template to retrieve gateway URIs.
    /// </summary>
    internal string GatewaysQueryKey = gatewaysQueryKey;

    /// <summary>
    /// A query template to retrieve a single row of membership data.
    /// </summary>
    internal string MembershipReadRowKey = membershipReadRowKey;

    /// <summary>
    /// A query template to retrieve all membership data.
    /// </summary>
    internal string MembershipReadAllKey = membershipReadAllKey;

    /// <summary>
    /// A query template to insert a membership version row.
    /// </summary>
    internal string InsertMembershipVersionKey = insertMembershipVersionKey;

    /// <summary>
    /// A query template to update "I Am Alive Time".
    /// </summary>
    internal string UpdateIAmAlivetimeKey = updateIAmAlivetimeKey;

    /// <summary>
    /// A query template to insert a membership row.
    /// </summary>
    internal string InsertMembershipKey = insertMembershipKey;

    /// <summary>
    /// A query template to update a membership row.
    /// </summary>
    internal string UpdateMembershipKey = updateMembershipKey;

    /// <summary>
    /// A query template to delete membership entries.
    /// </summary>
    internal string DeleteMembershipTableEntriesKey = deleteMembershipTableEntriesKey;

    /// <summary>
    /// A query template to cleanup defunct silo entries.
    /// </summary>
    internal string CleanupDefunctSiloEntriesKey = cleanupDefunctSiloEntriesKey;

    internal static class Converters {

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
            if (record.FieldCount != 1) {
                throw new InvalidOperationException("Expected a single column");
            }

            return Convert.ToBoolean(record.GetValue(0));
        }
    }

    internal class Columns {
        private readonly IDbCommand command;

        internal Columns(IDbCommand cmd) {
            this.command = cmd;
        }

        private void Add<T>(string paramName, T paramValue) {
            this.command.AddParameter(paramName, paramValue);
        }

        private void Add<T>(string paramName, T paramValue, DbType dbType) {
            this.command.AddParameter(paramName, paramValue, dbType: dbType);
        }

        private void Add<T>(string paramName, T paramValue, DbType dbType, int size) {
            this.command.AddParameter(paramName, paramValue, dbType: dbType, size: size);
        }


        private void AddAddress(string name, IPAddress address) {
            this.Add(name, address.ToString(), dbType: DbType.AnsiString, size: 45);
        }

        private void AddGrainHash(string name, uint grainHash) {
            this.Add(name, (int)grainHash, DbType.Int32);
        }

        internal string ClientId {
            set { this.Add(nameof(this.ClientId), value, DbType.AnsiString); }
        }

        internal int GatewayPort {
            set { this.Add(nameof(this.GatewayPort), value); }
        }

        internal IPAddress GatewayAddress {
            set { this.AddAddress(nameof(this.GatewayAddress), value); }
        }

        internal string SiloId {
            set { this.Add(nameof(this.SiloId), value); }
        }

        internal string Id {
            set { this.Add(nameof(this.Id), value); }
        }

        internal string Name {
            set { this.Add(nameof(this.Name), value); }
        }

        internal const string IsValueDelta = nameof(IsValueDelta);
        internal const string StatValue = nameof(StatValue);
        internal const string Statistic = nameof(Statistic);

        internal SiloAddress SiloAddress {
            set {
                this.Address = value.Endpoint.Address;
                this.Port = value.Endpoint.Port;
                this.Generation = value.Generation;
            }
        }

        internal int Generation {
            set { this.Add(nameof(this.Generation), value); }
        }

        internal int Port {
            set { this.Add(nameof(this.Port), value); }
        }

        internal uint BeginHash {
            set { this.AddGrainHash(nameof(this.BeginHash), value); }
        }

        internal uint EndHash {
            set { this.AddGrainHash(nameof(this.EndHash), value); }
        }

        internal uint GrainHash {
            set { this.AddGrainHash(nameof(this.GrainHash), value); }
        }

        internal DateTime StartTime {
            set { this.Add(nameof(this.StartTime), value); }
        }

        internal IPAddress Address {
            set { this.AddAddress(nameof(this.Address), value); }
        }

        internal string ServiceId {
            set { this.Add(nameof(this.ServiceId), value, dbType: DbType.String, 150); }
        }

        internal string DeploymentId {
            set { this.Add(nameof(this.DeploymentId), value, dbType: DbType.String, 150); }
        }

        internal string SiloName {
            set { this.Add(nameof(this.SiloName), value, dbType: DbType.String, size: 150); }
        }

        internal string HostName {
            set { this.Add(nameof(this.HostName), value, dbType: DbType.String, size: 150); }
        }

        internal string Version {
            set { this.Add(nameof(this.Version), int.Parse(value)); }
        }

        internal DateTime IAmAliveTime {
            set { this.Add(nameof(this.IAmAliveTime), value); }
        }

        internal string GrainId {
            set { this.Add(nameof(this.GrainId), value, dbType: DbType.AnsiString, size: 150); }
        }

        internal string ReminderName {
            set { this.Add(nameof(this.ReminderName), value, dbType: DbType.String, size: 150); }
        }

        internal TimeSpan Period {
            set {
                if (value.TotalMilliseconds <= int.MaxValue) {
                    // Original casting when old schema is used.  Here to maintain backwards compatibility
                    this.Add(nameof(this.Period), (int)value.TotalMilliseconds);
                } else {
                    this.Add(nameof(this.Period), (long)value.TotalMilliseconds);
                }
            }
        }

        internal SiloStatus Status {
            set { this.Add(nameof(this.Status), (int)value); }
        }

        internal int ProxyPort {
            set { this.Add(nameof(this.ProxyPort), value); }
        }

        internal List<Tuple<SiloAddress, DateTime>> SuspectTimes {
            set {
                this.Add(nameof(this.SuspectTimes),
                    value == null
                    ? null
                    : string.Join("|", value.Select(
                        s => $"{s.Item1.ToParsableString()},{LogFormatter.PrintDate(s.Item2)}")),
                    dbType: DbType.AnsiString,
                    size: 8000);
            }
        }
    }
}