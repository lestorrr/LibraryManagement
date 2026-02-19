using LibraryManagement.Application.DTOs;
using LibraryManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly ILogger<BooksController> _logger;

    public BooksController(IBookService bookService, ILogger<BooksController> logger)
    {
        _bookService = bookService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetAllBooks()
    {
        try
        {
            var books = await _bookService.GetAllBooksAsync();
            return Ok(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all books");
            return StatusCode(500, "An error occurred while retrieving books");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetBookById(Guid id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            
            if (book == null)
            {
                return NotFound($"Book with ID {id} not found");
            }
            
            return Ok(book);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting book with ID: {BookId}", id);
            return StatusCode(500, "An error occurred while retrieving the book");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BookDto>> CreateBook(CreateBookDto createBookDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // extract user id from token
            var userIdClaim = User.FindFirst("id")?.Value;
            Guid ownerId = Guid.Empty;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsed))
            {
                ownerId = parsed;
            }

            var book = await _bookService.CreateBookAsync(createBookDto, ownerId);
            return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, book);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book");
            return StatusCode(500, "An error occurred while creating the book");
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateBook(Guid id, CreateBookDto updateBookDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // ensure current user is owner
            var existing = await _bookService.GetBookByIdAsync(id);
            if (existing == null)
                return NotFound($"Book with ID {id} not found");

            var userIdClaim = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId) || existing.OwnerId != userId)
            {
                return Forbid();
            }

            var updatedBook = await _bookService.UpdateBookAsync(id, updateBookDto);
            
            if (updatedBook == null)
            {
                return NotFound($"Book with ID {id} not found");
            }
            
            return Ok(updatedBook);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating book with ID: {BookId}", id);
            return StatusCode(500, "An error occurred while updating the book");
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteBook(Guid id)
    {
        try
        {
            var existing = await _bookService.GetBookByIdAsync(id);
            if (existing == null)
                return NotFound($"Book with ID {id} not found");

            var userIdClaim = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId) || existing.OwnerId != userId)
            {
                return Forbid();
            }

            var deleted = await _bookService.DeleteBookAsync(id);
            
            if (!deleted)
            {
                return NotFound($"Book with ID {id} not found");
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book with ID: {BookId}", id);
            return StatusCode(500, "An error occurred while deleting the book");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooks([FromQuery] string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return BadRequest("Search term cannot be empty");
            }

            var books = await _bookService.SearchBooksAsync(term);
            return Ok(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books with term: {SearchTerm}", term);
            return StatusCode(500, "An error occurred while searching books");
        }
    }

    [HttpGet("author/{author}")]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooksByAuthor(string author)
    {
        try
        {
            var books = await _bookService.GetBooksByAuthorAsync(author);
            return Ok(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting books by author: {Author}", author);
            return StatusCode(500, "An error occurred while retrieving books");
        }
    }

    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooksByCategory(string category)
    {
        try
        {
            var books = await _bookService.GetBooksByCategoryAsync(category);
            return Ok(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting books by category: {Category}", category);
            return StatusCode(500, "An error occurred while retrieving books");
        }
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetMyBooks()
    {
        try
        {
            var userIdClaim = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var books = await _bookService.GetBooksByUserAsync(userId);
            return Ok(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user's books");
            return StatusCode(500, "An error occurred while retrieving books");
        }
    }
}
