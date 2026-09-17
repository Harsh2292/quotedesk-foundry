> **This is the live, authoritative plan for this fork.** If you are a fresh Claude Code session
> picking this project up, read this file plus the last entry in `docs/SESSION-LOG.md` before doing
> anything else — together they replace the conversation that produced this plan, which you don't
> have access to. `tasks/README.md` is the day-to-day work queue derived from this plan; `/task
> foundry-NN` implements one row of it at a time, the same convention the base project used for tasks
> 00–11. Update this file's "Compliance audit" and "Schedule" sections as work lands, the same way the
> base project's `docs/SPEC.md` accumulated "Resolved in taskNN" sections over time — don't let it go
> stale the way `docs/AGENT-A-THON.md` did.

# QuoteDesk Foundry — build plan for the Agent-a-Thon (Level 3 Architect)

## Context

Harsh is entering the **Microsoft Agent-a-Thon, Level 3 (Architect)**. Kickoff **17 Sep 2026**,
submission **24 Sep 2026 23:59 PDT**. Judged 30/30/30 on Innovation, Usability, Impact by the course's
Pre-Learning Instructors, from a **≤3-minute video (≤150 MB)** and a **document**. The spec that
matters is module 8's final activity (Steps 1–3: multi-agent design, production readiness,
end-to-end workflow). Research and rules: `docs/AGENT-A-THON.md` (superseded in places — see its own
banner; this file is current).

The entry reuses QuoteDesk's domain, data and pipeline (explicitly allowed) in this **new public
repo**, forked from `Harsh2292/Quote-Desk` @ `development` `fb45bdb`. **That repo is deployed and is
never touched from here.**

**Settled design (supersedes every earlier draft, including `docs/AGENT-A-THON.md`):**

- **Two agents, split by cognitive role.** **Intake Agent** perceives — reads text or a photo, and may
  call one tool, `verify_catalogue_term`, when a word is unclear. **Resolve Agent** decides — the
  existing, tested agent, unchanged. The Customer/Catalogue split (an earlier draft) is dead: it broke
  the worked example's own "same as last time" disambiguation by putting `get_customer_history` and
  `check_stock` on the wrong side of the split.
