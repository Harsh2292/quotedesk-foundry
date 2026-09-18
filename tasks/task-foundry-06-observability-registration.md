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

### Pending approvals vs a changed workflow shape — check first, build only if needed

Added 2026-09-17 (Harsh's decision). A run paused at the approval gate holds a checkpoint of the
workflow's shape; `Microsoft.Agents.AI.Workflows` refuses to resume it if executor ids, executor
types or edges have changed since (`checkpoint.Workflow.IsMatch(Workflow)`). The Extract→Intake
rename in foundry-03 stranded 5 local pending approvals this way.

1. **Before writing tracing code**, decide whether this task's changes alter the workflow shape. Agent
   `Id`s on `ChatClientAgentOptions` are agent identity, not executor identity — but check whether
   `WorkflowBuilder.WithOpenTelemetry`, or any wrapping of executors, changes executor ids/types/edges.
   Verify against the installed 1.19.0 package (XML docs or decompile), not by assumption. Proof it
   doesn't: a run paused at approval **before** the change still approves **after** it.
2. **If the shape does change**, build version-stamping in this task, before the shape change lands:
   - `AgentRuns.WorkflowVersion` (EF migration) stamped on every new run — a fingerprint of executor
     ids + executor type names + a `QuoteDeskWorkflow.ShapeVersion` constant for edge changes.
   - Before any resume (`ResumeAsync`, `ProcessAsync`'s failed-run resume): a mismatching fingerprint
     never reaches the framework — the run is marked `expired` with a clear message ("the pipeline
     changed since this was prepared — run the enquiry again"), not a generic `internal` error.
   - Startup sweep in the Api: pending runs with an old fingerprint are marked `expired`, so none sits
     stuck on the Approvals screen.
   - A guard unit test that snapshots the fingerprint and fails when the shape changes, forcing a
     deliberate `ShapeVersion` bump.
   - Tests: mismatched version expires cleanly; matching version still resumes.
3. **If the shape does not change**, don't build it here — it moves to the Extras queue
   (`tasks/README.md`, extra-05). Record which way it went in Notes on completion.

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
- [ ] Workflow-shape check done and recorded: either proven unchanged (a pre-change pending approval
      still approves), or version-stamping built with its tests
- [ ] Both build configs and the full non-eval test suite pass with no Azure Monitor connection configured

## Out of scope

Evaluation itself (`foundry-07`) — this task only makes traces exist and be attributable; scoring
them is the next task.

## Notes on completion

**Code half done 2026-09-18; the portal half is Harsh's and is what remains.**

**Package:** `Azure.Monitor.OpenTelemetry.AspNetCore` **1.6.0** added to `QuoteDesk.Api` — the version
NuGet resolved, clean under `-warnaserror` (no `NU1903` advisory, which is what ruled
`Microsoft.AspNetCore.OpenApi` out back in task 01). Named in `docs/FOUNDRY-PLAN.md` Step 3 already.

**Stable agent identity.** `AgentIdentity` and `AgentInstrumentation` (new,
`src/QuoteDesk.Agents/Pipeline/AgentInstrumentation.cs`) hold the three ids —
`quotedesk-intake:1`, `quotedesk-resolve:1`, `quotedesk-narrate:1` — and the single
`.AsBuilder().UseOpenTelemetry(...)` wrapping. `ResolveExecutor` had to move from
`AsAIAgent(instructions:, name:, …)` to the `ChatClientAgentOptions` overload, because only that one
can set `Id`. Narrate is given an id too, though it is not registered in the portal: it makes no
decision worth evaluating, but anonymous spans are worse than attributable ones.

**Why this was worth a dedicated test.** The failure mode is silent — with no explicit `Id`, MAF
generates a fresh random one per `ChatClientAgent`, QuoteDesk builds its agents per run, and every run
would appear in Foundry as a new agent that ran once. Nothing throws; the Traces tab is simply empty
for the registered agent. `AgentTelemetryTests` therefore attaches an `ActivityListener` to the
`QuoteDesk.Agents` source, runs the pipeline **twice**, and asserts the `gen_ai.agent.id` values from
the second run equal the first and are the registered ids — the acceptance criterion's own wording,
rather than reading the id back off the options object, which would pass even if the framework never
used it. (Extracted `EnquiryPipelineFactory` so that test builds the pipeline identically to
`EnquiryPipelineTests`, rather than a near-copy that could drift.)

**Instrumented once, deliberately.** `OpenTelemetryAgent` auto-wires the inner chat client's telemetry
— confirmed in the installed 1.19.0 XML docs, whose `autoWireChatClient` parameter defaults on and
skips an already-instrumented client. Adding `UseOpenTelemetry` to `ChatClientRegistry` as well is the
obvious-looking way to "make sure" model calls are traced and would double every span.

**Inert without Azure, and that is load-bearing.** `Program.cs` registers the OpenTelemetry pipeline
only when `AzureMonitor:ConnectionString` is non-empty. Registering nothing is a stronger guarantee
than registering an exporter with nowhere to send: CI, the integration suite and any local run stay
fully offline. Verified — 224 unit + 85 integration tests pass with no Azure settings configured.

### Workflow shape: unchanged — version-stamping stays in Extras (extra-05)

Branch 3 of this task file. Checked against the installed package rather than assumed:
`Microsoft.Agents.AI.Workflows.Checkpointing.TypeId.IsMatch` compares **the simple assembly name and
the type's full name** (its own XML docs say so explicitly, and that version, culture and public key
token are ignored). Nothing this task changed touches any of that:

- Executor ids are still the literals `"Intake"`, `"Resolve"`, `"Price"`, `"Approve"`.
- Executor type names and assembly are unchanged — the added `bool traceSensitiveData` constructor
  parameter is not part of a `TypeId`.
- Edges are unchanged.
- **`WorkflowBuilder.WithOpenTelemetry` was deliberately not added.** It was the one item that could
  plausibly wrap executors and so change the shape, and its XML docs do not say either way. It is
  listed as "optional, cheap" here, but it is only cheap if it is free — and the alternative branch
  costs an EF migration, a startup sweep and a guard test. Agent-level spans already carry everything
  Foundry's registration and trace evaluation read. Revisit in extra-05 with a decompile, not a guess.

**Still to confirm empirically:** the 15 runs sitting at `pending_approval` in the local dev database
predate this change. The reasoning above says they will still approve; the cheap confirmation is
clicking Approve on one in the UI. Not done here, because it writes a real quote row to Harsh's dev
database and the check is his to spend.

## What is left, and it needs Harsh

None of this can be done from a terminal — it is portal clicks and a credential.

1. **Connect Application Insights** — Foundry project → Agents → Traces → Connect.
2. **Grant roles** — Foundry User for Harsh; **Log Analytics Reader** for the project's managed
   identity on the App Insights resource *and* its workspace. Trace-based evaluation (foundry-07)
   fails without this second one.
3. **Set the connection string locally:**
   `dotnet user-secrets set "AzureMonitor:ConnectionString" "<the App Insights connection string>" --project src/QuoteDesk.Api`
4. **Turn on sensitive data locally:**
   `dotnet user-secrets set "Llm:TraceSensitiveData" "true" --project src/QuoteDesk.Api`
   Without it, spans carry no `gen_ai.input.messages`/`output.messages` and every Foundry quality
   evaluator scores `None`. This is the documented trade-off, not an oversight.
5. **Run one enquiry** through the Desk, wait 2–5 minutes for ingestion.
6. **Register both agents** — Foundry → Build → Agents → New agent → **Link external agent**, twice:
   `quotedesk-intake` with OTel id `quotedesk-intake:1`, and `quotedesk-resolve` with
   `quotedesk-resolve:1`.
7. **Open each agent's Traces tab, confirm spans, and screenshot both now** — not on video day.

**A correction worth carrying into the submission document:** Foundry **guardrails / content-filter
policies do not apply here**, because the models are called through instant access (preview), which
`docs/FOUNDRY-PLAN.md` Step 0 already records as not supporting custom guardrails. QuoteDesk's actual
guardrails are in code — the human approval gate, write tools unreachable from Resolve, untrusted-
content wrapping, deterministic pricing — and they are visible in the in-app trace panel, not in the
portal. Say that plainly rather than implying the platform provides them.
