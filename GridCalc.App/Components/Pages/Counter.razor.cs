using GridCalc.App.CryptoExchange;
using Microsoft.AspNetCore.Components;

namespace GridCalc.App.Components.Pages;

public partial class Counter
{
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
