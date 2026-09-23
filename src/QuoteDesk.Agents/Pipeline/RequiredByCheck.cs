using QuoteDesk.Agents.Tools.Results;

namespace QuoteDesk.Agents.Pipeline;

/// <summary>
/// Compares each priced line's delivery date with the date the customer asked for — in code, since a
/// date that decides whether a promise can be kept is not the model's to judge (CLAUDE.md rule 1).
/// docs/DOMAIN.md's worked example depends on it ("The customer asked for the 5th. Flagged."), and
/// until the foundry-07 baseline run nothing compared the two at all.
/// </summary>
public static class RequiredByCheck
{
    /// <summary>IST has no daylight saving, so a fixed offset is exact.</summary>
    private static readonly TimeSpan Ist = TimeSpan.FromHours(5.5);

    /// <summary>The enquiry's own calendar date in India, where the customer wrote it.</summary>
    public static DateOnly ReceivedOn(DateTimeOffset receivedAt) =>
        DateOnly.FromDateTime(receivedAt.ToOffset(Ist).DateTime);

    public static IReadOnlyList<string> Warnings(
        IReadOnlyList<PricedQuoteLine> lines, DateOnly? requiredBy, DateOnly receivedOn)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (requiredBy is not DateOnly asked)
        {
            return [];
        }

        // A date already behind us was misread or is stale; flagging every line as late would bury
        // that, so it is one warning for the human to check instead.
        if (asked < receivedOn)
        {
            return [$"The requested date {asked:yyyy-MM-dd} is before the enquiry arrived ({receivedOn:yyyy-MM-dd}) — confirm the date with the customer."];
        }

        return [.. lines
            .Where(l => l.DeliveryDate is DateOnly delivery && delivery > asked)
            .Select(l => $"'{l.Sku}' delivers {l.DeliveryDate:yyyy-MM-dd}, after the requested {asked:yyyy-MM-dd}.")];
    }
}
