using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using QuoteDesk.Agents.Prompts;
using QuoteDesk.Domain;

namespace QuoteDesk.UnitTests.Agents;

/// <summary>
/// The quotation policy (task foundry-05) is Narrate's one knowledge source, and the only document in
/// this system that restates rules the code already encodes. That makes drift the whole risk: a slab
/// rate changed in <see cref="SlabDiscountPolicy"/> and not in the document would have the narration
/// confidently cite a rule the pricing no longer follows — worse than citing nothing, because it
/// reads as authoritative.
///
/// So these tests read the numbers back out of the shipped Markdown and assert them against
/// QuoteDesk.Domain's actual constants, rather than eyeballing that the file "matches docs/DOMAIN.md".
/// A change to either side fails here until both are changed together.
/// </summary>
public class QuotationPolicyGroundingTests
{
    private static readonly string Policy = new PromptLibrary().QuotationPolicy;

    /// <summary>
    /// The policy with every run of whitespace collapsed to one space. Prose in the document is hard
    /// wrapped, so a sentence a test cares about routinely straddles a line break — asserting against
    /// the raw text would make these tests fail on a reflow that changed nothing, and would tempt a
    /// future reader to reflow the document to suit the tests instead. Table rows are asserted
    /// against the raw text, since a Markdown row is a single line by definition.
    /// </summary>
    private static readonly string PolicyProse = Regex.Replace(Policy, @"\s+", " ");

    [Fact]
    public void Policy_IsVersioned()
    {
        // A knowledge source a model cites has to be identifiable — "which version said that?" is a
        // question the submission document answers, so the line must exist and be findable.
        Policy.Should().MatchRegex(@"\*\*Version v\d+ — \d{4}-\d{2}-\d{2}\.\*\*");
    }

    [Fact]
    public void Policy_SlabLadder_MatchesTheDomainDefaultLadder()
    {
        foreach (var slab in SlabDiscountPolicy.DefaultLadder)
        {
            var quantityWords = slab.MinQty == 1 ? "1 or more" : $"{slab.MinQty} or more";
            var row = $"| {quantityWords} | {AsPercent(slab.DiscountPct)} |";

            Policy.Should().Contain(row, $"the policy document must state the {slab.MinQty}+ slab exactly as SlabDiscountPolicy computes it");
        }

        // And nothing extra: a rung in the document that the code does not have is drift in the other
        // direction, and would be cited just as confidently.
        SlabRowsInPolicy().Should().HaveCount(SlabDiscountPolicy.DefaultLadder.Count);
    }

    [Theory]
    [InlineData(CustomerTier.A)]
    [InlineData(CustomerTier.B)]
    [InlineData(CustomerTier.C)]
    public void Policy_TierDiscount_MatchesTheDomainTierPolicy(CustomerTier tier)
    {
        var expected = $"| {tier} | {AsPercent(TierDiscountPolicy.ResolveDiscountPct(tier))} |";

        Policy.Should().Contain(expected);
    }

    [Fact]
    public void Policy_CombinedCap_MatchesPricingEngine()
    {
        Policy.Should().Contain($"**{AsPercent(PricingEngine.MaxCombinedDiscountPct)}**");
    }

    [Fact]
    public void Policy_MarginFloor_MatchesMarginFloorPolicy()
    {
        Policy.Should().Contain($"**{AsPercent(MarginFloorPolicy.FloorPct)}**");
    }

    [Fact]
    public void Policy_GstRate_MatchesQuoteTotalsCalculator()
    {
        Policy.Should().Contain($"**GST at {AsPercent(QuoteTotalsCalculator.GstRatePct)}**");
    }

    [Theory]
    [InlineData(FreightZone.Local)]
    [InlineData(FreightZone.Regional)]
    [InlineData(FreightZone.National)]
    public void Policy_FreightZone_MatchesFreightPolicy(FreightZone zone)
    {
        var config = FreightPolicy.Zones[zone];
        var transit = config.TransitDays == 1 ? "1 day" : $"{config.TransitDays} days";
        var row = $"| {zone} | ₹{config.FlatFee.ToString("#,0", CultureInfo.InvariantCulture)} | {transit} |";

        Policy.Should().Contain(row);
    }

    [Fact]
    public void Policy_FreightWaiverThreshold_MatchesFreightPolicy()
    {
        var threshold = FreightPolicy.WaiverThreshold.ToString("#,0", CultureInfo.InvariantCulture);

        PolicyProse.Should().Contain($"above ₹{threshold}");
        // The boundary is the part a human gets wrong: exactly at the threshold still pays.
        PolicyProse.Should().Contain($"exactly ₹{threshold} still pays freight");
    }

    [Fact]
    public void Policy_ForbidsComputingFromItself()
    {
        // The document travels into the model's context, so the "cite, never compute" instruction has
        // to live in the document itself — not only in narrate.md, which a future edit could separate
        // it from.
        PolicyProse.Should().Contain("never a second source of arithmetic");
        PolicyProse.Should().Contain("never add two percentages together");
    }

    [Fact]
    public void Policy_StatesTheUnknownSenderRule()
    {
        PolicyProse.Should().Contain("no tier discount and no credit terms");
    }

    private static MatchCollection SlabRowsInPolicy() =>
        // `\r?$` rather than a bare `$`: in .NET's multiline mode `$` matches before `\n`, so a CRLF
        // checkout would leave a `\r` between the closing pipe and the anchor and this would silently
        // match nothing. `.gitattributes` normalises to LF today, but a test that only passes because
        // of a setting in another file is a trap for whoever changes that setting (code review,
        // 2026-09-18).
        Regex.Matches(Policy, @"^\| \d+ or more \| \d+% \|\r?$", RegexOptions.Multiline);

    /// <summary>A domain fraction (0.06m) as the document writes it ("6%"). Whole percentages only —
    /// every rate in QuoteDesk.Domain is one, and a fractional rate should fail loudly here rather
    /// than be silently rendered as something the document does not contain.</summary>
    private static string AsPercent(decimal fraction)
    {
        var percent = fraction * 100m;
        percent.Should().Be(decimal.Truncate(percent), "the policy document writes whole percentages");

        return $"{decimal.Truncate(percent).ToString(CultureInfo.InvariantCulture)}%";
    }
}
