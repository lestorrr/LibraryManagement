using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface IFileRepository : IRepository<FileEntity>
{
    Task<IEnumerable<FileEntity>> GetFilesByUserIdAsync(Guid userId);
}