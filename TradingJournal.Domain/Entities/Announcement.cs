using TradingJournal.Domain.Enums;

namespace TradingJournal.Domain.Entities;

public class Announcement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Info;
    
    public Guid AdminId { get; set; }
    public User Admin { get; set; } = null!;
}
