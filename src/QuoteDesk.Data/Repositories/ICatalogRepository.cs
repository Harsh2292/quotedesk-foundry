namespace QuoteDesk.Data.Repositories;

public interface ICatalogRepository
{
    /// <summary>Case-insensitive substring match against SKU and name — good enough for the demo's
    /// catalogue size; a real search index is future work, not this project.</summary>
    Task<IReadOnlyList<CatalogItemRecord>> SearchAsync(string query, CancellationToken cancellationToken);

    Task<CatalogItemRecord?> GetBySkuAsync(string sku, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogItemRecord>> GetByCategoryAsync(string category, CancellationToken cancellationToken);

    /// <summary>Every catalogue item, ordered by SKU. The catalogue is a few hundred rows, so this is
    /// cheap; it backs <c>verify_catalogue_term</c>'s vocabulary of real catalogue words.</summary>
    Task<IReadOnlyList<CatalogItemRecord>> GetAllAsync(CancellationToken cancellationToken);
}
