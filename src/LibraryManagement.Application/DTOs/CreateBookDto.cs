using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LibraryManagement.Application.DTOs;

public class CreateBookDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Author { get; set; } = string.Empty;
    
    [Required]
    [StringLength(13, MinimumLength = 10)]
    public string ISBN { get; set; } = string.Empty;
    
    [Range(1000, 2100)]
    public int PublicationYear { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Publisher { get; set; } = string.Empty;
    
    [Range(1, 10000)]
    public int PageCount { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    // Optional file uploads
    public IFormFile? BookFile { get; set; }
    public IFormFile? CoverImage { get; set; }
}
