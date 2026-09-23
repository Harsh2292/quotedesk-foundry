using FluentAssertions;
using QuoteDesk.Domain;
using Xunit;

namespace QuoteDesk.UnitTests.Domain;

public class QuoteTotalsCalculatorTests
{
    private static PricedLine Line(decimal lineTotal) => new()
    {
        Sku = "TEST-SKU",
        Quantity = 1,
        ListPrice = lineTotal,
        DiscountPct = 0m,
        SlabDiscountPct = 0m,
        TierDiscountPct = 0m,
        DiscountCapped = false,
        NetUnitPrice = lineTotal,
        LineTotal = lineTotal,
        MarginPct = 0.20m,
        RequiresOverride = false,
        MarginShortfallPct = 0m,
    };

    [Fact]
    public void Calculate_LocalZone_NoFreightAndGstOnLinesOnly()
    {
        var totals = QuoteTotalsCalculator.Calculate([Line(1_000m)], FreightZone.Local);

        totals.Subtotal.Should().Be(1_000m);
        totals.Freight.Should().Be(0m);
        totals.Tax.Should().Be(180m);
        totals.GrandTotal.Should().Be(1_180m);
    }

    [Theory]
    [InlineData(FreightZone.Local)]
    [InlineData(FreightZone.Regional)]
    [InlineData(FreightZone.National)]
    public void Calculate_NoLines_EverythingIsZeroIncludingFreight(FreightZone zone)
    {
        var totals = QuoteTotalsCalculator.Calculate([], zone);

        totals.Subtotal.Should().Be(0m);
        totals.Freight.Should().Be(0m, "a quote with nothing on it has nothing to ship");
        totals.Tax.Should().Be(0m);
        totals.GrandTotal.Should().Be(0m);
    }

    [Fact]
    public void Calculate_OneZeroValueLine_StillChargesFreight()
    {
        // The boundary of the rule: it is about having no lines, not about a zero subtotal.
        var totals = QuoteTotalsCalculator.Calculate([Line(0m)], FreightZone.Regional);

        totals.Freight.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Calculate_RegionalZoneBelowWaiverThreshold_ChargesFreightAndTaxesIt()
    {
        var totals = QuoteTotalsCalculator.Calculate([Line(1_000m)], FreightZone.Regional);

        totals.Freight.Should().Be(450m);
        // GST applies after freight: (1000 + 450) * 18% = 261.
        totals.Tax.Should().Be(261m);
        totals.GrandTotal.Should().Be(1_711m);
    }

    [Fact]
    public void Calculate_AboveWaiverThreshold_FreightIsWaivedRegardlessOfZone()
    {
        var totals = QuoteTotalsCalculator.Calculate([Line(60_000m)], FreightZone.National);

        totals.Freight.Should().Be(0m);
        totals.Tax.Should().Be(10_800m);
        totals.GrandTotal.Should().Be(70_800m);
    }
}
