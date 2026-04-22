namespace TradingJournal.Application.DTOs.Analytics;

public class AiInsightDto
{
    public string Category { get; set; } = string.Empty; // Overtrading, Risk, Performance
    public string Severity { get; set; } = string.Empty; // Info, Warning, Critical
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class AiAnalysisDto
{
    public string OverallScore { get; set; } = string.Empty;
    public string BestInstrument { get; set; } = string.Empty;
    public string BestTimeOfDay { get; set; } = string.Empty;
    public string MostCommonMistake { get; set; } = string.Empty;
    public List<AiInsightDto> Insights { get; set; } = new();
}

public class RiskCalculationDto
{
    public decimal AccountBalance { get; set; }
    public decimal RiskPercent { get; set; }
    public Guid? InstrumentId { get; set; }
}

public class RiskResultDto
{
    public decimal RiskAmount { get; set; }
    public decimal SuggestedLotSize { get; set; }
    public decimal MaxAllowedLotSize { get; set; }
    public int MaxTradesPerDay { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
}

public class AlertDto
{
    public Guid Id { get; set; }
    public decimal DailyLossLimit { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public int MaxTradesPerDay { get; set; }
    public bool IsActive { get; set; }
    public bool EmailAlertEnabled { get; set; }
    public string? Email { get; set; }
}

public class CreateAlertDto
{
    public decimal DailyLossLimit { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public int MaxTradesPerDay { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public bool EmailAlertEnabled { get; set; } = false;
    public string? Email { get; set; }
}
