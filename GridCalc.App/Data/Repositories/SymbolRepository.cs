using GridCalc.App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridCalc.App.Data.Repositories;

public class SymbolRepository(ILogger<SymbolRepository> logger, IDbContextFactory<GridCalcDbContext> dbFactory)
{
    public async Task UpsertAsync(SymbolRecord record)
    {
        logger.LogDebug("Starting UpsertAsync for SymbolRecord: {Symbol}", record.Symbol);

        await using var db = await dbFactory.CreateDbContextAsync();

        var exists = await db.Symbols.AnyAsync(s => s.ExchangeId == record.ExchangeId && s.BaseAsset == record.BaseAsset && s.QuoteAsset == record.QuoteAsset);

        if (exists)
        {
            logger.LogDebug("Updating existing SymbolRecord: {Symbol}", record.Symbol);
            db.Symbols.Update(record);
        }
        else
        {
            logger.LogDebug("Adding new SymbolRecord: {Symbol}", record.Symbol);
            db.Symbols.Add(record);
        }

        await db.SaveChangesAsync();
        logger.LogDebug("SaveChangesAsync completed for SymbolRecord: {Symbol}", record.Symbol);
    }

    public async Task<List<SymbolRecord>> GetAllAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Symbols.ToListAsync();
    }

    public async Task UpsertAsync(List<SymbolRecord> symbols)
    {
        foreach (var symbol in symbols)
        {
            await UpsertAsync(symbol);
        }
    }
}
