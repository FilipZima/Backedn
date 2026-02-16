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
                var result = await _contactService.StoreContactAsync(contact);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing contact");
                return StatusCode(500, "An error occurred while storing the contact");
            }
        }
    }
}