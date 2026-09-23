using FluentAssertions;
using QuoteDesk.Agents.Pipeline;
using Xunit;

namespace QuoteDesk.UnitTests.Agents;

/// <summary>Live run 6012 (2026-09-23) headed an unknown sender's approval card "Shreeji Textiles" —
/// the company from Narrate's own worked example — because the customer name it was handed was empty.</summary>
public class NarrationCustomerNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NarrationCustomerName_NoCustomerMatched_SaysSoInsteadOfLeavingItEmpty(string? resolved)
    {
        var name = PriceExecutor.NarrationCustomerName(resolved);

        name.Should().NotBeNullOrWhiteSpace();
        name.Should().Contain("UNKNOWN");
        name.Should().Contain("never a company name");
    }

    [Fact]
    public void NarrationCustomerName_CustomerMatched_IsPassedThroughUnchanged()
    {
        PriceExecutor.NarrationCustomerName("Shreeji Textiles").Should().Be("Shreeji Textiles");
    }
}
