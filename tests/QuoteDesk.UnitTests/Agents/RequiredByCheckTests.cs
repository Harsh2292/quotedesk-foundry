using FluentAssertions;
using QuoteDesk.Agents.Pipeline;
using QuoteDesk.Agents.Tools.Results;
using Xunit;

namespace QuoteDesk.UnitTests.Agents;

public class RequiredByCheckTests
{
    private static readonly DateOnly ReceivedOn = new(2026, 9, 22);

    private static PricedQuoteLine Line(string sku, DateOnly? delivery) => new()
    {
        Sku = sku,
        Quantity = 1,
        ListPrice = 100m,
        DiscountPct = 0m,
        SlabDiscountPct = 0m,
        TierDiscountPct = 0m,
        DiscountCapped = false,
        NetUnitPrice = 100m,
        LineTotal = 100m,
        RequiresOverride = false,
        DeliveryDate = delivery,
    };

    [Fact]
    public void Warnings_NoRequestedDate_ReturnsNone()
    {
        RequiredByCheck.Warnings([Line("A", new DateOnly(2026, 12, 1))], requiredBy: null, ReceivedOn)
            .Should().BeEmpty();
    }

    [Fact]
    public void Warnings_DeliveryAfterRequestedDate_FlagsThatLineOnly()
    {
        var warnings = RequiredByCheck.Warnings(
            [Line("ON-TIME", new DateOnly(2026, 10, 3)), Line("LATE", new DateOnly(2026, 10, 6))],
            new DateOnly(2026, 10, 5), ReceivedOn);

        warnings.Should().ContainSingle().Which.Should().Contain("LATE").And.Contain("2026-10-06").And.Contain("2026-10-05");
    }

    [Fact]
    public void Warnings_DeliveryExactlyOnRequestedDate_IsNotLate()
    {
        RequiredByCheck.Warnings([Line("A", new DateOnly(2026, 10, 5))], new DateOnly(2026, 10, 5), ReceivedOn)
            .Should().BeEmpty();
    }

    [Fact]
    public void Warnings_LineWithNoDeliveryDate_IsNotFlagged()
    {
        RequiredByCheck.Warnings([Line("NO-STOCK-RECORD", null)], new DateOnly(2026, 10, 5), ReceivedOn)
            .Should().BeEmpty();
    }

    [Fact]
    public void Warnings_RequestedDateBeforeReceipt_OneCheckTheDateWarningNotOnePerLine()
    {
        var warnings = RequiredByCheck.Warnings(
            [Line("A", new DateOnly(2026, 9, 24)), Line("B", new DateOnly(2026, 10, 2))],
            new DateOnly(2026, 9, 5), ReceivedOn);

        warnings.Should().ContainSingle().Which.Should().Contain("before the enquiry arrived");
    }

    [Fact]
    public void Warnings_RequestedDateIsTheReceivedDay_IsNotTreatedAsPast()
    {
        var warnings = RequiredByCheck.Warnings([Line("A", new DateOnly(2026, 9, 23))], ReceivedOn, ReceivedOn);

        warnings.Should().ContainSingle().Which.Should().Contain("after the requested");
    }

    [Fact]
    public void Warnings_NoLines_ReturnsNone()
    {
        RequiredByCheck.Warnings([], new DateOnly(2026, 10, 5), ReceivedOn).Should().BeEmpty();
    }

    [Fact]
    public void ReceivedOn_LateEveningUtc_IsTheNextDayInIndia()
    {
        // 20:00 UTC on the 22nd is 01:30 IST on the 23rd.
        RequiredByCheck.ReceivedOn(new DateTimeOffset(2026, 9, 22, 20, 0, 0, TimeSpan.Zero))
            .Should().Be(new DateOnly(2026, 9, 23));
    }
}
