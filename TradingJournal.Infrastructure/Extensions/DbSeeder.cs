using Microsoft.EntityFrameworkCore;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;
using TradingJournal.Infrastructure.Data;

namespace TradingJournal.Infrastructure.Extensions;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        // ── Prop Firm Presets (always runs — idempotent) ───────────────────────
        // Must be BEFORE the user guard so existing DBs get presets after migration
        await SeedPropFirmPresetsAsync(db);

        // ── Demo users + trades (only on fresh DB) ─────────────────────────────
        if (await db.Users.AnyAsync()) return;

        // Seed Admin user
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var admin = new User
        {
            Id = adminId,
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@tradingjournal.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            AccountBalance = 100000,
            IsActive = true
        };

        var user = new User
        {
            Id = userId,
            FirstName = "Mitesh",
            LastName = "Trader",
            Email = "trader@tradingjournal.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Trader@123"),
            Role = UserRole.User,
            AccountBalance = 10000,
            IsActive = true
        };

        db.Users.AddRange(admin, user);

        // Instruments
        var goldId = Guid.NewGuid();
        var btcId = Guid.NewGuid();
        var usdJpyId = Guid.NewGuid();
        var eurusdId = Guid.NewGuid();
        var sp500Id = Guid.NewGuid();

        var instruments = new List<Instrument>
        {
            new() { Id = goldId, UserId = userId, Name = "GOLD", Symbol = "XAUUSD", SafeLotSize = 0.1m, MaxLot = 1m, VolatilityLevel = VolatilityLevel.High, Description = "Gold vs US Dollar", Notes = "Trade during London/NY sessions only" },
            new() { Id = btcId, UserId = userId, Name = "BITCOIN", Symbol = "BTCUSD", SafeLotSize = 0.01m, MaxLot = 0.5m, VolatilityLevel = VolatilityLevel.High, Description = "Bitcoin vs US Dollar", Notes = "High volatility crypto asset" },
            new() { Id = usdJpyId, UserId = userId, Name = "USDJPY", Symbol = "USDJPY", SafeLotSize = 0.5m, MaxLot = 5m, VolatilityLevel = VolatilityLevel.Medium, Description = "US Dollar vs Japanese Yen", Notes = "Watch BOJ announcements" },
            new() { Id = eurusdId, UserId = userId, Name = "EURUSD", Symbol = "EURUSD", SafeLotSize = 1m, MaxLot = 10m, VolatilityLevel = VolatilityLevel.Low, Description = "Euro vs US Dollar", Notes = "Most liquid forex pair" },
            new() { Id = sp500Id, UserId = userId, Name = "S&P500", Symbol = "SPX", SafeLotSize = 0.1m, MaxLot = 2m, VolatilityLevel = VolatilityLevel.Medium, Description = "S&P 500 Index", Notes = "Trade during US market hours" }
        };

        db.Instruments.AddRange(instruments);

        // Seed 25 trades over last 2 months
        var trades = new List<Trade>();
        var rng = new Random(42);
        var baseDate = DateTime.UtcNow.AddDays(-60);
        var instrumentList = new[] { goldId, btcId, usdJpyId, eurusdId, sp500Id };
        var tradeTypes = new[] { TradeType.Buy, TradeType.Sell };

        for (int i = 0; i < 25; i++)
        {
            var instrId = instrumentList[rng.Next(instrumentList.Length)];
            var tType = tradeTypes[rng.Next(2)];
            var entry = 1800m + (decimal)(rng.NextDouble() * 200);
            var sl = tType == TradeType.Buy ? entry - (decimal)(rng.NextDouble() * 20 + 5) : entry + (decimal)(rng.NextDouble() * 20 + 5);
            var tp = tType == TradeType.Buy ? entry + (decimal)(rng.NextDouble() * 40 + 10) : entry - (decimal)(rng.NextDouble() * 40 + 10);
            var exit = rng.Next(0, 3) > 0 ? tp : sl;
            var lot = 0.1m + (decimal)(rng.NextDouble() * 0.4);
            var pl = tType == TradeType.Buy ? (exit - entry) * lot * 100 : (entry - exit) * lot * 100;
            var result = pl > 0 ? TradeResult.Win : pl < 0 ? TradeResult.Loss : TradeResult.BreakEven;
            var risk = Math.Abs(entry - sl);
            var reward = Math.Abs(tp - entry);

            trades.Add(new Trade
            {
                UserId = userId,
                InstrumentId = instrId,
                LotSize = Math.Round(lot, 2),
                EntryPrice = Math.Round(entry, 2),
                ExitPrice = Math.Round(exit, 2),
                StopLoss = Math.Round(sl, 2),
                TakeProfit = Math.Round(tp, 2),
                ProfitLoss = Math.Round(pl, 2),
                RiskPercentage = Math.Round((decimal)(rng.NextDouble() * 2 + 0.5), 2),
                RiskRewardRatio = risk > 0 ? Math.Round(reward / risk, 2) : 1,
                TradeDate = baseDate.AddDays(i * 2 + rng.Next(0, 2)).AddHours(rng.Next(8, 18)),
                TradeDurationMinutes = rng.Next(15, 480),
                TradeType = tType,
                Result = result,
                Tags = i % 3 == 0 ? "Breakout" : i % 3 == 1 ? "Scalp" : "Swing",
                Notes = "Sample trade for demonstration"
            });
        }

        db.Trades.AddRange(trades);

        // Notes
        var notes = new List<Note>
        {
            new() { UserId = userId, Title = "Trading Rules", Content = "1. Never risk more than 2% per trade\n2. Always set stop loss\n3. No revenge trading\n4. Trade with the trend\n5. Journal every trade", IsPinned = true, Tags = "Rules,Discipline" },
            new() { UserId = userId, Title = "Gold Trading Strategy", Content = "Entry: Wait for price to test key support/resistance\nConfirmation: RSI divergence + volume spike\nTP: Previous high/low\nSL: 20 pips below entry", Tags = "Gold,Strategy" },
            new() { UserId = userId, Title = "Weekly Review - Week 1", Content = "3 wins, 2 losses this week. Overall profitable. Need to work on patience - entered 2 trades early without proper confirmation.", Tags = "Review" }
        };

        db.Notes.AddRange(notes);

        // Alert
        db.Alerts.Add(new Alert
        {
            UserId = userId,
            DailyLossLimit = 200,
            MaxDrawdownPercent = 10,
            MaxTradesPerDay = 5,
            IsActive = true
        });

        // Trading accounts (regular + a prop firm demo account)
        db.TradingAccounts.AddRange(
            new TradingAccount
            {
                UserId = userId, Name = "Main Account", Balance = 10000,
                Currency = "USD", Broker = "MetaTrader 5", IsDefault = true
            },
            new TradingAccount
            {
                UserId = userId, Name = "Funding Pips $10K", Balance = 10000,
                Currency = "USD", Broker = "Funding Pips", IsDefault = false,
                IsPropFirm = true, PropFirmName = "Funding Pips", PropFirmPlan = "$10,000 — 2 Step",
                DailyDrawdownLimitPct = 4, MaxOverallLossPct = 8, ProfitTargetPct = 8,
                ProfitSplitPct = 80, MinTradingDays = 0, MaxAllowedLotSize = 10,
                Has5xLotRule = true, UseDynamicEquity = true,
                NewsTradeAllowed = true, WeekendHoldingAllowed = true,
                MaxRiskPerTradePctOfDailyLimit = 40
            }
        );

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Idempotent — seeds prop firm presets if the table is empty.
    /// Called independently of the user seeding guard so it runs on existing DBs
    /// after the AddPropFirmPresetAndAccountFields migration.
    /// </summary>
    private static async Task SeedPropFirmPresetsAsync(ApplicationDbContext db)
    {
        // IgnoreQueryFilters() ensures we see soft-deleted rows too — truly idempotent
        if (await db.PropFirmPresets.IgnoreQueryFilters().AnyAsync()) return;

        var presets = new List<PropFirmPreset>
        {
            // ── FUNDING PIPS ────────────────────────────────────────────────────
            new() { FirmName = "Funding Pips", PlanName = "$5,000 — 2 Step",
                AccountSize = 5000, DailyDrawdownLimitPct = 4, MaxOverallLossPct = 8,
                ProfitTargetPct = 8, ProfitSplitPct = 80, MinTradingDays = 0,
                MaxAllowedLotSize = 5, Has5xLotRule = true, UseDynamicEquity = true,
                NewsTradeAllowed = true, WeekendHoldingAllowed = true,
                MaxRiskPerTradePctOfDailyLimit = 40 },

            new() { FirmName = "Funding Pips", PlanName = "$10,000 — 2 Step",
                AccountSize = 10000, DailyDrawdownLimitPct = 4, MaxOverallLossPct = 8,
                ProfitTargetPct = 8, ProfitSplitPct = 80, MinTradingDays = 0,
                MaxAllowedLotSize = 10, Has5xLotRule = true, UseDynamicEquity = true,
                NewsTradeAllowed = true, WeekendHoldingAllowed = true,
                MaxRiskPerTradePctOfDailyLimit = 40 },

            new() { FirmName = "Funding Pips", PlanName = "$25,000 — 2 Step",
                AccountSize = 25000, DailyDrawdownLimitPct = 4, MaxOverallLossPct = 8,
                ProfitTargetPct = 8, ProfitSplitPct = 80, MinTradingDays = 0,
                MaxAllowedLotSize = 25, Has5xLotRule = true, UseDynamicEquity = true,
                NewsTradeAllowed = true, WeekendHoldingAllowed = true,
                MaxRiskPerTradePctOfDailyLimit = 40 },

            new() { FirmName = "Funding Pips", PlanName = "$50,000 — 2 Step",
                AccountSize = 50000, DailyDrawdownLimitPct = 4, MaxOverallLossPct = 8,
                ProfitTargetPct = 8, ProfitSplitPct = 80, MinTradingDays = 0,
                MaxAllowedLotSize = 50, Has5xLotRule = true, UseDynamicEquity = true,
                NewsTradeAllowed = true, WeekendHoldingAllowed = true,
                MaxRiskPerTradePctOfDailyLimit = 40 },

            new() { FirmName = "Funding Pips", PlanName = "$100,000 — 2 Step",
                AccountSize = 100000, DailyDrawdownLimitPct = 4, MaxOverallLossPct = 8,
                ProfitTargetPct = 8, ProfitSplitPct = 80, MinTradingDays = 0,
                MaxAllowedLotSize = 100, Has5xLotRule = true, UseDynamicEquity = true,
                NewsTradeAllowed = true, WeekendHoldingAllowed = true,
                MaxRiskPerTradePctOfDailyLimit = 40 },

            // ── FTMO ────────────────────────────────────────────────────────────
            new() { FirmName = "FTMO", PlanName = "$10,000 — 2 Step",
                AccountSize = 10000, DailyDrawdownLimitPct = 5, MaxOverallLossPct = 10,
                ProfitTargetPct = 10, ProfitSplitPct = 80, MinTradingDays = 4,
                MaxAllowedLotSize = 10, Has5xLotRule = false, UseDynamicEquity = false,
                NewsTradeAllowed = false, WeekendHoldingAllowed = false,
                MaxRiskPerTradePctOfDailyLimit = 50 },

            new() { FirmName = "FTMO", PlanName = "$25,000 — 2 Step",
                AccountSize = 25000, DailyDrawdownLimitPct = 5, MaxOverallLossPct = 10,
                ProfitTargetPct = 10, ProfitSplitPct = 80, MinTradingDays = 4,
                MaxAllowedLotSize = 25, Has5xLotRule = false, UseDynamicEquity = false,
                NewsTradeAllowed = false, WeekendHoldingAllowed = false,
                MaxRiskPerTradePctOfDailyLimit = 50 },

            new() { FirmName = "FTMO", PlanName = "$50,000 — 2 Step",
                AccountSize = 50000, DailyDrawdownLimitPct = 5, MaxOverallLossPct = 10,
                ProfitTargetPct = 10, ProfitSplitPct = 80, MinTradingDays = 4,
                MaxAllowedLotSize = 50, Has5xLotRule = false, UseDynamicEquity = false,
                NewsTradeAllowed = false, WeekendHoldingAllowed = false,
                MaxRiskPerTradePctOfDailyLimit = 50 },

            new() { FirmName = "FTMO", PlanName = "$100,000 — 2 Step",
                AccountSize = 100000, DailyDrawdownLimitPct = 5, MaxOverallLossPct = 10,
                ProfitTargetPct = 10, ProfitSplitPct = 80, MinTradingDays = 4,
                MaxAllowedLotSize = 100, Has5xLotRule = false, UseDynamicEquity = false,
                NewsTradeAllowed = false, WeekendHoldingAllowed = false,
                MaxRiskPerTradePctOfDailyLimit = 50 },

            // ── THE 5ERS ─────────────────────────────────────────────────────────
            new() { FirmName = "The5ers", PlanName = "$6,000 — Hyper Growth",
                AccountSize = 6000, DailyDrawdownLimitPct = 4, MaxOverallLossPct = 6,
                ProfitTargetPct = 6, ProfitSplitPct = 100, MinTradingDays = 3,
                MaxAllowedLotSize = 6, Has5xLotRule = false, UseDynamicEquity = false,
                NewsTradeAllowed = true, WeekendHoldingAllowed = true,
                MaxRiskPerTradePctOfDailyLimit = 50 },

            new() { FirmName = "The5ers", PlanName = "$20,000 — High Stakes",
                AccountSize = 20000, DailyDrawdownLimitPct = 4, MaxOverallLossPct = 6,
                ProfitTargetPct = 6, ProfitSplitPct = 100, MinTradingDays = 3,
                MaxAllowedLotSize = 20, Has5xLotRule = false, UseDynamicEquity = false,
                NewsTradeAllowed = true, WeekendHoldingAllowed = true,
                MaxRiskPerTradePctOfDailyLimit = 50 },

            // ── ALPHA CAPITAL ────────────────────────────────────────────────────
            new() { FirmName = "Alpha Capital", PlanName = "$10,000 — 2 Step",
                AccountSize = 10000, DailyDrawdownLimitPct = 5, MaxOverallLossPct = 10,
                ProfitTargetPct = 8, ProfitSplitPct = 85, MinTradingDays = 5,
                MaxAllowedLotSize = 10, Has5xLotRule = false, UseDynamicEquity = false,
                NewsTradeAllowed = true, WeekendHoldingAllowed = false,
                MaxRiskPerTradePctOfDailyLimit = 50 },

            // ── TOPSTEP ──────────────────────────────────────────────────────────
            new() { FirmName = "TopStep", PlanName = "$50,000 — Standard",
                AccountSize = 50000, DailyDrawdownLimitPct = 4, MaxOverallLossPct = 8,
                ProfitTargetPct = 10, ProfitSplitPct = 90, MinTradingDays = 0,
                MaxAllowedLotSize = 50, Has5xLotRule = false, UseDynamicEquity = false,
                NewsTradeAllowed = false, WeekendHoldingAllowed = false,
                MaxRiskPerTradePctOfDailyLimit = 50 },

            // ── CUSTOM / MANUAL ──────────────────────────────────────────────────
            new() { FirmName = "Custom", PlanName = "Manual Configuration",
                AccountSize = 10000, DailyDrawdownLimitPct = 3, MaxOverallLossPct = 6,
                ProfitTargetPct = 10, ProfitSplitPct = 80, MinTradingDays = 0,
                MaxAllowedLotSize = 5, Has5xLotRule = true, UseDynamicEquity = true,
                NewsTradeAllowed = true, WeekendHoldingAllowed = false,
                MaxRiskPerTradePctOfDailyLimit = 40 },
        };

        db.PropFirmPresets.AddRange(presets);
        await db.SaveChangesAsync();
    }
}
