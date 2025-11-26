using SoccerQueryAPI.Data;
using SoccerQueryAPI.Services;
using System.Text.Json;

namespace SoccerQueryAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped<SemanticKernelService>();
            builder.Services.AddScoped<DatabaseHelper>();
            builder.Services.AddSingleton<SqlValidator>();

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

});
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthorization();
            app.MapControllers();


            //var kernelBuilder = Kernel.CreateBuilder();
            //kernelBuilder.AddOpenAIChatCompletion(
            //    modelId: "gpt-4o",
            //    apiKey: builder.Configuration["OPENAI:APIKEY"]
            //);

            //var kernel = kernelBuilder.Build();

            //const string promptTemplate = @"
            //You are a helpful assistant. Answer the following question:
            //Question: {{$input}}
            //Answer:";

            //var semanticFunction = kernel.CreateFunctionFromPrompt(
            //    promptTemplate,
            //    functionName: "AnswerPrompt"
            //);

            //// Prompt the user for input
            //Console.WriteLine("Enter a question:");
            //string userQuestion = Console.ReadLine() ?? "";

            //// Setup kernel arguments
            //var kernelArguments = new KernelArguments();
            //kernelArguments["input"] = userQuestion;

            //// Await the semantic function execution
            //var functionResult = await semanticFunction.InvokeAsync(kernel, kernelArguments);

            //// Display the output — FunctionResult.ToString() or inspect the result object
            //Console.WriteLine("\nResponse from Semantic Kernel:");
            //Console.WriteLine(functionResult.ToString());


            app.Run();
        }
    }
}
