using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Data.Repositories;

public class BookRepository : Repository<Book>, IBookRepository
{
    public BookRepository(LibraryContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Book>> GetBooksByAuthorAsync(string author)
    {
        return await _dbSet
            .Where(b => b.Author.Contains(author))
            .Include(b => b.Loans)
            .ToListAsync();
    }

    public async Task<IEnumerable<Book>> GetAvailableBooksAsync()
    {
        return await _dbSet
            .Where(b => b.Status == Domain.Enums.BookStatus.Available)
            .Include(b => b.Loans)
            .ToListAsync();
    }

    public async Task<Book?> GetBookWithLoansAsync(Guid id)
    {
        return await _dbSet
            .Include(b => b.Loans)
            .ThenInclude(l => l.Member)
            .FirstOrDefaultAsync(b => b.Id == id);
    }
}
