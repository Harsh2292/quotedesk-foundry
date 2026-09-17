using Microsoft.EntityFrameworkCore;

namespace QuoteDesk.Data.Repositories;

public sealed class CatalogRepository(QuoteDeskDbContext db) : ICatalogRepository
{
    private const string LikeEscape = "\\";

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);

    public async Task<IReadOnlyList<CatalogItemRecord>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // The query is model- and customer-influenced text, so LIKE's own wildcards in it are escaped:
        // a term of "%" or "__" must search for those literal characters, not match every row. The
        // value was always parameterised (no SQL injection); this is about the pattern language
        // (foundry-03 security review).
        var pattern = $"%{EscapeLike(query)}%";
        var items = await db.CatalogItems.AsNoTracking()
            .Where(c => EF.Functions.Like(c.Sku, pattern, LikeEscape) || EF.Functions.Like(c.Name, pattern, LikeEscape))
            .OrderBy(c => c.Sku)
            .ToListAsync(cancellationToken);

        return [.. items.Select(ToRecord)];
    }

    public async Task<CatalogItemRecord?> GetBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        var item = await db.CatalogItems.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Sku == sku, cancellationToken);

        return item is null ? null : ToRecord(item);
    }

    public async Task<IReadOnlyList<CatalogItemRecord>> GetByCategoryAsync(string category, CancellationToken cancellationToken)
    {
        var items = await db.CatalogItems.AsNoTracking()
            .Where(c => c.Category == category)
            .OrderBy(c => c.Sku)
            .ToListAsync(cancellationToken);

        return [.. items.Select(ToRecord)];
    }

    private static CatalogItemRecord ToRecord(Entities.CatalogItem c) =>
        new(c.Id, c.Sku, c.Name, c.Category, c.Uom, c.ListPrice, c.CostPrice, c.Attributes);
}
