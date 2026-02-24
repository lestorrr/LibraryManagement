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

    // Public endpoints - no authentication required
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

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooks([FromQuery] string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest("Search term cannot be empty");

            var books = await _bookService.SearchBooksAsync(term);
            return Ok(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books with term: {SearchTerm}", term);
            return StatusCode(500, "An error occurred while searching books");
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadBookFile(Guid id)
    {
        try
        {
            var (content, name, type) = await _bookService.GetBookFileAsync(id);
            if (content == null)
                return NotFound("No file found for this book");

            var contentType = type ?? "application/octet-stream";
            var fileName = name ?? "download";
            return File(content, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file for book ID: {BookId}", id);
            return StatusCode(500, "An error occurred while downloading the file");
        }
    }

    [HttpGet("{id}/preview")]
    public async Task<IActionResult> PreviewBookFile(Guid id)
    {
        try
        {
            var (content, name, type) = await _bookService.GetBookFileAsync(id);
            if (content == null)
                return NotFound("No file found for this book");

            var contentType = type ?? "application/octet-stream";
            return File(content, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing file for book ID: {BookId}", id);
            return StatusCode(500, "An error occurred while previewing the file");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetBookById(Guid id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            
            if (book == null)
                return NotFound($"Book with ID {id} not found");
            
            return Ok(book);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting book with ID: {BookId}", id);
            return StatusCode(500, "An error occurred while retrieving the book");
        }
    }

    // Protected endpoints - require authentication
    [Authorize]
    [HttpGet("mybooks")]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetMyBooks()
    {
        try
        {
            var books = await _bookService.GetMyBooksAsync();
            return Ok(books);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user books");
            return StatusCode(500, "An error occurred while retrieving your books");
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<BookDto>> CreateBook([FromForm] CreateBookDto createBookDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var book = await _bookService.CreateBookAsync(createBookDto);
            return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, book);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
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

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBook(Guid id, UpdateBookDto updateBookDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedBook = await _bookService.UpdateBookAsync(id, updateBookDto);
            
            if (updatedBook == null)
                return NotFound($"Book with ID {id} not found");
            
            return Ok(updatedBook);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("You don't have permission to update this book");
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

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBook(Guid id)
    {
        try
        {
            var deleted = await _bookService.DeleteBookAsync(id);
            
            if (!deleted)
                return NotFound($"Book with ID {id} not found");
            
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("You don't have permission to delete this book");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book with ID: {BookId}", id);
            return StatusCode(500, "An error occurred while deleting the book");
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
}
