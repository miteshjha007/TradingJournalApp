using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.DTOs.Forum;

public class AnnouncementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementPriority Priority { get; set; }
    public Guid AdminId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
