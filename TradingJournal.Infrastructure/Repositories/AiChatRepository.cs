using Microsoft.EntityFrameworkCore;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.Infrastructure.Repositories;

public class UserAiSettingsRepository : IUserAiSettingsRepository
{
    private readonly ApplicationDbContext _db;
    public UserAiSettingsRepository(ApplicationDbContext db) => _db = db;

    public async Task<UserAiSettings?> GetByUserIdAsync(Guid userId) =>
        await _db.UserAiSettings.FirstOrDefaultAsync(s => s.UserId == userId);

    public async Task<UserAiSettings> UpsertAsync(UserAiSettings settings)
    {
        var existing = await _db.UserAiSettings.FirstOrDefaultAsync(s => s.UserId == settings.UserId);
        if (existing == null)
        {
            _db.UserAiSettings.Add(settings);
        }
        else
        {
            existing.Provider = settings.Provider;
            existing.ApiKeyEncrypted = settings.ApiKeyEncrypted;
            existing.ModelName = settings.ModelName;
            existing.CustomBaseUrl = settings.CustomBaseUrl;
            existing.IsConfigured = settings.IsConfigured;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return existing ?? settings;
    }
}

public class AiChatSessionRepository : IAiChatSessionRepository
{
    private readonly ApplicationDbContext _db;
    public AiChatSessionRepository(ApplicationDbContext db) => _db = db;

    public async Task<List<AiChatSession>> GetByUserIdAsync(Guid userId) =>
        await _db.AiChatSessions.Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt).ToListAsync();

    public async Task<AiChatSession?> GetByIdAsync(Guid id, Guid userId) =>
        await _db.AiChatSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

    public async Task<AiChatSession> CreateAsync(AiChatSession session)
    {
        _db.AiChatSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task<AiChatSession> UpdateAsync(AiChatSession session)
    {
        session.UpdatedAt = DateTime.UtcNow;
        _db.AiChatSessions.Update(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var session = await _db.AiChatSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (session != null) { session.IsDeleted = true; await _db.SaveChangesAsync(); }
    }
}
