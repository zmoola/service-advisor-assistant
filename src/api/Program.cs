using Azure.AI.OpenAI;
using ServiceAdvisorApi.Services;
using System.ClientModel;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllers();
builder.Services.AddScoped<LLMClient>();
builder.Services.AddScoped(_ =>
{
    var endpoint = builder.Configuration.GetSection("AZURE_OPENAI_ENDPOINT").Value ?? throw new ArgumentException("Missing AZURE_OPENAI_ENDPOINT env var");
    var apiKey = builder.Configuration.GetSection("AZURE_OPENAI_API_KEY").Value ?? throw new ArgumentException("Missing AZURE_OPENAI_API_KEY env var");
    var credential = new ApiKeyCredential(apiKey);
    return new AzureOpenAIClient(new Uri(endpoint), credential);
});
builder.Services.AddOpenApi();
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors();
app.MapControllers();

app.Run();
