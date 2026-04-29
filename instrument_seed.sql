-- =====================================================================
-- Instrument Seed Data — TradingJournalApp
-- Safe lots calibrated for $5,000 FundedFirm account (1% risk = $50)
-- Pip value formula: XAUUSD $1/pip/0.01lot, Forex $0.10/pip/0.01lot
-- Run this against TradingJournalDb in PostgreSQL
-- =====================================================================

DO $$
DECLARE
    uid UUID;
    now_ts TIMESTAMPTZ := NOW();
BEGIN
    FOR uid IN SELECT "Id" FROM "Users" WHERE "IsDeleted" = false LOOP

        -- =====================================
        -- METALS
        -- =====================================

        -- XAUUSD: pip=$1/pip per 0.01 lot. $50 risk / (15 pips * $1) = 0.03 lot safe
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'Gold (XAUUSD)', 'XAUUSD',
               'Gold vs US Dollar — most popular metal on MT5',
               'Pip=$1 per 0.01 lot. For $5k account: safe 0.03 lot with 15-pip SL = $45 risk. Max drawdown $150/day.',
               0.03, 0.15, 2, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'XAUUSD' AND "IsDeleted" = false
        );

        -- XAGUSD: pip=$0.50/pip per 0.01 lot. $50 / (20 pips * $0.50) = 0.05 safe
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'Silver (XAGUSD)', 'XAGUSD',
               'Silver vs US Dollar',
               'Pip=$0.50 per 0.01 lot. Safe lot 0.05 with 20-pip SL. High volatility — use tight SL.',
               0.05, 0.25, 2, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'XAGUSD' AND "IsDeleted" = false
        );

        -- =====================================
        -- FOREX — Major Pairs
        -- =====================================

        -- EURUSD: pip=$0.10/pip per 0.01 lot. $50 / (20 pips * $0.10) = 0.25 lots. Conservative: 0.05
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'EUR/USD', 'EURUSD',
               'Euro vs US Dollar — highest liquidity Forex pair',
               'Pip=$0.10 per 0.01 lot. Safe 0.05 lot with 20-pip SL = $10 risk per pip. Low spread.',
               0.05, 0.50, 1, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'EURUSD' AND "IsDeleted" = false
        );

        -- GBPUSD: high volatility, use smaller lot
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'GBP/USD', 'GBPUSD',
               'British Pound vs US Dollar — volatile major pair',
               'Pip=$0.10 per 0.01 lot. Use wider SL (20-25 pips) due to volatility. Safe 0.04 lot.',
               0.04, 0.40, 2, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'GBPUSD' AND "IsDeleted" = false
        );

        -- USDJPY: pip value slightly different (~$0.09 per 0.01 lot at 150 rate)
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'USD/JPY', 'USDJPY',
               'US Dollar vs Japanese Yen — classic safe-haven pair',
               'Pip≈$0.09 per 0.01 lot (at 150 rate). Safe 0.05 lot with 15-pip SL ≈ $6.75 loss.',
               0.05, 0.50, 1, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'USDJPY' AND "IsDeleted" = false
        );

        -- AUDUSD
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'AUD/USD', 'AUDUSD',
               'Australian Dollar vs US Dollar — commodity currency',
               'Pip=$0.10 per 0.01 lot. Follows gold and commodity prices. Safe 0.05 lot.',
               0.05, 0.50, 1, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'AUDUSD' AND "IsDeleted" = false
        );

        -- USDCAD
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'USD/CAD', 'USDCAD',
               'US Dollar vs Canadian Dollar — oil correlated pair',
               'Pip=$0.10 per 0.01 lot. Correlated to crude oil. Safe 0.05 lot with 20-pip SL.',
               0.05, 0.50, 1, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'USDCAD' AND "IsDeleted" = false
        );

        -- USDCHF
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'USD/CHF', 'USDCHF',
               'US Dollar vs Swiss Franc — safe-haven pair',
               'Pip=$0.10 per 0.01 lot. Low volatility. Great for tight SL strategies. Safe 0.05 lot.',
               0.05, 0.50, 0, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'USDCHF' AND "IsDeleted" = false
        );

        -- NZDUSD
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'NZD/USD', 'NZDUSD',
               'New Zealand Dollar vs US Dollar',
               'Pip=$0.10 per 0.01 lot. Follows AUD. Low spread on MT5. Safe 0.05 lot.',
               0.05, 0.50, 1, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'NZDUSD' AND "IsDeleted" = false
        );

        -- =====================================
        -- FOREX — Cross Pairs
        -- =====================================

        -- GBPJPY: very volatile cross — use tight lot
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'GBP/JPY', 'GBPJPY',
               'British Pound vs Japanese Yen — highly volatile cross',
               'Pip≈$0.09/pip per 0.01 lot. Very volatile — use SL of 25+ pips. Safe 0.03 lot.',
               0.03, 0.30, 2, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'GBPJPY' AND "IsDeleted" = false
        );

        -- EURJPY
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'EUR/JPY', 'EURJPY',
               'Euro vs Japanese Yen',
               'Pip≈$0.09 per 0.01 lot. Moderate volatility cross pair. Safe 0.04 lot.',
               0.04, 0.40, 2, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'EURJPY' AND "IsDeleted" = false
        );

        -- EURGBP: very low volatility
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'EUR/GBP', 'EURGBP',
               'Euro vs British Pound — low range cross pair',
               'Pip=$0.10 per 0.01 lot. Very tight range usually. Great for scalping. Safe 0.05 lot.',
               0.05, 0.50, 0, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'EURGBP' AND "IsDeleted" = false
        );

        -- =====================================
        -- CRYPTO
        -- =====================================

        -- BTCUSD: 1 point = $1. $50 risk / (500 pts SL * $0.001/0.01lot) = use 0.001 safe
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'Bitcoin (BTC/USD)', 'BTCUSD',
               'Bitcoin vs US Dollar — leading cryptocurrency',
               'High risk. 1 point movement = $1 per lot. Use SL 500+ pts. Safe 0.001 lot for $5k account.',
               0.001, 0.005, 2, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'BTCUSD' AND "IsDeleted" = false
        );

        -- ETHUSD
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'Ethereum (ETH/USD)', 'ETHUSD',
               'Ethereum vs US Dollar',
               'High risk. Use SL 100+ pts. Safe 0.01 lot for conservative risk on $5k account.',
               0.01, 0.05, 2, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'ETHUSD' AND "IsDeleted" = false
        );

        -- USDMXN: exotic — high spread
        INSERT INTO "Instruments" ("Id","UserId","Name","Symbol","Description","Notes","SafeLotSize","MaxLot","VolatilityLevel","CreatedAt","UpdatedAt","IsDeleted")
        SELECT gen_random_uuid(), uid, 'USD/MXN', 'USDMXN',
               'US Dollar vs Mexican Peso — exotic pair',
               'High spread. Pip=$0.01 per 0.01 lot approx. Avoid during news. Safe 0.03 lot.',
               0.03, 0.20, 2, now_ts, now_ts, false
        WHERE NOT EXISTS (
            SELECT 1 FROM "Instruments" WHERE "UserId" = uid AND "Symbol" = 'USDMXN' AND "IsDeleted" = false
        );

    END LOOP;

    RAISE NOTICE 'Instrument seed complete for all users.';
END;
$$;

-- Verify
SELECT u."Email", i."Symbol", i."Name", i."SafeLotSize", i."MaxLot",
       CASE i."VolatilityLevel" WHEN 0 THEN 'Low' WHEN 1 THEN 'Medium' WHEN 2 THEN 'High' END AS "Volatility"
FROM "Instruments" i
JOIN "Users" u ON u."Id" = i."UserId"
WHERE i."IsDeleted" = false
ORDER BY u."Email", i."Symbol";
