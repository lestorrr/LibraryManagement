using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibraryManagement.Domain.Enums;

namespace LibraryManagement.Domain.Entities;

[Table("Books")]
public class Book
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }
    
    [Column("Title")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Column("Author")]
    [StringLength(100)]
    public string Author { get; set; } = string.Empty;
    
    [Column("ISBN")]
    [StringLength(13)]
    public string ISBN { get; set; } = string.Empty;
    
    [Column("PublicationYear")]
    public int PublicationYear { get; set; }
    
    [Column("Publisher")]
    [StringLength(100)]
    public string Publisher { get; set; } = string.Empty;
    
    [Column("PageCount")]
    public int PageCount { get; set; }
    
    [Column("Category")]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;
    
    [Column("Description")]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [Column("Status")]
    public BookStatus Status { get; set; }
    
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
    
    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
    
    [Column("CoverImageUrl")]
    [StringLength(500)]
    public string? CoverImageUrl { get; set; }
    
    [Column("FileUrl")]
    [StringLength(500)]
    public string? FileUrl { get; set; }
    
    [Column("FileName")]
    [StringLength(255)]
    public string? FileName { get; set; }
    
    [Column("FileSize")]
    public long? FileSize { get; set; }
    
    [Column("FileType")]
    [StringLength(100)]
    public string? FileType { get; set; }
    
    [Column("FileContent")]
    public byte[]? FileContent { get; set; }
    
    // inventory properties used by UI but not stored in database
    [NotMapped]
    public int Quantity { get; set; } = 1;
    [NotMapped]
    public int Available { get; set; } = 1;
    
    [Column("UserId")]
    public Guid UserId { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
