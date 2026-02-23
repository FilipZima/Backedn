using Microsoft.AspNetCore.Mvc;
using FileStorage;

namespace ReactApp1.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IContactStore _contactService;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IContactStore contactService)
        {
            _logger = logger;
            _contactService = contactService;
        }

        [HttpPost("/Contact/Store")]
        public async Task<IActionResult> Post(ContactMessage contact)
        {
            if (contact == null)
            {
                _logger.LogWarning("Null contact object received");
                return BadRequest("Contact data is required");
            }

            try
            {
                await _contactService.StoreContactAsync(contact);
                return Ok("Gay");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing contact");
                return StatusCode(500, "An error occurred while storing the contact");
            }
        }

        [HttpGet("/Contact/StoreAll")]
        public string Get()
        {
            return _contactService.GetAllJson();
        }

        // New long-polling endpoint: client passes known version and waits for changes
        [HttpGet("/Contact/WaitForChanges")]
        public async Task<IActionResult> WaitForChanges([FromQuery] long sinceVersion = 0, [FromQuery] int timeoutMs = 30000, CancellationToken cancellationToken = default)
        {
            try
            {
                var (json, version) = await _contactService.WaitForChangesAsync(sinceVersion, timeoutMs, cancellationToken);
                return Ok(new { json, version });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(204); // No content if cancelled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for changes");
                return StatusCode(500);
            }
        }
    }
}