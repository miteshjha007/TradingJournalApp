using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Interfaces;

public interface IAnnouncementRepository
{
    Task<List<Announcement>> GetAnnouncementsAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<Announcement> CreateAsync(Announcement announcement);
    Task<Announcement?> GetByIdAsync(Guid id);
    Task DeleteAsync(Announcement announcement);
}
