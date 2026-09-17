namespace QuoteDesk.Agents.Tools.Results;

/// <summary>One catalogue candidate <c>search_catalog</c> ranked. Deliberately narrow — just enough
/// for the model to choose between candidates. Price and unit are not here: the model never prices,
/// and every field costs tokens that are re-sent on every turn of the tool loop.</summary>
public sealed record CatalogCandidate
{
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }

    /// <summary>The one distinguishing attribute for near-identical variants (e.g. "6mm" vs "8mm"),
    /// or null when the item has none.</summary>
    public string? Attributes { get; init; }

    /// <summary>0.0 to 1.0 — how strongly this candidate matches the search terms, weighted so a
    /// rare, distinguishing word counts for more than a common family word.</summary>
    public required double Confidence { get; init; }
}

/// <summary>One line item's worth of search input — <c>search_catalog</c> takes an array of these
/// instead of one query at a time, so the model resolves every line in a single tool call rather than
/// one call per line (found live: the per-line version cost 3 real Gemini calls for a 3-line enquiry,
/// eating into the free-tier daily quota faster than necessary for no benefit — see
/// docs/SESSION-LOG.md).</summary>
public sealed record CatalogSearchQuery
{
    public required string Query { get; init; }
    public string[] Hints { get; init; } = [];
}

/// <summary>
/// <c>verify_catalogue_term</c>'s answer — whether a word the Intake agent is unsure of (a misspelling,
/// an illegible scrawl, a nickname) is a real catalogue word, and roughly what kind of item it belongs
/// to. Deliberately <b>no SKU, price or cost</b>: Intake perceives, it never identifies a part — that
/// stays Resolve's job (docs/FOUNDRY-PLAN.md §4b). Checked by
/// <c>ToolResultBoundaryTests.CatalogueTermCheck_HasNoSkuPriceOrCostProperty</c>.
/// </summary>
public sealed record CatalogueTermCheck
{
    /// <summary>Echoes the term as the model sent it.</summary>
    public required string Term { get; init; }

    /// <summary>True when every meaningful word of <see cref="Term"/> appears as a <i>whole word</i>
    /// in at least one catalogue item.</summary>
    public required bool Known { get; init; }

    /// <summary>The product families (Bearings, Belts, Gears, SpindleTapes) the term appears in.</summary>
    public required IReadOnlyList<string> Families { get; init; }

    /// <summary>At most three item names containing the term, so the model can see the word in
    /// context — enough to confirm a reading, never enough to pick a part.</summary>
    public required IReadOnlyList<string> ExampleNames { get; init; }
}

/// <summary>
/// One query's result within a <c>search_catalog</c> call. SPEC originally described this tool as
/// returning a bare <c>CatalogMatch[]</c>, but an array has no way to express "I cannot tell which of
/// these you mean" — <see cref="Outcome"/> carries that explicitly, corrected in docs/SPEC.md §7 in
/// the same commit as this type.
/// </summary>
public sealed record CatalogSearchResult
{
    /// <summary>Echoes the <see cref="CatalogSearchQuery.Query"/> this result answers, so a batched
    /// call's results can be matched back to the line item that produced each one.</summary>
    public required string Query { get; init; }

    /// <summary>"resolved" | "ambiguous" | "not_found".</summary>
    public required string Outcome { get; init; }

    /// <summary>Set only when <see cref="Outcome"/> is "resolved".</summary>
    public string? ResolvedSku { get; init; }

    public required IReadOnlyList<CatalogCandidate> Candidates { get; init; }

    public required string Reason { get; init; }
}
