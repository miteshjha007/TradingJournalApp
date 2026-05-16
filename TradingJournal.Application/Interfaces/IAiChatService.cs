using TradingJournal.Application.DTOs.Ai;

namespace TradingJournal.Application.Interfaces;

public interface IAiChatService
{
    Task<UserAiSettingsDto> GetSettingsAsync(Guid userId);
    Task SaveSettingsAsync(Guid userId, SaveAiSettingsDto dto);
    IAsyncEnumerable<string> SendMessageAsync(Guid userId, SendAiMessageDto dto);
    Task<List<AiChatSessionDto>> GetSessionsAsync(Guid userId);
    Task<AiChatSessionDto?> GetSessionAsync(Guid sessionId, Guid userId);
    Task DeleteSessionAsync(Guid sessionId, Guid userId);

    // Natural Language Strategy Analyzer
    Task<ExtractedStrategyFilters> ExtractFiltersAsync(Guid userId, StrategyQueryDto query);
    Task<StrategyAnalysisResult> AnalyzeStrategyAsync(Guid userId, StrategyQueryDto query);
    IAsyncEnumerable<string> StreamStrategyInsightAsync(Guid userId, StrategyAnalysisResult result, string originalQuestion);
}

public interface IUserAiSettingsRepository
{
    Task<Domain.Entities.UserAiSettings?> GetByUserIdAsync(Guid userId);
    Task<Domain.Entities.UserAiSettings> UpsertAsync(Domain.Entities.UserAiSettings settings);
}

public interface IAiChatSessionRepository
{
    Task<List<Domain.Entities.AiChatSession>> GetByUserIdAsync(Guid userId);
    Task<Domain.Entities.AiChatSession?> GetByIdAsync(Guid id, Guid userId);
    Task<Domain.Entities.AiChatSession> CreateAsync(Domain.Entities.AiChatSession session);
    Task<Domain.Entities.AiChatSession> UpdateAsync(Domain.Entities.AiChatSession session);
    Task DeleteAsync(Guid id, Guid userId);
}
