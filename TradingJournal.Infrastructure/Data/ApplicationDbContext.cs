using Microsoft.EntityFrameworkCore;
using TradingJournal.Domain.Entities;
using TradingJournal.Domain.Enums;

namespace TradingJournal.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<TradingAccount> TradingAccounts => Set<TradingAccount>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<ForumMessage> ForumMessages => Set<ForumMessage>();
    public DbSet<PropFirmPreset> PropFirmPresets => Set<PropFirmPreset>();
    public DbSet<PlaybookRule> PlaybookRules => Set<PlaybookRule>();
    public DbSet<TradeChecklist> TradeChecklists => Set<TradeChecklist>();
    public DbSet<UserAiSettings> UserAiSettings => Set<UserAiSettings>();
    public DbSet<AiChatSession> AiChatSessions => Set<AiChatSession>();
    public DbSet<BacktestResult> BacktestResults => Set<BacktestResult>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Global soft-delete filter
        mb.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Instrument>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Trade>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Note>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<TradingAccount>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Announcement>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<ForumMessage>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<PlaybookRule>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<UserAiSettings>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<AiChatSession>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<BacktestResult>().HasQueryFilter(e => !e.IsDeleted);

        // User
        mb.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.AccountBalance).HasColumnType("decimal(18,2)");
        });

        // Instrument
        mb.Entity<Instrument>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.SafeLotSize).HasColumnType("decimal(18,4)");
            e.Property(x => x.MaxLot).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.User).WithMany(u => u.Instruments).HasForeignKey(x => x.UserId);
        });

        // Trade
        mb.Entity<Trade>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.InstrumentId);
            e.HasIndex(x => x.TradeDate);
            e.Property(x => x.LotSize).HasColumnType("decimal(18,4)");
            e.Property(x => x.EntryPrice).HasColumnType("decimal(18,5)");
            e.Property(x => x.ExitPrice).HasColumnType("decimal(18,5)");
            e.Property(x => x.StopLoss).HasColumnType("decimal(18,5)");
            e.Property(x => x.TakeProfit).HasColumnType("decimal(18,5)");
            e.Property(x => x.ProfitLoss).HasColumnType("decimal(18,2)");
            e.Property(x => x.RiskPercentage).HasColumnType("decimal(5,2)");
            e.Property(x => x.RiskRewardRatio).HasColumnType("decimal(5,2)");
            e.HasOne(x => x.User).WithMany(u => u.Trades).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Instrument).WithMany(i => i.Trades).HasForeignKey(x => x.InstrumentId);
            e.HasOne(x => x.TradingAccount).WithMany(a => a.Trades).HasForeignKey(x => x.TradingAccountId).IsRequired(false);
        });

        // Note
        mb.Entity<Note>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.User).WithMany(u => u.Notes).HasForeignKey(x => x.UserId);
        });

        // Alert
        mb.Entity<Alert>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.DailyLossLimit).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.User).WithMany(u => u.Alerts).HasForeignKey(x => x.UserId);
        });

        // TradingAccount
        mb.Entity<TradingAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.PropFirmName).HasMaxLength(100);
            e.Property(x => x.PropFirmPlan).HasMaxLength(150);
            e.Property(x => x.Balance).HasColumnType("decimal(18,2)");
            e.Property(x => x.DailyDrawdownLimitPct).HasColumnType("decimal(5,2)");
            e.Property(x => x.MaxOverallLossPct).HasColumnType("decimal(5,2)");
            e.Property(x => x.ProfitTargetPct).HasColumnType("decimal(5,2)");
            e.Property(x => x.ProfitSplitPct).HasColumnType("decimal(5,2)");
            e.Property(x => x.MaxRiskPerTradePctOfDailyLimit).HasColumnType("decimal(5,2)");
            e.Property(x => x.MaxAllowedLotSize).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.User).WithMany(u => u.TradingAccounts).HasForeignKey(x => x.UserId);
        });

        // PropFirmPreset
        mb.Entity<PropFirmPreset>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.FirmName, x.PlanName }).IsUnique();
            e.Property(x => x.FirmName).HasMaxLength(100).IsRequired();
            e.Property(x => x.PlanName).HasMaxLength(150).IsRequired();
            e.Property(x => x.AccountSize).HasColumnType("decimal(18,2)");
            e.Property(x => x.DailyDrawdownLimitPct).HasColumnType("decimal(5,2)");
            e.Property(x => x.MaxOverallLossPct).HasColumnType("decimal(5,2)");
            e.Property(x => x.ProfitTargetPct).HasColumnType("decimal(5,2)");
            e.Property(x => x.ProfitSplitPct).HasColumnType("decimal(5,2)");
            e.Property(x => x.MaxAllowedLotSize).HasColumnType("decimal(18,4)");
            e.Property(x => x.MaxRiskPerTradePctOfDailyLimit).HasColumnType("decimal(5,2)");
        });



        // Announcement
        mb.Entity<Announcement>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.AdminId);
            e.HasIndex(x => x.CreatedAt).IsDescending();
            e.Property(x => x.Title).HasMaxLength(255).IsRequired();
            e.Property(x => x.Priority).HasConversion<int>();
            e.HasOne(x => x.Admin).WithMany(u => u.Announcements).HasForeignKey(x => x.AdminId).OnDelete(DeleteBehavior.Restrict);
        });

        // ForumMessage
        mb.Entity<ForumMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.AuthorId);
            e.HasIndex(x => x.ReceiverId);
            e.HasIndex(x => x.SenderId);
            e.HasIndex(x => x.ChannelType);
            e.HasIndex(x => x.CreatedAt).IsDescending();
            e.Property(x => x.ChannelType).HasConversion<int>();

            e.HasOne(x => x.Author)
             .WithMany(u => u.ForumMessages)
             .HasForeignKey(x => x.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ParentMessage)
             .WithMany(m => m.Replies)
             .HasForeignKey(x => x.ParentMessageId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // PlaybookRule
        mb.Entity<PlaybookRule>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Category).HasConversion<int>();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // TradeChecklist (no soft-delete filter — hard delete for clean replacement)
        mb.Entity<TradeChecklist>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TradeId, x.RuleId }).IsUnique();
            e.HasOne(x => x.Trade).WithMany(t => t.Checklists).HasForeignKey(x => x.TradeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PlaybookRule).WithMany(r => r.Checklists).HasForeignKey(x => x.RuleId).OnDelete(DeleteBehavior.Cascade);
        });

        // UserAiSettings
        mb.Entity<UserAiSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.Provider).HasConversion<int>();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // AiChatSession
        mb.Entity<AiChatSession>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Title).HasMaxLength(255);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // BacktestResult
        mb.Entity<BacktestResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.TotalPL).HasColumnType("decimal(18,2)");
            e.Property(x => x.WinRate).HasColumnType("decimal(5,2)");
            e.Property(x => x.ProfitFactor).HasColumnType("decimal(10,4)");
            e.Property(x => x.SharpeRatio).HasColumnType("decimal(10,4)");
            e.Property(x => x.SortinoRatio).HasColumnType("decimal(10,4)");
            e.Property(x => x.MaxDrawdown).HasColumnType("decimal(18,2)");
            e.Property(x => x.MaxDrawdownPercent).HasColumnType("decimal(5,2)");
            e.Property(x => x.AverageRRR).HasColumnType("decimal(5,2)");
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Trade — add new columns
        mb.Entity<Trade>(e =>
        {
            e.Property(x => x.ChecklistCompliancePercent).HasColumnType("decimal(5,2)");
            e.Property(x => x.ChartImageUrl).HasMaxLength(500);
        });
    }
}
