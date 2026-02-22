using System.ComponentModel.DataAnnotations;
using LibraryManagement.Domain.Enums;

namespace LibraryManagement.Application.DTOs;

public class UpdateBookDto
{
    [StringLength(200)]
    public string? Title { get; set; }
    
    [StringLength(100)]
    public string? Author { get; set; }
    
    [StringLength(13)]
    public string? ISBN { get; set; }
    
    [Range(1000, 2100)]
    public int? PublicationYear { get; set; }
    
    [StringLength(100)]
    public string? Publisher { get; set; }
    
    [Range(1, 10000)]
    public int? PageCount { get; set; }
    
    [StringLength(50)]
    public string? Category { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public BookStatus? Status { get; set; }
}
