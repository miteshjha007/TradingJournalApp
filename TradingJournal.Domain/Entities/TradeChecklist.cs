namespace TradingJournal.Domain.Entities;

public class TradeChecklist : BaseEntity
{
    public Guid TradeId { get; set; }
    public Guid RuleId { get; set; }
    public bool IsChecked { get; set; }

    public Trade Trade { get; set; } = null!;
    public PlaybookRule PlaybookRule { get; set; } = null!;
}
