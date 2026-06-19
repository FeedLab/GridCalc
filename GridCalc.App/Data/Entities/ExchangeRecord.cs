namespace GridCalc.App.Data.Entities;

public record ExchangeRecord
{
    public ExchangeRecord(Guid id, string name, string displayName, DateTime timestamp)
    {
        Id = id;
        Name = name;
        DisplayName = displayName;
        Timestamp = timestamp;
    }

    public DateTime Timestamp { get; init; }
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string DisplayName { get; init; }
}
