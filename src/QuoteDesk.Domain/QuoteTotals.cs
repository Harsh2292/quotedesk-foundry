namespace QuoteDesk.Domain;

/// <summary>Line totals, freight, GST and the grand total for one quote.</summary>
public sealed record QuoteTotals
{
    public required decimal Subtotal { get; init; }
    public required decimal Freight { get; init; }
    public required decimal Tax { get; init; }
    public required decimal GrandTotal { get; init; }
}

/// <summary>GST at 18% on the taxable value, applied after all discounts and after freight.</summary>
public static class QuoteTotalsCalculator
{
    public const decimal GstRatePct = 0.18m;

    public static QuoteTotals Calculate(IReadOnlyList<PricedLine> lines, FreightZone zone)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var subtotal = Money.Round(lines.Sum(l => l.LineTotal));
        // Nothing to ship, nothing to charge: a quote whose every line went unresolved must not
        // carry a freight charge on its own (foundry-07 baseline — an unknown sender's all-unresolved
        // quote showed ₹531 for no goods).
        var freight = lines.Count == 0 ? 0m : FreightPolicy.ResolveFreight(zone, subtotal);
        var taxableValue = subtotal + freight;
        var tax = Money.Round(taxableValue * GstRatePct);
        var grandTotal = Money.Round(taxableValue + tax);

        return new QuoteTotals
        {
            Subtotal = subtotal,
            Freight = freight,
            Tax = tax,
            GrandTotal = grandTotal,
        };
    }
}
