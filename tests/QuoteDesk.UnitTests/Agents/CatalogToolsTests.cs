using FluentAssertions;
using QuoteDesk.Agents.Tools;
using QuoteDesk.Agents.Tools.Results;
using QuoteDesk.Data;
using QuoteDesk.UnitTests.Agents.Fakes;

namespace QuoteDesk.UnitTests.Agents;

/// <summary>
/// <c>search_catalog</c> is a two-stage ranker: a cheap substring shortlist, then a whole-word,
/// rarity-weighted re-rank capped at five candidates. These tests run against a fake catalogue built
/// to the same grid shape as the real seed data (Bearings 44, Belts 45, SpindleTapes 32, Gears 100),
/// because the bugs this ranker fixes only show up at realistic volume.
/// </summary>
public class CatalogToolsTests
{
    [Fact]
    public async Task SearchCatalogAsync_NullQueries_Throws()
    {
        var tools = new CatalogTools(new FakeCatalogRepository());

        var act = async () => await tools.SearchCatalogAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchCatalogAsync_NullHintsOnOneQuery_Throws()
    {
        var tools = new CatalogTools(new FakeCatalogRepository());

        var act = async () => await tools.SearchCatalogAsync(
            [new CatalogSearchQuery { Query = "bearing", Hints = null! }], CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchCatalogAsync_TwentyFiveMmPuTimingBelt_ResolvesToPuAndNotRubber()
    {
        var tools = new CatalogTools(SeededCatalog());

        var result = await SearchOne(tools, "25mm PU timing belt", "25mm", "PU");

        result.Outcome.Should().Be("resolved");
        result.ResolvedSku.Should().Be("BELT-PU-25MM");
        result.Candidates.Should().NotContain(c => c.Sku == "BELT-RTB-25MM" && c.Confidence >= result.Candidates[0].Confidence);
    }

    [Fact]
    public async Task SearchCatalogAsync_RingFrameSpindleTape_DoesNotPullInBearingsDespiteTheSubstring()
    {
        var tools = new CatalogTools(SeededCatalog());

        var result = await SearchOne(tools, "ring frame spindle tape", "thicker");

        result.Candidates.Should().OnlyContain(c => c.Category == "SpindleTapes");
        result.Candidates.Should().NotContain(c => c.Sku.StartsWith("BRG-"));
    }

    [Fact]
    public async Task SearchCatalogAsync_RingFrameSpindleTapeTheThickerOne_IsAmbiguousAcrossThicknesses()
    {
        var tools = new CatalogTools(SeededCatalog());

        var result = await SearchOne(tools, "ring frame spindle tape", "thicker");

        result.Outcome.Should().Be("ambiguous");
        result.ResolvedSku.Should().BeNull();
        result.Candidates.Should().OnlyContain(c => c.Sku.StartsWith("SPT-RF-"));
    }

    [Fact]
    public async Task SearchCatalogAsync_NeverReturnsMoreThanFiveCandidates_EvenForABareFamilyWord()
    {
        var tools = new CatalogTools(SeededCatalog());

        foreach (var vague in new[] { "bearing", "belt", "gear", "spindle tape" })
        {
            var result = await SearchOne(tools, vague);
            result.Candidates.Count.Should().BeLessThanOrEqualTo(5, "'{0}' is too generic to answer precisely", vague);
        }
    }

    [Fact]
    public async Task SearchCatalogAsync_AStrayHintWordDoesNotDragTheRightAnswerBelowResolved()
    {
        var tools = new CatalogTools(SeededCatalog());

        // "same as last time" tokenises to stop-words plus nothing distinguishing; earlier this
        // pushed a perfect match under the threshold. It must not.
        var withJunk = await SearchOne(tools, "6203 2RS bearing", "same as last time");
        var clean = await SearchOne(tools, "6203 2RS bearing");

        withJunk.Outcome.Should().Be("resolved");
        withJunk.ResolvedSku.Should().Be("BRG-6203-2RS");
        withJunk.ResolvedSku.Should().Be(clean.ResolvedSku);
    }

    [Fact]
    public async Task SearchCatalogAsync_HinglishPhrasing_StillResolves()
    {
        var tools = new CatalogTools(SeededCatalog());

        var result = await SearchOne(tools, "6210 ZZ bearing ka rate bhejo");

        result.Outcome.Should().Be("resolved");
        result.ResolvedSku.Should().Be("BRG-6210-ZZ");
    }

    [Fact]
    public async Task SearchCatalogAsync_FullySpecifiedGear_ResolvesToOneSku()
    {
        var tools = new CatalogTools(SeededCatalog());

        var result = await SearchOne(tools, "module 3 spur gear 36T");

        result.Outcome.Should().Be("resolved");
        result.ResolvedSku.Should().Be("GEAR-M3-36T");
    }

    [Fact]
    public async Task SearchCatalogAsync_NoMatch_ReturnsNotFound()
    {
        var tools = new CatalogTools(SeededCatalog());

        var result = await SearchOne(tools, "hydraulic pump seal kit");

        result.Outcome.Should().Be("not_found");
        result.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchCatalogAsync_MultipleQueriesInOneCall_ReturnsOneResultPerQueryInOrder()
    {
        var tools = new CatalogTools(SeededCatalog());

        var results = await tools.SearchCatalogAsync(
            [Q("6203 2RS bearing"), Q("hydraulic widget"), Q("25mm PU timing belt", "PU")],
            CancellationToken.None);

        results.Should().HaveCount(3);
        results[0].Query.Should().Be("6203 2RS bearing");
        results[0].Outcome.Should().Be("resolved");
        results[1].Outcome.Should().Be("not_found");
        results[2].ResolvedSku.Should().Be("BELT-PU-25MM");
    }

    [Fact]
    public async Task SearchCatalogAsync_EveryCandidateCarriesAConfidenceInRange()
    {
        var tools = new CatalogTools(SeededCatalog());

        var result = await SearchOne(tools, "6205 bearing");

        result.Candidates.Should().OnlyContain(c => c.Confidence >= 0 && c.Confidence <= 1);
    }

    // ── verify_catalogue_term ────────────────────────────────────────────────

    [Fact]
    public async Task VerifyCatalogueTermAsync_KnownTerm_ReturnsKnownWithItsFamily()
    {
        var tools = new CatalogTools(SeededCatalog());

        var check = await tools.VerifyCatalogueTermAsync("spindle tape", CancellationToken.None);

        check.Term.Should().Be("spindle tape");
        check.Known.Should().BeTrue();
        check.Families.Should().Equal("SpindleTapes");
        check.ExampleNames.Should().NotBeEmpty().And.OnlyContain(n => n.Contains("Spindle Tape"));
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_UnknownTerm_ReturnsNotKnownAndNothingElse()
    {
        var tools = new CatalogTools(SeededCatalog());

        var check = await tools.VerifyCatalogueTermAsync("hydraulic seal", CancellationToken.None);

        check.Known.Should().BeFalse();
        check.Families.Should().BeEmpty();
        check.ExampleNames.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VerifyCatalogueTermAsync_EmptyOrWhitespace_ReturnsNotKnown(string term)
    {
        var tools = new CatalogTools(SeededCatalog());

        var check = await tools.VerifyCatalogueTermAsync(term, CancellationToken.None);

        check.Known.Should().BeFalse();
        check.Families.Should().BeEmpty();
        check.ExampleNames.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_OnlyStopWords_ReturnsNotKnown()
    {
        var tools = new CatalogTools(SeededCatalog());

        var check = await tools.VerifyCatalogueTermAsync("the same", CancellationToken.None);

        check.Known.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_WordOnlyInsideAnotherWord_IsNotKnown()
    {
        var tools = new CatalogTools(SeededCatalog());

        // "ring" is a substring of "bearing" but not a whole word of any bearing — the exact bug the
        // two-stage ranker fixed for search_catalog. On its own it matches only "Ring Frame" tapes.
        var check = await tools.VerifyCatalogueTermAsync("ring", CancellationToken.None);

        check.Known.Should().BeTrue();
        check.Families.Should().Equal("SpindleTapes");

        var nonsense = await tools.VerifyCatalogueTermAsync("earin", CancellationToken.None);
        nonsense.Known.Should().BeFalse("'earin' appears only inside 'bearing', never as a word");
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_BroadTerm_CapsExampleNamesAtThree()
    {
        var tools = new CatalogTools(SeededCatalog());

        var check = await tools.VerifyCatalogueTermAsync("bearing", CancellationToken.None);

        check.Known.Should().BeTrue();
        check.ExampleNames.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("tming belt", "timing belt", "Belts")]
    [InlineData("spindel tap", "spindle tape", "SpindleTapes")]
    [InlineData("bearng", "bearing", "Bearings")]
    public async Task VerifyCatalogueTermAsync_Misspelling_IsNotKnownButSuggestsTheClosestCatalogueWords(
        string misspelt, string expectedSuggestion, string expectedFamily)
    {
        var tools = new CatalogTools(SeededCatalog());

        var check = await tools.VerifyCatalogueTermAsync(misspelt, CancellationToken.None);

        check.Known.Should().BeFalse("the term as written is not in the catalogue");
        check.Suggestions.Should().Contain(expectedSuggestion);
        check.Families.Should().Contain(expectedFamily);
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_IllegiblePvBelt_SuggestsPuBeltAmongTheReadings()
    {
        var tools = new CatalogTools(SeededCatalog());

        // The crafted demo photo: "PU" handwritten so it could pass for "PV".
        var check = await tools.VerifyCatalogueTermAsync("PV belt", CancellationToken.None);

        check.Known.Should().BeFalse();
        check.Suggestions.Should().Contain("pu belt");
        check.Suggestions.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_KnownTerm_HasNoSuggestions()
    {
        var tools = new CatalogTools(SeededCatalog());

        var check = await tools.VerifyCatalogueTermAsync("timing belt", CancellationToken.None);

        check.Known.Should().BeTrue();
        check.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_WrongPartNumber_IsNeverCorrectedToANeighbouringNumber()
    {
        var tools = new CatalogTools(SeededCatalog());

        // "6211" is one edit from "6201"/"6210", but a number is a spec: suggesting a neighbour would
        // nudge the model towards a different part. Numbers are never corrected.
        var check = await tools.VerifyCatalogueTermAsync("6211 bearing", CancellationToken.None);

        check.Known.Should().BeFalse();
        check.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_UnrelatedWord_HasNoSuggestions()
    {
        var tools = new CatalogTools(SeededCatalog());

        var check = await tools.VerifyCatalogueTermAsync("hydraulic seal", CancellationToken.None);

        check.Suggestions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("SpindleTapes", "SpindleTapes")]
    [InlineData("Gears", "Gears")]
    public async Task VerifyCatalogueTermAsync_FamilyName_IsKnown(string term, string family)
    {
        var tools = new CatalogTools(SeededCatalog());

        // Recall used to search only SKU and name, so a family name that appears in neither was
        // reported unknown (foundry-03 code review).
        var check = await tools.VerifyCatalogueTermAsync(term, CancellationToken.None);

        check.Known.Should().BeTrue();
        check.Families.Should().Equal(family);
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_Suggestions_NeverContainASkuOnlyCode()
    {
        var tools = new CatalogTools(SeededCatalog());

        // "VBLX" is one edit from the SKU code "VBLT", which appears in no item name — a suggestion
        // built from SKU fragments would leak part numbers to Intake.
        var check = await tools.VerifyCatalogueTermAsync("vblx", CancellationToken.None);

        check.Suggestions.Should().NotContain(s => s.Contains("vblt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_LongNearMissTerm_ReturnsPromptlyWithNoSuggestions()
    {
        var tools = new CatalogTools(SeededCatalog());

        // Twenty distinct words, each within the edit budget of three catalogue words ("RING",
        // "TIMING", "ROVING"...). Building every combination is 3^20 readings — the tool must refuse
        // to try rather than spin, since the term comes from a model reading customer text.
        const string term =
            "sering raring rering reaing taring tering eiring eoring biring boring " +
            "riming rtming raming reming toring troing trving riding rising riving";

        // Task.Run so a runaway synchronous loop still lets the timeout fire instead of hanging the run.
        var check = await Task.Run(() => tools.VerifyCatalogueTermAsync(term, CancellationToken.None))
            .WaitAsync(TimeSpan.FromSeconds(10));

        check.Known.Should().BeFalse();
        check.Suggestions.Should().BeEmpty();
        check.Families.Should().BeEmpty();
        check.ExampleNames.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_TermLongerThanSixtyFourCharacters_HasNoSuggestions()
    {
        var tools = new CatalogTools(SeededCatalog());

        // Only two meaningful words ("tming belt"), which on its own suggests "timing belt" — but a
        // term this long is not "one unclear word", so no spelling search is attempted.
        const string term = "please send the quote for the tming belt, urgent, as usual, asap, please send";
        term.Length.Should().BeGreaterThan(64);

        var check = await tools.VerifyCatalogueTermAsync(term, CancellationToken.None);

        check.Known.Should().BeFalse();
        check.Suggestions.Should().BeEmpty();
        check.Families.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_CancelledToken_Throws()
    {
        var tools = new CatalogTools(SeededCatalog());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await tools.VerifyCatalogueTermAsync("tming belt", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_ManyCloserSpellingsThatDoNotFit_StillSuggestsTheOneThatDoes()
    {
        // "PV" is one edit from PA–PG (alphabetically first; "PC" is skipped, it is a stop word) and from PU. Only "PU" appears with "belt"
        // in any item, so a per-word cut to the first five spellings must not drop it (code review).
        var catalog = new FakeCatalogRepository();
        var id = 0;
        foreach (var code in new[] { "PA", "PB", "PD", "PE", "PF", "PG" })
        {
            catalog.Items.Add(new CatalogItemRecord(++id, $"BRG-{code}", $"{code} Bearing", "Bearings", "Nos", 100m, 70m, null));
        }

        catalog.Items.Add(new CatalogItemRecord(++id, "BELT-PU-25MM", "25mm PU Timing Belt", "Belts", "Mtr", 30m, 21m, null));

        var check = await new CatalogTools(catalog).VerifyCatalogueTermAsync("PV belt", CancellationToken.None);

        check.Suggestions.Should().Contain("pu belt");
    }

    [Fact]
    public async Task VerifyCatalogueTermAsync_NullTerm_Throws()
    {
        var tools = new CatalogTools(SeededCatalog());

        var act = async () => await tools.VerifyCatalogueTermAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static CatalogSearchQuery Q(string query, params string[] hints) =>
        new() { Query = query, Hints = hints };

    private static async Task<CatalogSearchResult> SearchOne(CatalogTools tools, string query, params string[] hints)
    {
        var results = await tools.SearchCatalogAsync([Q(query, hints)], CancellationToken.None);
        return results.Should().ContainSingle().Subject;
    }

    /// <summary>The four seeded families, on the same 2-axis grid as <c>DeterministicSeeder</c>.</summary>
    private static FakeCatalogRepository SeededCatalog()
    {
        var catalog = new FakeCatalogRepository();
        var id = 0;

        string[] series =
        [
            "6200", "6201", "6202", "6203", "6204", "6205", "6206", "6207", "6208", "6209", "6210",
            "6300", "6301", "6302", "6303", "6304", "6305", "6306", "6307", "6308",
        ];
        string[] suffixes = ["2RS", "ZZ", "RS", "2Z"];
        foreach (var s in series)
        {
            foreach (var suffix in suffixes)
            {
                catalog.Items.Add(new CatalogItemRecord(
                    ++id, $"BRG-{s}-{suffix}", $"{s} Series Ball Bearing ({suffix})", "Bearings", "Nos", 100m, 70m, null));
            }
        }

        string[] widths = ["10", "15", "20", "25", "30", "35", "40", "45", "50"];
        (string Name, string Code)[] beltTypes =
        [
            ("PU Timing Belt", "PU"), ("Rubber V-Belt", "VBLT"), ("Flat Belt", "FLAT"),
            ("Rubber Timing Belt", "RTB"), ("Cogged V-Belt", "CVB"),
        ];
        foreach (var w in widths)
        {
            foreach (var (name, code) in beltTypes)
            {
                catalog.Items.Add(new CatalogItemRecord(
                    ++id, $"BELT-{code}-{w}MM", $"{w}mm {name}", "Belts", "Mtr", 30m, 21m, null));
            }
        }

        (string App, string Code)[] tapeApps =
            [("Ring Frame", "RF"), ("Simplex", "SPX"), ("Doubling Frame", "DF"), ("Roving Frame", "RVF")];
        string[] thicknesses = ["4mm", "5mm", "6mm", "7mm", "8mm", "9mm", "10mm", "11mm"];
        foreach (var (app, code) in tapeApps)
        {
            foreach (var t in thicknesses)
            {
                catalog.Items.Add(new CatalogItemRecord(
                    ++id, $"SPT-{code}-{t.ToUpperInvariant()}", $"{app} Spindle Tape", "SpindleTapes", "Mtr", 20m, 14m, t));
            }
        }

        string[] modules = ["1", "1.5", "2", "2.5", "3", "3.5", "4", "5", "6", "8"];
        string[] teeth = ["18T", "20T", "24T", "28T", "30T", "36T", "40T", "44T", "48T", "54T"];
        foreach (var m in modules)
        {
            foreach (var teethCount in teeth)
            {
                catalog.Items.Add(new CatalogItemRecord(
                    ++id, $"GEAR-M{m}-{teethCount}", $"Module {m} Spur Gear ({teethCount})", "Gears", "Nos", 100m, 70m, null));
            }
        }

        return catalog;
    }
}
