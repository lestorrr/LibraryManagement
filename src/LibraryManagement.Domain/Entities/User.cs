using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Domain.Entities;

[Table("Users")]
public class User
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }
    
    [Column("Username")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Column("Email")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Column("PasswordHash")]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Column("FirstName")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Column("LastName")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Column("Bio")]
    [StringLength(500)]
    public string? Bio { get; set; }
    
    [Column("ProfilePictureUrl")]
    [StringLength(200)]
    public string? ProfilePictureUrl { get; set; }
    
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
    
    [Column("LastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
    
    [Column("IsActive")]
    public bool IsActive { get; set; }
    
    // Navigation properties
    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
