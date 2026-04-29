using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.DTOs.Forum;

public class CreateAnnouncementDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Info;
}
