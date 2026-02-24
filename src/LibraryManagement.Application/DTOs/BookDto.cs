using LibraryManagement.Domain.Enums;

namespace LibraryManagement.Application.DTOs;

public class BookDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public string Publisher { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BookStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // File information
    public string? CoverImageUrl { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? FileType { get; set; }

    // inventory
    public int Quantity { get; set; }
    public int Available { get; set; }
    
    // User info
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    
    // UI helpers
    public string FormattedFileSize => FileSize.HasValue 
        ? FormatFileSize(FileSize.Value) 
        : "N/A";
    
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
