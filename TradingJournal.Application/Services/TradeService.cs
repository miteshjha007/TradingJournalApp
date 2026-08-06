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
        var symbolStr = instrument?.Symbol ?? instrument?.Name;
        var pl = CalculatePL(dto.TradeType, dto.EntryPrice, dto.ExitPrice, dto.LotSize, symbolStr);
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

        var instrument = await _instrumentRepository.GetByIdAsync(trade.InstrumentId, userId);
        var symbolStr = instrument?.Symbol ?? instrument?.Name ?? trade.Instrument?.Symbol ?? trade.Instrument?.Name;
        var pl = CalculatePL(dto.TradeType, dto.EntryPrice, dto.ExitPrice, dto.LotSize, symbolStr);
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

    private static string NormalizeSymbol(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var s = raw.Trim();
        var upper = s.ToUpper();
        
        // Quick 6-char forex symbol (e.g. AUDCHF, AUDNZD, USDJPY, EURUSD, GBPCAD)
        var cleanNoPunct = upper.Replace("/", "").Replace("-", "").Replace(" ", "").Trim();
        if (cleanNoPunct.Length == 6 && !cleanNoPunct.Contains("GOLD"))
        {
            return cleanNoPunct;
        }

        // Full instrument names parsing (e.g. "Australian Dollar / Swiss Franc")
        string baseC = "", quoteC = "";
        var lower = s.ToLower();

        if (lower.Contains("gold") || lower.Contains("xau")) return "XAUUSD";

        // Base currency matching
        if (lower.Contains("australian dollar") || lower.StartsWith("aud")) baseC = "AUD";
        else if (lower.Contains("euro") || lower.StartsWith("eur")) baseC = "EUR";
        else if (lower.Contains("british pound") || lower.StartsWith("gbp")) baseC = "GBP";
        else if (lower.Contains("new zealand dollar") || lower.StartsWith("nzd")) baseC = "NZD";
        else if (lower.Contains("us dollar") || lower.StartsWith("usd")) baseC = "USD";
        else if (lower.Contains("canadian dollar") || lower.StartsWith("cad")) baseC = "CAD";
        else if (lower.Contains("swiss franc") || lower.StartsWith("chf")) baseC = "CHF";
        else if (lower.Contains("japanese yen") || lower.StartsWith("jpy")) baseC = "JPY";

        // Quote currency matching (after slash or in second half of name)
        var slashIdx = s.IndexOf('/');
        var searchQuoteIn = slashIdx >= 0 ? s.Substring(slashIdx + 1).ToLower() : lower;

        if (searchQuoteIn.Contains("swiss franc") || searchQuoteIn.EndsWith("chf")) quoteC = "CHF";
        else if (searchQuoteIn.Contains("new zealand dollar") || searchQuoteIn.EndsWith("nzd")) quoteC = "NZD";
        else if (searchQuoteIn.Contains("us dollar") || searchQuoteIn.EndsWith("usd")) quoteC = "USD";
        else if (searchQuoteIn.Contains("canadian dollar") || searchQuoteIn.EndsWith("cad")) quoteC = "CAD";
        else if (searchQuoteIn.Contains("japanese yen") || searchQuoteIn.EndsWith("jpy")) quoteC = "JPY";
        else if (searchQuoteIn.Contains("australian dollar") || searchQuoteIn.EndsWith("aud")) quoteC = "AUD";
        else if (searchQuoteIn.Contains("british pound") || searchQuoteIn.EndsWith("gbp")) quoteC = "GBP";

        if (!string.IsNullOrEmpty(baseC) && !string.IsNullOrEmpty(quoteC))
        {
            return baseC + quoteC;
        }

        return cleanNoPunct;
    }

    private decimal CalculatePL(TradeType type, decimal entry, decimal exit, decimal lotSize, string? instrumentSymbol)
    {
        var pips = type == TradeType.Buy ? exit - entry : entry - exit;
        
        decimal multiplier = 100000; // Standard forex lot multiplier
        var symbol = NormalizeSymbol(instrumentSymbol ?? string.Empty);

        if (!string.IsNullOrEmpty(symbol))
        {
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

        var rawPL = pips * lotSize * multiplier;

        // Convert raw P&L (in Quote Currency) to Account Currency (USD)
        if (!string.IsNullOrEmpty(symbol) && symbol.Length >= 6)
        {
            var baseCurr = symbol.Substring(0, 3);
            var quoteCurr = symbol.Substring(3, 3);

            if (baseCurr == "USD" && quoteCurr != "USD")
            {
                // Base is USD, Quote is non-USD (e.g., USDJPY, USDCAD, USDCHF)
                var rate = exit != 0 ? exit : entry;
                if (rate != 0)
                {
                    rawPL /= rate;
                }
            }
            else if (quoteCurr != "USD")
            {
                // Cross Pairs where Quote Currency is non-USD (e.g. AUDNZD, AUDCHF, GBPCAD, GBPJPY)
                switch (quoteCurr)
                {
                    case "NZD":
                        rawPL *= 0.587m; // 1 NZD = ~0.587 USD (NZDUSD rate)
                        break;
                    case "CHF":
                        rawPL /= 0.89m;  // 1 USD = ~0.89 CHF (USDCHF rate)
                        break;
                    case "CAD":
                        rawPL /= 1.37m;  // 1 USD = ~1.37 CAD (USDCAD rate)
                        break;
                    case "JPY":
                        rawPL /= 157.0m; // 1 USD = ~157 JPY (USDJPY rate)
                        break;
                    case "AUD":
                        rawPL *= 0.65m;  // 1 AUD = ~0.65 USD (AUDUSD rate)
                        break;
                    case "GBP":
                        rawPL *= 1.28m;  // 1 GBP = ~1.28 USD (GBPUSD rate)
                        break;
                    case "EUR":
                        rawPL *= 1.09m;  // 1 EUR = ~1.09 USD (EURUSD rate)
                        break;
                }
            }
        }

        return Math.Round(rawPL, 2);
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
