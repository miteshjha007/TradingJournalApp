using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using TradingJournal.Application.Interfaces;

namespace TradingJournal.Infrastructure.Services;

public class LocalStorageService : IStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IWebHostEnvironment env, ILogger<LocalStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string> SaveChartAsync(Stream imageStream, string contentType, Guid userId, Guid tradeId)
    {
        var dir = Path.Combine(_env.WebRootPath, "charts", userId.ToString());
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, $"{tradeId}.webp");

        using var image = await Image.LoadAsync(imageStream);

        if (image.Width > 1920)
            image.Mutate(x => x.Resize(1920, 0));

        var encoder = new WebpEncoder { Quality = 85 };
        await image.SaveAsync(filePath, encoder);

        _logger.LogInformation("Chart saved for trade {TradeId} by user {UserId}", tradeId, userId);
        return $"/charts/{userId}/{tradeId}.webp";
    }

    public Task DeleteChartAsync(Guid userId, Guid tradeId)
    {
        var filePath = Path.Combine(_env.WebRootPath, "charts", userId.ToString(), $"{tradeId}.webp");
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }

    public bool ChartExists(Guid userId, Guid tradeId)
    {
        var filePath = Path.Combine(_env.WebRootPath, "charts", userId.ToString(), $"{tradeId}.webp");
        return File.Exists(filePath);
    }

    public string GetChartUrl(Guid userId, Guid tradeId) =>
        $"/charts/{userId}/{tradeId}.webp";
}
