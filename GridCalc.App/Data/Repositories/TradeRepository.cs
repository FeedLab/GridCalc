using GridCalc.App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridCalc.App.Data.Repositories;

public class TradeRepository(ILogger<TradeRepository> logger, IDbContextFactory<GridCalcDbContext> dbFactory, GridTradeCache cacheTrades)
{
    public async Task AddAsync(TradeRecord record)
    {
        logger.LogDebug("Storing trade: {Symbol} {Quantity} @ {Price}", record.Symbol, record.Quantity, record.Price);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.Trades.Add(record);
        await db.SaveChangesAsync();
    }

    public async Task<List<TradeRecord>> GetBySymbolAsync(Guid exchangeId, string symbol)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Trades
            .Where(t => t.ExchangeId == exchangeId && t.Symbol == symbol)
            .OrderBy(o => o.Timestamp)
            .ToListAsync();
    }
    
    public async Task<List<TradeRecord>> GetBySymbolAsync(Guid exchangeId, DateTime startTime, string symbol)
    {
        if (cacheTrades.ExpirerAt < DateTime.UtcNow)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var result = await db.Trades
                .Where(t => t.ExchangeId == exchangeId && t.Symbol == symbol && t.Timestamp >= startTime)
                .OrderBy(o => o.Timestamp)
                .ToListAsync();

            cacheTrades.ExpirerAt = DateTime.UtcNow.AddMinutes(60);
            cacheTrades.TradeRecords = result;
        }

        if (cacheTrades.TradeRecords is null)
        {
            return new List<TradeRecord>();
        }
        
        return cacheTrades.TradeRecords;
    }

    public async Task<List<TradeRecord>> GetBySymbolAsync(string symbol)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Trades.Where(t => t.Symbol == symbol).ToListAsync();
    }
}

public class GridTradeCache
{
    public DateTime ExpirerAt { get; set; }
    public List<TradeRecord>? TradeRecords { get; set; }
}