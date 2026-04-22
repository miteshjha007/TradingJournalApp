namespace TradingJournal.Domain.Entities;

public class Note : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public bool IsPinned { get; set; } = false;

    // Navigation
    public User User { get; set; } = null!;
}
