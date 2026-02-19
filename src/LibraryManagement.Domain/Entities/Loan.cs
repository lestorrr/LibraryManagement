namespace LibraryManagement.Domain.Entities;

public class Loan
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public Guid MemberId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public decimal? LateFee { get; set; }
    public string Notes { get; set; } = string.Empty;
    
    public virtual Book Book { get; set; } = null!;
    public virtual Member Member { get; set; } = null!;
    
    public bool IsOverdue => !ReturnDate.HasValue && DateTime.UtcNow > DueDate;
    public bool IsReturned => ReturnDate.HasValue;
}
