using System.Text.Json.Serialization;

namespace ServiceAdvisorApi.Models
{
    public class AdvisorResponse
    {
        [JsonPropertyName("rephrasedComplaint")]
        public string RephrasedComplaint { get; set; } = string.Empty;

        [JsonPropertyName("solutions")]
        public List<Solution> Solutions { get; set; } = new List<Solution>();

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }
}
