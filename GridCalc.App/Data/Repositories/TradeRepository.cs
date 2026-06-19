using GridCalc.App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridCalc.App.Data.Repositories;

public class TradeRepository(ILogger<TradeRepository> logger, IDbContextFactory<GridCalcDbContext> dbFactory)
{
    public async Task AddAsync(TradeRecord record)
    {
        logger.LogDebug("Storing trade: {Symbol} {Quantity} @ {Price}", record.Symbol, record.Quantity, record.Price);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.Trades.Add(record);
        await db.SaveChangesAsync();
    }

    public async Task<List<TradeRecord>> GetBySymbolAsync(string symbol)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Trades.Where(t => t.Symbol == symbol).ToListAsync();
    }
}
