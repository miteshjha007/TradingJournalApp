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

    public TradeService(ITradeRepository tradeRepository, IInstrumentRepository instrumentRepository)
    {
        _tradeRepository = tradeRepository;
        _instrumentRepository = instrumentRepository;
    }

    public async Task<PagedTradesDto> GetAllAsync(Guid userId, TradeFilterDto filter)
    {
        var (trades, total) = await _tradeRepository.GetPagedAsync(
            userId, filter.Page, filter.PageSize,
            filter.FromDate, filter.ToDate,
            filter.InstrumentId, filter.Result, filter.TradeType);

        return new PagedTradesDto
        {
            Trades = trades.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)total / filter.PageSize)
        };
    }

    public async Task<TradeDto?> GetByIdAsync(Guid id, Guid userId)
    {
        var trade = await _tradeRepository.GetByIdAsync(id, userId);
        return trade != null ? MapToDto(trade) : null;
    }

    public async Task<TradeDto> CreateAsync(CreateTradeDto dto, Guid userId)
    {
        var pl = CalculatePL(dto.TradeType, dto.EntryPrice, dto.ExitPrice, dto.LotSize);
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
            TradeDate = dto.TradeDate,
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

        var pl = CalculatePL(dto.TradeType, dto.EntryPrice, dto.ExitPrice, dto.LotSize);
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
        trade.TradeDate = dto.TradeDate;
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
        var from = new DateTime(year, month, 1);
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

    private decimal CalculatePL(TradeType type, decimal entry, decimal exit, decimal lotSize)
    {
        var pips = type == TradeType.Buy ? exit - entry : entry - exit;
        return Math.Round(pips * lotSize * 100000, 2); // Standard forex calculation
    }

    private decimal CalculateRRR(decimal entry, decimal sl, decimal tp, TradeType type)
    {
        var risk = Math.Abs(entry - sl);
        var reward = Math.Abs(tp - entry);
        if (risk == 0) return 0;
        return Math.Round(reward / risk, 2);
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
