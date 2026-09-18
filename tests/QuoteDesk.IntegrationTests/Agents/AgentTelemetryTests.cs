using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.AI;
using QuoteDesk.Agents.Pipeline;
using QuoteDesk.Data;
using QuoteDesk.IntegrationTests.Data;

namespace QuoteDesk.IntegrationTests.Agents;

/// <summary>
/// Task foundry-06's central risk, and the reason it gets its own test file: the Microsoft Agent
/// Framework fills the <c>gen_ai.agent.id</c> span attribute from <see cref="AIAgent.Id"/>, and
/// generates a fresh random one per instance when nothing sets it. QuoteDesk builds its agents per
/// run, so the default would give every run a different agent id — and the failure is silent. Nothing
/// throws, the traces still arrive, they simply never group, and an agent registered in the Foundry
/// portal matches none of them. That is exactly the kind of bug you discover the evening before a
/// deadline, staring at an empty Traces tab.
///
/// So it is asserted the way the acceptance criterion words it — by listening to the spans two
/// consecutive runs actually emit and comparing the ids — rather than by reading the id back off the
/// options object, which would pass even if the framework never used it.
/// </summary>
[Collection("Repository")]
public class AgentTelemetryTests(RepositoryFixture fixture)
{
    private const int ShreejiEnquiryId = 1;

    [Fact]
    public async Task TwoConsecutiveRuns_EmitTheSameStableAgentIdsForEveryStage()
    {
        using var listener = new AgentIdSpanListener();

        await RunOnceAsync();
        var afterFirstRun = listener.DistinctAgentIds;

        await RunOnceAsync();
        var afterSecondRun = listener.DistinctAgentIds;

        afterFirstRun.Should().NotBeEmpty("the agents are wrapped for OpenTelemetry, so each run must emit agent spans");

        // Compared as *distinct sets*, not as two positional slices of the captured list. An
        // ActivityListener is process-global and filtered only by source name, and xUnit runs separate
        // collections in parallel — AgentStreamEndpointTests drives the identically-instrumented
        // pipeline from another collection, so a foreign span can land here mid-test. Slicing by
        // position would then misattribute it and fail for a reason unrelated to the thing under test
        // (found in code review, 2026-09-18).
        //
        // A set comparison is immune to exactly that, and for a pleasing reason: a foreign span
        // carries one of these same three ids *because* the ids are stable, which is the property
        // being asserted. If the ids were not stable, a second run would introduce a new distinct id
        // and this fails — which is the real invariant.
        afterSecondRun.Should().BeEquivalentTo(
            afterFirstRun,
            "a second run is the same agents doing the same work — a fresh random id per run is the default this task exists to override");

        // And they are the registered ids, not merely stable: a stable-but-wrong id matches nothing
        // in the portal either.
        afterFirstRun.Should().OnlyContain(id =>
            id == AgentIdentity.Intake.Id || id == AgentIdentity.Resolve.Id || id == AgentIdentity.Narrate.Id);
        afterFirstRun.Should().Contain(AgentIdentity.Intake.Id).And.Contain(AgentIdentity.Resolve.Id);
    }

    /// <summary>
    /// A photographed enquiry's image must never reach a span, even with sensitive-data capture on.
    /// `Microsoft.Extensions.AI` serializes a <c>DataContent</c> part into <c>gen_ai.input.messages</c>
    /// as its full base64 payload, so the otherwise-reasonable "turn on message capture so Foundry's
    /// evaluators can read the prompts" setting would ship a customer's photo into Application
    /// Insights. Asserted on the spans themselves rather than on the flag, because the flag's effect
    /// travels through two framework layers (agent → auto-wired chat client) and the whole point is
    /// what comes out the far end.
    /// </summary>
    [Fact]
    public async Task PhotoEnquiry_WithSensitiveDataOn_EmitsSpansCarryingNoImagePayload()
    {
        // A distinctive payload, so its absence cannot be a coincidence — the same technique the
        // checkpoint test in EnquiryPipelineTests uses.
        var imageBase64 = Convert.ToBase64String(Enumerable.Range(0, 3000).Select(i => (byte)(i % 249)).ToArray());
        var dataUrl = $"data:image/jpeg;base64,{imageBase64}";

        // Hoisted out of the assertion: a range expression is not allowed inside an expression tree.
        var imageFingerprint = imageBase64.Substring(0, 200);

        using var listener = new SpanTagListener();
        await RunOnceAsync(traceSensitiveData: true, imageDataUrl: dataUrl);

        listener.TagValues.Should().NotBeEmpty("the run must still be traced — the fix removes the payload, not the span");
        listener.TagValues.Should().NotContain(
            v => v.Contains(imageFingerprint, StringComparison.Ordinal),
            "a photo's base64 bytes must never reach a span attribute, however sensitive-data capture is configured");
        listener.TagValues.Should().NotContain(
            v => v.Contains("data:image/", StringComparison.OrdinalIgnoreCase),
            "nor may the data URI itself");
    }

