using System.Text.Json.Serialization;

namespace ServiceAdvisorApi.Models
{
    public class Solution
    {
        [JsonPropertyName("issue")]
        public string Issue { get; set; } = string.Empty;

        [JsonPropertyName("suggestedFix")]
        public string SuggestedFix { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }
    }
}
