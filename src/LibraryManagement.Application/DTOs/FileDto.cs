using Microsoft.AspNetCore.Http;

namespace LibraryManagement.Application.DTOs;

public class FileUploadDto
{
    public IFormFile File { get; set; } = null!;
}

public class FileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
}