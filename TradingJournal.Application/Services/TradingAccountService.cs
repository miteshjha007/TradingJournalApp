
using TradingJournal.Application.DTOs.Accounts;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;

namespace TradingJournal.Application.Services;

public class TradingAccountService : ITradingAccountService
{
    private readonly ITradingAccountRepository _accountRepository;

    public TradingAccountService(ITradingAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<List<TradingAccountDto>> GetUserAccountsAsync(Guid userId)
    {
        var accounts = await _accountRepository.GetByUserIdAsync(userId);
        return accounts.Select(MapToDto).ToList();
    }

    public async Task<TradingAccountDto?> GetAccountByIdAsync(Guid id, Guid userId)
    {
        var account = await _accountRepository.GetByIdAsync(id, userId);
        return account == null ? null : MapToDto(account);
    }

    public async Task<TradingAccountDto> CreateAccountAsync(CreateTradingAccountDto dto, Guid userId)
    {
        var account = new TradingAccount
        {
            UserId = userId,
            Name = dto.Name,
            Balance = dto.Balance,
            Currency = dto.Currency,
            Broker = dto.Broker,
            IsDefault = dto.IsDefault,
            IsPropFirm = dto.IsPropFirm,
            PropFirmName = dto.PropFirmName,
            PropFirmPlan = dto.PropFirmPlan,
            MinTradingDays = dto.MinTradingDays,
            NewsTradeAllowed = dto.NewsTradeAllowed,
            WeekendHoldingAllowed = dto.WeekendHoldingAllowed,
            DailyDrawdownLimitPct = dto.DailyDrawdownLimitPct,
            MaxOverallLossPct = dto.MaxOverallLossPct,
            ProfitTargetPct = dto.ProfitTargetPct,
            ProfitSplitPct = dto.ProfitSplitPct,
            MaxRiskPerTradePctOfDailyLimit = dto.MaxRiskPerTradePctOfDailyLimit,
            MaxAllowedLotSize = dto.MaxAllowedLotSize,
            UseDynamicEquity = dto.UseDynamicEquity,
            Has5xLotRule = dto.Has5xLotRule
        };

        var created = await _accountRepository.CreateAsync(account);
        return MapToDto(created);
    }

    public async Task<TradingAccountDto> UpdateAccountAsync(UpdateTradingAccountDto dto, Guid userId)
    {
        var account = await _accountRepository.GetByIdAsync(dto.Id, userId);
        if (account == null)
            throw new Exception("Trading account not found");

        account.Name = dto.Name;
        account.Balance = dto.Balance;
        account.Currency = dto.Currency;
        account.Broker = dto.Broker;
        account.IsDefault = dto.IsDefault;
        account.IsPropFirm = dto.IsPropFirm;
        account.PropFirmName = dto.PropFirmName;
        account.PropFirmPlan = dto.PropFirmPlan;
        account.MinTradingDays = dto.MinTradingDays;
        account.NewsTradeAllowed = dto.NewsTradeAllowed;
        account.WeekendHoldingAllowed = dto.WeekendHoldingAllowed;
        account.DailyDrawdownLimitPct = dto.DailyDrawdownLimitPct;
        account.MaxOverallLossPct = dto.MaxOverallLossPct;
        account.ProfitTargetPct = dto.ProfitTargetPct;
        account.ProfitSplitPct = dto.ProfitSplitPct;
        account.MaxRiskPerTradePctOfDailyLimit = dto.MaxRiskPerTradePctOfDailyLimit;
        account.MaxAllowedLotSize = dto.MaxAllowedLotSize;
        account.UseDynamicEquity = dto.UseDynamicEquity;
        account.Has5xLotRule = dto.Has5xLotRule;

        var updated = await _accountRepository.UpdateAsync(account);
        return MapToDto(updated);
    }

    public async Task DeleteAccountAsync(Guid id, Guid userId)
    {
        await _accountRepository.DeleteAsync(id, userId);
    }

    public async Task<TradingAccountDto?> GetDefaultAccountAsync(Guid userId)
    {
        var account = await _accountRepository.GetDefaultByUserIdAsync(userId);
        return account == null ? null : MapToDto(account);
    }

    private static TradingAccountDto MapToDto(TradingAccount entity)
    {
        return new TradingAccountDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            Balance = entity.Balance,
            Currency = entity.Currency,
            Broker = entity.Broker,
            IsDefault = entity.IsDefault,
            IsPropFirm = entity.IsPropFirm,
            PropFirmName = entity.PropFirmName,
            PropFirmPlan = entity.PropFirmPlan,
            MinTradingDays = entity.MinTradingDays,
            NewsTradeAllowed = entity.NewsTradeAllowed,
            WeekendHoldingAllowed = entity.WeekendHoldingAllowed,
            DailyDrawdownLimitPct = entity.DailyDrawdownLimitPct,
            MaxOverallLossPct = entity.MaxOverallLossPct,
            ProfitTargetPct = entity.ProfitTargetPct,
            ProfitSplitPct = entity.ProfitSplitPct,
            MaxRiskPerTradePctOfDailyLimit = entity.MaxRiskPerTradePctOfDailyLimit,
            MaxAllowedLotSize = entity.MaxAllowedLotSize,
            UseDynamicEquity = entity.UseDynamicEquity,
            Has5xLotRule = entity.Has5xLotRule,
            CreatedAt = entity.CreatedAt
        };
    }
}
