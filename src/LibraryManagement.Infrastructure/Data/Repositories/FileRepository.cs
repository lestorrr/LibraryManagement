using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Data.Repositories;

public class FileRepository : Repository<FileEntity>, IFileRepository
{
    public FileRepository(LibraryContext context) : base(context)
    {
    }

    public async Task<IEnumerable<FileEntity>> GetFilesByUserIdAsync(Guid userId)
    {
        return await _context.Set<FileEntity>()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }
}