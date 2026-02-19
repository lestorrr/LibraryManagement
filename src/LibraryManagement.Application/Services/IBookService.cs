using LibraryManagement.Application.DTOs;

namespace LibraryManagement.Application.Services;

public interface IBookService
{
    Task<BookDto?> GetBookByIdAsync(Guid id);
    Task<IEnumerable<BookDto>> GetAllBooksAsync();
    Task<IEnumerable<BookDto>> GetBooksByAuthorAsync(string author);
    Task<IEnumerable<BookDto>> GetBooksByCategoryAsync(string category);
    Task<BookDto> CreateBookAsync(CreateBookDto createBookDto);
    Task<BookDto?> UpdateBookAsync(Guid id, CreateBookDto updateBookDto);
    Task<bool> DeleteBookAsync(Guid id);
    Task<bool> BookExistsAsync(string isbn);
    Task<IEnumerable<BookDto>> SearchBooksAsync(string searchTerm);
}
