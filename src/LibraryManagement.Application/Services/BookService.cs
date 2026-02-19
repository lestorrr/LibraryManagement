using AutoMapper;
using LibraryManagement.Application.DTOs;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Application.Services;

public class BookService : IBookService
{
    private readonly IRepository<Book> _bookRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<BookService> _logger;

    public BookService(
        IRepository<Book> bookRepository, 
        IMapper mapper,
        ILogger<BookService> logger)
    {
        _bookRepository = bookRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BookDto?> GetBookByIdAsync(Guid id)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(id);
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
            var books = await _bookRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<BookDto>>(books);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all books");
            throw;
        }
    }

    public async Task<IEnumerable<BookDto>> GetBooksByAuthorAsync(string author)
    {
        try
        {
            var books = await _bookRepository.FindAsync(b => b.Author.Contains(author));
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
            if (await BookExistsAsync(createBookDto.ISBN))
            {
                throw new InvalidOperationException($"Book with ISBN {createBookDto.ISBN} already exists.");
            }

            var book = _mapper.Map<Book>(createBookDto);
            var createdBook = await _bookRepository.AddAsync(book);
            
            _logger.LogInformation("Created new book: {BookTitle} with ID: {BookId}", book.Title, book.Id);
            
            return _mapper.Map<BookDto>(createdBook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book: {BookTitle}", createBookDto.Title);
            throw;
        }
    }

    public async Task<BookDto?> UpdateBookAsync(Guid id, CreateBookDto updateBookDto)
    {
        try
        {
            var existingBook = await _bookRepository.GetByIdAsync(id);
            if (existingBook == null)
            {
                return null;
            }

            if (existingBook.ISBN != updateBookDto.ISBN && await BookExistsAsync(updateBookDto.ISBN))
            {
                throw new InvalidOperationException($"Book with ISBN {updateBookDto.ISBN} already exists.");
            }

            _mapper.Map(updateBookDto, existingBook);
            existingBook.UpdatedAt = DateTime.UtcNow;
            
            await _bookRepository.UpdateAsync(existingBook);
            
            _logger.LogInformation("Updated book: {BookId}", id);
            
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
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null)
            {
                return false;
            }

            await _bookRepository.DeleteAsync(book);
            
            _logger.LogInformation("Deleted book: {BookId}", id);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book: {BookId}", id);
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
}
