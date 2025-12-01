using System.ComponentModel;

namespace SoccerQueryAPI.DTO
{
    public class Response
    {
    }

    public class QueryRequest
    {
        public string Question { get; set; } = string.Empty;
    }

    public class GenerateRequest
    {
        public string Question { get; set; } = string.Empty;
    }

    public class ExecuteRequest
    {
        public string Sql { get; set; } = string.Empty;
        [DefaultValue(false)]
        public bool BypassValidation { get; set; } = false;
    }

    public class CombinedRequest
    {
        public string Question { get; set; } = string.Empty;
        [DefaultValue(false)]
        public bool BypassValidation { get; set; } = false; // only if you want to bypass validator (restricted)
    }

    public class ApiResponse<T>
    {
        public string Status { get; set; } = "success";
        public T? Data { get; set; }
        public string? Message { get; set; }
        public long APIExecutionMs { get; set; } = 0;
        public long? DatabaseExecutionMs { get; set; } = 0;
    }

    public class QueryHistoryEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid(); //Globally Unique Identifier. - 128-bit
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Question { get; set; } = string.Empty;
        public string GeneratedSql { get; set; } = string.Empty;
        public string ExecutedSql { get; set; } = string.Empty;
        public long ApiExecutionMs { get; set; } = 0;
        public long? DatabaseExecutionMs { get; set; } = null;
        public int ResultCount { get; set; } = 0;
        public string Note { get; set; } = string.Empty;
    }
}
