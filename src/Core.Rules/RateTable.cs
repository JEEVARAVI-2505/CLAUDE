namespace BuildingStudio.Core.Rules;

public sealed class RateItem
{
    public string Description { get; set; } = "";
    public string Unit { get; set; } = "";
    public double Rate { get; set; }
}

/// <summary>Editable regional rate table (rules/rates.*.json).</summary>
public sealed class RateTable
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Currency { get; set; } = "INR";
    public int Year { get; set; }
    public string Disclaimer { get; set; } = "";
    public double ContractorMarginPct { get; set; } = 10;
    public double GstPct { get; set; } = 18;
    public double ContingencyPct { get; set; } = 3;
    public Dictionary<string, RateItem> Items { get; set; } = [];

    public RateItem Get(string code) =>
        Items.TryGetValue(code, out var r) ? r : throw new KeyNotFoundException($"Rate '{code}' missing from {Id}.");
}
