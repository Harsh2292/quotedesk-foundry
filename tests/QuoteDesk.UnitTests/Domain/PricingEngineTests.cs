using FluentAssertions;
using QuoteDesk.Domain;
using Xunit;

namespace QuoteDesk.UnitTests.Domain;

public class PricingEngineTests
{
    [Fact]
    public void PriceLine_UnknownCustomer_AppliesSlabButNotTierDiscount()
    {
        var request = new PricingLineRequest
        {
            Sku = "BRG-6203-2RS",
            Quantity = 250,
            ListPrice = 250.00m,
            CostPrice = 197.80m,
        };

        var line = PricingEngine.PriceLine(request, tier: null);

        // 250 units crosses the 200+ slab (6%); no tier discount because there is no customer match.
        line.DiscountPct.Should().Be(0.06m);
        line.NetUnitPrice.Should().Be(235.00m);
    }

    [Fact]
    public void PriceLine_QuantityZero_LineTotalIsZero()
    {
        var request = new PricingLineRequest
        {
            Sku = "BRG-6203-2RS",
            Quantity = 0,
            ListPrice = 250.00m,
            CostPrice = 197.80m,
        };

        var line = PricingEngine.PriceLine(request, CustomerTier.B);

        line.LineTotal.Should().Be(0m);
    }

    [Fact]
    public void PriceLine_DiscountWouldBreachMarginFloor_FlagsRequiresOverrideWithShortfall()
    {
        // Slab 6% (200+) + tier A 4% = 10% off a line with only a 10% list-to-cost spread —
        // the net margin lands at exactly 0%, ten points under the floor.
        var request = new PricingLineRequest
        {
            Sku = "GEAR-90",
            Quantity = 200,
            ListPrice = 100.00m,
            CostPrice = 90.00m,
        };

        var line = PricingEngine.PriceLine(request, CustomerTier.A);

        line.MarginPct.Should().Be(0.00m);
        line.RequiresOverride.Should().BeTrue();
        line.MarginShortfallPct.Should().Be(0.10m);
    }

    [Fact]
    public void PriceLine_CombinedDiscountWouldExceedCap_ClampsAtMaxCombinedDiscountPct()
    {
        var request = new PricingLineRequest
        {
            Sku = "SPINDLE-TAPE-8MM",
            Quantity = 1_000,
            ListPrice = 100.00m,
            CostPrice = 50.00m,
            Slabs = [new QuantitySlab(1, 0.12m)],
        };

        // 12% slab + 4% tier A would be 16%, but the combined cap is 15%.
        var line = PricingEngine.PriceLine(request, CustomerTier.A);

        line.DiscountPct.Should().Be(PricingEngine.MaxCombinedDiscountPct);
    }

    /// <summary>
    /// The slab and tier components are reported separately (task foundry-05) so the narration can
    /// name the rule behind a discount instead of working it out. That makes them part of the
    /// contract, not a convenience: if the engine stopped populating them, the explanation would
    /// quietly go back to being the model's own arithmetic.
    /// </summary>
    [Fact]
    public void PriceLine_ReportsTheSlabAndTierComponentsSeparately()
    {
        var request = new PricingLineRequest
        {
            Sku = "BRG-6203-2RS",
            Quantity = 250,
            ListPrice = 250.00m,
            CostPrice = 197.80m,
        };

        var line = PricingEngine.PriceLine(request, CustomerTier.B);

        // docs/DOMAIN.md's worked example: the 200+ slab is 6%, tier B adds 2%, total 8%.
        line.SlabDiscountPct.Should().Be(0.06m);
        line.TierDiscountPct.Should().Be(0.02m);
        line.DiscountPct.Should().Be(0.08m);
        line.DiscountCapped.Should().BeFalse();
    }

    [Fact]
    public void PriceLine_UnknownCustomer_ReportsZeroTierComponent()
    {
        var request = new PricingLineRequest
        {
            Sku = "BRG-6203-2RS",
            Quantity = 250,
            ListPrice = 250.00m,
            CostPrice = 197.80m,
        };

        var line = PricingEngine.PriceLine(request, tier: null);

        line.SlabDiscountPct.Should().Be(0.06m);
        line.TierDiscountPct.Should().Be(0m);
        line.DiscountCapped.Should().BeFalse();
    }

    [Fact]
    public void PriceLine_CombinedDiscountExceedsCap_ReportsTheUncappedComponentsAndFlagsTheCap()
    {
        var request = new PricingLineRequest
        {
            Sku = "SPINDLE-TAPE-8MM",
            Quantity = 1_000,
            ListPrice = 100.00m,
            CostPrice = 50.00m,
            Slabs = [new QuantitySlab(1, 0.12m)],
        };

        var line = PricingEngine.PriceLine(request, CustomerTier.A);

        // The components are what the rules produced (12% + 4% = 16%); DiscountPct is what was
        // actually applied. Both are true, and the narration needs to be able to tell them apart.
        line.SlabDiscountPct.Should().Be(0.12m);
        line.TierDiscountPct.Should().Be(0.04m);
        line.DiscountPct.Should().Be(0.15m);
        line.DiscountCapped.Should().BeTrue();
    }

    /// <summary>Exactly at the cap is not capped — the rule is "never exceeds 15%", so a sum landing
    /// precisely on it was applied in full and must not be reported as reduced.</summary>
    [Fact]
    public void PriceLine_CombinedDiscountExactlyAtCap_IsNotFlaggedAsCapped()
    {
        var request = new PricingLineRequest
        {
            Sku = "SPINDLE-TAPE-8MM",
            Quantity = 1_000,
            ListPrice = 100.00m,
            CostPrice = 50.00m,
            Slabs = [new QuantitySlab(1, 0.11m)],
        };

        var line = PricingEngine.PriceLine(request, CustomerTier.A);

        line.DiscountPct.Should().Be(0.15m);
        line.DiscountCapped.Should().BeFalse();
    }
}
