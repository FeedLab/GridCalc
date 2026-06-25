using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients.SpotApi;
using ccxt;
using GridCalc.App.Data.Entities;
using GridCalc.App.Data.Repositories;

namespace GridCalc.App.CryptoExchange;

public interface IExchange
{
    Task Initialize();
    Task ImportCandles();
    ExchangeRecord ExchangeData { get; }
}

public class BinanceExchange : IExchange
{
    private readonly ILogger<BinanceExchange> logger;
    private readonly ExchangeRepository exchangeRepository;
    private readonly SymbolRepository symbolRepository;
    private readonly TradeRepository tradeRepository;
    private readonly Candle1MinutesRepository candle1MinutesRepository;
    private BinanceRestClient? exchangeClient;
    private BinanceSocketClient? exchangeSocketClient;


    // private readonly string key;
    // private readonly string secret;
    public ExchangeRecord ExchangeData { get; private set; }
    private readonly List<SymbolRecord> listOfSymbols = [];
    private readonly Guid exchangeId = Guid.Parse("e8063a5f-5c10-4476-9748-191a05b2d4e9");

    public BinanceExchange(ILogger<BinanceExchange> logger, IConfiguration config,
        ExchangeRepository exchangeRepository, SymbolRepository symbolRepository, TradeRepository tradeRepository, Candle1MinutesRepository candle1MinutesRepository)
    {
        this.logger = logger;
        this.exchangeRepository = exchangeRepository;
        this.symbolRepository = symbolRepository;
        this.tradeRepository = tradeRepository;
        this.candle1MinutesRepository = candle1MinutesRepository;

        IsConnected = false;
        HasKeyAndSecretLoaded = true;

        ExchangeData = new ExchangeRecord
        (
            exchangeId,
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

        IsConnected = true;
    }

    public async Task ImportCandles()
    {
        foreach (var symbol in listOfSymbols)
        {
            var candle = await candle1MinutesRepository.GetLatestByExchangeAndSymbolAsync(ExchangeData.Id, symbol.Symbol);

            var timestamp = 0L;
            
            if (candle is null)
            {
                timestamp = DateTimeOffset.UtcNow.AddDays(-90).ToUnixTimeMilliseconds();
            }
            else
            {
                var utcTimestamp = DateTime.SpecifyKind(candle.Timestamp, DateTimeKind.Utc);
                var unixMilliseconds = new DateTimeOffset(utcTimestamp).ToUnixTimeMilliseconds();

                timestamp = unixMilliseconds;
            }

            await ImportCandles(symbol, timestamp);
        }
    }
    
    public async Task ImportCandles(SymbolRecord symbols, long since)
    {
        var candles = await GetCandles(since, $"{symbols.BaseAsset}/{symbols.QuoteAsset}");

        var candleRecords = candles.Select(s => new CandleRecord(
            Guid.CreateVersion7(),
            ExchangeData.Id,
            symbols.Symbol,
            (decimal)(s.open ?? 0),   // default to 0 if null
            (decimal)(s.close ?? 0),
            (decimal)(s.high ?? 0),
            (decimal)(s.low ?? 0),
            (decimal)(s.volume ?? 0),
            s.timestamp.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(s.timestamp.Value).UtcDateTime
                : DateTime.UtcNow
        )).ToList();

        
        await candle1MinutesRepository.AddAsync(candleRecords);
    }
    
    private async Task CreateSymbolRecords()
    {
        if(ExchangeData is null)
            throw new InvalidOperationException("Exchange Record can not be null");
        
        var symbolBtcUsdt = new SymbolRecord(ExchangeData.Id, "BTC/USDT","BTC", "USDT", DateTime.UtcNow);
        var symbolEthUsdt = new SymbolRecord(ExchangeData.Id, "ETH/USDT","ETH", "USDT", DateTime.UtcNow);
        var symbolAdaUsdt = new SymbolRecord(ExchangeData.Id, "ADA/USDT","ADA", "USDT", DateTime.UtcNow);
        
        listOfSymbols.AddRange(symbolBtcUsdt, symbolEthUsdt, symbolAdaUsdt);
        
        await symbolRepository.UpsertAsync(listOfSymbols);
    }

    private async Task CreateExchangeRecord()
    {
        await exchangeRepository.UpsertAsync(ExchangeData);
    }
    
    private async Task<List<OHLCV>> GetCandles(long startDate, string symbol)
    {
        var binance = new ccxt.binance();

        var allCandles = new List<ccxt.OHLCV>();

        var since = startDate;
        var now = binance.milliseconds();

        while (since < now)
        {
            var candles = await binance.FetchOHLCV(symbol, "1m", since, 1000);

            if (candles.Count == 0)
                break;

            allCandles.AddRange(candles);

            var last = candles[^1];
            
            if (last.timestamp.HasValue)
            {
                since = last.timestamp.Value + 60_000;
            }
            else
            {
                break;
            }

            var timestampLong = candles.Last().timestamp;
            var timestamp = timestampLong.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestampLong.Value).UtcDateTime
                : DateTime.UtcNow;

            
            Console.WriteLine($"Fetched {timestamp}-{candles.Count}, total {allCandles.Count}");
        }


        Console.WriteLine($"Final total candles: {allCandles.Count}");

        return allCandles;
    }

    public bool IsConnected { get; set; }
    public bool HasKeyAndSecretLoaded { get; set; }
}