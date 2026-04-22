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

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Global soft-delete filter
        mb.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Instrument>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Trade>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Note>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<TradingAccount>().HasQueryFilter(e => !e.IsDeleted);

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
            e.Property(x => x.Balance).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.User).WithMany(u => u.TradingAccounts).HasForeignKey(x => x.UserId);
        });
    }
}
