using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients.SpotApi;
using GridCalc.App.Data.Entities;
using GridCalc.App.Data.Repositories;

namespace GridCalc.App.CryptoExchange;

public interface IExchange
{
    Task Initialize();
}

public class BinanceExchange : IExchange
{
    private readonly ILogger<BinanceExchange> logger;
    private readonly ExchangeRepository exchangeRepository;
    private BinanceRestClient? exchangeClient;
    private BinanceSocketClient? exchangeSocketClient;

    private record TradeRecord(DateTime Timestamp, decimal Price, decimal Amount, string Symbol, string QuoteAsset);

    private readonly string key;
    private readonly string secret;

    public BinanceExchange(ILogger<BinanceExchange> logger, IConfiguration config,
        ExchangeRepository exchangeRepository)
    {
        this.logger = logger;
        this.exchangeRepository = exchangeRepository;

        key = config["BINANCE_EXCHANGE_KEY"] ?? throw new InvalidOperationException("Exchange Key can not be null");
        secret = config["BINANCE_EXCHANGE_SECRET"] ??
                 throw new InvalidOperationException("Exchange Secret can not be null");

        IsConnected = false;
        HasKeyAndSecretLoaded = true;

        logger.LogInformation("Binance Exchange Initialized");
    }

    public async Task Initialize()
    {
        await CreateExchangeRecord();

        exchangeClient = new BinanceRestClient(options =>
        {
            options.ApiCredentials = new BinanceCredentials(key, secret);
            options.Environment = BinanceEnvironment.Testnet;
            options.OutputOriginalData = true;
        });

        exchangeSocketClient = new BinanceSocketClient(options =>
        {
            options.ApiCredentials = new BinanceCredentials(key, secret);
            options.Environment = BinanceEnvironment.Testnet;
            options.OutputOriginalData = true;
        });

        if (exchangeClient == null || exchangeSocketClient == null)
        {
            IsConnected = false;

            throw new InvalidOperationException("Exchange Client or Socket Client can not be null");
        }

        var subscription = await exchangeSocketClient.SpotApi.ExchangeData.SubscribeToTradeUpdatesAsync("BTCUSDT",
            data => { logger.LogDebug($"{data.Data.TradeTime}: {data.Data.Quantity} @ {data.Data.Price}"); });

        if (!subscription.Success)
        {
            logger.LogError("Failed to sub: " + subscription.Error);
            IsConnected = false;
            return;
        }

        IsConnected = true;
    }

    private async Task CreateExchangeRecord()
    {
        var exchangeRecord = new ExchangeRecord
        (
            Guid.Parse("e8063a5f-5c10-4476-9748-191a05b2d4e9"),
            "Binance",
            "Binance Exchange",
            DateTime.UtcNow
        );

        await exchangeRepository.UpsertAsync(exchangeRecord);
    }

    public bool IsConnected { get; set; }
    public bool HasKeyAndSecretLoaded { get; set; }
}