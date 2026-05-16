namespace TradingJournal.Application.DTOs.StrategyTemplates;

public class StrategyTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Methodology { get; set; } = string.Empty;
    public string Instrument { get; set; } = string.Empty;
    public List<string> Rules { get; set; } = new();
    public string DefaultFilters { get; set; } = "{}";
    public string? SessionBadge { get; set; }
    public string? TimeframeBadge { get; set; }
    public decimal MinRRR { get; set; }
    public bool IsSystemTemplate { get; set; }
    public bool IsActive { get; set; }
}

public class CreateStrategyTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Methodology { get; set; } = string.Empty;
    public string Instrument { get; set; } = string.Empty;
    public List<string> Rules { get; set; } = new();
    public string DefaultFilters { get; set; } = "{}";
    public string? SessionBadge { get; set; }
    public string? TimeframeBadge { get; set; }
    public decimal MinRRR { get; set; }
}
