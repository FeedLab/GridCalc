using GridCalc.App.CryptoExchange;
using GridCalc.App.Data.Entities;
using GridCalc.App.Data.Repositories;
using Microsoft.AspNetCore.Components;

namespace GridCalc.App.Components.Pages;

public partial class Exchange
{
    private readonly ILogger<Exchange> logger;
    private readonly TradeRepository tradeRepository;
    private readonly ExchangeRepository exchangeRepository;
    private readonly IExchange binanceExchange;
    private readonly Candle1MinutesRepository candle1MinutesRepository;
    private List<ExchangeRecord> exchanges;
    private string selectedExchangeId;
    private int arbitrages = 0;
    private int  numberOfDays = 7;
    private decimal gridStepFee = 0.02m; // 0.02%

    public Exchange(ILogger<Exchange> logger, TradeRepository tradeRepository, ExchangeRepository exchangeRepository,
        IExchange binanceExchange, Candle1MinutesRepository candle1MinutesRepository)
    {
        this.logger = logger;
        this.tradeRepository = tradeRepository;
        this.exchangeRepository = exchangeRepository;
        this.binanceExchange = binanceExchange;
        this.candle1MinutesRepository = candle1MinutesRepository;
    }

    protected override async Task OnInitializedAsync()
    {
        exchanges = await exchangeRepository.GetAllAsync();

        if (exchanges.Any())
        {
            selectedExchangeId = exchanges.First().Id.ToString();
        }
    }


    private async Task AutoCalculateGridTrade()
    {
        var startTime = DateTime.UtcNow.AddDays(-numberOfDays);
        var trades = await candle1MinutesRepository.GetByExchangeAndSymbolAsync(Guid.Parse(selectedExchangeId), startTime, "BTC/USDT");

        var startPrice = trades.First().OpenPrice;
        var spreadUpper = startPrice * 1.01m;
        var spreadLower = startPrice * 0.99m;
        var spreadSize = (spreadUpper - spreadLower);
        var stepSizeCalc = spreadSize / 150;
        var gridTrades = new Dictionary<decimal, List<GridTrade>>();

        for (var index = stepSizeCalc; index <= spreadSize; index += stepSizeCalc)
        {
            var numberOfGridTrade = CalculateNumberOfTrades(startPrice, trades, index);
            gridTrades.Add(index, numberOfGridTrade);
        }

        var tradePairs = gridTrades
            .Select(t => new
            {
                Pair = t,
                ProfitSum = t.Value.Sum(s => s.Profit)
            })
            .Where(x => x.Pair.Value.Count > 0 && x.ProfitSum > 0m)
            .OrderByDescending(x => x.ProfitSum)
            .Select(x => x.Pair)
            .ToDictionary(x => x.Key, x => x.Value);

        foreach (var gridTrade in tradePairs.Take(10))
        {
            var profitSum = gridTrade.Value.Sum(t => t.Profit);

            logger.LogDebug("GridTrade: {Count} - Profit: {Profit:n2} - Grid size: {GridSize:n2}",
                gridTrade.Value.Count,
                profitSum,
                gridTrade.Key);
        }

        if (tradePairs.Any())
        {
            var investment = 330m;
            arbitrages = (int)tradePairs.First().Key;
            
            var minDate = tradePairs.Values
                .SelectMany(v => v)          // flatten all lists into one sequence
                .Min(m => m.TradeTimestamp);

            var maxDate = tradePairs.Values
                .SelectMany(v => v)          // flatten all lists into one sequence
                .Max(m => m.TradeTimestamp);
            
            logger.LogDebug("Min date: {MinDate}, Max date: {MaxDate}", minDate, maxDate);
            
            
            var minPrice = tradePairs.Values
                .SelectMany(v => v)          // flatten all lists into one sequence
                .Min(m => m.PriceBuy);

            var maxPrice = tradePairs.Values
                .SelectMany(v => v)          // flatten all lists into one sequence
                .Max(m => m.PriceBuy);
            
            var tradingSpread = maxPrice - minPrice;
            var numberOfArbitrages = (int)tradePairs.First().Key;
            // var gridStep = tradePairs.First().Value.First().PriceSpread;
            // var arbitrages = tradingSpread / numberOfArbitrages;
            var investmentPerStep = investment / arbitrages;
            
            
            logger.LogDebug("Min price: {MinPrice}, Max price: {MaxPrice}", minPrice, maxPrice);
            
            foreach (var gridTrade in tradePairs.First().Value. Take(5))
            {
                logger.LogDebug(gridTrade.ToString());
            }
        }
    }

