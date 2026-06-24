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
    private List<ExchangeRecord> exchanges;
    private string selectedExchangeId;
    private int arbitrages = 0;
    private decimal gridStepSize = 50;
    private decimal gridStepFee = 0.02m / 100; // 0.02%

    public Exchange(ILogger<Exchange> logger, TradeRepository tradeRepository, ExchangeRepository exchangeRepository,
        IExchange binanceExchange)
    {
        this.logger = logger;
        this.tradeRepository = tradeRepository;
        this.exchangeRepository = exchangeRepository;
        this.binanceExchange = binanceExchange;
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
        var startTime = DateTime.UtcNow.AddDays(-1);
        var trades = await tradeRepository.GetBySymbolAsync(Guid.Parse(selectedExchangeId), startTime, "BTCUSDT");

        var startPrice = trades.First().Price;
        var spreadUpper = startPrice * 1.02m;
        var spreadLower = startPrice * 0.98m;
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

        foreach (var gridTrade in tradePairs)
        {
            var profitSum = gridTrade.Value.Sum(t => t.Profit);

            logger.LogDebug("GridTrade: {Count} - Profit: {Profit:n2} - Grid size: {GridSize:n2}",
                gridTrade.Value.Count,
                profitSum,
                gridTrade.Key);
        }

        if (tradePairs.Any())
        {
            arbitrages = (int)tradePairs.First().Key;
        }
    }

    private async Task CalculateGridTrade()
    {
        var trades = await tradeRepository.GetBySymbolAsync(Guid.Parse(selectedExchangeId), "BTCUSDT");

        var startPrice = trades.First().Price;

        var numberOfGridTrade = CalculateNumberOfTrades(startPrice, trades, gridStepSize);

        arbitrages = numberOfGridTrade.Count;
    }

    private List<GridTrade> CalculateNumberOfTrades(decimal startPrice, List<TradeRecord> trades, decimal tickSize)
    {
        var numberOfGridTrade = 0;

        var gridTrades = new List<GridTrade>();
        var currentTradePrice = startPrice;
        foreach (var trade in trades)
        {
            while (trade.Price > currentTradePrice + tickSize)
            {
                var gridTrade = new GridTrade(gridStepFee, currentTradePrice, currentTradePrice + tickSize, tickSize);
                gridTrades.Add(gridTrade);

                currentTradePrice += tickSize;
                numberOfGridTrade++;
                //logger.LogDebug("GridTrade up: {numberOfGridTrade}, {tradePrice}", numberOfGridTrade,
                //    currentTradePrice);
            }

            while (trade.Price < currentTradePrice - tickSize)
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
    public GridTrade(decimal fee, decimal priceBuy, decimal priceSell, decimal priceSpread, decimal quantity = 10.0m)
    {
        PriceSpread = priceSpread;
        PriceBuy = priceBuy;
        PriceSell = priceSell;
        Quantity = quantity;
        Fee = fee;
    }

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