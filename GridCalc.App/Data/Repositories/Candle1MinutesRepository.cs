using System.Collections;
using System.Collections.Concurrent;
using ccxt;
using GridCalc.App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridCalc.App.Data.Repositories;

public class Candle1MinutesRepository(
    ILogger<Candle1MinutesRepository> logger,
    IDbContextFactory<GridCalcDbContext> dbFactory,
    ConcurrentDictionary<string, CandleCache1Minutes> candleCache1Minutes )
{
    public async Task AddAsync(CandleRecord record)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Candle1Minutes.Add(record);
        await db.SaveChangesAsync();
    }

    
    public async Task AddAsync(List<CandleRecord> candles)
    {
        if (!candles.Any())
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        db.Candle1Minutes.AddRange(candles);
        await db.SaveChangesAsync();
    }
    
    public async Task<CandleRecord?> GetLatestByExchangeAndSymbolAsync(Guid exchangeId, string symbol)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.Candle1Minutes
            .Where(t => t.ExchangeId == exchangeId && t.Symbol == symbol)
            .OrderByDescending(o => o.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CandleRecord>> GetByExchangeAndSymbolAsync(Guid exchangeId, DateTime startTime, string symbol)
    {
        var keyCandleCache = $"{exchangeId}_{symbol}";
        var candleCache1Minute = candleCache1Minutes.GetOrAdd(keyCandleCache, _ => new CandleCache1Minutes());

        await candleCache1Minute.CacheLock.WaitAsync();
        try
        {
            if (candleCache1Minute.ExpirerAt < DateTime.UtcNow || !candleCache1Minute.TradeRecords.Any())
            {
                var dbStartTime = DateTime.UtcNow.AddDays(-900);

                if (candleCache1Minute.TradeRecords.Any())
                {
                    var lastCandle = candleCache1Minute.TradeRecords.Last();
                    dbStartTime = lastCandle.Timestamp;
                }

                await using var db = await dbFactory.CreateDbContextAsync();
                var candleRecords = await db.Candle1Minutes
                    .Where(t => t.ExchangeId == exchangeId && t.Symbol == symbol && t.Timestamp > dbStartTime)
                    .OrderBy(o => o.Timestamp)
                    .ToListAsync();

                candleCache1Minute.ExpirerAt = DateTime.UtcNow.AddMinutes(5);
                candleCache1Minute.TradeRecords = candleRecords;
            }

            return candleCache1Minute.TradeRecords
                .Where(t => t.ExchangeId == exchangeId && t.Symbol == symbol && t.Timestamp > startTime)
                .OrderBy(o => o.Timestamp)
                .ToList();
            
            return candleCache1Minute.TradeRecords;
        }
        finally
        {
            candleCache1Minute.CacheLock.Release();
        }
    }

}

public class CandleCache1Minutes
{
    public SemaphoreSlim CacheLock { get; } = new(1, 1);

    public DateTime ExpirerAt { get; set; }
    public List<CandleRecord> TradeRecords { get; set; } = [];
}