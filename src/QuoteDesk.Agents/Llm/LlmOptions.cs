using Microsoft.Extensions.AI;

namespace QuoteDesk.Agents.Llm;

/// <summary>
/// Bound from the "Llm" config section by QuoteDesk.Api (task 07) and passed to
/// <c>AddQuoteDeskAgents</c> — the same pattern QuoteDesk.Data's connection string uses, rather than
/// this project taking a dependency on <c>Microsoft.Extensions.Configuration</c> itself.
/// <see cref="Endpoint"/>/<see cref="ApiKey"/>/<see cref="Model"/> are empty-string defaults in
/// appsettings.json, filled locally via <c>dotnet user-secrets</c>. Task 09 added per-stage routing
/// (<see cref="IntakeModel"/>, <see cref="ResolveModel"/>, <see cref="NarrateModel"/>) once the
/// live pipeline proved that one model for every call bunches ~6 sequential requests against a
/// single free-tier requests-per-minute ceiling; routing stages onto different models also spreads
/// them across different quota buckets. <see cref="QuoteDesk.Agents.Llm.ChatClientRegistry"/> is
/// what turns these into actual <c>IChatClient</c>s, one per distinct model name.
/// </summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>"foundry" (this fork's default, set explicitly in appsettings.json — task foundry-02),
    /// "gemini" or "github" — which branch <c>ChatClientFactory.Create</c> builds. Added once adopting
    /// <c>Google.GenAI</c> for the "gemini" profile (docs/SPEC.md §4's `thought_signature` correction)
    /// meant the profiles could no longer share one OpenAI-compatible client differing only by
    /// <see cref="Endpoint"/> — Google's native SDK takes an API key, not an arbitrary base URL, so
    /// the code that builds the client has to know which one to build. "foundry" and "github" both
    /// still go through the plain OpenAI-compatible path. The class default stays "gemini" rather than
    /// following appsettings.json's "foundry" default: <c>GeminiWorkedExampleEval</c> and its siblings
    /// build an <see cref="LlmOptions"/> without setting this property, relying on the default to route
    /// through <c>Google.GenAI</c>'s native SDK — changing it here would silently reintroduce the
    /// `thought_signature` bug for those evals.</summary>
    public string Provider { get; init; } = "gemini";

    /// <summary>Meaningful for <see cref="Provider"/> "foundry" (the Foundry resource's
    /// <c>/openai/v1/</c> URL — docs/FOUNDRY-PLAN.md Step 0) and "github" — Google's native SDK
    /// (<c>Google.GenAI</c>) has no endpoint override, so this is not "any OpenAI-compatible endpoint"
    /// universally any more, just for those two profiles.</summary>
    public required string Endpoint { get; init; }
    public required string ApiKey { get; init; }

    /// <summary>The default model, and the one every stage falls back to when its own
    /// <see cref="IntakeModel"/>/<see cref="ResolveModel"/>/<see cref="NarrateModel"/> is unset —
    /// what keeps the evals and any single-model config binding unchanged.</summary>
    public required string Model { get; init; }

    /// <summary>Model for the Intake stage — messy text (or a photo, foundry-04) into JSON, no
    /// judgement calls beyond checking an unclear term against the catalogue. Falls back to
    /// <see cref="Model"/>. docs/FOUNDRY-PLAN.md Step 0: routed to the cheap, vision-capable
    /// <c>gpt-5-nano</c> since nothing here is worth the capable model's scarce quota. Renamed from
    /// <c>ExtractModel</c> in task foundry-02 — the property only; the executor/stage rename itself is
    /// foundry-03.</summary>
    public string? IntakeModel { get; init; }

    /// <summary>Model for the Resolve stage — the one autonomous node: a tool-calling loop that has
    /// to weigh candidates and know when it genuinely cannot tell. Falls back to <see cref="Model"/>.
    /// docs/FOUNDRY-PLAN.md Step 0: the one call worth paying for the capable model
    /// (<c>gpt-5-mini</c>).</summary>
    public string? ResolveModel { get; init; }

    /// <summary>Model for the Narrate stage — one sentence built from numbers
    /// <c>QuoteDesk.Domain</c> already computed. Falls back to <see cref="Model"/>. docs/FOUNDRY-PLAN.md
    /// Step 0: routed to the cheap model, same reasoning as <see cref="IntakeModel"/>.</summary>
    public string? NarrateModel { get; init; }

    /// <summary>
    /// The reasoning effort sent on the light stages — Intake and Narrate, never Resolve. Bound from a
    /// name ("None", "Low", …), so a misspelt value fails at startup when Program.cs binds this
    /// section, not mid-run. Null (the class
    /// default) sends no reasoning option at all, so a provider only receives one where it has been
    /// verified: "None" is set for the "foundry" profile in appsettings.json, checked live on
    /// 2026-09-17 against <c>gpt-5-nano</c> (HTTP 200, 0 reasoning tokens) — the model those stages
    /// actually route to. Not verified for the "gemini" profile, where Google.GenAI maps None to a
    /// zero thinking budget that some Gemini 3.x models refuse — which is why this is config rather
    /// than a constant in the pipeline.
    /// </summary>
    public ReasoningEffort? LightStageReasoningEffort { get; init; }

    /// <summary>tasks/task-06: "Max 8 tool calls per run, then a forced summary".</summary>
    public int MaxToolCalls { get; init; } = 8;

    /// <summary>tasks/task-06: "Per-conversation token budget, returning a clean budget_exceeded
    /// rather than looping". Generous enough for the worked example's handful of tool calls plus
    /// narration; not tuned against a live model yet.</summary>
    public int TokenBudget { get; init; } = 20_000;

    /// <summary>
    /// Ask the provider to enforce a JSON schema on the stages that must return structured data,
    /// instead of asking politely in the prompt and parsing whatever comes back. On by default: it is
    /// the single biggest reliability lever available, and <see cref="StructuredModelCall"/> falls
    /// back to plain-text parsing if the provider rejects it.
    ///
    /// Whether <c>gemini-3.6-flash</c> honours it is unverified — if a live run logs the
    /// "provider rejected schema-enforced output" warning, set this to false so the pipeline stops
    /// paying for the rejected attempt on every run.
    /// </summary>
    public bool UseStructuredOutput { get; init; } = true;
}
