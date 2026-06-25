namespace GridCalc.App.Data.Entities;

public record CandleRecord(Guid Id, Guid ExchangeId, string Symbol, decimal OpenPrice, decimal ClosePrice, decimal High, decimal Low, decimal Volume, DateTime Timestamp);
