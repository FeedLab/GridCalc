using GridCalc.App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridCalc.App.Data;

public class GridCalcDbContext(DbContextOptions<GridCalcDbContext> options) : DbContext(options)
{
    public DbSet<ExchangeRecord> Exchanges => Set<ExchangeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExchangeRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}
