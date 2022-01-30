using Microsoft.AspNetCore.Mvc;
using MyApp.Backend.Models;

namespace MyApp.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public HealthDto Get()
    {
        return new HealthDto()
        {
            Status = "OK"
        };
    }
}