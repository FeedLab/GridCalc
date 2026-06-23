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

    private async Task CalculateGridTrade()
    {
        var trades = await tradeRepository.GetBySymbolAsync(Guid.Parse(selectedExchangeId), "BTCUSDT");

        var startPrice = trades.First().Price;

        CalculateNumberOfTrades(startPrice, trades, gridStepSize);
    }

    private void CalculateNumberOfTrades(decimal startPrice, List<TradeRecord> trades, decimal tickSize)
    {
        var numberOfGridTrade = 0;

        var currentTradePrice = startPrice;
        foreach (var trade in trades)
        {
            while (trade.Price > currentTradePrice + tickSize)
            {
                currentTradePrice += tickSize;
                numberOfGridTrade++;
                logger.LogDebug("GridTrade up: {numberOfGridTrade}, {tradePrice}", numberOfGridTrade,
                    currentTradePrice);
            }

            while (trade.Price < currentTradePrice - tickSize)
            {
                currentTradePrice -= tickSize;
                logger.LogDebug("GridTrade down: {numberOfGridTrade}, {tradePrice}", numberOfGridTrade,
                    currentTradePrice);
            }
        }

        arbitrages = numberOfGridTrade;
    }
}