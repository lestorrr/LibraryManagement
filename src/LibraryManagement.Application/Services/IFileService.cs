using LibraryManagement.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace LibraryManagement.Application.Services;

public interface IFileService
{
    Task<FileDto> UploadFileAsync(IFormFile file, Guid userId);
    Task<FileDto?> GetFileAsync(Guid fileId);
    Task<IEnumerable<FileDto>> GetUserFilesAsync(Guid userId);
    Task<bool> DeleteFileAsync(Guid fileId, Guid userId);
    Task<(byte[] content, string contentType, string fileName)> DownloadFileAsync(Guid fileId);
}