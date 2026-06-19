namespace GridCalc.App.CryptoExchange;

public class TradeCollectorService : BackgroundService
{
    private readonly ILogger<TradeCollectorService> _logger;
    private readonly IEnumerable<IExchange> exchanges;

    public TradeCollectorService(ILogger<TradeCollectorService> logger, IEnumerable<IExchange> exchanges)
    {
        _logger = logger;
        this.exchanges = exchanges;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TradeCollectorService started at: {time}", DateTimeOffset.Now);

        foreach (var exchange in exchanges)
        {
            await exchange.Initialize();
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Example: fetch trades or price levels here
                _logger.LogInformation("Fetching trades at: {time}", DateTimeOffset.Now);

                // TODO: Insert your Binance/Kraken API call + SQLite insert logic

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // run every minute
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TradeCollectorService");
            }
        }

        _logger.LogInformation("TradeCollectorService stopped at: {time}", DateTimeOffset.Now);
    }
}