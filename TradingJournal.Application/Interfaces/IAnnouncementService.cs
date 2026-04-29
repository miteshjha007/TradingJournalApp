using TradingJournal.Application.DTOs.Forum;

namespace TradingJournal.Application.Interfaces;

public interface IAnnouncementService
{
    Task<(List<AnnouncementDto> Announcements, int TotalCount)> GetAnnouncementsAsync(int page, int pageSize);
    Task<AnnouncementDto> CreateAnnouncementAsync(Guid adminId, CreateAnnouncementDto dto);
    Task DeleteAnnouncementAsync(Guid id);
}
