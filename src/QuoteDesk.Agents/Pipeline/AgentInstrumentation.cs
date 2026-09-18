using Microsoft.Agents.AI;

namespace QuoteDesk.Agents.Pipeline;

/// <summary>
/// One agent's stable identity, and the OpenTelemetry wrapping that publishes it.
///
/// <para>
/// This exists because of a default that is easy to miss and impossible to spot afterwards: the
/// Microsoft Agent Framework sets the <c>gen_ai.agent.id</c> span attribute from
/// <see cref="AIAgent.Id"/>, and when nothing sets that, every <c>ChatClientAgent</c> instance gets a
/// fresh random one. QuoteDesk builds its agents per run — <c>TracedAIFunction</c> needs the live
/// <c>IWorkflowContext</c>, so they cannot be shared — which would mean every run appeared in Foundry
/// as a brand-new agent that had run exactly once. Nothing would error; the traces would simply never
/// group, and an agent registered in the portal would never match any of them.
/// </para>
///
/// <para>
/// The ids are written <c>name:version</c>, which is the form Foundry's external-agent registration
/// and its trace-based evaluation expect. Bump the version half deliberately when an agent's role
/// changes enough that its old traces should not be pooled with its new ones — that is a judgement
/// about continuity, not a mechanical consequence of editing a prompt.
/// </para>
/// </summary>
public sealed record AgentIdentity(string Id, string Name)
{
    /// <summary>Perceives: reads the enquiry text or photograph, and may check one unclear word
    /// against the catalogue.</summary>
    public static readonly AgentIdentity Intake = new("quotedesk-intake:1", "quotedesk-intake");

    /// <summary>Decides: the one autonomous node, choosing its own lookups over the catalogue and
    /// this customer's history.</summary>
    public static readonly AgentIdentity Resolve = new("quotedesk-resolve:1", "quotedesk-resolve");

    /// <summary>Explains: writes one grounded sentence from numbers QuoteDesk.Domain already
    /// computed. Not registered in the portal — it makes no decision to evaluate — but it is given an
    /// id anyway so its spans are attributable rather than anonymous.</summary>
    public static readonly AgentIdentity Narrate = new("quotedesk-narrate:1", "quotedesk-narrate");
}

/// <summary>
/// The single place an agent is wrapped for tracing, so the "do not instrument twice" rule below has
/// one place to be true rather than three.
/// </summary>
public static class AgentInstrumentation
{
    /// <summary>The <c>ActivitySource</c> name every QuoteDesk agent span is published under. The Api
    /// subscribes to exactly this name when an Azure Monitor connection string is configured; nothing
    /// is exported when it is not.</summary>
    public const string ActivitySourceName = "QuoteDesk.Agents";

    /// <summary>
    /// Wraps an agent so its runs emit OpenTelemetry spans under <see cref="ActivitySourceName"/>.
    ///
    /// <para>
    /// <paramref name="enableSensitiveData"/> controls whether the spans carry the prompts and replies
    /// themselves. Foundry's quality evaluators read exactly those attributes and score <c>None</c>
    /// without them, so trace-based evaluation needs it on — at the price of enquiry text landing in
    /// Application Insights. See <c>LlmOptions.TraceSensitiveData</c>.
    /// </para>
    ///
    /// <para>
    /// Deliberately the <i>only</i> instrumentation applied: <c>OpenTelemetryAgent</c> auto-wires the
    /// inner chat client's telemetry as well (confirmed in the installed 1.19.0 XML docs — its
    /// <c>autoWireChatClient</c> parameter defaults on and skips a client that is already
    /// instrumented). Adding <c>UseOpenTelemetry</c> to <c>ChatClientRegistry</c>'s clients too would
    /// be the obvious-looking way to "make sure" model calls are traced, and would be wrong.
    /// </para>
    /// </summary>
    public static AIAgent Instrument(AIAgent agent, bool enableSensitiveData)
    {
        ArgumentNullException.ThrowIfNull(agent);

        return agent
            .AsBuilder()
            .UseOpenTelemetry(ActivitySourceName, a => a.EnableSensitiveData = enableSensitiveData)
            .Build();
    }
}
