using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace QuoteDesk.Agents.Pipeline;

/// <summary>
/// The Intake stage — the perceiving agent (task foundry-03, docs/FOUNDRY-PLAN.md §4b). It reads the
/// enquiry and returns structure: line items, ship-to, required-by, commercial asks. The enquiry text
/// is wrapped in <see cref="UntrustedContent"/> before it ever reaches the model.
///
/// Unlike the single model call it replaced (the old Extract stage), Intake is an agent with one tool,
/// <c>verify_catalogue_term</c>, which it may call when a word is unclear or illegible. That tool
/// confirms a word exists in the catalogue; it never identifies a part — deciding which item a line
/// means stays with <see cref="ResolveExecutor"/>. Two agents, split by cognitive role: Intake
/// perceives, Resolve decides.
///
/// Built the same way as <see cref="ResolveExecutor"/>: the agent and its tool wrappers are created
/// inside <see cref="HandleAsync"/>, because <see cref="TracedAIFunction"/> needs this invocation's
/// live <see cref="IWorkflowContext"/> to emit trace events. The <see cref="ToolCallBudget"/> is
/// passed in and shared with Resolve, so <c>Llm:MaxToolCalls</c> caps the whole run.
/// </summary>
public sealed class IntakeExecutor(
    string id,
    IChatClient baseChatClient,
    string model,
    IReadOnlyList<AIFunction> tools,
    string instructions,
    ReasoningOptions? reasoning,
    int maxToolCalls,
    ToolCallBudget budget,
    ILogger logger)
    : Executor<EnquiryInput, ExtractionResult>(id, options: null, declareCrossRunShareable: false)
{
    public override async ValueTask<ExtractionResult> HandleAsync(
        EnquiryInput message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await context.AddEventAsync(
            new AgentTraceEvent(new StageEvent { Stage = "intake", At = DateTimeOffset.UtcNow, Model = model }), cancellationToken);

        ValueTask Emit(AgentEvent evt, CancellationToken ct) => context.AddEventAsync(new AgentTraceEvent(evt), ct);
        var tracedTools = tools.Select(t => (AITool)new TracedAIFunction(t, budget, Emit)).ToList();

        var chatClient = new ChatClientBuilder(baseChatClient)
            .UseFunctionInvocation(configure: c => c.MaximumIterationsPerRequest = maxToolCalls)
            .Build();

        var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "Intake",
            ChatOptions = new ChatOptions { Instructions = instructions, Tools = tracedTools, Reasoning = reasoning },
        });

        // Schema-enforced output is off for the same reason as Resolve: this stage can now call a tool,
        // and a strict response format would apply to the tool-call turns too, not just the final JSON.
        // StructuredModelCall's retry-with-the-parse-error-fed-back still guards the final shape.
        var prompt = UntrustedContent.Wrap(message.RawBody);
        var extracted = await StructuredModelCall.RunAsync<ExtractedEnquiry>(
            agent, prompt, useSchema: false, logger, cancellationToken);

        return new ExtractionResult { Enquiry = message, Extracted = extracted };
    }
}
