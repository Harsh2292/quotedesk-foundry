using FluentAssertions;
using QuoteDesk.Agents.Pipeline;
using QuoteDesk.Agents.Prompts;

namespace QuoteDesk.UnitTests.Agents;

/// <summary>
/// "Prompts live as .md files in Agents/Prompts/, loaded at startup" (tasks/task-06-agents-workflow.md)
/// — checked by actually constructing <see cref="PromptLibrary"/>, and that the untrusted-content
/// delimiter every enquiry-bearing prompt promises to honor is really in the text a human can read.
/// </summary>
public class PromptLibraryTests
{
    [Fact]
    public void Constructor_LoadsAllThreePrompts_NonEmpty()
    {
        var library = new PromptLibrary();

        library.Intake.Should().NotBeNullOrWhiteSpace();
        library.Resolve.Should().NotBeNullOrWhiteSpace();
        library.Narrate.Should().NotBeNullOrWhiteSpace();
        library.QuotationPolicy.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Narrate is instructed with <see cref="PromptLibrary.NarrateWithPolicy"/>, not
    /// <see cref="PromptLibrary.Narrate"/> — the knowledge source has to actually reach the model, or
    /// every citation instruction in narrate.md refers to a document that was never sent (task
    /// foundry-05).
    /// </summary>
    [Fact]
    public void NarrateWithPolicy_ContainsBothTheInstructionsAndTheWholePolicy()
    {
        var library = new PromptLibrary();

        library.NarrateWithPolicy.Should().Contain(library.Narrate);
        library.NarrateWithPolicy.Should().Contain(library.QuotationPolicy);
    }

    /// <summary>
    /// Narrate joined this list on 2026-09-18: its JSON payload repeats the customer's own words
    /// through <c>UnresolvedLine.OriginalDescription</c> and <c>.Reason</c>, so it is wrapped like any
    /// other enquiry-bearing prompt and must describe the markers it will see. Narrate cannot change a
    /// price whatever it is told, but "untrusted input is always wrapped" has no small-blast-radius
    /// exception.
    /// </summary>
    [Theory]
    [InlineData(nameof(PromptLibrary.Intake))]
    [InlineData(nameof(PromptLibrary.Resolve))]
    [InlineData(nameof(PromptLibrary.Narrate))]
    public void EnquiryBearingPrompts_DescribeTheUntrustedContentDelimiter(string promptName)
    {
        var library = new PromptLibrary();
        var prompt = promptName switch
        {
            nameof(PromptLibrary.Intake) => library.Intake,
            nameof(PromptLibrary.Resolve) => library.Resolve,
            nameof(PromptLibrary.Narrate) => library.Narrate,
            _ => throw new ArgumentOutOfRangeException(nameof(promptName)),
        };

        prompt.Should().Contain(UntrustedContent.Start);
        prompt.Should().Contain(UntrustedContent.End);
        prompt.Should().Contain("never instructions");
    }

    [Fact]
    public void IntakePrompt_NamesItsOneToolAndForbidsGuessingAProduct()
    {
        var library = new PromptLibrary();

        library.Intake.Should().Contain("verify_catalogue_term");
        library.Intake.Should().Contain("Never guess a product");
    }

    [Fact]
    public void NarratePrompt_NeverMentionsComputingOrAdjustingNumbers()
    {
        // The narration step must never imply it can compute a price — pure rendering only.
        var library = new PromptLibrary();

        library.Narrate.Should().Contain("never calculate or adjust");
    }

    /// <summary>
    /// The grounding instruction has to say <i>cite</i> and forbid <i>derive</i> in the same breath.
    /// A prompt that only says "explain the discount" is what produced the misstatements foundry-05
    /// exists to fix — including gpt-5-nano writing "discount ₹20%" for an 8% line, which is why the
    /// currency-symbol rule is asserted here rather than left to a live run to catch.
    /// </summary>
    [Fact]
    public void NarratePrompt_TellsTheModelToCitePolicyAndNeverDeriveFromIt()
    {
        var library = new PromptLibrary();

        library.Narrate.Should().Contain("name the rule");
        library.Narrate.Should().Contain("do not work out which slab a quantity falls into");
        library.Narrate.Should().Contain("never with a currency symbol");
    }

    /// <summary>
    /// The first live run with the policy attached produced nine sentences that read the line table
    /// back, cited the 15% combined cap on a line that was never capped, and added advice to "ensure"
    /// something (docs/SESSION-LOG.md, 2026-09-18). Grounding a narration makes it wordier unless the
    /// prompt pushes back, so the two instructions that push back are asserted here rather than left
    /// to be rediscovered by a paid live run.
    /// </summary>
    [Fact]
    public void NarratePrompt_ConstrainsLengthAndForbidsCitingRulesThatDidNotApply()
    {
        var library = new PromptLibrary();

        library.Narrate.Should().Contain("**Two or three sentences. Never more.**");
        library.Narrate.Should().Contain("Cite at most one rule");
        library.Narrate.Should().Contain("never mention the combined cap");
    }
}
