namespace LibraryManagement.Domain.Entities;

public class Member
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime MembershipDate { get; set; }
    public DateTime? MembershipExpiryDate { get; set; }
    public bool IsActive { get; set; }
    
    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
    
    public string FullName => $"{FirstName} {LastName}";
}
