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

        public QueryController(SemanticKernelService service, DatabaseHelper db, SqlValidator validator, ILogger<QueryController> logger)
        {
            _service = service;
            _db = db;
            _validator = validator;
            _logger = logger;
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
        public async Task<ActionResult<ApiResponse<List<Dictionary<string, object>>>>> Execute([FromBody] ExecuteRequest req, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var sql = req.Sql ?? string.Empty;
                if (!_validator.IsSelectOnly(sql))
                    return BadRequest(new ApiResponse<List<Dictionary<string, object>>> { Status = "error", Message = "Only SELECT statements are allowed.", APIExecutionMs = sw.ElapsedMilliseconds });

                if (!_validator.ContainsOnlyAllowedTablesAndColumns(sql, out var reason))
                    return BadRequest(new ApiResponse<List<Dictionary<string, object>>> { Status = "error", Message = $"SQL validation failed: {reason}", APIExecutionMs = sw.ElapsedMilliseconds });

                sql = _validator.EnforceRowLimit(sql);
                //var data = await _db.ExecuteQueryAsync(sql, cancellationToken);
                var (data, dbTime) = await _db.ExecuteQueryAsync(sql, cancellationToken);

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
                    return BadRequest(new ApiResponse<object> { Status = "error", Message = "Model returned empty SQL.", APIExecutionMs = sw.ElapsedMilliseconds });

                if (!req.BypassValidation)
                {
                    if (!_validator.IsSelectOnly(sql))
                        return BadRequest(new ApiResponse<object> { Status = "error", Message = "Generated SQL is not a SELECT statement.", APIExecutionMs = sw.ElapsedMilliseconds });

                    if (!_validator.ContainsOnlyAllowedTablesAndColumns(sql, out var reason))
                        return BadRequest(new ApiResponse<object> { Status = "error", Message = $"SQL validation failed: {reason}", APIExecutionMs = sw.ElapsedMilliseconds });
                }

                sql = _validator.EnforceRowLimit(sql);
                //var data = await _db.ExecuteQueryAsync(sql, cancellationToken);
                var (data, dbTime) = await _db.ExecuteQueryAsync(sql, cancellationToken);

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

    }
}

