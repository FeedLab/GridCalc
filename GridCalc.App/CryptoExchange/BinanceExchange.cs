using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients.SpotApi;

namespace GridCalc.App.CryptoExchange;

public class BinanceExchange
{
    private BinanceRestClient exchangeClient;
    private BinanceSocketClient exchangeSocketClient;

    public async Task<IBinanceRestClientSpotApiShared> Initialize(string key, string secret)
    {
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
        
        return exchangeClient.SpotApi.SharedClient;
    }
}