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
}
