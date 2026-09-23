# QuoteDesk on Microsoft Foundry

> A multi-agent system that turns a messy customer enquiry — typed, or a photo of a handwritten list in
> English, Hindi and Gujarati — into a priced, checked draft quotation, and refuses to guess at the
> parts a human should decide. **The model never decides money**, and nothing is sent until a person
> approves.

Built for the **Microsoft Agent-a-Thon, Level 3 (Architect)**. The domain, data layer and pipeline are
adapted from my earlier project, [Quote-Desk](https://github.com/Harsh2292/Quote-Desk); the Foundry
integration, the two-agent design, image intake, observability and evaluation were built for this entry.

**Submission document:** [`docs/submission/QuoteDesk-submission.html`](docs/submission/QuoteDesk-submission.html)
(exported to PDF for the entry) · **Diagrams:** [`docs/submission/diagrams/`](docs/submission/diagrams/)

---

## The problem

A distributor of textile-machinery spares gets enquiries from people in a hurry who know products by
nickname: *"250 nos of the 6203 bearings, same as last time · 12 pcs ring frame spindle tape, the
thicker one · need by 5th · last time you gave 8%"*. Often it's a phone photo of a handwritten list on
WhatsApp. Turning one into a quotation takes a salesperson about twenty minutes of lookups, usually at
the end of the day. QuoteDesk puts a checked draft in front of them in about a minute.

## How it works

```
Enquiry (text + photo) → Intake Agent → Resolve Agent → Price (C#) → Approve (human) → Create · Send
```

| Stage | What it does | Who |
|---|---|---|
| **Intake** — *perceives* | Reads the text and the photo into structured lines; checks every handwritten product phrase against the catalogue; writes *"(could not read: 2Z or ZZ)"* rather than picking | `gpt-5-nano` (text) · `gpt-5.6-sol` (photo) |
| **Resolve** — *decides* | Chooses its own tool calls: match the customer, search the catalogue, read order history, check stock. Resolves "same as last time" from history; leaves "the thicker one" unresolved | `gpt-5-mini` |
| **Price** | Slab and tier discounts, 15% cap, 10% margin floor, freight, GST, delivery dates, late-delivery and short-stock warnings — all deterministic C# | code |
| **Narrate** | One short summary grounded in a versioned quotation-policy document; cites the rule, never computes | `gpt-5-nano` |
| **Approve** | One card: the photo beside the lines, priced / override / unresolved, each with its reason. Approve, reject, or edit and re-run | a person |

The sequence is fixed — it never reorders or skips. The workflow is **Microsoft Agent Framework**
(.NET), checkpointed to SQL so a run paused for approval survives a restart.

**Four rules the code enforces:** the model never decides money · no raw SQL from the model (typed
tools, EF Core) · write tools are unreachable from the agents · every stage and tool call is traced.

## Microsoft Foundry, four ways

| | |
|---|---|
| **Inference** | Every model call goes to Foundry models, routed per stage. |
| **Tracing** | Each agent emits OpenTelemetry spans (stable ids `quotedesk-intake-v1`, `quotedesk-resolve-v1`, `quotedesk-narrate-v1`) to the Application Insights resource connected to the project. Photographs are deliberately kept out of spans. |
| **Registration** | Both agents are registered as **external agents**, so Traces, Monitor and Insights work per agent. |
| **Evaluation & Insights** | A hand-written dataset with ground truth, run as a baseline and again after the fixes it found (below). Insights independently flagged a real defect from traces, fixed the same day. |

## Evaluation — what it found, what changed

Same 8 cases, same judge (`gpt-5.6-luna`, separate from both agents), same 8 evaluators:

| Evaluator | v1 baseline | v2 after fixes |
|---|---|---|
| ResponseCompleteness | 7/8 · 3.38 | **8/8 · 3.75** |
| Relevance | 7/8 · 3.88 | **8/8 · 4.25** |
| IntentResolution | 6/8 · 3.12 | **8/8 · 3.38** |
| Coherence | 8/8 · 4.50 | 8/8 · **4.88** |
| Fluency | 8/8 · 3.88 | 8/8 · 3.75 |
| Violence · IndirectAttack | 8/8 · 8/8 | 8/8 · 8/8 |
| TaskCompletion | 2/8 | 2/8 |

The baseline caught real bugs — order history hid older purchases, "need by 5th" was read as a past
date, a stock shortfall went unstated, an empty quote was charged freight — all fixed in code with
tests. **TaskCompletion stays at 2/8 by design:** every failure reads *"stopped at the human approval
gate"*. A default evaluator rewards autonomy; this system is built to stop for a person.

Results files: [`tests/QuoteDesk.Evals/results/`](tests/QuoteDesk.Evals/results/) ·
dataset and its corrections: [`tests/QuoteDesk.Evals/dataset/`](tests/QuoteDesk.Evals/dataset/)

## Run it locally

Needs .NET 10 SDK, Node 24, Docker, a Microsoft Foundry resource, and a Google OAuth client id for
sign-in.

```bash
docker compose up -d sql

dotnet user-secrets set "Llm:ApiKey" "<foundry-key>"                      --project src/QuoteDesk.Api
dotnet user-secrets set "AzureMonitor:ConnectionString" "<app-insights>"   --project src/QuoteDesk.Api   # optional: tracing
dotnet run --project src/QuoteDesk.Api --launch-profile http              # http://localhost:5080

cd src/QuoteDesk.Web && npm install && npm run dev                        # http://localhost:8080
```

The Foundry endpoint and per-stage models are in `src/QuoteDesk.Api/appsettings.json` (`Llm` section).
Secrets never go in that file.

## Tests

```bash
dotnet build QuoteDesk.sln -warnaserror
dotnet build QuoteDesk.sln -c Release -warnaserror
dotnet test --filter "FullyQualifiedName!~Evals"      # 245 unit + 90 integration, no network, no API key
```

Integration tests use a stubbed chat client, so CI needs no key. Live evals in `tests/QuoteDesk.Evals`
call a real model and are run deliberately, never in the default suite.

## Honest limits

- **Handwriting has a ceiling.** On a messy eight-line photo the reader still mistakes look-alike
  characters (6209/6204, 30/80). The design answer is to flag doubt and put the photo beside the lines
  for a human — not to claim perfect reading.
- **Resolve is the slow stage** (roughly 25–50 s on a typed enquiry, over a minute on a long handwritten list). A faster design — routine lookups in
  code before Resolve — was designed and estimated, and deliberately not built, to keep the agent's own tool choices.
- **Not built:** WhatsApp and email intake (paste and photo today; every channel maps to the same
  enquiry shape), several photos per enquiry, a line picker on the approval card, cloud deployment of
  this fork.

## Repository map

```
src/QuoteDesk.Domain/   pricing rules — zero dependencies, exhaustively tested
src/QuoteDesk.Data/     EF Core, migrations, deterministic seed
src/QuoteDesk.Agents/   agents, typed tools, workflow, prompts (Prompts/*.md)
src/QuoteDesk.Intake/   channel adapters
src/QuoteDesk.Api/      minimal APIs, SSE, auth, telemetry
src/QuoteDesk.Web/      React — Desk, Approvals, Quotes
tests/                  UnitTests · IntegrationTests · Evals (dataset + results)
docs/                   SPEC · DOMAIN · FOUNDRY-PLAN · SESSION-LOG · submission/
```
