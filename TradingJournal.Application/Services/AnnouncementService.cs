using TradingJournal.Application.DTOs.Forum;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository _repository;

    public AnnouncementService(IAnnouncementRepository repository)
    {
        _repository = repository;
    }

    public async Task<(List<AnnouncementDto> Announcements, int TotalCount)> GetAnnouncementsAsync(int page, int pageSize)
    {
        var entities = await _repository.GetAnnouncementsAsync(page, pageSize);
        var totalCount = await _repository.GetTotalCountAsync();

        var dtos = entities.Select(a => new AnnouncementDto
        {
            Id = a.Id,
            Title = a.Title,
            Content = a.Content,
            Priority = a.Priority,
            AdminId = a.AdminId,
            AdminName = a.Admin != null ? $"{a.Admin.FirstName} {a.Admin.LastName}".Trim() : "Admin",
            CreatedAt = a.CreatedAt
        }).ToList();

        return (dtos, totalCount);
    }

    public async Task<AnnouncementDto> CreateAnnouncementAsync(Guid adminId, CreateAnnouncementDto dto)
    {
        var announcement = new Announcement
        {
            Title = dto.Title,
            Content = dto.Content,
            Priority = dto.Priority,
            AdminId = adminId
        };

        var created = await _repository.CreateAsync(announcement);
        
        // Return dto (without reloading admin user for now, assumed admin)
        return new AnnouncementDto
        {
            Id = created.Id,
            Title = created.Title,
            Content = created.Content,
            Priority = created.Priority,
            AdminId = created.AdminId,
            AdminName = created.Admin != null ? $"{created.Admin.FirstName} {created.Admin.LastName}".Trim() : "System Admin",
            CreatedAt = created.CreatedAt
        };
    }

    public async Task DeleteAnnouncementAsync(Guid id)
    {
        var announcement = await _repository.GetByIdAsync(id);
        if (announcement != null)
        {
            await _repository.DeleteAsync(announcement);
        }
    }
}
