using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using LibraryManagement.Application.DTOs;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Enums;
using LibraryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<BookService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BookService(
        IBookRepository bookRepository,
        IMapper mapper,
        ILogger<BookService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _bookRepository = bookRepository;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (userIdClaim == null)
            throw new UnauthorizedAccessException("User not authenticated");
            
        return Guid.Parse(userIdClaim);
    }

    public async Task<BookDto?> GetBookByIdAsync(Guid id)
    {
        try
        {
            var book = await _bookRepository.GetBookWithLoansAsync(id);
            return book != null ? _mapper.Map<BookDto>(book) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting book by ID: {BookId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<BookDto>> GetAllBooksAsync()
    {
        try
        {
            var books = await _bookRepository.GetAvailableBooksAsync();
            return _mapper.Map<IEnumerable<BookDto>>(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all books");
            throw;
        }
    }

    public async Task<IEnumerable<BookDto>> GetMyBooksAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            var books = await _bookRepository.GetUserBooksWithDetailsAsync(userId);
            return _mapper.Map<IEnumerable<BookDto>>(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user books");
            throw;
        }
    }

    // Interface-compatible: get books for a specific user
    public async Task<IEnumerable<BookDto>> GetBooksByUserAsync(Guid userId)
    {
        try
        {
            var books = await _bookRepository.GetUserBooksWithDetailsAsync(userId);
            return _mapper.Map<IEnumerable<BookDto>>(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting books for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<BookDto>> GetBooksByAuthorAsync(string author)
    {
        try
        {
            var books = await _bookRepository.GetBooksByAuthorAsync(author);
            return _mapper.Map<IEnumerable<BookDto>>(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting books by author: {Author}", author);
            throw;
        }
    }

    public async Task<IEnumerable<BookDto>> GetBooksByCategoryAsync(string category)
    {
        try
        {
            var books = await _bookRepository.FindAsync(b => b.Category == category);
            return _mapper.Map<IEnumerable<BookDto>>(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting books by category: {Category}", category);
            throw;
        }
    }

    public async Task<BookDto> CreateBookAsync(CreateBookDto createBookDto)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (await BookExistsAsync(createBookDto.ISBN))
            {
                throw new InvalidOperationException($"Book with ISBN {createBookDto.ISBN} already exists.");
            }

            var book = _mapper.Map<Book>(createBookDto);
            book.UserId = userId;
            book.Status = BookStatus.Available;
            
            if (createBookDto.BookFile != null)
            {
                await SaveBookFileAsync(book, createBookDto.BookFile);
            }
            
            if (createBookDto.CoverImage != null)
            {
                await SaveCoverImageAsync(book, createBookDto.CoverImage);
            }

            var createdBook = await _bookRepository.AddAsync(book);
            
            _logger.LogInformation("User {UserId} created new book: {BookTitle}", userId, book.Title);
            
            return _mapper.Map<BookDto>(createdBook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book: {BookTitle}", createBookDto.Title);
            throw;
        }
    }

    // Overload allowing explicit ownerId (used by some controllers)
    public async Task<BookDto> CreateBookAsync(CreateBookDto createBookDto, Guid ownerId)
    {
        try
        {
            if (await BookExistsAsync(createBookDto.ISBN))
            {
                throw new InvalidOperationException($"Book with ISBN {createBookDto.ISBN} already exists.");
            }

            var book = _mapper.Map<Book>(createBookDto);
            book.UserId = ownerId;
            book.Status = BookStatus.Available;

            var createdBook = await _bookRepository.AddAsync(book);
            _logger.LogInformation("User {UserId} created new book: {BookTitle}", ownerId, book.Title);
            return _mapper.Map<BookDto>(createdBook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book: {BookTitle}", createBookDto.Title);
            throw;
        }
    }

    public async Task<BookDto?> UpdateBookAsync(Guid id, UpdateBookDto updateBookDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var existingBook = await _bookRepository.GetByIdAsync(id);
            
            if (existingBook == null)
                return null;
                
            if (existingBook.UserId != userId)
                throw new UnauthorizedAccessException("You don't have permission to update this book");

            if (!string.IsNullOrWhiteSpace(updateBookDto.Title))
                existingBook.Title = updateBookDto.Title;
            
            if (!string.IsNullOrWhiteSpace(updateBookDto.Author))
                existingBook.Author = updateBookDto.Author;
            
            if (!string.IsNullOrWhiteSpace(updateBookDto.ISBN) && existingBook.ISBN != updateBookDto.ISBN)
            {
                if (await BookExistsAsync(updateBookDto.ISBN))
                    throw new InvalidOperationException($"Book with ISBN {updateBookDto.ISBN} already exists.");
                existingBook.ISBN = updateBookDto.ISBN;
            }
            
            if (updateBookDto.PublicationYear.HasValue)
                existingBook.PublicationYear = updateBookDto.PublicationYear.Value;
            
            if (!string.IsNullOrWhiteSpace(updateBookDto.Publisher))
                existingBook.Publisher = updateBookDto.Publisher;
            
            if (updateBookDto.PageCount.HasValue)
                existingBook.PageCount = updateBookDto.PageCount.Value;
            
            if (!string.IsNullOrWhiteSpace(updateBookDto.Category))
                existingBook.Category = updateBookDto.Category;
            
            if (!string.IsNullOrWhiteSpace(updateBookDto.Description))
                existingBook.Description = updateBookDto.Description;
            
            if (updateBookDto.Status.HasValue)
                existingBook.Status = updateBookDto.Status.Value;

            existingBook.UpdatedAt = DateTime.UtcNow;
            
            await _bookRepository.UpdateAsync(existingBook);
            
            _logger.LogInformation("User {UserId} updated book: {BookId}", userId, id);
            
            return _mapper.Map<BookDto>(existingBook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating book: {BookId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteBookAsync(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var book = await _bookRepository.GetByIdAsync(id);
            
            if (book == null)
                return false;
                
            if (book.UserId != userId)
                throw new UnauthorizedAccessException("You don't have permission to delete this book");

            await _bookRepository.DeleteAsync(book);
            
            _logger.LogInformation("User {UserId} deleted book: {BookId}", userId, id);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book: {BookId}", id);
            throw;
        }
    }

    // Interface-compatible update that accepts CreateBookDto as the update payload
    public async Task<BookDto?> UpdateBookAsync(Guid id, CreateBookDto updateBookDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var existingBook = await _bookRepository.GetByIdAsync(id);

            if (existingBook == null)
                return null;

            if (existingBook.UserId != userId)
                throw new UnauthorizedAccessException("You don't have permission to update this book");

            // Apply fields from create DTO as full update
            existingBook.Title = updateBookDto.Title;
            existingBook.Author = updateBookDto.Author;
            if (!string.IsNullOrWhiteSpace(updateBookDto.ISBN) && existingBook.ISBN != updateBookDto.ISBN)
            {
                if (await BookExistsAsync(updateBookDto.ISBN))
                    throw new InvalidOperationException($"Book with ISBN {updateBookDto.ISBN} already exists.");
                existingBook.ISBN = updateBookDto.ISBN;
            }
            existingBook.PublicationYear = updateBookDto.PublicationYear;
            existingBook.Publisher = updateBookDto.Publisher;
            existingBook.PageCount = updateBookDto.PageCount;
            existingBook.Category = updateBookDto.Category;
            existingBook.Description = updateBookDto.Description;

            existingBook.UpdatedAt = DateTime.UtcNow;

            await _bookRepository.UpdateAsync(existingBook);

            _logger.LogInformation("User {UserId} updated book: {BookId}", userId, id);

            return _mapper.Map<BookDto>(existingBook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating book: {BookId}", id);
            throw;
        }
    }

    public async Task<bool> BookExistsAsync(string isbn)
    {
        return await _bookRepository.ExistsAsync(b => b.ISBN == isbn);
    }

    public async Task<IEnumerable<BookDto>> SearchBooksAsync(string searchTerm)
    {
        try
        {
            var books = await _bookRepository.FindAsync(b => 
                b.Title.Contains(searchTerm) || 
                b.Author.Contains(searchTerm) || 
                b.ISBN.Contains(searchTerm) ||
                b.Category.Contains(searchTerm));
            
            return _mapper.Map<IEnumerable<BookDto>>(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    private async Task SaveBookFileAsync(Book book, IFormFile file)
    {
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            book.FileContent = memoryStream.ToArray();
            book.FileName = file.FileName;
            book.FileSize = file.Length;
            book.FileType = file.ContentType;
        }
    }

    private async Task SaveCoverImageAsync(Book book, IFormFile image)
    {
        using (var memoryStream = new MemoryStream())
        {
            await image.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            book.CoverImageUrl = $"data:{image.ContentType};base64,{Convert.ToBase64String(bytes)}";
        }
    }

    public async Task<byte[]?> DownloadBookFileAsync(Guid id)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(id);
            return book?.FileContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file for book ID: {BookId}", id);
            throw;
        }
    }

    public async Task<string?> GetBookFileUrlAsync(Guid id)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(id);
            return book?.FileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file URL for book ID: {BookId}", id);
            throw;
        }
    }
}
