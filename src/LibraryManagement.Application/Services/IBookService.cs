using LibraryManagement.Application.DTOs;

namespace LibraryManagement.Application.Services;

public interface IBookService
{
    Task<BookDto?> GetBookByIdAsync(Guid id);
    Task<IEnumerable<BookDto>> GetAllBooksAsync();
    Task<IEnumerable<BookDto>> GetBooksByAuthorAsync(string author);
    Task<IEnumerable<BookDto>> GetBooksByCategoryAsync(string category);
    Task<IEnumerable<BookDto>> GetBooksByUserAsync(Guid userId);
    Task<IEnumerable<BookDto>> GetMyBooksAsync();
    Task<BookDto> CreateBookAsync(CreateBookDto createBookDto);
    Task<BookDto> CreateBookAsync(CreateBookDto createBookDto, Guid ownerId);
    Task<BookDto?> UpdateBookAsync(Guid id, CreateBookDto updateBookDto);
    Task<BookDto?> UpdateBookAsync(Guid id, UpdateBookDto updateBookDto);
    Task<byte[]?> DownloadBookFileAsync(Guid id);
        
    /// <summary>
    /// Retrieves the file bytes along with its original name and content type.
    /// </summary>
    Task<(byte[]? Content, string? FileName, string? ContentType)> GetBookFileAsync(Guid id);
    Task<bool> DeleteBookAsync(Guid id);
    Task<bool> BookExistsAsync(string isbn);
    Task<IEnumerable<BookDto>> SearchBooksAsync(string searchTerm);
}
