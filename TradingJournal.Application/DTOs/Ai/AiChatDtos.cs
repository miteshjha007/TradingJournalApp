using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.DTOs.Ai;

public class UserAiSettingsDto
{
    public AiProvider Provider { get; set; }
    public string? ModelName { get; set; }
    public string? CustomBaseUrl { get; set; }
    public bool IsConfigured { get; set; }
    public bool HasApiKey { get; set; }
}

public class SaveAiSettingsDto
{
    public AiProvider Provider { get; set; }
    public string? ApiKey { get; set; }
    public string? ModelName { get; set; }
    public string? CustomBaseUrl { get; set; }
}

public class AiChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class AiChatSessionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<AiChatMessageDto> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class SendAiMessageDto
{
    public Guid? SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
}