- **Foundry is used four ways:** model inference, tracing, agent registration, and evaluation.
- **Evaluation is both dataset-based (module 5's own flow) and trace-based on registered external
  agents** — see Step 5c. An earlier draft tried to force a query-response-dataset-only approach onto
  everything; that's dropped in favour of doing both, since module 5 asks for ground truth a live
  trace can't provide.
- **Narrate is grounded in a Quotation Policy document** (embedded, versioned, cited). Foundry-hosted
  file search is not used: it belongs to the Foundry Agent Service runtime, which this app doesn't run.
- **Cut:** Azure deployment of the app, WhatsApp, email, audio transcription, generated sample
  enquiries.

**Verified on this machine (13 Sep):** .NET SDK 10.0.400 ✓ · Azure CLI 2.90 ✓ · GitHub CLI 2.98
(**not logged in — run `gh auth login`**) · Node 24 ✓ · Docker 29 ✓ · `ilspycmd` ✓.

---

## Compliance audit — checked against every source actually read

**Sources checked:** the module 5 PDF (*Evaluate agent quality*, confirmed 2026-09-13 against the
actual document — its six evaluation dimensions, six-step configuration flow, and six-stage lifecycle
match what this plan already had, almost line for line), the module 8 final-activity brief, and the
official Founderz rules. **Confirmed 2026-09-13: these two documents are the complete course material
for this track — no other modules exist or will be supplied.** The earlier caveat in this section
("modules 1–4, 6 and 7 not checked") is retired. Anything not explicitly covered by these two
documents — agent design, knowledge sources, orchestration, tracing mechanics — is this plan's own
architectural judgment, stated as such, not a guess against unseen material.

### Module 5 — Evaluate agent quality

| Module 5 step | Where it's covered | Status |
|---|---|---|
| Monitoring ≠ evaluation | 5a vs 5c; stated in the document | ✅ |
| Six quality dimensions | 5c.4 | ✅ |
| Coherence and fluency don't prove correctness | 5c.9 | ✅ |
| 10 scenarios across severity bands | 5c.1 — resolvable / needs a flag / must refuse | ✅ |
| Each entry: input query + ground-truth expected outcome | 5c.1–5c.2 (Run A) | ✅ |
| Choose target + evaluate individual turns | 5c.2 | ✅ |
| Upload dataset, verify field mapping (query → input, ground truth → expected) | 5c.2 | ✅ |
| Separate judge model | Step 0 `judge` deployment | ✅ |
| Choose evaluators; meaningful, traceable run name | 5c.4 | ✅ |
| Review token usage (target + judge) alongside scores | 5c.7 | ✅ |
| Lifecycle 1 — baseline before deployment | `-baseline-v1` runs | ✅ |
| Lifecycle 2 — acceptance thresholds | 5c.5 | ✅ |
| Lifecycle 3 — re-evaluate after a change | 5c.6 — a real `v2` run after the latency fix | ✅ (do it, don't just describe it) |
| Lifecycle 4 — detect regressions by comparing runs | 5c.6 — portal v1-vs-v2 comparison + screenshot | ✅ |
| Lifecycle 5 — monitor quality drift on a schedule | 5c.8 — manual-dispatch GitHub Action | ✅ (cuttable, see Schedule) |
| Lifecycle 6 — expand risk coverage | safety + prompt injection + tool use + groundedness | ✅ |

### Module 8 — the final activity brief

| Requirement | Status |
|---|---|
| S1 business problem · intended users · ≥2 specialised agents · tools/data/knowledge per agent · why multi-agent · information flow | ✅ written into the document plan, Step 6 |
| S2 observability — which traces *and metrics*, and how they diagnose issues | ✅ 5a — metrics list + three real diagnosis incidents |
| S2 evaluation — dataset, criteria, lifecycle | ✅ after the module 5 alignment above |
| S2 governance — consistent outputs, safe behaviour, maintainability, grounding | ✅ named document section, Step 6 |
| S3 sequence · information passed · tool calls · final output | ✅ |
| S3 how it would be *deployed*, monitored, evaluated, improved | ✅ deployment is cut from the build, so the document *describes* it (Container Apps recipe; the original deployed QuoteDesk as evidence the recipe works) |
| Video: demonstrate + *explain the decisions* + reflect on prototype → production | ✅ a decisions segment is in the video script, Step 6 |
| Diagrams, workflow illustrations, screenshots | ✅ the blueprint diagrams; Fig. 03 needs updating once evaluation is dataset+trace, not query-response only |
| Don't expose confidential or personal information | ✅ screen-hygiene checklist, Step 6 |

### Official Founderz rules and judging

| Rule | Status |
|---|---|
| Built with Microsoft Foundry | ✅ inference, tracing, registration and evaluation |
| Video ≤ 3:00 and ≤ 150 MB (over-length is grounds for rejection) | ✅ timed script |
| Documentation as PPTX / PDF / DOCX / DOC / PPT / TXT | plan: export as **PDF** |
| "Clear purpose and audience, how it was *built and refined*, screenshots and example interactions, *key lessons learned*" | ✅ document sections, Step 6, drawn from real project incidents |
| Written description: problem, impact, how it was built on Foundry | ✅ |
| Judged 30/30/30; ties break on Innovation first | ✅ the hook leads with the Innovation claim |
| Original work, uploaded to Founderz inside the entry window | plan: upload 23 Sep |
| **Judging is anonymous** | ⚠️ **still open** — check the Founderz upload form for whether a name or repo URL may appear anywhere in the video or document |

**Not adopted from the original hackathon guidelines file:** a compliance footnote claiming the work
was built *"entirely within the official event window."* Work starts 13 Sep, before the 17 Sep
window opens, so that sentence is false. The README states provenance without claiming a date.

---

## Step 0 — Azure portal and logins (Harsh's, done hands-on in the portal — see note below)

**Harsh does all Foundry portal configuration himself** — resource, deployments, Application
Insights, roles — as the way he's chosen to actually learn Foundry. This is a deliberate exception
to "Claude does the building": even where a step is technically automatable via `az`, Foundry
portal work stays his. Claude's role here is limited to verifying what he's built (a `curl`/test call)
and writing the config values he provides into user-secrets — never to logging into the portal or
running `az` mutations against Foundry resources on his behalf.

**Status as of 2026-09-13:**

- ✅ Foundry resource `pharshin29-2918-resource` and project `pharshin29-2918` exist, in `quotedesk-rg`
  (`westus3`), under the existing Azure subscription used for the original QuoteDesk deployment.
- ✅ `az login` and `az` CLI both already authenticated on this machine.
- ✅ API key obtained and stored via `dotnet user-secrets set "Llm:ApiKey" ... --project src/QuoteDesk.Api`
  — **not** in any file, not in `appsettings.json`. *(The key was pasted in plaintext into a chat
  session while getting it into user-secrets — Harsh should regenerate it in the portal (Keys and
  Endpoint → Regenerate) as routine hygiene, independent of anything Claude did with it.)*
- ✅ **Correct endpoint confirmed working, live:** `https://pharshin29-2918-resource.services.ai.azure.com/openai/v1/`
  — note this is the **resource** endpoint with `/openai/v1/`, not the **project** endpoint
  (`.../api/projects/pharshin29-2918`) the portal shows on the project overview page; the two are easy
  to confuse and only the former works for chat completions. A live test call to `gpt-4o-mini`
  returned a real completion (HTTP 200) with **zero deployment required** — Foundry's "instant access"
  lets some models be called by name with no deployment step at all.
- ⏳ **Deployments — decided, not yet created (Harsh doing this next, in the portal):**
  - **`gpt-5-nano`** → Intake's target. Cheapest available tier, vision-capable.
  - **`gpt-5-mini`** → Resolve's target. A step up for the harder cross-referencing work.
  - **No third "judge" deployment.** Module 5 only requires the judge differ from *the specific
    agent it's grading* — `gpt-5-mini` can judge Intake's traces, `gpt-5-nano` can judge Resolve's,
    and that alone satisfies the rule for both evaluation runs. Saves a whole deployment's cost.
  - Both as **Standard (pay-per-token)**, **never Provisioned/PTU** (PTU bills hourly whether used or
    not), at the **lowest rate-limit/TPM setting the portal offers**.
  - `gpt-4o-mini` was tried and rejected for new deployments — it's retired for new deployments in
    this account's catalog (confirmed via `az cognitiveservices account list-models`), which is
    exactly why the two GPT-5-tier models above are the real choice, not a fallback.
- ⏳ Application Insights not yet connected (Foundry project → Agents → Traces → Connect).
- ⏳ Roles not yet granted (Foundry User for Harsh; Log Analytics Reader for the project's managed
  identity on the App Insights resource and its workspace — trace evaluation fails without this one).
- ⏳ Budget alert at ₹200 not yet set (a smoke detector, not a brake — see `docs/AGENT-A-THON.md` §4a
  for why an Azure Budget alone doesn't cap spend, and what actually does).

**Resolved 2026-09-14 — no deployments needed at all.** Verified live: Microsoft Foundry's "instant
access" (preview) feature lets a supported model be called by name with zero deployment step, through
the exact same resource endpoint and OpenAI-compatible wire protocol `foundry-02` already plans to
use. It's region-locked to **West US 3** during preview — `pharshin29-2918-resource` already lives
there — and `az rest` against the model catalog confirms `gpt-5-mini` and `gpt-5-nano` both show
`instant: true` for this subscription in that region. Live proof: a real `chat/completions` POST to
`https://pharshin29-2918-resource.services.ai.azure.com/openai/v1/chat/completions` with
`"model": "gpt-5-mini"` (and again with `"gpt-5-nano"`) returned `HTTP 200` with a real completion —
no deployment existed at the time of either call. The two planned "Standard, lowest TPM" deployments
in the table above are **not needed**; `foundry-02` can point `IntakeModel`/`ResolveModel` straight at
`gpt-5-nano`/`gpt-5-mini` today. Caveats worth carrying into the submission document: instant access
is a preview feature, draws from a separate shared *global* quota pool rather than a deployment's own
regional quota, and doesn't support custom guardrails/content-filter policies or reserved throughput
— all fine for a demo, not a substitute for a deployment in a real production setup.

**`foundry-02` can start now — nothing further is blocked on Harsh's Azure portal work.**

---

## Step 1 — Fork mechanics (done 13 Sep)

Cloned `development` @ `fb45bdb` from `E:/DevStuff/Quotedesk/quotedesk` into this folder, then
stripped `.git` for a fresh history. What a plain clone got wrong, and how it was fixed — see
`tasks/task-foundry-00-fork-and-repo.md` for the full record, including what's still outstanding
(the `UserSecretsId` change, `.env.local`, and the `.gitignore`/`CLAUDE.md`/`README.md` edits).

---

## Step 2 — New GitHub repo, and the working rhythm after it

1. `git init -b main`, then `git switch -c development`. All work goes on `development`, same as the
   old repo.
2. First commit (**Harsh runs it**):
   `chore: import QuoteDesk baseline from Harsh2292/Quote-Desk@fb45bdb` — an honest record of where
   the code came from.
3. Create the remote — I propose it, Harsh approves:
   `gh repo create quotedesk-foundry --public --source=. --remote=origin`. **Harsh pushes.** Needs
   `gh auth login` first (not done yet as of 13 Sep).
4. GitHub settings: add the repo **Variable** `VITE_GOOGLE_CLIENT_ID` so the existing `ci.yml` web job
   passes. CI needs no other change: it already runs on `main`/`development`, needs no API key, and
   uses a SQL service container.
5. README top line, stating provenance: *"Domain, data layer and pipeline adapted from my earlier
   project [Quote-Desk]; the Foundry integration, the two-agent design, observability, evaluation and
   image intake were built for the Agent-a-Thon."*
6. **Rhythm for every step below:** one task at a time (`/task foundry-NN`) → build both
   configurations + tests + `npm run build` → I stage and write the conventional-commit message →
   Harsh commits and pushes → `/session-log`.

---

## Step 3 — Packages (proposed; Harsh approves each)

Verified against the installed packages: **`QuoteDesk.Agents` needs nothing new.**
`OpenTelemetryAgent`/`UseOpenTelemetry`, `WorkflowBuilder.WithOpenTelemetry`,
`ChatClientAgentOptions.Id`, `DataContent`, and `AIAgent.RunAsync(ChatMessage)` all ship in the
already-referenced `Microsoft.Agents.AI` 1.19.0 / `Microsoft.Extensions.AI` 10.9.0. Inference reuses
the existing OpenAI-compatible client.

| Package | Project | Why |
|---|---|---|
| `Azure.Monitor.OpenTelemetry.AspNetCore` | `QuoteDesk.Api` | Sends agent traces to the Application Insights resource connected to Foundry. Microsoft names it the preferred package for ASP.NET Core. |
| `Azure.AI.Projects` | new `tools/QuoteDesk.FoundryOps` | The Foundry SDK: `ProjectOpenAIClient.GetEvaluationClient()` for dataset and trace-based evaluation runs. |
| `Azure.Identity` | `tools/QuoteDesk.FoundryOps` | `DefaultAzureCredential` for the evaluation console only. The app stays on an API key. |

Rules: take whatever versions NuGet resolves and record them in `docs/SPEC.md` §3. **Never add
`Azure.AI.Projects.OpenAI` (preview) next to `Azure.AI.Extensions.OpenAI`** (it arrives with
`Azure.AI.Projects`): the two define the same types and the build fails on ambiguous references.
`Azure.AI.Projects.Agents` (prerelease) is **not** needed, because agents are registered in the portal.

---

## Step 4 — Build the new system into the fork

### 4a. Foundry inference (first, smallest)

- `src/QuoteDesk.Agents/Llm/ChatClientFactory.cs` — add `"foundry" => CreateOpenAiCompatible(options, model)`.
  **Keep the `gemini` and `github` branches** as a fallback for recording night.
- `LlmOptions` — rename `ExtractModel` → `IntakeModel`. `ChatClientRegistry.Extract` → `Intake`.
  Fix the doc comment on `Endpoint`.
- `appsettings.json` — `Provider: "foundry"`, `Endpoint` set to the resource `/openai/v1/` URL,
  `IntakeModel`/`ResolveModel`/`NarrateModel` set to the deployment names, **`TokenBudget` 20000 → 60000**
  (vision payloads plus the extra agent), `MaxToolCalls` 8 → 10.
- `QuoteDeskApiFactory` — set `Llm__Provider` explicitly.
- **Gate:** `tests/QuoteDesk.Evals/FoundryWorkedExampleEval.cs` (copied from the Gemini one) runs the
  full worked example live, with real multi-turn tool calls. If a strict-schema rejection warning
  appears, set `Llm:UseStructuredOutput: false`.

### 4b. Intake Agent (replaces Extract) — ✅ done 2026-09-17 (foundry-03)

- **New tool** in `src/QuoteDesk.Agents/Tools/CatalogTools.cs`:
  `VerifyCatalogueTermAsync(string term)` → `CatalogueTermCheck { Term, Known, Families[], ExampleNames[≤3] }`.
  It is built on `ICatalogRepository.SearchAsync` plus the family grouping `SearchOneAsync` already does.
  It returns **no SKU, price or cost**, so `ToolResultBoundaryTests` still passes. Register it as
  `verify_catalogue_term` in `ReadToolRegistry`.
- **Rename `ExtractExecutor` → `IntakeExecutor`.** It takes an `IChatClient` and builds its agent
  **inside `HandleAsync`**, the same pattern as `ResolveExecutor`, because `TracedAIFunction` needs the
  live `IWorkflowContext`. Stage name `"intake"`. Output is still `ExtractionResult`, so **Resolve,
  Price, Approve, the contracts and the approval UI are untouched.**
  - It uses `useSchema: false`, for the same reason Resolve does: a strict response format would apply
    to the tool-call turns too. The existing retry-with-parse-error still guards the output.
- **Prompt:** `Prompts/intake.md`, taken from `extract.md`, plus: *call `verify_catalogue_term` only
  when a word is unclear or illegible; never guess a product.* Update `PromptLibrary`'s hand-written
  list in the same edit, because `Load` throws on a missing resource.
- **`EnquiryPipeline.BuildNodes`:** Intake gets only `verify_catalogue_term`; Resolve keeps its four.
  Create **one `ToolCallBudget` per run**, next to `TokenUsageTracker`, and pass it to both agents.
- **Stage union:** `AgentEvent.cs` doc comment; `web/src/api/agentEvents.ts` adds `'intake'` and keeps
  `'extract'` as a legacy value, so the fixtures and old `TraceJson` rows still parse; `traceLabels.ts`
  adds plain-language labels ("Read the enquiry", "Checked a word against the catalogue").
- **Tests:** stage-order assertions `extract` → `intake`; `ResolveAgentToolBoundaryTests` becomes a
  `[Theory]` over `IntakeExecutor` *and* `ResolveExecutor`; `WorkedExampleScript` **stays at 6 turns**
  (Intake answers without the tool); one new 7-turn test where Intake calls `verify_catalogue_term`;
  unit tests for the tool (known term, unknown term, empty, whitespace); `PromptLibraryTests` for
  `intake.md`.

### 4c. Image intake

The base64-over-JSON design:

1. `PasteEnquiryRequest.ImageDataUrl` (reject if over ~2 MB decoded).
2. Migration `Enquiries.ImageDataUrl nvarchar(max) null`; `NewEnquiry` gets the new parameter with a
   default value.
3. `PasteAdapter.FromPastedTextAndImage`. Never create an `ImageAdapter`, because
   `IntakeBoundaryTests` fails if `EnquiryChannel` appears in Api/Agents.
4. `EnquiryStatusRule`: blank body **and** no attachment → `needs_manual_entry`.
5. `EnquiryInput.ImageDataUrl = null` default. `IntakeExecutor` returns `message with { ImageDataUrl =
   null }`, so the image isn't written into every checkpoint.
6. `StructuredModelCall.RunAsync<T>(AIAgent, ChatMessage, …)` overload. The image goes in as
   `new DataContent(dataUri, mediaType)` (verified: this constructor accepts a data URI). The retry
   keeps the original contents.
7. Web: downscale on a canvas to ~1280px JPEG, and **don't** save the image to sessionStorage.

### 4d. Quotation Policy grounding

- `Prompts/quotation-policy.md` — the real rules from `docs/DOMAIN.md` (slab ladder, tier discounts, 15%
  cap, 10% margin floor, freight, 15-day validity), with a version line. Add it to `PromptLibrary`.
- Narrate's instructions include it, and the prompt says to cite policy when explaining a discount.
  **The numbers still come only from `QuoteDesk.Domain`** — the model quotes policy, it never computes.
- **Blocked on module 6 (orchestration) summary** — see the Compliance audit's open risk.

---

## Step 5 — Connect Foundry end to end

### 5a. Tracing (verified APIs)

- **Stable agent identity is required.** MAF sets `gen_ai.agent.id` from `AIAgent.Id`, and by default
  that is a **random id for each instance**. QuoteDesk builds fresh agents on every run, so Foundry
  could never match the traces to a registered agent. Build each agent with the
  `AsAIAgent(IChatClient, ChatClientAgentOptions, …)` overload:
  `new ChatClientAgentOptions { Id = "quotedesk-intake:1", Name = "quotedesk-intake", ChatOptions = new() { Instructions = …, Tools = … } }`.
  Do the same for `quotedesk-resolve:1` and `quotedesk-narrate:1`. (Foundry's trace evaluation expects
  ids in `name:version` form.)
- Wrap each one: `.AsBuilder().UseOpenTelemetry("QuoteDesk.Agents", a => a.EnableSensitiveData = llm.TraceSensitiveData).Build()`.
  `OpenTelemetryAgent` also turns on inner chat-client telemetry, so **don't** add `UseOpenTelemetry`
  to `ChatClientRegistry` as well — that would duplicate every span.
- Sensitive data is required for evaluation: without `gen_ai.input.messages`/`output.messages`, the
  quality evaluators score `None`. Add `Llm:TraceSensitiveData` (default `false`, `true` in local
  user-secrets). It captures prompts and enquiry text in our own App Insights resource. State this
  trade-off in the document.
- Optional, cheap: `QuoteDeskWorkflow.Build` → `.WithOpenTelemetry(...)` for workflow and executor spans.
- `Program.cs` (after `AddQuoteDeskAgentPipeline`): when `AzureMonitor:ConnectionString` is set,
  `AddOpenTelemetry().UseAzureMonitor(...).WithTracing(t => t.AddSource("QuoteDesk.Agents"))`.
  **Skip it when the setting is empty**, so CI and integration tests stay offline. Check the exact
  extension signatures in the installed XML docs after adding the package.
- **Metrics the document names (brief Step 2.1), all already captured:**
  - per-stage latency (`StageEvent.At`, `DoneEvent.At`);
  - tool calls per run and the tool failure rate (`ToolEndEvent.Ok`);
  - tokens per run (`AgentRuns.PromptTokens/CompletionTokens`, plus span usage);
  - error rate by code (`provider_rate_limited`, `budget_exceeded`, `internal`);
  - **refusal rate** — the share of lines left unresolved on purpose;
  - approve vs reject rate at the human gate.
- **"How the insights diagnose issues" — three real incidents from this project, not hypotheticals:**
  - a 49.6 s Resolve found through stage timing;
  - a 342-candidate catalogue flood found through the size of a tool result;
  - the Gemini `thought_signature` 400 found through the error trace.

  Each one led to a real fix that's already in the code.

### 5b. Register the two agents (portal, one-off)

Foundry → **Build → Agents → New agent → Link external agent**, twice:
`quotedesk-intake` with OTel id `quotedesk-intake:1`, and `quotedesk-resolve` with `quotedesk-resolve:1`.
Run one enquiry, wait 2–5 minutes, then open each agent's **Traces** tab. **Take the screenshot now.**

### 5c. Evaluation — module 5's six steps and six lifecycle stages, followed in order

1. **The dataset — 10 cases, each with an input query and a written expected outcome** (module 5's
   exact shape). Commit it as `tests/QuoteDesk.Evals/dataset/quotedesk-eval-v1.jsonl`:
   - **Resolvable:** a clean single line; the Shreeji worked example; a repeat customer.
   - **Needs a flag:** short stock; a margin-floor line; an unknown sender.
   - **Must refuse:** the spindle tape with no history; a prompt-injection enquiry; an empty enquiry;
     an unreadable one — plus the **photo with one illegible word**, so Intake's tool fires.

   The `ground_truth` is written by hand *before* anything runs; that's what makes it ground truth.
   Run all 10 through the real Desk UI, which doubles as live verification, then copy each run's
   final output into `response`.
2. **Run A — dataset evaluation, module 5's steps 3–6 in order.** Upload the JSONL and name the
   dataset → **verify the field mapping** (`query` → input, `ground_truth` → expected outcome,
   `response` → output) → pick the separate `judge` deployment → **scope: individual turns** →
   choose evaluators → run it as `quotedesk-dataset-baseline-v1`.
   - **One honest adaptation, stated in the document:** module 5's step 2 selects a Foundry-invokable
     target. Foundry never invokes an external agent, so the responses come pre-captured in the
     dataset instead.
   - Add **`response_completeness`**, which actually uses `ground_truth`. Without it, mapping the
     ground truth would be decorative.
3. **Run B — trace evaluation on the registered agents.** This is what the registration buys: real
   production traces, with no dataset to build.
   - In the portal: open `quotedesk-intake` / `quotedesk-resolve` → Evaluation → traces.
   - As code, repeatably, in `tools/QuoteDesk.FoundryOps`:
     `evaluate --agent quotedesk-resolve:1 --run-name traces-baseline-v1`, using the `azure_ai_traces`
     data source with an agent filter.
4. **Evaluators** (names checked against Foundry's built-in list), mapped to module 5's six dimensions:
   `coherence`, `fluency`, `task_adherence` (pass/fail), `tool_call_accuracy` (1–5, "tool usage"),
   `groundedness` and `relevance` (1–5), `violence` ("safety"), plus `response_completeness` in run A
   and `intent_resolution` for Intake in run B.
5. **Lifecycle stage 2 — acceptance thresholds, fixed before the baseline:** every 1–5 metric ≥ 4;
   pass/fail metrics ≥ 9 of 10; safety 10 of 10; the prompt-injection case must pass
   `task_adherence`.
6. **Lifecycle stages 3 + 4 — actually do a re-evaluation and a regression check; don't just describe
   them.** The latency fix (Step 6) changes a model deployment or a prompt. Re-run both evaluations
   as `…-v2`, **compare v1 and v2 in the portal's comparison view**, and screenshot it. "An
   improvement in one area weakened another" is exactly what module 5 says this step is for.
7. **Review token usage (module 5, "interpreting results").** Record target and judge tokens for each
   run next to the scores in the document.
8. **Lifecycle stage 5 — drift monitoring.** Add `.github/workflows/eval-drift.yml`: runs FoundryOps
   trace evaluation with a 24-hour lookback, **`workflow_dispatch` only**. Present it as the scheduled
   job a production deployment would switch on, and trigger it once by hand as proof that it works.
   It stays manual so it never spends tokens unattended.
9. **Interpret responsibly — say what the scores don't prove:**
   - pricing is never judged by a model; it's proven by the `QuoteDesk.Domain` unit tests;
   - two perfect scores say nothing about the metrics that weren't run;
   - external agents are a preview feature — no human evaluation, no trace-to-dataset, no red teaming.
10. **Take the screenshots when each run finishes** (run A, run B, the v1-vs-v2 comparison), not on
    video day.

---

## Step 6 — What turns a working entry into a winning one

- **Demo content, chosen on purpose:** the photo has one illegible word, so Intake's tool call is
  visible; the spindle tape still escalates to a human. Two agents making two different kinds of
  "I won't guess" call.
- **Latency, before recording.** The last live run spent 49.6 s in Resolve. Pick faster deployments or
  trim tool results, and cut dead air in the edit.
  - **Decided 2026-09-17 — keep the architecture as it is; present the faster alternative as a
    considered design, don't build it.** Measured on Foundry (3 live runs of the worked example): 31 s
    average end to end, Resolve 25 s of that (~80%), 4.3 model round trips and 3.7 tool calls per run,
    the final judgement turn alone ~10–11 s; both judgement calls correct 3/3. The alternative
    considered: move the *routine* lookups (customer match by email domain, catalogue search for every
    extracted line) into code, run in parallel before Resolve, while Resolve still chooses the
    order-history check itself and makes the judgement — ~4 round trips → ~2, Resolve ~25 s → ~14 s,
    ~20 s end to end (estimated, not built). Rejected for now because latency is not a judging criterion
    on its own, and the fully autonomous Resolve is the stronger agent story. Also rejected: moving
    *all* lookups into code (Resolve would show no tool calls, weakening the agent claim and Foundry's
    tool-call-accuracy evaluation), lowering Resolve's reasoning (risks the judgement calls), and more
    tool calls / different models (the limit is not the bottleneck; per-stage models stay as they are).
    **Say this in both the video's decisions segment and the document** — it shows the trade-off was
    measured and reasoned, not defaulted.
- **Video (≤3:00, ≤150 MB) — demonstrate, explain the decisions, reflect (all three are in the brief):**
  - 0:00–0:15 — the hook: a two-person sales team losing their evening, then "the model is forbidden
    from the part that matters".
  - 0:15–0:35 — the photo goes in, Intake checks a word.
  - 0:35–1:20 — the trace: two named agents on two deployments; the 6203 resolved with its reason;
    the spindle tape flagged.
  - 1:20–1:45 — a human approves, and the quote is created.
  - 1:45–2:15 — **the decisions**, over the blueprint diagram: two agents split by role; pricing
    kept in code; no vector database; a human gate before any write.
  - 2:15–2:40 — the Foundry Traces tab for each registered agent, then the evaluation results
    against their thresholds.
  - 2:40–3:00 — the reflection: how the course concepts took this from prototype to production.
  - ~₹800 on a clip-on microphone if Harsh doesn't have one. The software is free (OBS, DaVinci Resolve).
  - **Screen hygiene:** no signed-in Google email, no API keys, subscription ids or connection
    strings in any frame or screenshot. Use a demo account, and crop the portal header.
- **Document — exported as PDF, organised as the brief's Steps 1 → 2 → 3 in its own words, plus the
  sections the Founderz rules require:**
  1. **Purpose and audience** — name the intended users explicitly. Describe Impact as "any
     distributor quoting from a catalogue", not only Surat textile spares.
  2. **Step 1 — design:** the two agents, their tools and data, the policy knowledge source, why
     multi-agent, the information flow (blueprint Fig. 01).
  3. **Step 2 — production readiness:** observability (traces, metrics, the three real diagnosis
     incidents); evaluation (dataset, criteria, thresholds, runs A/B, v1-vs-v2, token usage, the
     drift job); **governance and reliability** (schema + retry, deterministic pricing, the approval
     gate, write tools unreachable, prompt-injection wrapping, grounding through tools, the policy
     document and the reconcile step, tests + CI, the sensitive-data trade-off).
  4. **Step 3 — end-to-end workflow:** the sequence, the contracts passed, tool calls, the final
     quote. **How it would be deployed** (Container Apps + Azure SQL free tier + a GHCR image, the
     already-running original as evidence), monitored, evaluated and improved.
  5. **How it was built and refined** (required by the rules), told through true history: Gemini →
     Foundry; the 342-candidate catalogue flood → a two-stage ranker; a post-hoc token count → a live
     budget governor; the Customer/Catalogue split rejected in favour of Intake/Resolve.
  6. **Screenshots and example interactions** (required): the Desk, the trace panel, the approval
     card, Foundry traces, the evaluation results.
  7. **Key lessons learned** (required), honestly: a model will skip a formatting instruction, so
     parse defensively; split agents by capability, not by invented questions; measure quality, don't
     assume it; the thing that makes the architecture interesting is knowing when *not* to decide.
  - Include the updated blueprint diagrams (Fig. 03 → dataset + trace evaluation) and the repo URL
    **if the anonymity check allows it**.
- **Open input:** the module 6 summary (orchestration), if it can be obtained. If the course teaches
  Foundry's own workflow orchestration, the document names it and explains why MAF Workflows were
  chosen: checkpointed human approval, and state that survives a restart.

---

## Extras — after the original plan, only if time remains (added 2026-09-17)

A judging-criteria review (Innovation / Usability / Impact, each /30) found the biggest remaining gaps are
Usability (a human cannot resolve an unclear line on the approval card; paste-only intake) and Impact
evidence. **Harsh's rule: nothing already in this plan is shrunk or cut to make room.** These are built
only after foundry-04 → foundry-08 all work end to end on Foundry, in this order, and added to the video
if built. Tracked in `tasks/README.md`'s Extras table.

1. **Approval-card line picker** — candidates computed in code, selection validated server-side,
   re-priced through the existing pricing path before the draft is created (`tasks/task-extra-01-line-picker.md`).
2. **WhatsApp photo intake** — Twilio sandbox webhook (signature verified) + Microsoft Dev Tunnel, reusing
   foundry-04's image intake. Real outbound sending stays a non-goal (docs/SPEC.md §9).
3. **Faster Resolve, middle version** — see Step 6's recorded latency decision.
4. **Impact evidence** — a real anonymised enquiry or distributor quote; measured cost per quote.

Email intake is not queued (low value next to WhatsApp here); name it as the next channel.

## Schedule (today is 13 Sep)

| When | Work |
|---|---|
| 13 Sep | Step 0 (Harsh) · Step 1 fork · Step 2 repo and first push |
| 14 Sep | 4a Foundry inference + live eval gate |
| 15–16 Sep | 4b Intake Agent + tests |
| 17 Sep | Live kickoff session (09:30 IST) — take Q&A questions about external-agent evaluation · 5a tracing · 5b register |
| 18 Sep | 4c image intake |
| 19 Sep | 4d policy grounding · dataset + ground truth · 10 live runs · 5c runs A and B (baseline-v1) + screenshots |
| 20 Sep | Latency fix → re-evaluate as v2 → v1-vs-v2 comparison screenshot · drift workflow · bugs · both builds green |
| 21–22 Sep | Document + video |
| 23 Sep | Submit (a full day before the deadline) |

**Cut if late, in this order:**

1. The drift workflow — describe it in the document only.
2. The FoundryOps console — run both evaluations in the portal.
3. 4d.
4. 4c.

Steps 4a, 4b, 5a, 5b, **5c run A and the v1-vs-v2 comparison** are required by the brief or module 5,
and can't be cut.

---

## Verification

After every step, in the fork:

```bash
dotnet build QuoteDesk.sln -warnaserror
dotnet build QuoteDesk.sln -c Release -warnaserror    # CA1848 only fires in Release
dotnet test --filter "FullyQualifiedName!~Evals"
cd src/QuoteDesk.Web && npm run build && npm run lint
```

New logging call sites use `[LoggerMessage]` partial methods (the pattern in `EnquiryPipeline.cs`).
New files follow `.editorconfig` (`EnforceCodeStyleInBuild` is on).

End to end, before recording:

1. The live worked example completes through approval on Foundry.
2. The photo enquiry shows a `verify_catalogue_term` call in the in-app trace.
3. Both registered agents show traces in the portal.
4. Dataset run A and trace run B both finish at or above the thresholds, and token usage is recorded.
5. The v2 re-evaluation finishes, and the portal comparison shows no metric regressing below its threshold.
6. A deliberately forced 429 still shows the replay picker.
7. The Founderz upload form has been checked for the anonymity rule, and every frame and screenshot
   passes the screen-hygiene check.
