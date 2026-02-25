using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly ILogger<HomeController> _logger;
    private readonly LibraryContext _context;

    public HomeController(ILogger<HomeController> logger, LibraryContext context)
    {
        _logger = logger;
        _context = context;
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

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        try
        {
            var userCount = await _context.Users.CountAsync();
            var bookCount = await _context.Books.CountAsync();
            var loanCount = await _context.Loans.CountAsync();
            var activeLoans = await _context.Loans.Where(l => l.ReturnDate == null).CountAsync();
            
            return Ok(new
            {
                TotalBooks = bookCount,
                TotalUsers = userCount,
                TotalLoans = loanCount,
                ActiveLoans = activeLoans,
                DatabaseStatus = "Connected",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database query failed: {Message}", ex.Message);
            return Ok(new
            {
                TotalBooks = 0,
                TotalUsers = 0,
                TotalLoans = 0,
                ActiveLoans = 0,
                DatabaseStatus = "Error",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("test-db")]
    public async Task<IActionResult> TestDatabase()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            var connectionString = _context.Database.GetConnectionString();
            
            return Ok(new
            {
                CanConnect = canConnect,
                ConnectionString = connectionString?.Substring(0, Math.Min(50, connectionString.Length)) + "...",
                DatabaseProvider = _context.Database.ProviderName,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Error = ex.Message,
                StackTrace = ex.StackTrace,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
