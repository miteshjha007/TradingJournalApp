using TradingJournal.Domain.Enums;

namespace TradingJournal.Domain.Entities;

public class PlaybookRule : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PlaybookCategory Category { get; set; }
    public bool IsActive { get; set; } = true;
    public int OrderIndex { get; set; }

    public User User { get; set; } = null!;
    public ICollection<TradeChecklist> Checklists { get; set; } = new List<TradeChecklist>();
}
