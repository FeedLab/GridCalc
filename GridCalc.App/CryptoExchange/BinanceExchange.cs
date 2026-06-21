using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients.SpotApi;
using GridCalc.App.Data.Entities;
using GridCalc.App.Data.Repositories;

namespace GridCalc.App.CryptoExchange;

public interface IExchange
{
    Task Initialize();
    ExchangeRecord ExchangeData { get; }
}

public class BinanceExchange : IExchange
{
    private readonly ILogger<BinanceExchange> logger;
    private readonly ExchangeRepository exchangeRepository;
    private readonly SymbolRepository symbolRepository;
    private readonly TradeRepository tradeRepository;
    private BinanceRestClient? exchangeClient;
    private BinanceSocketClient? exchangeSocketClient;


    private readonly string key;
    private readonly string secret;
    public ExchangeRecord ExchangeData { get; private set; }
    private List<SymbolRecord> listOfSymbols = [];

    public BinanceExchange(ILogger<BinanceExchange> logger, IConfiguration config,
        ExchangeRepository exchangeRepository, SymbolRepository symbolRepository, TradeRepository tradeRepository)
    {
        this.logger = logger;
        this.exchangeRepository = exchangeRepository;
        this.symbolRepository = symbolRepository;
        this.tradeRepository = tradeRepository;

        key = config["BINANCE_EXCHANGE_KEY"] ?? throw new InvalidOperationException("Exchange Key can not be null");
        secret = config["BINANCE_EXCHANGE_SECRET"] ??
                 throw new InvalidOperationException("Exchange Secret can not be null");

        IsConnected = false;
        HasKeyAndSecretLoaded = true;

        ExchangeData = new ExchangeRecord
        (
            Guid.Parse("e8063a5f-5c10-4476-9748-191a05b2d4e9"),
            "Binance",
            "Binance Exchange",
            DateTime.UtcNow
        );
        
        logger.LogInformation("Binance Exchange Initialized");
    }

    public async Task Initialize()
    {
        await CreateExchangeRecord();
        await CreateSymbolRecords();

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

        var listOfSymbolPairs = listOfSymbols.Select(s => s.Symbol).ToList();
        var subscription = await exchangeSocketClient.SpotApi.ExchangeData.SubscribeToTradeUpdatesAsync(listOfSymbolPairs,
            data =>
            {
                logger.LogDebug($"{data.Data.TradeTime}: {data.Data.Symbol} - {data.Data.Quantity} @ {data.Data.Price}");

                if (ExchangeData is null)
                {
                    logger.LogError("Exchange Record can not be null");
                    return;
                }
                var trade = new TradeRecord(
                    Guid.CreateVersion7(),
                    ExchangeData!.Id,
                    data.Data.Symbol,
                    data.Data.Price,
                    data.Data.Quantity,
                    data.Data.TradeTime);

                // _ = tradeRepository.AddAsync(trade);
            });

        if (!subscription.Success)
        {
            logger.LogError("Failed to sub: " + subscription.Error);
            IsConnected = false;
            return;
        }

        IsConnected = true;
    }

    private async Task CreateSymbolRecords()
    {
        if(ExchangeData is null)
            throw new InvalidOperationException("Exchange Record can not be null");
        
        var symbolBtcUsdt = new SymbolRecord(ExchangeData.Id, "BTCUSDT","BTC", "USDT", DateTime.UtcNow);
        var symbolEthUsdt = new SymbolRecord(ExchangeData.Id, "ETHUSDT","ETH", "USDT", DateTime.UtcNow);
        var symbolAdaUsdt = new SymbolRecord(ExchangeData.Id, "ADAUSDT","ADA", "USDT", DateTime.UtcNow);
        
        listOfSymbols.AddRange(symbolBtcUsdt, symbolEthUsdt, symbolAdaUsdt);
        
        await symbolRepository.UpsertAsync(listOfSymbols);
    }

    private async Task CreateExchangeRecord()
    {
        await exchangeRepository.UpsertAsync(ExchangeData);
    }

    public bool IsConnected { get; set; }
    public bool HasKeyAndSecretLoaded { get; set; }
}