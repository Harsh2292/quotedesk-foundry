# Task foundry-06 — Observability and agent registration

**Depends on:** foundry-02 (needs the Foundry endpoint working), Step 0 (Application Insights
connected, roles granted). Plan: `docs/FOUNDRY-PLAN.md` §5a–5b. Confirm every API against the
installed package's XML docs before writing — this task adds a new package
(`Azure.Monitor.OpenTelemetry.AspNetCore`), propose it to Harsh first per CLAUDE.md.

## Goal

Every model call and tool invocation from both agents becomes a real OpenTelemetry span in the
Foundry-connected Application Insights resource, attributable to a specific, stably-identified,
registered agent — not just visible in the in-app trace panel.

## What to build

### Stable agent identity (the part that's easy to get wrong)

MAF sets `gen_ai.agent.id` from `AIAgent.Id`, and by default that's a **random id generated per
instance**. QuoteDesk builds a fresh agent on every pipeline run, so without an explicit id, Foundry
could never attribute two runs' traces to "the same agent." Build each agent with the
`AsAIAgent(IChatClient, ChatClientAgentOptions, ...)` overload and an explicit, stable id:
`new ChatClientAgentOptions { Id = "quotedesk-intake:1", Name = "quotedesk-intake", ChatOptions = new()
{ Instructions = ..., Tools = ... } }` — likewise `quotedesk-resolve:1` and `quotedesk-narrate:1`.
Foundry's own convention for these ids is `name:version`.

### Tracing

1. Wrap each built agent: `.AsBuilder().UseOpenTelemetry("QuoteDesk.Agents", a =>
   a.EnableSensitiveData = llm.TraceSensitiveData).Build()`. `OpenTelemetryAgent` also activates the
   inner chat client's telemetry automatically — **do not** additionally call `UseOpenTelemetry` on
   `ChatClientRegistry`'s clients, or every span doubles.
2. New config `Llm:TraceSensitiveData` (default `false`; `true` in local user-secrets for this
   project specifically). Without it, `gen_ai.input.messages`/`output.messages` are empty and every
   quality evaluator in `foundry-07` scores `None`. This is a real trade-off — prompts and enquiry
   text land in Application Insights — and the submission document states it plainly.
3. `Program.cs`, after `AddQuoteDeskAgentPipeline`: when `AzureMonitor:ConnectionString` is set,
   `AddOpenTelemetry().UseAzureMonitor(...).WithTracing(t => t.AddSource("QuoteDesk.Agents"))`.
   **Skip entirely when the setting is empty**, so CI and every integration test stay fully offline —
   this must not become a new thing tests need a real Azure resource for.
4. Optional, cheap: `QuoteDeskWorkflow.Build() → .WithOpenTelemetry(...)` for workflow/executor-level
   spans alongside the agent-level ones.

### Register both agents (portal, one-off, not code)

Foundry → **Build → Agents → New agent → Link external agent**, twice: `quotedesk-intake` with OTel
id `quotedesk-intake:1`, and `quotedesk-resolve` with `quotedesk-resolve:1`. Run one enquiry, wait
2–5 minutes, open each agent's **Traces** tab, confirm spans appear, **screenshot both now**.

## Acceptance criteria

- [ ] Every agent built with a stable, explicit `Id` — verified by inspecting two consecutive runs'
      spans and confirming the same `gen_ai.agent.id`
- [ ] `Llm:TraceSensitiveData` wired through, default `false`
- [ ] Tracing is fully inert (no Azure calls, no package initialisation cost beyond a no-op) when
      `AzureMonitor:ConnectionString` is empty — confirmed by running the full test suite with it unset
- [ ] Both `quotedesk-intake` and `quotedesk-resolve` registered in the Foundry portal, showing real
      traces from a real run
- [ ] Screenshots of both agents' Traces tabs saved for the document
- [ ] Both build configs and the full non-eval test suite pass with no Azure Monitor connection configured

## Out of scope

Evaluation itself (`foundry-07`) — this task only makes traces exist and be attributable; scoring
them is the next task.

## Notes on completion

*(fill in once run)*
