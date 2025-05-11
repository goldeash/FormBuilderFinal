using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace FormBuilder.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SupportController> _logger;

        public SupportController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SupportController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] SupportTicket ticket)
        {
            try
            {
                _logger.LogInformation("Creating support ticket for user: {User}", ticket.ReportedBy);

                var json = JsonConvert.SerializeObject(ticket);
                var fileName = $"ticket_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

                var tempPath = Path.GetTempPath();
                var filePath = Path.Combine(tempPath, fileName);
                await System.IO.File.WriteAllTextAsync(filePath, json);

                var result = await UploadToOneDrive(json);

                if (!result)
                {
                    _logger.LogError("Failed to upload ticket to OneDrive");
                    return StatusCode(500, new { error = "Failed to submit ticket" });
                }

                _logger.LogInformation("Ticket created and uploaded successfully");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating support ticket");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<bool> UploadToOneDrive(string json)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    _configuration["OneDrive:UploadUrl"],
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Power Automate returned error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                }
                else
                {
                    _logger.LogInformation("Power Automate response: {Content}", responseContent);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while uploading to OneDrive");
                return false;
            }
        }
    }

    public class SupportTicket
    {
        public string ReportedBy { get; set; }
        public string Summary { get; set; }
        public string Priority { get; set; }
        public string Link { get; set; }
        public string Timestamp { get; set; }
    }
}
