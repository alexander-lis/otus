using Microsoft.AspNetCore.Mvc;

namespace MyApp.Billing.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("health")]
    public dynamic GetHealth()
    {
        return new
        {
            Status = "OK"
        };
    }
    
    [HttpGet("ready")]
    public dynamic GetReady()
    {
        return new
        {
            Status = "OK"
        };
    }
}