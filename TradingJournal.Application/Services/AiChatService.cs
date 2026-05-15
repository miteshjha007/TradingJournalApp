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
}

public class AiChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
