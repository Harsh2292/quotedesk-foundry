using FluentAssertions;
using QuoteDesk.Agents.Pipeline;

namespace QuoteDesk.UnitTests.Agents;

/// <summary>
/// The run-wide tool-call budget and the Intake agent's capped child of it (task foundry-03 review):
/// Intake's calls count toward the run's total, but Intake can never spend more than its own cap, so a
/// garbled enquiry cannot leave Resolve without the lookups it needs.
/// </summary>
public class ToolCallBudgetTests
{
    [Fact]
    public void TryReserve_ExactlyAtMax_AllowsAndTheNextIsRefused()
    {
        var budget = new ToolCallBudget(2);

        budget.TryReserve().Should().BeTrue();
        budget.TryReserve().Should().BeTrue();
        budget.TryReserve().Should().BeFalse();
    }

    [Fact]
    public void TryReserve_ZeroMax_RefusesTheFirstCall()
    {
        new ToolCallBudget(0).TryReserve().Should().BeFalse();
    }

    [Fact]
    public void TryReserve_ChildAtItsOwnCap_IsRefusedAndLeavesTheParentUntouched()
    {
        var run = new ToolCallBudget(10);
        var intake = new ToolCallBudget(2, parent: run);

        intake.TryReserve().Should().BeTrue();
        intake.TryReserve().Should().BeTrue();
        intake.TryReserve().Should().BeFalse("Intake's own cap is 2, however much the run has left");

        run.Count.Should().Be(2, "a call refused by the child's own cap must not spend the parent's budget");
    }

    [Fact]
    public void TryReserve_ChildCalls_CountTowardTheParent_SoResolveKeepsTheRest()
    {
        var run = new ToolCallBudget(10);
        var intake = new ToolCallBudget(2, parent: run);

        intake.TryReserve();
        intake.TryReserve();
        intake.TryReserve(); // refused by Intake's cap

        var resolveCalls = 0;
        while (run.TryReserve())
        {
            resolveCalls++;
        }

        resolveCalls.Should().Be(8, "Resolve is guaranteed MaxToolCalls - IntakeMaxToolCalls");
    }

    [Fact]
    public void TryReserve_ParentExhausted_RefusesTheChildEvenUnderItsOwnCap()
    {
        var run = new ToolCallBudget(1);
        var intake = new ToolCallBudget(2, parent: run);

        run.TryReserve().Should().BeTrue();

        intake.TryReserve().Should().BeFalse("the run-wide cap still binds the child");
    }
}