    // private async Task CalculateGridTrade()
    // {
    //     var trades = await tradeRepository.GetBySymbolAsync(Guid.Parse(selectedExchangeId), "BTCUSDT");
    //
    //     var startPrice = trades.First().Price;
    //
    //     var numberOfGridTrade = CalculateNumberOfTrades(startPrice, trades, gridStepSize);
    //
    //     arbitrages = numberOfGridTrade.Count;
    // }

    private List<GridTrade> CalculateNumberOfTrades(decimal startPrice, List<CandleRecord> trades, decimal tickSize)
    {
        var numberOfGridTrade = 0;
    
        var gridTrades = new List<GridTrade>();
        var currentTradePrice = startPrice;
        foreach (var trade in trades)
        {
            while (trade.OpenPrice > currentTradePrice + tickSize)
            {
                var gridTrade = new GridTrade(gridStepFee/ 100.0m, currentTradePrice, currentTradePrice + tickSize, tickSize, 0.005m, trade.Timestamp);
                gridTrades.Add(gridTrade);

                currentTradePrice += tickSize;
                numberOfGridTrade++;
                //logger.LogDebug("GridTrade up: {numberOfGridTrade}, {tradePrice}", numberOfGridTrade,
                //    currentTradePrice);
            }

            while (trade.OpenPrice < currentTradePrice - tickSize)
            {
                currentTradePrice -= tickSize;
                //logger.LogDebug("GridTrade down: {numberOfGridTrade}, {tradePrice}", numberOfGridTrade,
                //    currentTradePrice);
            }
        }

        return gridTrades;
    }
}

public class GridTrade
{
    public GridTrade(decimal fee, decimal priceBuy, decimal priceSell, decimal priceSpread, decimal quantity,
        DateTime tradeTimestamp)
    {
        TradeTimestamp = tradeTimestamp;
        PriceSpread = priceSpread;
        PriceBuy = priceBuy;
        PriceSell = priceSell;
        Quantity = quantity;
        Fee = fee;
    }

    public override string ToString()
    {
        return $"GridTrade: Timestamp={TradeTimestamp} Buy={PriceBuy:F3}, Sell={PriceSell:F3}, Spread={PriceSpread:F3}, " +
               $"Qty={Quantity}, CostBuy={CostBuy:F3}, IncomeSell={IncomeSell:F3}, " +
               $"FeeBuy={FeeBuy:F3}, FeeSell={FeeSell:F3}, Profit={Profit:F3} ({ProfitInPercent:P2})";
    }

    
    public DateTime TradeTimestamp { get; set; }
    public decimal PriceSpread { get; set; }

    public decimal Fee { get; set; }

    public decimal PriceBuy { get; set; }
    public decimal PriceSell { get; set; }
    public decimal Quantity { get; set; }

    public decimal CostBuy
    {
        get { return PriceBuy * Quantity; }
    }

    public decimal IncomeSell
    {
        get { return PriceSell * Quantity; }
    }

    public decimal FeeBuy
    {
        get { return CostBuy * Fee; }
    }

    public decimal FeeSell
    {
        get { return IncomeSell * Fee; }
    }

    public decimal RawProfit
    {
        get { return IncomeSell - CostBuy; }
    }

    public decimal Profit
    {
        get { return RawProfit - FeeBuy - FeeSell; }
    }

    public decimal ProfitInPercent
    {
        get { return Profit / CostBuy; }
    }
}