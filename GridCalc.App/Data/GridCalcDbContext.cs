using GridCalc.App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridCalc.App.Data;

public class GridCalcDbContext(DbContextOptions<GridCalcDbContext> options) : DbContext(options)
{
    public DbSet<ExchangeRecord> Exchanges => Set<ExchangeRecord>();
    public DbSet<SymbolRecord> Symbols => Set<SymbolRecord>();
    public DbSet<TradeRecord> Trades => Set<TradeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExchangeRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<SymbolRecord>(entity =>
        {
            entity.HasKey(s => new { s.ExchangeId, s.BaseAsset, s.QuoteAsset });
        });

        modelBuilder.Entity<TradeRecord>(entity =>
        {
            entity.HasKey(t => t.Id);
        });
    }
}
