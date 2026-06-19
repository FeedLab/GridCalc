namespace GridCalc.App.Data.Entities;

public record TradeRecord(Guid Id, Guid ExchangeId, string Symbol, decimal Price, decimal Quantity, DateTime Timestamp);
