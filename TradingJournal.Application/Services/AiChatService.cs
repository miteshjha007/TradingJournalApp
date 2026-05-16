using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingJournal.Application.DTOs.Ai;
using TradingJournal.Application.Interfaces;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;

namespace TradingJournal.Application.Services;

public class AiChatService : IAiChatService
{
    private readonly IUserAiSettingsRepository _settingsRepo;
    private readonly IAiChatSessionRepository _sessionRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITradeRepository _tradeRepo;
    private readonly IDashboardService _dashboardService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AiChatService> _logger;

    public AiChatService(
        IUserAiSettingsRepository settingsRepo,
        IAiChatSessionRepository sessionRepo,
        IUserRepository userRepo,
        ITradeRepository tradeRepo,
        IDashboardService dashboardService,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<AiChatService> logger)
    {
        _settingsRepo = settingsRepo;
        _sessionRepo = sessionRepo;
        _userRepo = userRepo;
        _tradeRepo = tradeRepo;
        _dashboardService = dashboardService;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<UserAiSettingsDto> GetSettingsAsync(Guid userId)
    {
        var s = await _settingsRepo.GetByUserIdAsync(userId);
        if (s == null) return new UserAiSettingsDto();
        return new UserAiSettingsDto
        {
            Provider = s.Provider,
            ModelName = s.ModelName,
            CustomBaseUrl = s.CustomBaseUrl,
            IsConfigured = s.IsConfigured,
            HasApiKey = !string.IsNullOrEmpty(s.ApiKeyEncrypted)
        };
    }

    public async Task SaveSettingsAsync(Guid userId, SaveAiSettingsDto dto)
    {
        var existing = await _settingsRepo.GetByUserIdAsync(userId);
        var settings = existing ?? new UserAiSettings { UserId = userId };

        settings.Provider = dto.Provider;
        settings.ModelName = dto.ModelName;
        settings.CustomBaseUrl = dto.CustomBaseUrl;

        if (!string.IsNullOrEmpty(dto.ApiKey))
            settings.ApiKeyEncrypted = Encrypt(dto.ApiKey);

        settings.IsConfigured = !string.IsNullOrEmpty(settings.ApiKeyEncrypted);
        settings.UpdatedAt = DateTime.UtcNow;

        await _settingsRepo.UpsertAsync(settings);
        _logger.LogInformation("AI settings saved for user {UserId}", userId);
    }

    public async IAsyncEnumerable<string> SendMessageAsync(Guid userId, SendAiMessageDto dto)
    {
        var settings = await _settingsRepo.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("AI not configured. Please add your API key in settings.");

        if (!settings.IsConfigured)
            throw new InvalidOperationException("AI not configured. Please add your API key in settings.");

        var apiKey = Decrypt(settings.ApiKeyEncrypted!);

        // Load or create session
        AiChatSession session;
        if (dto.SessionId.HasValue)
        {
            session = await _sessionRepo.GetByIdAsync(dto.SessionId.Value, userId)
                ?? throw new KeyNotFoundException("Chat session not found.");
        }
        else
        {
            session = new AiChatSession { UserId = userId, Title = TruncateTitle(dto.Message) };
            session = await _sessionRepo.CreateAsync(session);
        }

        var messages = DeserializeMessages(session.MessagesJson);

        // Build context
        var user = await _userRepo.GetByIdAsync(userId);
        var context = await BuildTradingContextAsync(userId);
        var systemPrompt = $"You are a personal trading coach for {user?.FirstName}. " +
            $"Here is their trading data:\n{context}\n" +
            "Provide specific, actionable advice based ONLY on their actual data. Be direct and honest.";

        messages.Add(new AiChatMessage { Role = "user", Content = dto.Message, Timestamp = DateTime.UtcNow });

        var fullResponse = new StringBuilder();

        await foreach (var token in StreamFromProviderAsync(settings, apiKey, systemPrompt, messages))
        {
            fullResponse.Append(token);
            yield return token;
        }

        messages.Add(new AiChatMessage { Role = "assistant", Content = fullResponse.ToString(), Timestamp = DateTime.UtcNow });
        session.MessagesJson = SerializeMessages(messages);
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepo.UpdateAsync(session);

        _logger.LogInformation("AI message sent for user {UserId} in session {SessionId}", userId, session.Id);
    }

    public async Task<List<AiChatSessionDto>> GetSessionsAsync(Guid userId)
    {
        var sessions = await _sessionRepo.GetByUserIdAsync(userId);
        return sessions.Select(MapSessionToDto).ToList();
    }

    public async Task<AiChatSessionDto?> GetSessionAsync(Guid sessionId, Guid userId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId, userId);
        return session == null ? null : MapSessionToDto(session);
    }

