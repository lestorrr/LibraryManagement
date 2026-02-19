using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface IBookRepository : IRepository<Book>
{
    Task<IEnumerable<Book>> GetBooksByAuthorAsync(string author);
    Task<IEnumerable<Book>> GetAvailableBooksAsync();
    Task<Book?> GetBookWithLoansAsync(Guid id);
}
