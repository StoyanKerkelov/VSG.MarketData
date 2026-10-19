using Microsoft.EntityFrameworkCore;
using VSG.MarketData.Core.Models;

namespace VSG.MarketData.Infrastructure.Persistence;

public class MarketDataDbContext(DbContextOptions<MarketDataDbContext> options) : DbContext(options)
{
    public DbSet<MonitoredSymbol> MonitoredSymbols => Set<MonitoredSymbol>();
    public DbSet<PriceUpdate> PriceUpdates => Set<PriceUpdate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceUpdate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Symbol).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Price).HasPrecision(28, 12);
            entity.HasIndex(x => new { x.Symbol, x.Timestamp });
        });

        modelBuilder.Entity<MonitoredSymbol>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Symbol).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.Symbol).IsUnique();

            entity.HasData(
                new MonitoredSymbol { Id = 1, Symbol = "BTCUSDT", IsActive = true },
                new MonitoredSymbol { Id = 2, Symbol = "ADAUSDT", IsActive = true },
                new MonitoredSymbol { Id = 3, Symbol = "ETHUSDT", IsActive = true }
            );
        });
    }
}