    public async Task DeleteSessionAsync(Guid sessionId, Guid userId)
    {
        await _sessionRepo.DeleteAsync(sessionId, userId);
        _logger.LogInformation("AI session {SessionId} deleted for user {UserId}", sessionId, userId);
    }

    private async IAsyncEnumerable<string> StreamFromProviderAsync(
        UserAiSettings settings, string apiKey, string systemPrompt,
        List<AiChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(3);

        var msgs = messages.Select(m => new { role = m.Role, content = m.Content }).ToList();

        HttpRequestMessage request;

        if (settings.Provider == AiProvider.Anthropic)
        {
            var baseUrl = string.IsNullOrEmpty(settings.CustomBaseUrl)
                ? "https://api.anthropic.com"
                : settings.CustomBaseUrl;

            var body = new
            {
                model = settings.ModelName ?? "claude-sonnet-4-6",
                max_tokens = 2048,
                system = systemPrompt,
                messages = msgs.Where(m => m.role != "system").ToList(),
                stream = true
            };

            request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(body);
        }
        else if (settings.Provider == AiProvider.Gemini)
        {
            var model = settings.ModelName;
            if (string.IsNullOrEmpty(model)) model = "gemini-1.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent?alt=sse&key={apiKey}";

            // Gemini requires strictly alternating user/model roles.
            // Merge consecutive same-role messages to avoid 400 Bad Request.
            var geminiContents = new List<object>();
            foreach (var m in msgs)
            {
                var geminiRole = m.role == "assistant" ? "model" : "user";
                if (geminiContents.Count > 0)
                {
                    // Check if last entry has the same role — if so, merge content
                    var last = (dynamic)geminiContents[^1];
                    string lastRole = last.role;
                    if (lastRole == geminiRole)
                    {
                        // Replace last entry with merged content
                        var lastText = ((object[])last.parts)[0];
                        var lastContent = ((dynamic)lastText).text + "\n" + m.content;
                        geminiContents[^1] = new
                        {
                            role = geminiRole,
                            parts = new[] { new { text = lastContent } }
                        };
                        continue;
                    }
                }
                geminiContents.Add(new
                {
                    role = geminiRole,
                    parts = new[] { new { text = m.content } }
                });
            }

            // Gemini requires the conversation to start with a user turn
            if (geminiContents.Count > 0 && ((dynamic)geminiContents[0]).role != "user")
                geminiContents.RemoveAt(0);

            var bodyObj = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = geminiContents
            };

            // Log the request body for debugging
            var debugJson = JsonSerializer.Serialize(bodyObj, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogDebug("Gemini request body:\n{Body}", debugJson);
            Console.WriteLine($"[Gemini DEBUG] Request URL: {url}");
            Console.WriteLine($"[Gemini DEBUG] Request body:\n{debugJson}");

            request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = JsonContent.Create(bodyObj);
        }
        else
        {
            // OpenAI / DeepSeek / Custom (all OpenAI-compatible)
            string chatUrl;
            if (settings.Provider == AiProvider.Custom && !string.IsNullOrEmpty(settings.CustomBaseUrl))
            {
                // Custom base URLs (e.g. https://api.groq.com/openai/v1) already contain /v1,
                // so only append /chat/completions to avoid doubling the path.
                var trimmed = settings.CustomBaseUrl.TrimEnd('/');
                chatUrl = $"{trimmed}/chat/completions";
            }
            else
            {
                var baseUrl = settings.Provider == AiProvider.DeepSeek
                    ? "https://api.deepseek.com"
                    : "https://api.openai.com";
                chatUrl = $"{baseUrl}/v1/chat/completions";
            }

            var body = new
            {
                model = settings.ModelName ?? "gpt-4o",
                messages = new object[] { new { role = "system", content = systemPrompt } }
                    .Concat(msgs.Select(m => new { role = m.role, content = m.content })).ToArray(),
                stream = true
            };

            _logger.LogDebug("Custom/OpenAI-compatible request URL: {Url}", chatUrl);
            request = new HttpRequestMessage(HttpMethod.Post, chatUrl);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = JsonContent.Create(body);
        }

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "AI API Error — Provider: {Provider}, Model: {Model}, Status: {Status}, Body:\n{ErrorBody}",
                settings.Provider, settings.ModelName, response.StatusCode, errorBody);
            Console.WriteLine($"[AI ERROR] Provider={settings.Provider} Model={settings.ModelName} Status={response.StatusCode}");
            Console.WriteLine($"[AI ERROR] Full response body:\n{errorBody}");
            throw new InvalidOperationException($"AI API Error ({response.StatusCode}): {errorBody}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;

            string? token = null;

            if (settings.Provider == AiProvider.Anthropic)
            {
                if (line.StartsWith("data: "))
                {
                    var json = line[6..];
                    if (json == "[DONE]") break;
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("text", out var text))
                            token = text.GetString();
                    }
                    catch { /* ignore parse errors on partial chunks */ }
                }
            }
            else if (settings.Provider == AiProvider.Gemini)
            {
                try
                {
                    if (line.StartsWith("data: "))
                    {
                        var json = line[6..];
                        if (json == "[DONE]") break;
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("candidates", out var cands) && cands.GetArrayLength() > 0)
                        {
                            var contentProp = cands[0].GetProperty("content");
                            if (contentProp.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                            {
                                if (parts[0].TryGetProperty("text", out var t))
                                    token = t.GetString();
                            }
                        }
                    }
                }
                catch { }
            }
            else
            {
                if (line.StartsWith("data: "))
                {
                    var json = line[6..];
                    if (json == "[DONE]") break;
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var delta = choices[0].GetProperty("delta");
                            if (delta.TryGetProperty("content", out var content))
                                token = content.GetString();
                        }
                    }
                    catch { }
                }
            }

            if (!string.IsNullOrEmpty(token))
                yield return token;
        }
    }

    private async Task<string> BuildTradingContextAsync(Guid userId)
    {
        var allTrades = await _tradeRepo.GetByUserIdAsync(userId);
        var last50 = allTrades.TakeLast(50).ToList();
        var total = allTrades.Count;
        var wins = allTrades.Count(t => t.Result == Domain.Enums.TradeResult.Win);
        var totalPL = allTrades.Sum(t => t.ProfitLoss);
        var winRate = total > 0 ? Math.Round((decimal)wins / total * 100, 1) : 0;

        var sb = new StringBuilder();
        sb.AppendLine($"Total trades: {total}, Win rate: {winRate}%, Total P/L: ${totalPL:F2}");
        sb.AppendLine($"Last 50 trades summary:");
        foreach (var t in last50.TakeLast(10))
            sb.AppendLine($"  {t.TradeDate:yyyy-MM-dd} {t.Instrument?.Name} {t.TradeType} P/L:{t.ProfitLoss:F2} RRR:{t.RiskRewardRatio:F2}");

        return sb.ToString();
    }

    private string Encrypt(string plainText)
    {
        var key = GetEncryptionKey();
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var result = new byte[aes.IV.Length + encBytes.Length];
        aes.IV.CopyTo(result, 0);
        encBytes.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    private string Decrypt(string cipherText)
    {
        var key = GetEncryptionKey();
        var allBytes = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = key;
        var iv = allBytes[..16];
        var enc = allBytes[16..];
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(enc, 0, enc.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private byte[] GetEncryptionKey()
    {
        var keyStr = _config["AiEncryption:MasterKey"]
            ?? throw new InvalidOperationException("AiEncryption:MasterKey not configured.");
        return SHA256.HashData(Encoding.UTF8.GetBytes(keyStr));
    }

    private static string TruncateTitle(string msg) =>
        msg.Length > 50 ? msg[..50] + "..." : msg;

    private static List<AiChatMessage> DeserializeMessages(string json)
    {
        try { return JsonSerializer.Deserialize<List<AiChatMessage>>(json) ?? new(); }
        catch { return new(); }
    }

    private static string SerializeMessages(List<AiChatMessage> messages) =>
        JsonSerializer.Serialize(messages);

    private static AiChatSessionDto MapSessionToDto(AiChatSession s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        Messages = DeserializeMessages(s.MessagesJson).Select(m => new AiChatMessageDto
        {
            Role = m.Role,
            Content = m.Content,
            Timestamp = m.Timestamp
        }).ToList(),
        CreatedAt = s.CreatedAt
    };
    // ──────────────────────────────────────────────────────────────
    // Natural Language Strategy Analyzer
    // ──────────────────────────────────────────────────────────────

    public async Task<ExtractedStrategyFilters> ExtractFiltersAsync(Guid userId, StrategyQueryDto query)
    {
        var settings = await _settingsRepo.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("Please configure your AI provider in AI Chat settings first.");
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Please configure your AI provider in AI Chat settings first.");

        var apiKey = Decrypt(settings.ApiKeyEncrypted!);

        // Extract instrument names from user's existing trades
        var allTrades = await _tradeRepo.GetByUserIdAsync(userId);
        var instrumentNames = allTrades
            .Where(t => t.Instrument?.Name != null)
            .Select(t => t.Instrument!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var instrumentList = instrumentNames.Count > 0
            ? string.Join(", ", instrumentNames)
            : "No instruments found";

        var systemPrompt =
            "You are a trading data filter extractor. Extract structured filters from the user's natural language " +
            "query about their trading strategy. You must respond with ONLY valid JSON — no explanation, no markdown, " +
            "no backticks, just the raw JSON object.\n\n" +
            $"The user's available instruments are: {instrumentList}\n\n" +
            "Session definitions:\n" +
            "- london = hours 7-16 UTC\n" +
            "- newyork = hours 13-22 UTC\n" +
            "- asia = hours 0-9 UTC\n" +
            "- overlap = hours 13-16 UTC (London/NY overlap — most volatile)\n\n" +
            "Day of week: 0=Monday, 1=Tuesday, 2=Wednesday, 3=Thursday, 4=Friday\n\n" +
            "Respond with a JSON object containing only the fields that are relevant to the query. " +
            "For fields not mentioned, use null. Here is the exact schema:\n" +
            "{\n" +
            "  \"InstrumentName\": null or string (must match exactly one of the available instruments),\n" +
            "  \"FromHour\": null or int (0-23 UTC),\n" +
            "  \"ToHour\": null or int (0-23 UTC),\n" +
            "  \"DayOfWeek\": null or int (0-4),\n" +
            "  \"MinRRR\": null or decimal,\n" +
            "  \"MaxRRR\": null or decimal,\n" +
            "  \"MinLotSize\": null or decimal,\n" +
            "  \"MaxLotSize\": null or decimal,\n" +
            "  \"MinRiskPercent\": null or decimal,\n" +
            "  \"MaxRiskPercent\": null or decimal,\n" +
            "  \"Result\": null or \"Win\" or \"Loss\" or \"BreakEven\",\n" +
            "  \"TradeType\": null or \"Buy\" or \"Sell\",\n" +
            "  \"MinChecklistCompliance\": null or decimal (0-100),\n" +
            "  \"MinDurationMinutes\": null or int,\n" +
            "  \"MaxDurationMinutes\": null or int,\n" +
            "  \"Session\": null or \"london\" or \"newyork\" or \"asia\" or \"overlap\",\n" +
            "  \"FilterSummary\": string (human-readable summary, e.g. \"GOLD trades during London session with RRR above 1.5\")\n" +
            "}";

        var messages = new List<AiChatMessage>
        {
            new() { Role = "user", Content = query.UserMessage, Timestamp = DateTime.UtcNow }
        };

        var sb = new StringBuilder();
        try
        {
            await foreach (var token in StreamFromProviderAsync(settings, apiKey, systemPrompt, messages))
                sb.Append(token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Strategy filter extraction failed for user {UserId}", userId);
            return new ExtractedStrategyFilters
            {
                FilterSummary = "Could not parse filters — please rephrase your query"
            };
        }

        // Strip markdown fences if present
        var raw = sb.ToString().Trim();
        if (raw.StartsWith("```")) raw = raw[(raw.IndexOf('\n') + 1)..];
        if (raw.EndsWith("```")) raw = raw[..raw.LastIndexOf("```")].TrimEnd();
        raw = raw.Trim();

        ExtractedStrategyFilters filters;
        try
        {
            var opts = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };
            filters = System.Text.Json.JsonSerializer.Deserialize<ExtractedStrategyFilters>(raw, opts)
                      ?? new ExtractedStrategyFilters { FilterSummary = "Could not parse filters — please rephrase your query" };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JSON parse failed for strategy filters. Raw: {Raw}", raw);
            return new ExtractedStrategyFilters
            {
                FilterSummary = "Could not parse filters — please rephrase your query"
            };
        }

        // Validate instrument name
        if (!string.IsNullOrEmpty(filters.InstrumentName) &&
            !instrumentNames.Any(n => n.Equals(filters.InstrumentName, StringComparison.OrdinalIgnoreCase)))
        {
            filters.InstrumentName = null;
        }

        // Apply session → hour overrides
        if (!string.IsNullOrEmpty(filters.Session))
        {
            switch (filters.Session.ToLower())
            {
                case "london":  filters.FromHour = 7;  filters.ToHour = 16; break;
                case "newyork": filters.FromHour = 13; filters.ToHour = 22; break;
                case "asia":    filters.FromHour = 0;  filters.ToHour = 9;  break;
                case "overlap": filters.FromHour = 13; filters.ToHour = 16; break;
            }
        }

        if (string.IsNullOrEmpty(filters.FilterSummary))
            filters.FilterSummary = query.UserMessage;

        _logger.LogInformation("Strategy filters extracted for user {UserId}: {Summary}", userId, filters.FilterSummary);
        return filters;
    }

    public async Task<StrategyAnalysisResult> AnalyzeStrategyAsync(Guid userId, StrategyQueryDto query)
    {
        ExtractedStrategyFilters filters;
        try
        {
            filters = await ExtractFiltersAsync(userId, query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExtractFilters failed for user {UserId}", userId);
            return new StrategyAnalysisResult
            {
                HasData = false,
                Filters = new ExtractedStrategyFilters { FilterSummary = ex.Message }
            };
        }

        var cutoff = DateTime.UtcNow.AddDays(-query.DaysBack);
        var allTrades = await _tradeRepo.GetByUserIdAsync(userId);
        var periodTrades = allTrades.Where(t => t.TradeDate >= cutoff).ToList();

        // Apply filters
        var matched = periodTrades.AsEnumerable();

        if (!string.IsNullOrEmpty(filters.InstrumentName))
            matched = matched.Where(t => t.Instrument?.Name?.Equals(filters.InstrumentName, StringComparison.OrdinalIgnoreCase) == true);

        if (filters.FromHour.HasValue)
            matched = matched.Where(t => t.TradeDate.Hour >= filters.FromHour.Value);

        if (filters.ToHour.HasValue)
            matched = matched.Where(t => t.TradeDate.Hour <= filters.ToHour.Value);

        if (filters.DayOfWeek.HasValue)
        {
            // Spec: 0=Mon,1=Tue,...,4=Fri — map to C# DayOfWeek (Monday=1, Tuesday=2, ...)
            var csharpDow = filters.DayOfWeek.Value + 1;
            matched = matched.Where(t => (int)t.TradeDate.DayOfWeek == csharpDow);
        }

        if (filters.MinRRR.HasValue)
            matched = matched.Where(t => t.RiskRewardRatio >= filters.MinRRR.Value);

        if (filters.MaxRRR.HasValue)
            matched = matched.Where(t => t.RiskRewardRatio <= filters.MaxRRR.Value);

        if (filters.MinLotSize.HasValue)
            matched = matched.Where(t => t.LotSize >= filters.MinLotSize.Value);

        if (filters.MaxLotSize.HasValue)
            matched = matched.Where(t => t.LotSize <= filters.MaxLotSize.Value);

        if (!string.IsNullOrEmpty(filters.Result))
            matched = matched.Where(t => t.Result.ToString().Equals(filters.Result, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(filters.TradeType))
            matched = matched.Where(t => t.TradeType.ToString().Equals(filters.TradeType, StringComparison.OrdinalIgnoreCase));

        if (filters.MinChecklistCompliance.HasValue)
            matched = matched.Where(t => t.ChecklistCompliancePercent.HasValue &&
                                         t.ChecklistCompliancePercent.Value >= filters.MinChecklistCompliance.Value);

        if (filters.MinDurationMinutes.HasValue)
            matched = matched.Where(t => t.TradeDurationMinutes >= filters.MinDurationMinutes.Value);

        if (filters.MaxDurationMinutes.HasValue)
            matched = matched.Where(t => t.TradeDurationMinutes <= filters.MaxDurationMinutes.Value);

        if (filters.FromDate.HasValue)
            matched = matched.Where(t => t.TradeDate >= filters.FromDate.Value);

        if (filters.ToDate.HasValue)
            matched = matched.Where(t => t.TradeDate <= filters.ToDate.Value);

        var matchedList = matched.OrderBy(t => t.TradeDate).ToList();

        if (matchedList.Count == 0)
        {
            _logger.LogInformation("Strategy analysis for user {UserId}: 0 trades matched. Filters: {Summary}", userId, filters.FilterSummary);
            return new StrategyAnalysisResult
            {
                Filters = filters,
                HasData = false,
                MatchedTrades = 0,
                TotalTradesInPeriod = periodTrades.Count
            };
        }

        // Compute statistics
        var wins = matchedList.Where(t => t.Result == Domain.Enums.TradeResult.Win).ToList();
        var losses = matchedList.Where(t => t.Result == Domain.Enums.TradeResult.Loss).ToList();

        var totalPL = matchedList.Sum(t => t.ProfitLoss);
        var winRate = (decimal)wins.Count / matchedList.Count * 100;
        var avgRRR = matchedList.Any() ? matchedList.Average(t => t.RiskRewardRatio) : 0;
        var avgPL = totalPL / matchedList.Count;
        var maxWin = wins.Any() ? wins.Max(t => t.ProfitLoss) : 0;
        var maxLoss = losses.Any() ? losses.Min(t => t.ProfitLoss) : 0;
        var totalWins = wins.Sum(t => t.ProfitLoss);
        var totalLosses = Math.Abs(losses.Sum(t => t.ProfitLoss));
        var profitFactor = totalLosses > 0 ? totalWins / totalLosses : (totalWins > 0 ? 999 : 0);

        // Simplified Sharpe: group by date → daily PL → mean/stddev * sqrt(252)
        var dailyPL = matchedList
            .GroupBy(t => t.TradeDate.Date)
            .Select(g => (double)g.Sum(t => t.ProfitLoss))
            .ToList();

        var sharpe = 0m;
        if (dailyPL.Count > 1)
        {
            var mean = dailyPL.Average();
            var variance = dailyPL.Select(d => Math.Pow(d - mean, 2)).Average();
            var stdDev = Math.Sqrt(variance);
            if (stdDev > 0)
                sharpe = (decimal)(mean / stdDev * Math.Sqrt(252));
        }

        // Best instrument by total PL
        var bestInstrument = matchedList
            .GroupBy(t => t.Instrument?.Name ?? "Unknown")
            .OrderByDescending(g => g.Sum(t => t.ProfitLoss))
            .FirstOrDefault()?.Key;

        var avgLot = matchedList.Average(t => t.LotSize);
        var avgDuration = matchedList.Average(t => (decimal)t.TradeDurationMinutes);

        // Trade preview (first 10, show 3 in UI)
        var preview = matchedList.Take(10).Select(t => new StrategyTradePreview
        {
            TradeDate = t.TradeDate,
            InstrumentName = t.Instrument?.Name ?? "—",
            TradeType = t.TradeType.ToString(),
            LotSize = t.LotSize,
            ProfitLoss = t.ProfitLoss,
            RiskRewardRatio = t.RiskRewardRatio,
            Result = t.Result.ToString()
        }).ToList();

        _logger.LogInformation("Strategy analysis for user {UserId}: {Count} trades matched. Filters: {Summary}",
            userId, matchedList.Count, filters.FilterSummary);

        return new StrategyAnalysisResult
        {
            Filters = filters,
            MatchedTrades = matchedList.Count,
            TotalTradesInPeriod = periodTrades.Count,
            WinRate = Math.Round(winRate, 1),
            TotalPL = Math.Round(totalPL, 2),
            AverageRRR = Math.Round(avgRRR, 2),
            AveragePL = Math.Round(avgPL, 2),
            MaxWin = Math.Round(maxWin, 2),
            MaxLoss = Math.Round(maxLoss, 2),
            ProfitFactor = Math.Round(profitFactor, 2),
            SharpeRatio = Math.Round(sharpe, 2),
            WinCount = wins.Count,
            LossCount = losses.Count,
            AverageLotSize = Math.Round(avgLot, 2),
            AverageDurationMinutes = Math.Round(avgDuration, 1),
            BestInstrument = bestInstrument,
            HasData = true,
            TradePreview = preview
        };
    }

    public async IAsyncEnumerable<string> StreamStrategyInsightAsync(
        Guid userId, StrategyAnalysisResult result, string originalQuestion,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var settings = await _settingsRepo.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("Please configure your AI provider in AI Chat settings first.");
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Please configure your AI provider in AI Chat settings first.");

        var apiKey = Decrypt(settings.ApiKeyEncrypted!);

        var systemPrompt =
            "You are an expert trading coach analyzing a trader's historical results. " +
            "You are given statistical results from their trade data. Provide a clear, honest, specific analysis. " +
            "Use concrete numbers from the data. Be direct about weaknesses. Mention what the trader is doing well. " +
            "End with 2-3 specific, actionable recommendations. Keep it under 200 words. " +
            "Write in a conversational but professional tone — like a trading mentor, not a textbook.";

        var statsBlock = new StringBuilder();
        statsBlock.AppendLine($"Original question: {originalQuestion}");
        statsBlock.AppendLine($"Filter applied: {result.Filters.FilterSummary}");
        statsBlock.AppendLine();
        statsBlock.AppendLine($"Trades analyzed: {result.MatchedTrades} of {result.TotalTradesInPeriod} in period");
        statsBlock.AppendLine($"Win Rate: {result.WinRate}%");
        statsBlock.AppendLine($"Total P&L: ${result.TotalPL}");
        statsBlock.AppendLine($"Average P&L per trade: ${result.AveragePL}");
        statsBlock.AppendLine($"Average RRR: {result.AverageRRR}");
        statsBlock.AppendLine($"Profit Factor: {result.ProfitFactor}");
        statsBlock.AppendLine($"Sharpe Ratio: {result.SharpeRatio}");
        statsBlock.AppendLine($"Max Win: ${result.MaxWin} | Max Loss: ${result.MaxLoss}");
        statsBlock.AppendLine($"Win/Loss split: {result.WinCount}W / {result.LossCount}L");
        statsBlock.AppendLine($"Best Instrument: {result.BestInstrument ?? "N/A"}");
        statsBlock.AppendLine($"Average Lot Size: {result.AverageLotSize}");
        statsBlock.AppendLine($"Average Trade Duration: {result.AverageDurationMinutes} minutes");

        var messages = new List<AiChatMessage>
        {
            new() { Role = "user", Content = statsBlock.ToString(), Timestamp = DateTime.UtcNow }
        };

        await foreach (var token in StreamFromProviderAsync(settings, apiKey, systemPrompt, messages, ct))
            yield return token;
    }
}

public class AiChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
