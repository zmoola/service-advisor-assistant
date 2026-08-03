using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer()
.AddGoogle(opt =>
{
    opt.ClientId = builder.Configuration.GetSection("GOOGLE_CLIENT_ID").Value ?? throw new ArgumentException("Missing GOOGLE_CLIENT_ID env var");
    opt.ClientSecret = builder.Configuration.GetSection("GOOGLE_CLIENT_SECRET").Value ?? throw new ArgumentException("Missing GOOGLE_CLIENT_SECRET env var");
});
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
