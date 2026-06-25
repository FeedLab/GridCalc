using GridCalc.App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridCalc.App.Data;

public class GridCalcDbContext(DbContextOptions<GridCalcDbContext> options) : DbContext(options)
{
    public DbSet<ExchangeRecord> Exchanges => Set<ExchangeRecord>();
    public DbSet<SymbolRecord> Symbols => Set<SymbolRecord>();
    public DbSet<TradeRecord> Trades => Set<TradeRecord>();
    public DbSet<CandleRecord> Candle1Minutes => Set<CandleRecord>();

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
            
            entity.Property(e => e.Price)
                .HasPrecision(18, 8);   // 18 digits total, 8 after decimal

            entity.Property(e => e.Quantity)
                .HasPrecision(18, 8);
        });
        
        modelBuilder.Entity<CandleRecord>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_CandleRecord1Minutes_Timestamp");
            
            entity.HasIndex(e => new { e.ExchangeId, e.Symbol, e.Timestamp })
                .HasDatabaseName("IX_CandleRecord_Exchange_Symbol_Timestamp");


            entity.Property(e => e.OpenPrice).HasPrecision(18, 8);
            entity.Property(e => e.ClosePrice).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Volume).HasPrecision(18, 8);
        });
    }
}
