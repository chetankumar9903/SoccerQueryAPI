using SoccerQueryAPI.DTO;
using System.Text.Json;

namespace SoccerQueryAPI.Services
{
    public class QueryHistoryService
    {

        private readonly string _storagePath;
        private readonly ILogger<QueryHistoryService> _logger;
        private readonly List<QueryHistoryEntry> _cache = new();

        private readonly object _lock = new();

        public QueryHistoryService(IConfiguration config, ILogger<QueryHistoryService> logger)
        {
            _logger = logger;
            // Use Data/query_history.json inside project folder — create if missing
            var basePath = config.GetValue<string>("QueryHistory:StoragePath") ?? "Data/query_history.json";
            _storagePath = Path.GetFullPath(basePath);
            try
            {
                var dir = Path.GetDirectoryName(_storagePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (File.Exists(_storagePath))
                {
                    var text = File.ReadAllText(_storagePath);
                    var items = JsonSerializer.Deserialize<List<QueryHistoryEntry>>(text);
                    if (items != null) _cache.AddRange(items);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize QueryHistoryService");
            }
        }

        public Task<List<QueryHistoryEntry>> GetAllAsync()
        {
            lock (_lock)
            {
                // Return copy sorted by timestamp desc
                return Task.FromResult(_cache.OrderByDescending(x => x.TimestampUtc).ToList());
            }
        }

        public Task AddAsync(QueryHistoryEntry entry)
        {
            lock (_lock)
            {
                _cache.Add(entry);
                PersistToFile();
                return Task.CompletedTask;
            }
        }

        public Task ClearAsync()
        {
            lock (_lock)
            {
                _cache.Clear();
                PersistToFile();
                return Task.CompletedTask;
            }
        }

        private void PersistToFile()
        {
            try
            {
                var opt = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_storagePath, JsonSerializer.Serialize(_cache, opt));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist query history to file");
            }
        }

    }
}
