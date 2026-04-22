using TradingJournal.Application.DTOs.Instruments;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Services;

public class InstrumentService : IInstrumentService
{
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly ITradeRepository _tradeRepository;

    public InstrumentService(IInstrumentRepository instrumentRepository, ITradeRepository tradeRepository)
    {
        _instrumentRepository = instrumentRepository;
        _tradeRepository = tradeRepository;
    }

    public async Task<List<InstrumentDto>> GetAllAsync(Guid userId)
    {
        var instruments = await _instrumentRepository.GetByUserIdAsync(userId);
        var trades = await _tradeRepository.GetByUserIdAsync(userId);
        return instruments.Select(i => MapToDto(i, trades.Where(t => t.InstrumentId == i.Id).ToList())).ToList();
    }

    public async Task<InstrumentDto?> GetByIdAsync(Guid id, Guid userId)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, userId);
        if (instrument == null) return null;
        var trades = await _tradeRepository.GetByUserIdAsync(userId);
        return MapToDto(instrument, trades.Where(t => t.InstrumentId == instrument.Id).ToList());
    }

    public async Task<InstrumentDto> CreateAsync(CreateInstrumentDto dto, Guid userId)
    {
        var instrument = new Instrument
        {
            UserId = userId,
            Name = dto.Name,
            SafeLotSize = dto.SafeLotSize,
            MaxLot = dto.MaxLot,
            VolatilityLevel = dto.VolatilityLevel,
            Notes = dto.Notes,
            Description = dto.Description,
            Symbol = dto.Symbol
        };

        await _instrumentRepository.CreateAsync(instrument);
        return MapToDto(instrument, new List<Trade>());
    }

    public async Task<InstrumentDto> UpdateAsync(Guid id, UpdateInstrumentDto dto, Guid userId)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, userId)
            ?? throw new KeyNotFoundException("Instrument not found.");

        instrument.Name = dto.Name;
        instrument.SafeLotSize = dto.SafeLotSize;
        instrument.MaxLot = dto.MaxLot;
        instrument.VolatilityLevel = dto.VolatilityLevel;
        instrument.Notes = dto.Notes;
        instrument.Description = dto.Description;
        instrument.Symbol = dto.Symbol;
        instrument.UpdatedAt = DateTime.UtcNow;

        await _instrumentRepository.UpdateAsync(instrument);
        return MapToDto(instrument, new List<Trade>());
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        await _instrumentRepository.DeleteAsync(id, userId);
    }

    private InstrumentDto MapToDto(Instrument instrument, List<Trade> trades)
    {
        var wins = trades.Count(t => t.Result == Domain.Enums.TradeResult.Win);
        var total = trades.Count;
        return new InstrumentDto
        {
            Id = instrument.Id,
            Name = instrument.Name,
            SafeLotSize = instrument.SafeLotSize,
            MaxLot = instrument.MaxLot,
            VolatilityLevel = instrument.VolatilityLevel.ToString(),
            Notes = instrument.Notes,
            Description = instrument.Description,
            Symbol = instrument.Symbol,
            CreatedAt = instrument.CreatedAt,
            TotalTrades = total,
            TotalPL = trades.Sum(t => t.ProfitLoss),
            WinRate = total > 0 ? Math.Round((decimal)wins / total * 100, 2) : 0
        };
    }
}