    /// <summary>
    /// The control for the test above. Without this, that test would pass just as happily if message
    /// capture were broken altogether, or if the listener were watching the wrong source — and the
    /// reassurance would be worthless. A text enquiry with the same setting on must put the enquiry's
    /// own words into a span, which is the whole reason foundry-07 needs the setting at all.
    /// </summary>
    [Fact]
    public async Task TextEnquiry_WithSensitiveDataOn_DoesCaptureMessageContent()
    {
        using var listener = new SpanTagListener();

        await RunOnceAsync(traceSensitiveData: true);

        listener.TagValues.Should().Contain(
            v => v.Contains("6203", StringComparison.OrdinalIgnoreCase),
            "the seeded Shreeji enquiry names a 6203 bearing, so message capture being on must show it");
    }

    /// <summary>
    /// The other half of the same guarantee: the ids are written <c>name:version</c>, which is the
    /// shape Foundry's external-agent registration and its trace-based evaluation expect. A rename
    /// that quietly dropped the version suffix would register fine and evaluate against nothing.
    /// </summary>
    [Theory]
    [InlineData("quotedesk-intake:1")]
    [InlineData("quotedesk-resolve:1")]
    [InlineData("quotedesk-narrate:1")]
    public void RegisteredAgentIds_AreWrittenNameColonVersion(string expected)
    {
        new[] { AgentIdentity.Intake, AgentIdentity.Resolve, AgentIdentity.Narrate }
            .Select(a => a.Id)
            .Should().Contain(expected);
    }

    [Fact]
    public void AgentIdentities_NameMatchesTheIdWithoutItsVersion()
    {
        foreach (var identity in new[] { AgentIdentity.Intake, AgentIdentity.Resolve, AgentIdentity.Narrate })
        {
            identity.Id.Should().StartWith($"{identity.Name}:");
        }
    }

    private async Task RunOnceAsync(bool traceSensitiveData = false, string? imageDataUrl = null)
    {
        var shreeji = await fixture.Customers.FindByEmailDomainAsync("shreejitextiles.com", CancellationToken.None);

        var enquiryId = imageDataUrl is null
            ? ShreejiEnquiryId
            : await fixture.Enquiries.CreateAsync(
                new NewEnquiry("Paste", "kiran@shreejitextiles.com", string.Empty, EnquiryPipelineFactory.Now, CustomerId: null, "pending", imageDataUrl),
                CancellationToken.None);

        var stub = new StubChatClient(WorkedExampleScript.BuildWorkedExampleTurns(shreeji!.Id));
        var pipeline = EnquiryPipelineFactory.Build(fixture, stub, traceSensitiveData: traceSensitiveData);

        await foreach (var _ in pipeline.StartAsync(enquiryId, CancellationToken.None))
        {
            // Draining the stream is what actually runs the workflow; the events themselves are
            // asserted elsewhere. Here only the spans matter.
        }
    }

    /// <summary>Collects <c>gen_ai.agent.id</c> from every span QuoteDesk's own ActivitySource emits.
    /// Sampling is forced on, because with no exporter configured nothing else would record.</summary>
    private sealed class AgentIdSpanListener : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly List<string> _agentIds = [];
        private readonly Lock _gate = new();

        public AgentIdSpanListener()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == AgentInstrumentation.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    if (activity.GetTagItem("gen_ai.agent.id") is string id)
                    {
                        lock (_gate)
                        {
                            _agentIds.Add(id);
                        }
                    }
                },
            };

            ActivitySource.AddActivityListener(_listener);
        }

        /// <summary>The distinct ids seen so far. Distinct rather than every occurrence, so a span from
        /// a test running concurrently in another xUnit collection cannot change the answer.</summary>
        public IReadOnlyCollection<string> DistinctAgentIds
        {
            get
            {
                lock (_gate)
                {
                    return [.. _agentIds.Distinct()];
                }
            }
        }

        public void Dispose() => _listener.Dispose();
    }

    /// <summary>Collects every string-valued tag from every span QuoteDesk's ActivitySource emits —
    /// deliberately not just <c>gen_ai.input.messages</c>, because the point is that a photo's bytes
    /// appear in <i>no</i> attribute, not merely in the one we happened to think of.</summary>
    private sealed class SpanTagListener : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly List<string> _values = [];
        private readonly Lock _gate = new();

        public SpanTagListener()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == AgentInstrumentation.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    lock (_gate)
                    {
                        foreach (var tag in activity.TagObjects)
                        {
                            if (tag.Value?.ToString() is { Length: > 0 } value)
                            {
                                _values.Add(value);
                            }
                        }
                    }
                },
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public IReadOnlyList<string> TagValues
        {
            get
            {
                lock (_gate)
                {
                    return [.. _values];
                }
            }
        }

        public void Dispose() => _listener.Dispose();
    }
}
