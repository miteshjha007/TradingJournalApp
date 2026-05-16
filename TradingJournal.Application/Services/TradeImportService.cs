using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingJournal.Application.DTOs.Import;
using TradingJournal.Application.DTOs.Trades;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.Services;

public class TradeImportService : ITradeImportService
{
    private readonly ITradeImportRepository _importRepository;
    private readonly ITradeService _tradeService;
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly ITradingAccountRepository _tradingAccountRepository;
    private readonly ILogger<TradeImportService> _logger;
    private readonly IConfiguration _configuration;

    public TradeImportService(
        ITradeImportRepository importRepository,
        ITradeService tradeService,
        IInstrumentRepository instrumentRepository,
        ITradingAccountRepository tradingAccountRepository,
        ILogger<TradeImportService> logger,
        IConfiguration configuration)
    {
        _importRepository = importRepository;
        _tradeService = tradeService;
        _instrumentRepository = instrumentRepository;
        _tradingAccountRepository = tradingAccountRepository;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<CsvImportPreviewDto> ParseCsvAsync(CsvImportRequestDto dto, Guid userId)
    {
        _logger.LogInformation("Parsing CSV for user: {UserId}", userId);

        var preview = new CsvImportPreviewDto();
        var lines = dto.CsvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            preview.Errors.Add(new CsvParseErrorDto { RowNumber = 0, RawLine = "", Error = "Empty file" });
            return preview;
        }

        var format = DetectCsvFormat(lines);
        preview.CsvFormat = format;
        preview.TotalRows = Math.Max(0, lines.Length - 1);

        if (format == "Unknown")
        {
            preview.Errors.Add(new CsvParseErrorDto { RowNumber = 0, RawLine = lines[0], Error = "Unsupported CSV format. Please export from MT5 History tab." });
            return preview;
        }

        var instruments = await _instrumentRepository.GetByUserIdAsync(userId);
        var existingTrades = await _tradeService.GetAllAsync(userId, new TradeFilterDto { Page = 1, PageSize = 10000 });

        var parsedTrades = new List<ParsedTradeDto>();
        var errors = new List<CsvParseErrorDto>();

        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var parsed = ParseLine(line, format, i);
                if (parsed == null)
                {
                    errors.Add(new CsvParseErrorDto { RowNumber = i + 1, RawLine = line, Error = "Could not parse row" });
                    continue;
                }

                var match = MatchInstrument(parsed.Symbol, instruments, dto.ForceInstrumentId);
                parsed.InstrumentId = match.instrumentId;
                parsed.MappedInstrumentName = match.mappedName;

                var isDuplicate = CheckDuplicate(parsed, existingTrades.Trades);
                parsed.IsDuplicate = isDuplicate.isDuplicate;
                parsed.DuplicateReason = isDuplicate.reason;

                parsedTrades.Add(parsed);
            }
            catch (Exception ex)
            {
                errors.Add(new CsvParseErrorDto { RowNumber = i + 1, RawLine = lines[i], Error = ex.Message });
            }
        }

        preview.ValidTrades = parsedTrades.Where(t => !t.IsDuplicate && t.InstrumentId != null).ToList();
        preview.DuplicateTrades = parsedTrades.Where(t => t.IsDuplicate).ToList();
        preview.Errors = errors;

        if (parsedTrades.Any(t => !t.IsDuplicate && t.InstrumentId == null))
        {
            var unmatchedInstruments = parsedTrades.Where(t => !t.IsDuplicate && t.InstrumentId == null).Select(t => t.Symbol).Distinct();
            _logger.LogWarning("CSV parse: {Count} trades have no matching instrument: {Symbols}", unmatchedInstruments.Count(), string.Join(", ", unmatchedInstruments));
        }

        _logger.LogInformation("CSV parsed for user {UserId}: {Valid} valid, {Duplicate} duplicates, {Errors} errors",
            userId, preview.ValidTrades.Count, preview.DuplicateTrades.Count, preview.Errors.Count);

        return preview;
    }

    public async Task<ImportResultDto> ConfirmCsvImportAsync(CsvImportConfirmDto dto, Guid userId)
    {
        _logger.LogInformation("Confirming CSV import for user: {UserId}", userId);

        var preview = await ParseCsvAsync(new CsvImportRequestDto
        {
            CsvContent = dto.CsvContent,
            ForceInstrumentId = dto.ForceInstrumentId
        }, userId);

        var validTrades = preview.ValidTrades.Where(t => t.InstrumentId != null).ToList();
        if (dto.SkipDuplicates)
        {
            validTrades = validTrades.Where(t => !t.IsDuplicate).ToList();
        }

        var inserted = 0;
        var skipped = 0;
        var failed = 0;
        var insertedIds = new List<string>();
        var skippedReasons = new List<string>();
        var errors = new List<string>();

        foreach (var trade in validTrades)
        {
            try
            {
                var createDto = new CreateTradeDto
                {
                    InstrumentId = trade.InstrumentId!.Value,
                    LotSize = trade.LotSize,
                    EntryPrice = trade.EntryPrice,
                    ExitPrice = trade.ExitPrice,
                    StopLoss = trade.StopLoss,
                    TakeProfit = trade.TakeProfit,
                    RiskPercentage = 1.0m,
                    TradeDate = trade.OpenTime.ToUniversalTime(),
                    TradeDurationMinutes = trade.DurationMinutes,
                    TradeType = trade.TradeType.ToLower() == "buy" ? TradeType.Buy : TradeType.Sell,
                    Notes = $"[Imported from CSV] {trade.Comment}".Trim(),
                    Tags = "CSVImport",
                    TradingAccountId = dto.TradingAccountId
                };

                var created = await _tradeService.CreateAsync(createDto, userId);
                insertedIds.Add(created.Id.ToString());
                inserted++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"Trade {trade.Symbol} at {trade.OpenTime}: {ex.Message}");
                _logger.LogError(ex, "Failed to import trade: {Symbol}", trade.Symbol);
            }
        }

        if (dto.SkipDuplicates)
        {
            skipped = preview.DuplicateTrades.Count;
            skippedReasons.AddRange(preview.DuplicateTrades.Select(t => $"{t.Symbol}: {t.DuplicateReason}"));
        }

        var importLog = new TradeImportLog
        {
            UserId = userId,
            Source = ImportSource.CsvUpload,
            TotalReceived = preview.ValidTrades.Count + preview.DuplicateTrades.Count,
            TotalInserted = inserted,
            TotalSkipped = skipped,
            TotalFailed = failed,
            Status = failed == 0 ? ImportStatus.Success : (inserted > 0 ? ImportStatus.PartialSuccess : ImportStatus.Failed),
            InsertedTradeIds = JsonSerializer.Serialize(insertedIds),
            SkippedReasons = JsonSerializer.Serialize(skippedReasons),
            ErrorSummary = errors.Count > 0 ? string.Join("; ", errors) : null
        };

        await _importRepository.CreateLogAsync(importLog);

        var result = new ImportResultDto
        {
            Inserted = inserted,
            Skipped = skipped,
            Failed = failed,
            SkippedReasons = skippedReasons,
            Errors = errors,
            ImportLogId = importLog.Id,
            Summary = $"{inserted} trades imported, {skipped} duplicates skipped, {failed} failed"
        };

        _logger.LogInformation("CSV import completed for user {UserId}: {Summary}", userId, result.Summary);

        return result;
    }

    public async Task<Mt5WebhookConfigDto> GetOrCreateConfigAsync(Guid userId)
    {
        var config = await _importRepository.GetConfigByUserIdAsync(userId);
        var baseUrl = _configuration["App:BaseUrl"] ?? "https://trading-journal-api-mcc2.onrender.com";

        if (config == null)
        {
            config = new Mt5WebhookConfig
            {
                UserId = userId,
                WebhookToken = Guid.NewGuid().ToString("N"),
                IsActive = true
            };
            config = await _importRepository.CreateConfigAsync(config);
            _logger.LogInformation("Created new MT5 webhook config for user: {UserId}", userId);
        }

        return MapToDto(config, baseUrl);
    }

    public async Task<Mt5WebhookConfigDto> UpdateConfigAsync(Guid userId, UpdateMt5ConfigDto dto)
    {
        var config = await _importRepository.GetConfigByUserIdAsync(userId);
        if (config == null)
        {
            throw new InvalidOperationException("MT5 webhook config not found");
        }

        config.IsActive = dto.IsActive;
        config.Description = dto.Description;
        config.DefaultTradingAccountId = dto.DefaultTradingAccountId;
        config.DefaultInstrumentMappings = JsonSerializer.Serialize(dto.InstrumentMappings);

        config = await _importRepository.UpdateConfigAsync(config);

        var baseUrl = _configuration["App:BaseUrl"] ?? "https://trading-journal-api-mcc2.onrender.com";
        return MapToDto(config, baseUrl);
    }

    public async Task<ImportResultDto> ProcessMt5WebhookAsync(Mt5TradePayloadDto payload, string token)
    {
        _logger.LogInformation("Processing MT5 webhook for ticket: {Ticket}", payload.TicketNumber);

        var config = await _importRepository.GetConfigByTokenAsync(token);
        if (config == null || !config.IsActive)
        {
            _logger.LogWarning("MT5 webhook rejected: invalid or inactive token");
            return new ImportResultDto { Summary = "Invalid or inactive webhook token" };
        }

        if (await _importRepository.IsTicketAlreadyImportedAsync(config.UserId, payload.TicketNumber))
        {
            _logger.LogInformation("Duplicate ticket {Ticket} skipped for user {UserId}", payload.TicketNumber, config.UserId);
            return new ImportResultDto { Skipped = 1, Summary = "Duplicate ticket skipped" };
        }

        var instruments = await _instrumentRepository.GetByUserIdAsync(config.UserId);
        var mappings = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(config.DefaultInstrumentMappings))
        {
            mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(config.DefaultInstrumentMappings) ?? new();
        }

        var match = MatchInstrument(payload.Symbol, instruments, null, mappings);
        if (match.instrumentId == null)
        {
            var log = new TradeImportLog
            {
                UserId = config.UserId,
                Source = ImportSource.Mt5Webhook,
                TotalReceived = 1,
                TotalInserted = 0,
                TotalSkipped = 0,
                TotalFailed = 1,
                Status = ImportStatus.Failed,
                ErrorSummary = $"No instrument found for symbol: {payload.Symbol}"
            };
            await _importRepository.CreateLogAsync(log);

            _logger.LogWarning("MT5 webhook: no instrument found for {Symbol}", payload.Symbol);
            return new ImportResultDto { Failed = 1, Summary = $"No instrument found for {payload.Symbol}" };
        }

        var tradeType = payload.OrderType.ToLower() == "buy" ? TradeType.Buy : TradeType.Sell;
        var openTime = DateTime.Parse(payload.OpenTime).ToUniversalTime();
        var closeTime = DateTime.Parse(payload.CloseTime).ToUniversalTime();

        var createDto = new CreateTradeDto
        {
            InstrumentId = match.instrumentId.Value,
            LotSize = payload.Lots,
            EntryPrice = payload.OpenPrice,
            ExitPrice = payload.ClosePrice,
            StopLoss = payload.StopLoss,
            TakeProfit = payload.TakeProfit,
            RiskPercentage = 1.0m,
            TradeDate = openTime,
            TradeDurationMinutes = (int)(closeTime - openTime).TotalMinutes,
            TradeType = tradeType,
            Notes = $"[MT5 #{payload.TicketNumber}] {payload.Comment}".Trim(),
            Tags = "MT5Live",
            TradingAccountId = config.DefaultTradingAccountId
        };

        var created = await _tradeService.CreateAsync(createDto, config.UserId);

        config.LastUsedAt = DateTime.UtcNow;
        config.TotalTradesImported++;
        await _importRepository.UpdateConfigAsync(config);

        var logEntry = new TradeImportLog
        {
            UserId = config.UserId,
            Source = ImportSource.Mt5Webhook,
            TotalReceived = 1,
            TotalInserted = 1,
            TotalSkipped = 0,
            TotalFailed = 0,
            Status = ImportStatus.Success,
            InsertedTradeIds = JsonSerializer.Serialize(new List<string> { created.Id.ToString() })
        };
        await _importRepository.CreateLogAsync(logEntry);

        _logger.LogInformation("MT5 webhook trade inserted: ticket {Ticket} for user {UserId}", payload.TicketNumber, config.UserId);

        return new ImportResultDto
        {
            Inserted = 1,
            Summary = $"Trade imported: {payload.Symbol}"
        };
    }

    public async Task<List<ImportLogDto>> GetImportHistoryAsync(Guid userId, int page, int pageSize)
    {
        var logs = await _importRepository.GetLogsByUserIdAsync(userId, page, pageSize);

        return logs.Select(l => new ImportLogDto
        {
            Id = l.Id,
            Source = l.Source == ImportSource.CsvUpload ? "CSV Upload" : "MT5 Webhook",
            TotalReceived = l.TotalReceived,
            TotalInserted = l.TotalInserted,
            TotalSkipped = l.TotalSkipped,
            TotalFailed = l.TotalFailed,
            FileName = l.FileName,
            Status = l.Status == ImportStatus.Success ? "Success" : (l.Status == ImportStatus.PartialSuccess ? "Partial" : "Failed"),
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    private string DetectCsvFormat(string[] lines)
    {
        if (lines.Length < 2) return "Unknown";

        var header = lines[0].ToLower().Replace(" ", "").Replace("\"", "");

        if (header.Contains("ticket") && header.Contains("opentime") && header.Contains("closetime"))
            return "MT5 Detailed";
        if (header.Contains("time") && header.Contains("symbol") && header.Contains("type") && header.Contains("volume"))
            return "MT5 Summary";
        if (header.Contains("symbol") && header.Contains("type") && header.Contains("profit"))
            return "Generic";

        return "Unknown";
    }

    private ParsedTradeDto? ParseLine(string line, string format, int rowNumber)
    {
        var parts = SplitCsvLine(line);
        if (parts.Length < 5) return null;

        try
        {
            if (format == "MT5 Detailed")
            {
                return ParseMt5Detailed(parts);
            }
            else if (format == "MT5 Summary")
            {
                return ParseMt5Summary(parts);
            }
            else if (format == "Generic")
            {
                return ParseGeneric(parts);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private ParsedTradeDto ParseMt5Detailed(string[] parts)
    {
        var type = parts[2].ToLower().Trim();
        if (type.Contains("balance") || type.Contains("credit") || type.Contains("deposit") || type.Contains("withdrawal"))
            throw new Exception("Skipping non-trade row");

        return new ParsedTradeDto
        {
            Symbol = parts[4].Trim(),
            TradeType = type.Contains("buy") ? "Buy" : "Sell",
            LotSize = ParseDecimal(parts[3]),
            EntryPrice = ParseDecimal(parts[5]),
            StopLoss = ParseDecimal(parts[6]),
            TakeProfit = ParseDecimal(parts[7]),
            ExitPrice = ParseDecimal(parts[9]),
            ProfitLoss = ParseDecimal(parts[10]),
            OpenTime = ParseDateTime(parts[1]),
            CloseTime = ParseDateTime(parts[8]),
            DurationMinutes = (int)(ParseDateTime(parts[8]) - ParseDateTime(parts[1])).TotalMinutes,
            Comment = parts.Length > 11 ? parts[11] : null
        };
    }

    private ParsedTradeDto ParseMt5Summary(string[] parts)
    {
        var type = parts[2].ToLower().Trim();
        if (type.Contains("balance") || type.Contains("credit") || type.Contains("deposit") || type.Contains("withdrawal"))
            throw new Exception("Skipping non-trade row");

        return new ParsedTradeDto
        {
            Symbol = parts[1].Trim(),
            TradeType = type.Contains("buy") ? "Buy" : "Sell",
            LotSize = ParseDecimal(parts[3]),
            EntryPrice = ParseDecimal(parts[4]),
            StopLoss = ParseDecimal(parts[5]),
            TakeProfit = ParseDecimal(parts[6]),
            ProfitLoss = ParseDecimal(parts[7]),
            OpenTime = ParseDateTime(parts[0]),
            CloseTime = ParseDateTime(parts[0]),
            DurationMinutes = 0,
            Comment = null
        };
    }

    private ParsedTradeDto ParseGeneric(string[] parts)
    {
        var type = parts[1].ToLower().Trim();
        if (type.Contains("balance") || type.Contains("credit") || type.Contains("deposit") || type.Contains("withdrawal"))
            throw new Exception("Skipping non-trade row");

        return new ParsedTradeDto
        {
            Symbol = parts[0].Trim(),
            TradeType = type.Contains("buy") ? "Buy" : "Sell",
            LotSize = ParseDecimal(parts[2]),
            EntryPrice = ParseDecimal(parts[3]),
            ExitPrice = parts.Length > 6 ? ParseDecimal(parts[6]) : 0,
            StopLoss = 0,
            TakeProfit = 0,
            ProfitLoss = parts.Length > 7 ? ParseDecimal(parts[7]) : 0,
            OpenTime = DateTime.UtcNow,
            CloseTime = DateTime.UtcNow,
            DurationMinutes = 0,
            Comment = null
        };
    }

    private string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = "";

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current.Trim());

        return result.ToArray();
    }

    private decimal ParseDecimal(string s)
    {
        s = s.Replace("$", "").Replace(",", "").Trim();
        if (decimal.TryParse(s, out var result)) return result;
        return 0;
    }

    private DateTime ParseDateTime(string s)
    {
        s = s.Trim().Replace("\"", "");
        if (DateTime.TryParse(s, out var result)) return result;
        return DateTime.UtcNow;
    }

    private (Guid? instrumentId, string mappedName) MatchInstrument(
        string symbol,
        List<Instrument> instruments,
        Guid? forceInstrumentId,
        Dictionary<string, string>? customMappings = null)
    {
        if (forceInstrumentId.HasValue)
        {
            var forced = instruments.FirstOrDefault(i => i.Id == forceInstrumentId.Value);
            return (forceInstrumentId, forced?.Name ?? "Unknown");
        }

        if (customMappings != null && customMappings.TryGetValue(symbol.ToUpper(), out var mappedName))
        {
            var matched = instruments.FirstOrDefault(i => i.Name.Equals(mappedName, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
                return (matched.Id, matched.Name);
        }

        var upperSymbol = symbol.ToUpper();
        var exactMatch = instruments.FirstOrDefault(i => i.Symbol?.ToUpper() == upperSymbol);
        if (exactMatch != null)
            return (exactMatch.Id, exactMatch.Name);

        var nameMatch = instruments.FirstOrDefault(i => i.Name.Equals(upperSymbol, StringComparison.OrdinalIgnoreCase));
        if (nameMatch != null)
            return (nameMatch.Id, nameMatch.Name);

        var partialMatch = instruments.FirstOrDefault(i => !string.IsNullOrEmpty(i.Symbol) && upperSymbol.Contains(i.Symbol.ToUpper()));
        if (partialMatch != null)
            return (partialMatch.Id, partialMatch.Name);

        var aliasMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "XAUUSD", new[] { "GOLD", "XAU", "XAUUSD" } },
            { "XAGUSD", new[] { "SILVER", "XAG", "XAGUSD" } },
            { "BTCUSD", new[] { "BITCOIN", "BTC", "BTC" } },
            { "US30", new[] { "DOW", "US30", "DJ30" } },
            { "NAS100", new[] { "NASDAQ", "NAS100", "NAS" } },
            { "EURUSD", new[] { "EURUSD", "EUR" } },
            { "GBPUSD", new[] { "GBPUSD", "GBP" } },
            { "USDJPY", new[] { "USDJPY", "JPY" } }
        };

        if (aliasMap.TryGetValue(upperSymbol, out var aliases))
        {
            foreach (var alias in aliases)
            {
                var aliasMatch = instruments.FirstOrDefault(i =>
                    i.Name.Equals(alias, StringComparison.OrdinalIgnoreCase) ||
                    i.Symbol?.Equals(alias, StringComparison.OrdinalIgnoreCase) == true);
                if (aliasMatch != null)
                    return (aliasMatch.Id, aliasMatch.Name);
            }
        }

        return (null, symbol);
    }

    private (bool isDuplicate, string reason) CheckDuplicate(ParsedTradeDto trade, List<TradeDto> existingTrades)
    {
        var duplicates = existingTrades.Where(t =>
            t.TradeDate.Date == trade.OpenTime.Date &&
            Math.Abs(t.EntryPrice - trade.EntryPrice) < 0.001m &&
            Math.Abs(t.LotSize - trade.LotSize) < 0.001m).ToList();

        if (duplicates.Count > 0)
        {
            return (true, $"Trade on {trade.OpenTime:yyyy-MM-dd HH:mm} with same entry price and lot size already exists");
        }

        return (false, null!);
    }

    private Mt5WebhookConfigDto MapToDto(Mt5WebhookConfig config, string baseUrl)
    {
        var mappings = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(config.DefaultInstrumentMappings))
        {
            mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(config.DefaultInstrumentMappings) ?? new();
        }

        string? accountName = null;
        if (config.DefaultTradingAccountId.HasValue)
        {
            var accounts = _tradingAccountRepository.GetByUserIdAsync(config.UserId).Result;
            accountName = accounts.FirstOrDefault(a => a.Id == config.DefaultTradingAccountId.Value)?.Name;
        }

        return new Mt5WebhookConfigDto
        {
            Id = config.Id,
            WebhookToken = config.WebhookToken,
            IsActive = config.IsActive,
            Description = config.Description,
            DefaultTradingAccountId = config.DefaultTradingAccountId,
            DefaultTradingAccountName = accountName,
            InstrumentMappings = mappings,
            LastUsedAt = config.LastUsedAt,
            TotalTradesImported = config.TotalTradesImported,
            WebhookUrl = $"{baseUrl}/api/import/mt5-webhook"
        };
    }
}