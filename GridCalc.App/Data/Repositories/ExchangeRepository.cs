using GridCalc.App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridCalc.App.Data.Repositories;

public class ExchangeRepository(ILogger<ExchangeRepository> logger, IDbContextFactory<GridCalcDbContext> dbFactory)
{
    public async Task UpsertAsync(ExchangeRecord record)
    {
        logger.LogDebug("Starting UpsertAsync for ExchangeRecord with Id: {Id}", record.Id);
        
        await using var db = await dbFactory.CreateDbContextAsync();
        logger.LogDebug("DbContext created successfully");
        
        var exists = await db.Exchanges.AnyAsync(e => e.Id == record.Id);
        logger.LogDebug("Existence check completed. Record exists: {Exists}", exists);
        
        if (exists)
        {
            logger.LogDebug("Updating existing ExchangeRecord with Id: {Id}", record.Id);
            db.Exchanges.Update(record);
        }
        else
        {
            logger.LogDebug("Adding new ExchangeRecord with Id: {Id}", record.Id);
            db.Exchanges.Add(record);
        }
        
        await db.SaveChangesAsync();
        logger.LogDebug("SaveChangesAsync completed successfully for ExchangeRecord with Id: {Id}", record.Id);
    }

    public async Task<List<ExchangeRecord>> GetAllAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Exchanges.ToListAsync();
    }
}
