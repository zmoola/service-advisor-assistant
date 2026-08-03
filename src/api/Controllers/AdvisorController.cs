using Microsoft.AspNetCore.Mvc;
using ServiceAdvisorApi.Models;
using ServiceAdvisorApi.Services;
using System.Security.Cryptography;
using System.Text;

namespace ServiceAdvisorApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdvisorController : ControllerBase
{
    private readonly AzureOpenAiClient _openAi;
    private readonly ILogger<AdvisorController> _logger;

    public AdvisorController(AzureOpenAiClient openAi, ILogger<AdvisorController> logger)
    {
        _openAi = openAi;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] AdvisorRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Complaint))
        {
            return BadRequest(new { error = "Please provide a non-empty 'complaint' in the request body." });
        }

        // Log a hash of the complaint for traceability without storing the text
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(request.Complaint));
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        _logger.LogInformation("Received complaint (sha256): {Hash}", hash);

        try
        {
            var result = await _openAi.AnalyzeComplaintAsync(request.Complaint);
            if (result == null)
            {
                return StatusCode(502, new { error = "Failed to obtain a structured response from the language model." });
            }

            return Ok(result);
        }
        catch (AzureOpenAiClientException ex)
        {
            _logger.LogError(ex, "Azure OpenAI client error for complaint hash {Hash}", hash);
            return StatusCode(502, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing complaint hash {Hash}", hash);
            return StatusCode(500, new { error = "Unexpected server error" });
        }
    }
}
