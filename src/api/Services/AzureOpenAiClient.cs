using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ServiceAdvisorApi.Models;

namespace ServiceAdvisorApi.Services
{
    public class AzureOpenAiClientException : Exception
    {
        public AzureOpenAiClientException(string message) : base(message) { }
    }

    public class AzureOpenAiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<AzureOpenAiClient> _logger;
        private readonly string _endpoint;
        private readonly string _key;
        private readonly string _deployment;

        public AzureOpenAiClient(HttpClient http, ILogger<AzureOpenAiClient> logger)
        {
            _http = http;
            _logger = logger;

            _endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new ArgumentException("Missing AZURE_OPENAI_ENDPOINT env var");
            _key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? throw new ArgumentException("Missing AZURE_OPENAI_KEY env var");
            _deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? throw new ArgumentException("Missing AZURE_OPENAI_DEPLOYMENT env var");

            if (!_endpoint.StartsWith("http")) _endpoint = "https://" + _endpoint;
        }

        public async Task<AdvisorResponse?> AnalyzeComplaintAsync(string complaint)
        {
            // Keep temperature low for deterministic, repeatable outputs
            var requestBody = new
            {
                messages = new[] {
                    new { role = "system", content = SystemPrompt() },
                    new { role = "user", content = complaint }
                },
                max_tokens = 700,
                temperature = 0.1
            };

            var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deployment}/chat/completions?api-version=2023-05-15";

            var json = JsonSerializer.Serialize(requestBody);
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("api-key", _key);

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync(url, httpContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request to Azure OpenAI failed");
                throw new AzureOpenAiClientException("Failed to contact Azure OpenAI service");
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Azure OpenAI returned {Status}: {Body}", response.StatusCode, responseContent);
                throw new AzureOpenAiClientException("Azure OpenAI returned an error");
            }

            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                var choice = root.GetProperty("choices")[0];
                var assistantMsg = choice.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

                // The assistant is instructed to return JSON only. Try to parse it.
                var trimmed = assistantMsg.Trim();
                // If the model returns code fences, strip them
                if (trimmed.StartsWith("```"))
                {
                    // remove ```json or ```
                    var lines = trimmed.Split('\n');
                    var sb = new StringBuilder();
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("```")) continue;
                        sb.AppendLine(line);
                    }
                    trimmed = sb.ToString().Trim();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsed = JsonSerializer.Deserialize<AdvisorResponse>(trimmed, options);
                if (parsed == null)
                {
                    _logger.LogError("Failed to parse assistant response as JSON. Raw: {Raw}", assistantMsg);
                    throw new AzureOpenAiClientException("Received unexpected response format from language model");
                }

                return parsed;
            }
            catch (JsonException je)
            {
                _logger.LogError(je, "Failed to parse Azure OpenAI response JSON: {Body}", responseContent);
                throw new AzureOpenAiClientException("Failed to parse response from language model");
            }
        }

        private string SystemPrompt()
        {
            return @$"You are a service advisor assistant in an automotive workshop. You MUST respond with JSON only, no extra text. The JSON schema must be an object with these properties:
1) rephrasedComplaint: A concise rephrasing of the customer's statement that workshop staff can act on.
2) solutions: an array of objects each with: {"{ issue: string, suggestedFix: string, confidence?: number }"}
3) note: optional short note for humans about ambiguities or required checks.

Ensure:
- Output valid JSON. Do not include markdown or explanatory text.
- Provide 2-5 solution objects ordered by likelihood.
- Use short, actionable suggestedFix phrases (eg. {"\"Check brake pads for wear; measure rotor runout; replace pads if thickness < 3mm\""}).
- When uncertain, include a note describing what further diagnostic checks are recommended.

Example output:
{"{"}
  {"\"rephrasedComplaint\": \"Car pulls to the right under braking; customer hears squeal from front right.\","}
  {"\"solutions\": ["}
    {"{ \"issue\": \"Front right brake pad worn or foreign object\", \"suggestedFix\": \"Inspect front right brake pad and rotor; clean or replace pad as needed; test drive.\", \"confidence\": 0.7 },"}
    {"{ \"issue\": \"Wheel bearing play\", \"suggestedFix\": \"Check front right wheel bearing for play and noise; replace bearing if worn.\", \"confidence\": 0.2 }"}
  ],
  {"\"note\": \"Ask customer if the noise occurs at slow speeds only and whether braking force changes when cold.\""}
{"}"} 
";
        }
    }
}
