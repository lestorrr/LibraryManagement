using LibraryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Data;

public class LibraryContext : DbContext
{
    public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)
    {
    }

    public DbSet<Book> Books { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<FileEntity> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.PasswordHash)
                .IsRequired();
            
            entity.Property(e => e.FirstName)
                .HasMaxLength(50);
            
            entity.Property(e => e.LastName)
                .HasMaxLength(50);
            
            entity.Property(e => e.Bio)
                .HasMaxLength(500);
            
            entity.Property(e => e.ProfilePictureUrl)
                .HasMaxLength(200);
        });

        // Book configuration with User relationship
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Author).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ISBN).IsRequired().HasMaxLength(13);
            entity.Property(e => e.Publisher).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.ISBN).IsUnique();
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Books)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Member configuration (keep existing)
        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Loan configuration (keep existing)
        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LoanDate).IsRequired();
            entity.Property(e => e.DueDate).IsRequired();
            entity.Property(e => e.LateFee).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Book)
                .WithMany(b => b.Loans)
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Member)
                .WithMany(m => m.Loans)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // File configuration
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public void ClearDuplicateUsers()
    {
        var duplicateUsers = Users
            .GroupBy(u => u.Username)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        Users.RemoveRange(duplicateUsers);
        SaveChanges();
    }
}
