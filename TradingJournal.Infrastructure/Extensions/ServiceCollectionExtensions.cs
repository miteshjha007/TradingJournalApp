using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingJournal.Application.Interfaces;
using TradingJournal.Application.Services;
using TradingJournal.Infrastructure.Data;
using TradingJournal.Infrastructure.Repositories;
using TradingJournal.Infrastructure.Services;

namespace TradingJournal.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInstrumentRepository, InstrumentRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IInstrumentService, InstrumentService>();
        services.AddScoped<ITradeService, TradeService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }

    public static async Task MigrateAndSeedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }
}
