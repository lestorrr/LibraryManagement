namespace LibraryManagement.Domain.Entities;

public class FileEntity
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}