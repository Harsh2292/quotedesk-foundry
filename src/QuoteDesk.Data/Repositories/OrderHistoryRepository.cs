using Microsoft.EntityFrameworkCore;

namespace QuoteDesk.Data.Repositories;

public sealed class OrderHistoryRepository(QuoteDeskDbContext db) : IOrderHistoryRepository
{
    /// <summary>Caps one SKU's rows. Returning every row bloated the tool result and, worse, is
    /// re-sent on every turn of the tool loop — a real driver of runaway token cost
    /// (docs/SESSION-LOG.md).</summary>
    private const int MaxRowsForOneSku = 20;

    /// <summary>Caps the no-SKU overview. The seeded customers have at most 47 distinct SKUs, so this
    /// only guards against a customer with an unusually wide history.</summary>
    private const int MaxDistinctSkus = 60;

    /// <summary>With a SKU: that SKU's purchases, newest first. Without one: the latest purchase of
    /// <em>each</em> SKU the customer has bought, newest first.</summary>
    /// <remarks>The overview used to be the 20 most recent rows, which silently hid anything bought
    /// earlier: Jai Fabrics' 6209-2Z purchases sat behind 20 newer orders, so "the same 6209 bearings"
    /// looked like no history at all (foundry-07 baseline run, 2026-09-22). One row per SKU keeps the
    /// result small and never drops a product the customer has bought.</remarks>
    public async Task<IReadOnlyList<OrderHistoryRecord>> GetByCustomerAsync(
        int customerId, string? sku, CancellationToken cancellationToken)
    {
        var query = db.OrderHistory.AsNoTracking().Where(o => o.CustomerId == customerId);

        List<Entities.OrderHistory> orders;
        if (sku is not null)
        {
            orders = await query
                .Where(o => o.Sku == sku)
                .OrderByDescending(o => o.OrderedAt)
                .Take(MaxRowsForOneSku)
                .ToListAsync(cancellationToken);
        }
        else
        {
            // One customer's history is a few dozen rows, so grouping in memory is simpler and safer
            // than relying on EF Core's GroupBy translation.
            var all = await query.ToListAsync(cancellationToken);
            orders = [.. all
                .GroupBy(o => o.Sku)
                .Select(g => g.OrderByDescending(o => o.OrderedAt).ThenByDescending(o => o.Id).First())
                .OrderByDescending(o => o.OrderedAt)
                .ThenBy(o => o.Sku, StringComparer.Ordinal)
                .Take(MaxDistinctSkus)];
        }

        return [.. orders.Select(o => new OrderHistoryRecord(o.Id, o.CustomerId, o.Sku, o.Qty, o.UnitPrice, o.OrderedAt))];
    }
}
