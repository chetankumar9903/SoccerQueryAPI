using Microsoft.SemanticKernel;
namespace SoccerQueryAPI.Services
{
    public class SemanticKernelService
    {

        private readonly Kernel _kernel;
        private readonly string _sqlPromptTemplate;
        private readonly ILogger<SemanticKernelService> _logger;
        private readonly string _modelId;
        public SemanticKernelService(IConfiguration config, ILogger<SemanticKernelService> logger)
        {
            _logger = logger;
            var apiKey = config["OpenAI:ApiKey"] ?? config["OPENAI:APIKEY"];
            _modelId = config["OpenAI:Model"] ?? "gemini-2.5-flash";

            var builder = Kernel.CreateBuilder();
            // Try to add the Google Gemini connector if available in your environment.
            // If you use OpenAI replace or add AddOpenAIChatCompletion instead.
            builder.AddGoogleAIGeminiChatCompletion(modelId: _modelId, apiKey: apiKey);
            _kernel = builder.Build();

            _sqlPromptTemplate = @"
You are an expert SQL generator. Use ONLY the provided tables & columns for a SQLite database.
Tables: Match(match_api_id, date, home_team_api_id, away_team_api_id, home_team_goal, away_team_goal),
Team(team_api_id, team_long_name, team_short_name),
Player(player_api_id, player_name, birthday),
Player_Attributes(player_api_id, date, overall_rating, potential, sprint_speed)
Rules:
- Output ONLY a single SELECT query (no explanation, no code fences).
- Use proper JOINs if needed.
- Do NOT hallucinate columns or tables.
NLQ: {{$input}}
SQL:
";
        }

        public async Task<string> GenerateSqlAsync(string question, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question)) return string.Empty;

            try
            {
                var args = new KernelArguments();
                args["input"] = question;

                // Invoke the prompt
                var r = await _kernel.InvokePromptAsync(_sqlPromptTemplate, args, cancellationToken: cancellationToken);
                var text = r.GetValue<string>() ?? string.Empty;

                _logger.LogInformation("Very first response of AI {RawResponse}  - " + text);
                return text.Trim().Replace("```sql", "").Replace("```", "").Trim();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("SQL generation canceled due to timeout/cancellation.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating SQL from NLQ");
                throw;
            }
        }

        public async Task<string> TestModelAsync(string simplePrompt = "Say hello", CancellationToken cancellationToken = default)
        {

            var prompt = $"You are a friendly assistant. Respond in one sentence: {simplePrompt}";
            try
            {
                var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments { }, cancellationToken: cancellationToken);
                return result.GetValue<string>() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model test failed.");
                throw;
            }
        }
    }
}

