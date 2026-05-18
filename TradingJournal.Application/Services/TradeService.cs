using System.Text;
using TradingJournal.Application.DTOs.Trades;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.Services;

public class TradeService : ITradeService
{
    private readonly ITradeRepository _tradeRepository;
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly ITradingAccountRepository _accountRepository;

    public TradeService(ITradeRepository tradeRepository, IInstrumentRepository instrumentRepository, ITradingAccountRepository accountRepository)
    {
        _tradeRepository = tradeRepository;
        _instrumentRepository = instrumentRepository;
        _accountRepository = accountRepository;
    }

    public async Task<PagedTradesDto> GetAllAsync(Guid userId, TradeFilterDto filter)
    {
        var (trades, total) = await _tradeRepository.GetPagedAsync(
            userId, filter.Page, filter.PageSize,
            filter.FromDate, filter.ToDate,
            filter.InstrumentId, filter.Result, filter.TradeType);

        var accounts = await _accountRepository.GetByUserIdAsync(userId);

        return new PagedTradesDto
        {
            Trades = trades.Select(t => MapToDtoWithViolations(t, accounts)).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)total / filter.PageSize)
        };
    }

    public async Task<TradeDto?> GetByIdAsync(Guid id, Guid userId)
    {
        var trade = await _tradeRepository.GetByIdAsync(id, userId);
        if (trade == null) return null;
        var accounts = await _accountRepository.GetByUserIdAsync(userId);
        return MapToDtoWithViolations(trade, accounts);
    }

    public async Task<TradeDto> CreateAsync(CreateTradeDto dto, Guid userId)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(dto.InstrumentId, userId);
        var pl = CalculatePL(dto.TradeType, dto.EntryPrice, dto.ExitPrice, dto.LotSize, instrument?.Name);
        var result = pl > 0 ? TradeResult.Win : pl < 0 ? TradeResult.Loss : TradeResult.BreakEven;
        var rrr = CalculateRRR(dto.EntryPrice, dto.StopLoss, dto.TakeProfit, dto.TradeType);

        var trade = new Trade
        {
            UserId = userId,
            InstrumentId = dto.InstrumentId,
            LotSize = dto.LotSize,
            EntryPrice = dto.EntryPrice,
            ExitPrice = dto.ExitPrice,
            StopLoss = dto.StopLoss,
            TakeProfit = dto.TakeProfit,
            ProfitLoss = pl,
            RiskPercentage = dto.RiskPercentage,
            RiskRewardRatio = rrr,
            TradeDate = dto.TradeDate.ToUniversalTime(),
            TradeDurationMinutes = dto.TradeDurationMinutes,
            TradeType = dto.TradeType,
            Result = result,
            Notes = dto.Notes,
            Tags = dto.Tags,
            TradingAccountId = dto.TradingAccountId
        };

        await _tradeRepository.CreateAsync(trade);
        return MapToDto(trade);
    }

    public async Task<TradeDto> UpdateAsync(Guid id, UpdateTradeDto dto, Guid userId)
    {
        var trade = await _tradeRepository.GetByIdAsync(id, userId)
            ?? throw new KeyNotFoundException("Trade not found.");

        var pl = CalculatePL(dto.TradeType, dto.EntryPrice, dto.ExitPrice, dto.LotSize, trade.Instrument?.Name);
        var result = pl > 0 ? TradeResult.Win : pl < 0 ? TradeResult.Loss : TradeResult.BreakEven;
        var rrr = CalculateRRR(dto.EntryPrice, dto.StopLoss, dto.TakeProfit, dto.TradeType);

        trade.LotSize = dto.LotSize;
        trade.EntryPrice = dto.EntryPrice;
        trade.ExitPrice = dto.ExitPrice;
        trade.StopLoss = dto.StopLoss;
        trade.TakeProfit = dto.TakeProfit;
        trade.ProfitLoss = pl;
        trade.RiskPercentage = dto.RiskPercentage;
        trade.RiskRewardRatio = rrr;
        trade.TradeDate = dto.TradeDate.ToUniversalTime();
        trade.TradeDurationMinutes = dto.TradeDurationMinutes;
        trade.TradeType = dto.TradeType;
        trade.Result = result;
        trade.Notes = dto.Notes;
        trade.Tags = dto.Tags;
        trade.UpdatedAt = DateTime.UtcNow;

        await _tradeRepository.UpdateAsync(trade);
        return MapToDto(trade);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        await _tradeRepository.DeleteAsync(id, userId);
    }

    public async Task<List<TradeDto>> GetForCalendarAsync(Guid userId, int year, int month)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1).AddDays(-1);
        var trades = await _tradeRepository.GetByDateRangeAsync(userId, from, to);
        return trades.Select(MapToDto).ToList();
    }

    public async Task<byte[]> ExportToCsvAsync(Guid userId, TradeFilterDto filter)
    {
        filter.PageSize = 10000;
        var result = await GetAllAsync(userId, filter);

        var sb = new StringBuilder();
        sb.AppendLine("Date,Instrument,Type,LotSize,Entry,Exit,SL,TP,P/L,RRR,Risk%,Duration(min),Result,Tags,Notes");

        foreach (var t in result.Trades)
        {
            sb.AppendLine($"{t.TradeDate:yyyy-MM-dd},{t.InstrumentName},{t.TradeType},{t.LotSize}," +
                $"{t.EntryPrice},{t.ExitPrice},{t.StopLoss},{t.TakeProfit},{t.ProfitLoss}," +
                $"{t.RiskRewardRatio},{t.RiskPercentage},{t.TradeDurationMinutes}," +
                $"{t.Result},{t.Tags?.Replace(",", ";")},{t.Notes?.Replace(",", ";")}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private decimal CalculatePL(TradeType type, decimal entry, decimal exit, decimal lotSize, string? instrumentSymbol)
    {
        var pips = type == TradeType.Buy ? exit - entry : entry - exit;
        
        decimal multiplier = 100000; // Standard forex

        if (!string.IsNullOrEmpty(instrumentSymbol))
        {
            var symbol = instrumentSymbol.ToUpper();
            if (symbol.Contains("XAU") || symbol.Contains("GOLD"))
                multiplier = 100;
            else if (symbol.Contains("XAG") || symbol.Contains("SILVER"))
                multiplier = 5000;
            else if (symbol.Contains("BTC") || symbol.Contains("BITCOIN"))
                multiplier = 1;
            else if (symbol.Contains("US30") || symbol.Contains("DOW"))
                multiplier = 10;
            else if (symbol.Contains("NAS100") || symbol.Contains("NASDAQ"))
                multiplier = 20; 
        }

        return Math.Round(pips * lotSize * multiplier, 2);
    }

    private decimal CalculateRRR(decimal entry, decimal sl, decimal tp, TradeType type)
    {
        var risk = Math.Abs(entry - sl);
        var reward = Math.Abs(tp - entry);
        if (risk == 0) return 0;
        return Math.Round(reward / risk, 2);
    }

    private TradeDto MapToDtoWithViolations(Trade trade, List<TradingAccount> accounts)
    {
        var dto = MapToDto(trade);
        var account = trade.TradingAccountId.HasValue 
            ? accounts.FirstOrDefault(a => a.Id == trade.TradingAccountId.Value)
            : accounts.FirstOrDefault(a => a.IsDefault);

        if (account != null && account.IsPropFirm)
        {
            if (trade.LotSize > account.MaxAllowedLotSize)
                dto.RuleViolations.Add($"Max Lot Size Exceeded (>{account.MaxAllowedLotSize})");
                
            // Note: A true 40% rule calculation would require historical daily limit tracking.
            // Here we use a simplified warning if they risked more than the absolute % limit of the balance
            var maxRiskDollar = account.Balance * account.DailyDrawdownLimitPct / 100m * account.MaxRiskPerTradePctOfDailyLimit / 100m;
            var tradeRiskDollar = account.Balance * trade.RiskPercentage / 100m;
            
            if (tradeRiskDollar > maxRiskDollar)
                dto.RuleViolations.Add($"Violated {account.MaxRiskPerTradePctOfDailyLimit}% Rule (Risk > ${maxRiskDollar:F2})");
        }

        return dto;
    }

    private TradeDto MapToDto(Trade trade) => new TradeDto
    {
        Id = trade.Id,
        InstrumentId = trade.InstrumentId,
        InstrumentName = trade.Instrument?.Name ?? string.Empty,
        LotSize = trade.LotSize,
        EntryPrice = trade.EntryPrice,
        ExitPrice = trade.ExitPrice,
        StopLoss = trade.StopLoss,
        TakeProfit = trade.TakeProfit,
        ProfitLoss = trade.ProfitLoss,
        RiskPercentage = trade.RiskPercentage,
        RiskRewardRatio = trade.RiskRewardRatio,
        TradeDate = trade.TradeDate,
        TradeDurationMinutes = trade.TradeDurationMinutes,
        TradeType = trade.TradeType.ToString(),
        Result = trade.Result.ToString(),
        Notes = trade.Notes,
        Tags = trade.Tags,
        CreatedAt = trade.CreatedAt
    };
}
