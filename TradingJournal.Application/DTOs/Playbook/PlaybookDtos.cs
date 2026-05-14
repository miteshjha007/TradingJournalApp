using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.DTOs.Playbook;

public class PlaybookRuleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PlaybookCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePlaybookRuleDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PlaybookCategory Category { get; set; }
    public int OrderIndex { get; set; }
}

public class UpdatePlaybookRuleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PlaybookCategory Category { get; set; }
    public bool IsActive { get; set; }
    public int OrderIndex { get; set; }
}

public class TradeChecklistDto
{
    public Guid RuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public PlaybookCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsChecked { get; set; }
}

public class SaveChecklistDto
{
    public Guid TradeId { get; set; }
    public List<Guid> CheckedRuleIds { get; set; } = new();
}
