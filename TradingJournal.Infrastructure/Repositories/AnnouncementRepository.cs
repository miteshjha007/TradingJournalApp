using Microsoft.EntityFrameworkCore;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.Infrastructure.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly ApplicationDbContext _context;

    public AnnouncementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Announcement>> GetAnnouncementsAsync(int page, int pageSize)
    {
        return await _context.Announcements
            .Include(a => a.Admin)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Announcements.CountAsync();
    }

    public async Task<Announcement> CreateAsync(Announcement announcement)
    {
        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();
        return announcement;
    }

    public async Task<Announcement?> GetByIdAsync(Guid id)
    {
        return await _context.Announcements.FindAsync(id);
    }

    public async Task DeleteAsync(Announcement announcement)
    {
        announcement.IsDeleted = true;
        _context.Announcements.Update(announcement);
        await _context.SaveChangesAsync();
    }
}
