using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.AI.OpenAI;
using ServiceAdvisorApi.Models;
using OpenAI.Chat;
using System.ClientModel;

namespace ServiceAdvisorApi.Services
{
    public class AzureOpenAiClientException : Exception
    {
        public AzureOpenAiClientException(string message) : base(message) { }
    }

    public class LLMClient
    {
        private readonly ILogger<LLMClient> _logger;
        private readonly string _deployment;
        private readonly AzureOpenAIClient _openAiClient;
        private readonly ChatClient _chatClient;

        public LLMClient(AzureOpenAIClient openAiClient, IConfiguration configuration, ILogger<LLMClient> logger)
        {
            _logger = logger;
            _openAiClient = openAiClient;
            _deployment = configuration.GetSection("AZURE_OPENAI_DEPLOYMENT").Value ?? throw new ArgumentException("Missing AZURE_OPENAI_DEPLOYMENT env var");
            _chatClient = _openAiClient.GetChatClient(_deployment);
        }

        public async Task<AdvisorResponse?> AnalyzeComplaintAsync(string complaint)
        {
            // Use Azure.AI.OpenAI SDK to call the deployment
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt()),
                new UserChatMessage(complaint)
            };

            ClientResult<ChatCompletion>? response = null;
            try
            {
                response = await _chatClient.CompleteChatAsync(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure OpenAI SDK call failed");
                throw new AzureOpenAiClientException("Failed to contact Azure OpenAI service");
            }

            try
            {
                var completion = response?.Value;
                if (completion == null || !completion.Content.Any())
                {
                    _logger.LogError("Azure OpenAI returned empty response");
                    throw new AzureOpenAiClientException("Azure OpenAI returned an empty response");
                }

                if (completion.Content[0].Kind != ChatMessageContentPartKind.Text)
                {
                    _logger.LogError("Azure OpenAI returned non-text content");
                    throw new AzureOpenAiClientException("Azure OpenAI returned non-text content");
                }

                var assistantMsg = completion.Content[0].Text ?? string.Empty;

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

                var optionsJson = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsed = JsonSerializer.Deserialize<AdvisorResponse>(trimmed, optionsJson);
                if (parsed == null)
                {
                    _logger.LogError("Failed to parse assistant response as JSON. Raw: {Raw}", assistantMsg);
                    throw new AzureOpenAiClientException("Received unexpected response format from language model");
                }

                return parsed;
            }
            catch (JsonException je)
            {
                _logger.LogError(je, "Failed to parse Azure OpenAI response JSON: {Body}", response?.Value?.ToString());
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
