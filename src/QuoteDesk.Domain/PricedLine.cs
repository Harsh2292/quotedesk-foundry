namespace QuoteDesk.Domain;

/// <summary>One priced line — the result of <see cref="PricingEngine.PriceLine"/>. Cost price never
/// appears here: it fed the margin check but must not leave the server or reach the model.</summary>
public sealed record PricedLine
{
    public required string Sku { get; init; }
    public required int Quantity { get; init; }
    public required decimal ListPrice { get; init; }

    /// <summary>Slab discount + tier discount, capped at <see cref="PricingEngine.MaxCombinedDiscountPct"/>.</summary>
    public required decimal DiscountPct { get; init; }

    /// <summary>The quantity-slab component of <see cref="DiscountPct"/>, before the combined cap.
    /// Kept separately so an explanation can name the rule that produced the discount rather than
    /// re-deriving it — the narration cites this, it never adds it up (CLAUDE.md rule 1).</summary>
    public required decimal SlabDiscountPct { get; init; }

    /// <summary>The customer-tier component of <see cref="DiscountPct"/>, before the combined cap.
    /// Zero when the sender matched no customer (docs/DOMAIN.md, "Unknown sender").</summary>
    public required decimal TierDiscountPct { get; init; }

    /// <summary>True when <see cref="SlabDiscountPct"/> + <see cref="TierDiscountPct"/> exceeded
    /// <see cref="PricingEngine.MaxCombinedDiscountPct"/> and <see cref="DiscountPct"/> is therefore
    /// the cap rather than the sum.</summary>
    public required bool DiscountCapped { get; init; }
    public required decimal NetUnitPrice { get; init; }
    public required decimal LineTotal { get; init; }

    /// <summary>Net margin as a fraction (0.14 = 14%). Never surfaced to the model — see docs/DOMAIN.md.</summary>
    public required decimal MarginPct { get; init; }
    public required bool RequiresOverride { get; init; }
    public required decimal MarginShortfallPct { get; init; }
}
