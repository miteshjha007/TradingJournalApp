namespace TradingJournal.Application.DTOs.Ai;

public class StrategyQueryDto
{
    /// <summary>The user's plain English strategy question</summary>
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>Number of days to look back (default 30)</summary>
    public int DaysBack { get; set; } = 30;
}

public class ExtractedStrategyFilters
{
    /// <summary>Instrument name to filter by (null = all instruments)</summary>
    public string? InstrumentName { get; set; }

    /// <summary>UTC hour range start (0-23, null = no filter)</summary>
    public int? FromHour { get; set; }

    /// <summary>UTC hour range end (0-23, null = no filter)</summary>
    public int? ToHour { get; set; }

    /// <summary>Day of week: 0=Mon, 1=Tue, 2=Wed, 3=Thu, 4=Fri, null = all</summary>
    public int? DayOfWeek { get; set; }

    public decimal? MinRRR { get; set; }
    public decimal? MaxRRR { get; set; }
    public decimal? MinLotSize { get; set; }
    public decimal? MaxLotSize { get; set; }
    public decimal? MinRiskPercent { get; set; }
    public decimal? MaxRiskPercent { get; set; }

    /// <summary>"Win", "Loss", "BreakEven", null = all</summary>
    public string? Result { get; set; }

    /// <summary>"Buy", "Sell", null = all</summary>
    public string? TradeType { get; set; }

    /// <summary>Minimum checklist compliance %</summary>
    public decimal? MinChecklistCompliance { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public int? MinDurationMinutes { get; set; }
    public int? MaxDurationMinutes { get; set; }

    /// <summary>
    /// Trading session shortcut: "london" | "newyork" | "asia" | "overlap"
    /// Applied as FromHour/ToHour overrides by ExtractFiltersAsync
    /// </summary>
    public string? Session { get; set; }

    /// <summary>Human-readable description of extracted filters</summary>
    public string FilterSummary { get; set; } = string.Empty;
}

public class StrategyAnalysisResult
{
    public ExtractedStrategyFilters Filters { get; set; } = new();

    public int MatchedTrades { get; set; }
    public int TotalTradesInPeriod { get; set; }
    public decimal WinRate { get; set; }
    public decimal TotalPL { get; set; }
    public decimal AverageRRR { get; set; }
    public decimal AveragePL { get; set; }
    public decimal MaxWin { get; set; }
    public decimal MaxLoss { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal SharpeRatio { get; set; }
    public int WinCount { get; set; }
    public int LossCount { get; set; }
    public decimal AverageLotSize { get; set; }
    public decimal AverageDurationMinutes { get; set; }

    public string? BestInstrument { get; set; }
    public string? WorstDay { get; set; }

    public bool HasData { get; set; }

    /// <summary>LLM-generated narrative — populated by StreamStrategyInsightAsync, left empty here</summary>
    public string AiSummary { get; set; } = string.Empty;

    public List<StrategyTradePreview> TradePreview { get; set; } = new();
}

public class StrategyTradePreview
{
    public DateTime TradeDate { get; set; }
    public string InstrumentName { get; set; } = string.Empty;
    public string TradeType { get; set; } = string.Empty;
    public decimal LotSize { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal RiskRewardRatio { get; set; }
    public string Result { get; set; } = string.Empty;
}

public class StrategyStreamDto
{
    public StrategyAnalysisResult Result { get; set; } = new();
    public string OriginalQuestion { get; set; } = string.Empty;
}
