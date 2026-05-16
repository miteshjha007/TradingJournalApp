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
        await SeedStrategyTemplatesAsync(db);
        await SeedPlaybookRulesAsync(db);

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
    private static async Task SeedStrategyTemplatesAsync(ApplicationDbContext db)
    {
        if (await db.StrategyTemplates.IgnoreQueryFilters().AnyAsync()) return;

        var templates = new List<StrategyTemplate>
        {
            new()
            {
                Name = "BOS + Order Block Retest",
                Methodology = "SMC",
                Instrument = "GOLD",
                Description = "A core SMC strategy focused on structural breaks and high-probability retests of institutional supply/demand zones.",
                Rules = new List<string>
                {
                    "Wait for Break of Structure (BOS) on H1 chart",
                    "Mark the last Order Block formed before the BOS",
                    "Wait for price to return and retest the Order Block",
                    "Enter on rejection candle (engulf/pinbar) from OB",
                    "SL below Order Block low, TP at next liquidity pool",
                    "Only trade in direction of H4 or D1 trend"
                },
                DefaultFilters = "{\"InstrumentName\":\"GOLD\",\"FromHour\":7,\"ToHour\":16,\"MinRRR\":2.0,\"FilterSummary\":\"GOLD — London session — RRR above 2.0\"}",
                SessionBadge = "London open",
                TimeframeBadge = "H1 + H4",
                MinRRR = 2.0m,
                IsSystemTemplate = true,
                IsActive = true
            },
            new()
            {
                Name = "FVG Fill + Continuation",
                Methodology = "SMC",
                Instrument = "GOLD",
                Description = "Capitalizing on market imbalances by entering at the 50% equilibrium of a Fair Value Gap.",
                Rules = new List<string>
                {
                    "Identify a Fair Value Gap (imbalance) on H1",
                    "Wait for price to return to the 50% level of the FVG",
                    "Confirm with M15 structure shift in trade direction",
                    "Enter at 50% of FVG, SL beyond full FVG range",
                    "TP at next Point of Interest (POI) or liquidity pool",
                    "Only trade during high-volume sessions (London/NY)"
                },
                DefaultFilters = "{\"InstrumentName\":\"GOLD\",\"FromHour\":13,\"ToHour\":22,\"MinRRR\":2.5,\"FilterSummary\":\"GOLD — New York session — RRR above 2.5\"}",
                SessionBadge = "NY open",
                TimeframeBadge = "H1 + M15",
                MinRRR = 2.5m,
                IsSystemTemplate = true,
                IsActive = true
            },
            new()
            {
                Name = "London Open Sweep",
                Methodology = "Price Action",
                Instrument = "GOLD",
                Description = "A momentum-reversal strategy that takes advantage of Asian session liquidity sweeps during the London open volume surge.",
                Rules = new List<string>
                {
                    "Before 07:00 UTC: mark Asian session high and low",
                    "Wait for price to sweep (break and close beyond) one extreme",
                    "Enter in opposite direction immediately after sweep candle closes",
                    "SL beyond the sweep wick (including spread)",
                    "TP at opposite Asian session extreme",
                    "Only valid between 07:00–09:30 UTC London open window"
                },
                DefaultFilters = "{\"InstrumentName\":\"GOLD\",\"FromHour\":7,\"ToHour\":9,\"MinRRR\":3.0,\"FilterSummary\":\"GOLD — London open 07:00–09:30 UTC — RRR above 3.0\"}",
                SessionBadge = "07:00–09:30 UTC",
                TimeframeBadge = "M15 + H1",
                MinRRR = 3.0m,
                IsSystemTemplate = true,
                IsActive = true
            },
            new()
            {
                Name = "HTF POI Reversal",
                Methodology = "Price Action",
                Instrument = "GOLD",
                Description = "High-timeframe confluence strategy entering at key Weekly or Daily zones with lower-timeframe structural confirmation.",
                Rules = new List<string>
                {
                    "Mark key Weekly or Daily support/resistance levels",
                    "Wait for price to tap the level precisely on H4",
                    "Confirm reversal candle on H1 (engulfing or pin bar)",
                    "Enter after H1 candle closes away from the level",
                    "SL beyond the key level by 10 pips",
                    "TP at 50% retracement or next HTF key level"
                },
                DefaultFilters = "{\"InstrumentName\":\"GOLD\",\"MinRRR\":2.0,\"FilterSummary\":\"GOLD — Any session — RRR above 2.0 — key level entries\"}",
                SessionBadge = "Any session",
                TimeframeBadge = "W1 + D1 + H1",
                MinRRR = 2.0m,
                IsSystemTemplate = true,
                IsActive = true
            }
        };

        db.StrategyTemplates.AddRange(templates);
        await db.SaveChangesAsync();
    }

    private static async Task SeedPlaybookRulesAsync(ApplicationDbContext db)
    {
        var traderEmail = "trader@tradingjournal.com";
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == traderEmail);
        
        if (user == null) return;

        // Idempotency check: Only seed if user has zero rules
        if (await db.PlaybookRules.AnyAsync(r => r.UserId == user.Id)) return;

        var rules = new List<PlaybookRule>
        {
            // ENTRY RULES (Category = 1)
            new() { UserId = user.Id, Category = PlaybookCategory.Entry, OrderIndex = 1, IsActive = true,
                Title = "Is price above 200 EMA on H1?",
                Description = "Only trade in the direction of the 200 EMA on H1 timeframe" },
            new() { UserId = user.Id, Category = PlaybookCategory.Entry, OrderIndex = 2, IsActive = true,
                Title = "Is there a clear Break of Structure (BOS)?",
                Description = "Confirm BOS or CHoCH before entering — no BOS = no trade" },
            new() { UserId = user.Id, Category = PlaybookCategory.Entry, OrderIndex = 3, IsActive = true,
                Title = "Is there an Order Block or FVG at entry zone?",
                Description = "Price must be retesting a valid OB or Fair Value Gap" },
            new() { UserId = user.Id, Category = PlaybookCategory.Entry, OrderIndex = 4, IsActive = true,
                Title = "Is HTF (H4/Daily) trend aligned with entry?",
                Description = "Trade only in direction of higher timeframe trend" },

            // RISK RULES (Category = 2)
            new() { UserId = user.Id, Category = PlaybookCategory.Risk, OrderIndex = 1, IsActive = true,
                Title = "Is Stop Loss set BEFORE entering the trade?",
                Description = "Never enter without a predetermined stop loss level" },
            new() { UserId = user.Id, Category = PlaybookCategory.Risk, OrderIndex = 2, IsActive = true,
                Title = "Is risk below 1% of account balance?",
                Description = "Max risk per trade is 1% — check Risk Tool before entry" },
            new() { UserId = user.Id, Category = PlaybookCategory.Risk, OrderIndex = 3, IsActive = true,
                Title = "Is lot size within instrument Safe Lot Size limit?",
                Description = "Check instrument configuration — never exceed Max Lot" },
            new() { UserId = user.Id, Category = PlaybookCategory.Risk, OrderIndex = 4, IsActive = true,
                Title = "Have I used less than 50% of my daily loss limit today?",
                Description = "Check dashboard prop firm status card before trading" },

            // PSYCHOLOGY RULES (Category = 3)
            new() { UserId = user.Id, Category = PlaybookCategory.Psychology, OrderIndex = 1, IsActive = true,
                Title = "Am I trading out of FOMO or revenge?",
                Description = "If answer is YES — close the app and come back in 30 minutes" },
            new() { UserId = user.Id, Category = PlaybookCategory.Psychology, OrderIndex = 2, IsActive = true,
                Title = "Have I taken more than 3 trades today already?",
                Description = "More than 3 trades = overtrading risk. Review before continuing." },
            new() { UserId = user.Id, Category = PlaybookCategory.Psychology, OrderIndex = 3, IsActive = true,
                Title = "Did I sleep well and am I mentally focused?",
                Description = "Tired or stressed trading leads to poor decisions" },

            // EXIT RULES (Category = 4)
            new() { UserId = user.Id, Category = PlaybookCategory.Exit, OrderIndex = 1, IsActive = true,
                Title = "Is Take Profit at the next key level (S/R or liquidity)?",
                Description = "TP must be at a logical level — not a random number" },
            new() { UserId = user.Id, Category = PlaybookCategory.Exit, OrderIndex = 2, IsActive = true,
                Title = "Will I move SL to break-even when trade is at 1:1?",
                Description = "Protect capital — move SL to entry when profit = risk" },
            new() { UserId = user.Id, Category = PlaybookCategory.Exit, OrderIndex = 3, IsActive = true,
                Title = "Am I planning to let this trade run or will I close manually?",
                Description = "Decide exit plan before entry — stick to it" }
        };

        db.PlaybookRules.AddRange(rules);
        await db.SaveChangesAsync();
    }
}
