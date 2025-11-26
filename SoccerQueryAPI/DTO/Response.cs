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
    }

    public class CombinedRequest
    {
        public string Question { get; set; } = string.Empty;
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
}
