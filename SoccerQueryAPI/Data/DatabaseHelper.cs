using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Diagnostics;

namespace SoccerQueryAPI.Data
{
    public class DatabaseHelper
    {
        private const string ConnectionString = "Data Source=Data/database.sqlite";

        //public DataTable ExecuteQuery(string query)
        //{
        //    var dt = new DataTable();
        //    using var connection = new SqliteConnection(_connectionString);
        //    connection.Open();
        //    using var command = new SqliteCommand(query, connection);
        //    using var reader = command.ExecuteReader();
        //    dt.Load(reader);
        //    return dt;
        //}


        private readonly string _connectionString;
        private readonly ILogger<DatabaseHelper> _logger;
        private readonly int _commandTimeoutSeconds;

        public DatabaseHelper(IConfiguration config, ILogger<DatabaseHelper> logger)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ?? "Data Source=Data/database.sqlite";
            _logger = logger;
            _commandTimeoutSeconds = config.GetValue<int>("SqlExecution:CommandTimeoutSeconds", 15);
        }

        //public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
        //{
        //    _logger.LogInformation("Executing SQL query (trimmed preview): {Preview}", query.Length > 200 ? query[..200] + "..." : query);

        //    var result = new List<Dictionary<string, object>>();

        //    using var connection = new SqliteConnection(_connectionString);
        //    await connection.OpenAsync(cancellationToken);

        //    using var command = connection.CreateCommand();
        //    command.CommandText = query;
        //    command.CommandTimeout = _commandTimeoutSeconds;

        //    using var reader = await command.ExecuteReaderAsync(cancellationToken);
        //    var schema = reader.GetColumnSchema();

        //    while (await reader.ReadAsync(cancellationToken))
        //    {
        //        var row = new Dictionary<string, object?>();
        //        foreach (var col in schema)
        //        {
        //            var name = col.ColumnName ?? string.Empty;
        //            var value = reader[name];
        //            row[name] = value == DBNull.Value ? null : value;
        //        }
        //        result.Add(row!);
        //    }

        //    _logger.LogInformation("Executed; rows returned: {Count}", result.Count);
        //    return result;
        //}

        public async Task<(List<Dictionary<string, object>> Data, long ExecutionTimeMs)> ExecuteQueryAsync(
    string query,
    CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing SQL query (trimmed preview): {Preview}",
                query.Length > 200 ? query[..200] + "..." : query);

            var result = new List<Dictionary<string, object>>();
            var stopwatch = Stopwatch.StartNew();

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandTimeout = _commandTimeoutSeconds;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var schema = reader.GetColumnSchema();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                foreach (var col in schema)
                {
                    var name = col.ColumnName ?? string.Empty;
                    var value = reader[name];
                    row[name] = value == DBNull.Value ? null : value;
                }
                result.Add(row!);
            }

            stopwatch.Stop();
            //double elapsedSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 3);
            _logger.LogInformation("Executed; rows returned: {Count} in {Elapsed} ms",
                result.Count, stopwatch.ElapsedMilliseconds);

            return (result, stopwatch.ElapsedMilliseconds);
        }

    }
}
