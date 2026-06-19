namespace GridCalc.App.Data.Entities;

public record SymbolRecord(Guid ExchangeId, string Symbol, string BaseAsset, string QuoteAsset, DateTime Timestamp);
