using Microsoft.AspNetCore.Mvc;
using SoccerQueryAPI.Data;
using SoccerQueryAPI.DTO;
using SoccerQueryAPI.Services;
using System.Diagnostics;

namespace SoccerQueryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {


        private readonly SemanticKernelService _service;
        private readonly DatabaseHelper _db;
        private readonly SqlValidator _validator;
        private readonly ILogger<QueryController> _logger;
        private readonly QueryHistoryService _history;

        public QueryController(SemanticKernelService service, DatabaseHelper db, SqlValidator validator, ILogger<QueryController> logger, QueryHistoryService history)
        {
            _service = service;
            _db = db;
            _validator = validator;
            _logger = logger;
            _history = history;
        }


        [HttpPost("test")]
        public async Task<ActionResult<ApiResponse<string>>> TestAI([FromBody] GenerateRequest req, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var text = await _service.TestModelAsync(req?.Question ?? "Hello from test endpoint", cancellationToken);
                sw.Stop();
                return Ok(new ApiResponse<string> { Data = text, APIExecutionMs = sw.ElapsedMilliseconds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI test failed");
                return StatusCode(500, new ApiResponse<string> { Status = "error", Message = ex.Message, APIExecutionMs = sw.ElapsedMilliseconds });
            }
        }

        [HttpPost("generate-query")]
        public async Task<ActionResult<ApiResponse<string>>> Generate([FromBody] GenerateRequest request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var sql = await _service.GenerateSqlAsync(request.Question, cancellationToken);
                //store history
                await _history.AddAsync(new QueryHistoryEntry
                {
                    Question = request.Question ?? string.Empty,
                    GeneratedSql = sql,
                    ExecutedSql = string.Empty,
                    ApiExecutionMs = sw.ElapsedMilliseconds,
                    DatabaseExecutionMs = null,
                    ResultCount = 0,
                    Note = "Generated only"
                });

                sw.Stop();
                return Ok(new ApiResponse<string> { Data = sql, APIExecutionMs = sw.ElapsedMilliseconds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate SQL");
                return BadRequest(new ApiResponse<string> { Status = "error", Message = ex.Message, APIExecutionMs = sw.ElapsedMilliseconds });
            }
        }


        [HttpPost("execute-query")]
        public async Task<ActionResult<ApiResponse<object>>> Execute([FromBody] ExecuteRequest req, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var sql = req.Sql ?? string.Empty;
            try
            {

                if (string.IsNullOrWhiteSpace(sql))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Status = "error",
                        Message = "SQL query cannot be empty.",
                        APIExecutionMs = sw.ElapsedMilliseconds
                    });
                }

                if (!req.BypassValidation)
                {
                    if (!_validator.IsSelectOnly(sql))
                        return BadRequest(new ApiResponse<object> { Status = "error", Message = "Only SELECT statements are allowed.", APIExecutionMs = sw.ElapsedMilliseconds });

                    if (!_validator.ContainsOnlyAllowedTablesAndColumns(sql, out var reason))
                        return BadRequest(new ApiResponse<object> { Status = "error", Message = $"SQL validation failed: {reason}", APIExecutionMs = sw.ElapsedMilliseconds });

                }

                // // for without bypass

                //if (!_validator.IsSelectOnly(sql))
                //    return BadRequest(new ApiResponse<object> { Status = "error", Message = "Only SELECT statements are allowed.", APIExecutionMs = sw.ElapsedMilliseconds });

                //if (!_validator.ContainsOnlyAllowedTablesAndColumns(sql, out var reason))
                //    return BadRequest(new ApiResponse<object> { Status = "error", Message = $"SQL validation failed: {reason}", APIExecutionMs = sw.ElapsedMilliseconds });





                sql = _validator.EnforceRowLimit(sql);
                //var data = await _db.ExecuteQueryAsync(sql, cancellationToken);
                var (data, dbTime) = await _db.ExecuteQueryAsync(sql, cancellationToken);

                //store history
                await _history.AddAsync(new QueryHistoryEntry
                {
                    Question = string.Empty,
                    GeneratedSql = string.Empty,
                    ExecutedSql = sql,
                    ApiExecutionMs = sw.ElapsedMilliseconds,
                    DatabaseExecutionMs = dbTime,
                    ResultCount = data?.Count ?? 0,
                    Note = req.BypassValidation ? "Executed (bypass validation)" : "Executed"
                });

                sw.Stop();
                //return Ok(new ApiResponse<List<Dictionary<string, object>>> { Data = data, APIExecutionMs = sw.ElapsedMilliseconds });
                return Ok(new ApiResponse<object>
                {
                    Data = new { results = data },
                    APIExecutionMs = sw.ElapsedMilliseconds,
                    DatabaseExecutionMs = dbTime
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Query canceled by client or timeout.");
                return StatusCode(408, new ApiResponse<List<Dictionary<string, object>>> { Status = "error", Message = "Request timed out or canceled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL");
                return StatusCode(500, new ApiResponse<List<Dictionary<string, object>>> { Status = "error", Message = ex.Message, APIExecutionMs = sw.ElapsedMilliseconds });
            }
        }



        [HttpPost("generateAndExecuteQuery")]
        public async Task<ActionResult<ApiResponse<object>>> Combined([FromBody] CombinedRequest req, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var sql = await _service.GenerateSqlAsync(req.Question, cancellationToken);

                if (string.IsNullOrWhiteSpace(sql))
                    return BadRequest(new ApiResponse<object> { Status = "error", Message = "Model returned empty SQL.", Data = new { sql }, APIExecutionMs = sw.ElapsedMilliseconds });

                if (!req.BypassValidation)
                {
                    if (!_validator.IsSelectOnly(sql))
                        return BadRequest(new ApiResponse<object> { Status = "error", Message = "Generated SQL is not a SELECT statement.", Data = new { sql }, APIExecutionMs = sw.ElapsedMilliseconds });

                    if (!_validator.ContainsOnlyAllowedTablesAndColumns(sql, out var reason))
                        return BadRequest(new ApiResponse<object> { Status = "error", Message = $"SQL validation failed: {reason}", Data = new { sql }, APIExecutionMs = sw.ElapsedMilliseconds });
                }

                sql = _validator.EnforceRowLimit(sql);
                //var data = await _db.ExecuteQueryAsync(sql, cancellationToken);
                var (data, dbTime) = await _db.ExecuteQueryAsync(sql, cancellationToken);

                //store history
                await _history.AddAsync(new QueryHistoryEntry
                {
                    Question = req.Question ?? string.Empty,
                    GeneratedSql = sql,            // generated SQL (already enforced row limit)
                    ExecutedSql = sql,
                    ApiExecutionMs = sw.ElapsedMilliseconds,
                    DatabaseExecutionMs = dbTime,
                    ResultCount = data?.Count ?? 0,
                    Note = req.BypassValidation ? "Generated+Executed (bypass validation)" : "Generated+Executed"
                });



                sw.Stop();
                var response = new
                {
                    question = req.Question,
                    generatedSql = sql,
                    results = data
                };

                //return Ok(new ApiResponse<object> { Data = response, APIExecutionMs = sw.ElapsedMilliseconds });
                return Ok(new ApiResponse<object> { Data = response, APIExecutionMs = sw.ElapsedMilliseconds, DatabaseExecutionMs = dbTime });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Combined operation canceled");
                return StatusCode(408, new ApiResponse<object> { Status = "error", Message = "Request timed out or canceled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Combined generation+execution failed");
                return StatusCode(500, new ApiResponse<object> { Status = "error", Message = ex.Message, APIExecutionMs = sw.ElapsedMilliseconds });
            }
        }



        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<IEnumerable<QueryHistoryEntry>>>> GetHistory()
        {
            try
            {
                var items = await _history.GetAllAsync();
                return Ok(new ApiResponse<IEnumerable<QueryHistoryEntry>> { Data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get history");
                return StatusCode(500, new ApiResponse<IEnumerable<QueryHistoryEntry>> { Status = "error", Message = ex.Message });
            }
        }

        [HttpDelete("history")]
        public async Task<ActionResult<ApiResponse<string>>> ClearHistory()
        {
            try
            {
                await _history.ClearAsync();
                return Ok(new ApiResponse<string> { Data = "Cleared" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear history");
                return StatusCode(500, new ApiResponse<string> { Status = "error", Message = ex.Message });
            }
        }

    }
}

