using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return Ok(new
        {
            Message = "Welcome to Library Management API!",
            Version = "1.0.0",
            Status = "Running",
            Documentation = "/swagger",
            Endpoints = new
            {
                Books = "/api/books",
                Search = "/api/books/search?term={searchTerm}",
                Author = "/api/books/author/{author}",
                Category = "/api/books/category/{category}"
            },
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        });
    }
}
