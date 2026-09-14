> **Superseded 2026-09-13.** This was the first research pass on the event itself — the dates, prizes,
> judging rubric, official rules, and the corrections to the fabricated guidelines file are all still
> accurate and worth keeping as a record of how the decisions below were reached. **The plan, the
> two-agent design and the scoring in §3, §6, §7 and §8 are not current — read `docs/FOUNDRY-PLAN.md`
> instead**, which is the live source of truth for this fork and reflects everything decided after
> this file was written: Intake/Resolve (not Customer/Catalogue), Foundry tracing and evaluation
> wired in four ways, the cost analysis corrected once TPM headroom was actually worked out, and
> WhatsApp/email/audio all cut. Kept here unedited, warts included, because the reasoning trail is
> itself part of "how it was built and refined" for the submission document.

# Microsoft Agent-a-Thon 2026 — verified brief

Researched 2026-09-12 against Microsoft's own event page, the Founderz official rules, and Microsoft
Learn. **This file supersedes the `agent-a-thon-guidelines.md` that was handed to me** — roughly half
of that document is not in any official source, and one of its instructions would actively break a
real rule. Section 5 lists every discrepancy.

Everything below is either taken from an official source or marked as inference.

---

## 1. The event, verified

| Fact | Value | Source |
|---|---|---|
| Name | Microsoft Agent-a-Thon | microsoft.com/events |
| Kickoff | **17 September 2026**, 90-minute live guided build | microsoft.com/events |
| Live sessions (GMT) | **04:00, 08:00, 15:00** — three regional blocks | Official Rules |
| Asia block, local | 14:00–17:00 AEST → **09:30 IST** start | microsoft.com/events |
| Entry period | 17 Sep 2026 04:00 GMT → **24 Sep 2026 23:59 PDT** | Official Rules |
| Winners announced | 2 October 2026, via LinkedIn | Founderz |
| Prize | Surface Pro Copilot+ PC 12" — **18 winners, 6 per region** (EMEA / Americas / Asia) | Official Rules |
| Where you submit | **The Founderz platform** — not GitHub, not a Microsoft portal | Official Rules |
| Run by | Founderz, Microsoft's authorised Training Services Partner | Founderz |

### The three levels

| Level | Name | Tool |
|---|---|---|
| 1 | Explorer | Agent Builder in Microsoft 365 Copilot (no-code) |
| 2 | Maker | Copilot Studio (no-code) |
| 3 | **Architect** | **Microsoft Foundry** — "build production-grade agents", orchestration, multi-agent, enterprise workflows |

Level 3 is the one Harsh registered for. Note the naming moved: the May 2026 run of this event called
Level 3 "Master". For September it is "Architect".

---

## 2. What must actually be submitted

Straight from the Official Rules. This is the complete required deliverable list:

1. **A video** — "no more than three (3) minutes in duration and 150MB in size", demonstrating the
   innovation, impact and usability of the agent.
2. **Documentation** — in PPTX, PDF, DOCX, DOC, PPT or TXT format, again showing innovation, impact
   and usability.
3. **A description of the AI agent** — its "clear purpose and audience, a summary of how it was built
   and refined, screen shots and example interactions and key lessons learned".
4. **A written description** — the problem addressed, the impact, and how it was built using Copilot
   Studio Lite, Copilot Studio, or Microsoft Foundry.

Uploaded to the Founderz platform before the deadline. That is the whole list.

### Eligibility

18+, legally resident in an eligible EMEA / Americas / Asia country, holds or registers a Microsoft
account, registered for a level, agrees to the rules. India is eligible. Excluded: residents of
sanctioned countries (Brazil, Cuba, Iran, North Korea, Russia, Syria and others), Microsoft/Founderz
employees and their families, and the judges.

### Judging — 90 points total

Three criteria, **30 points each**, judged anonymously by the Pre-Learning Instructors (Microsoft
employees and MVPs):

| Criterion | Range | What it means |
|---|---|---|
| **Innovation** | 0–30 | Originality, creative application of AI |
| **Usability** | 0–30 | Performs well, consistently, intuitively |
| **Impact** | 0–30 | Quantitative and qualitative real-world potential |

Ties break on Innovation first, then Usability, then Impact, then a judge vote.

### Technology constraint

The only stack rule in the entire document: the entry must be built with **Copilot Studio Lite,
Copilot Studio, or Microsoft Foundry**. Nothing about languages, SDKs, packages or frameworks.

### Originality and disqualification

- The entry must be the participant's original work and "cannot have been previously selected as a
  winner in any other contest". Reusing your own existing code is not restricted anywhere.
- Microsoft may reject entries that don't conform to the rules, are incomplete or illegible,
  **exceed length specifications**, or arrive outside the entry period.
- Cheating — multiple IDs, bots, hacking, fraud, tampering — is immediate disqualification.
- Microsoft takes a broad non-exclusive, perpetual, worldwide, sub-licensable licence to use the
  entry, commercially or otherwise. You keep ownership; they get very wide usage rights. Worth
  reading before submitting anything commercially sensitive — relevant given QuoteDesk is intended
  to be sold as a paid implementation service.

---

## 3. What "Microsoft Foundry" means technically for us — *superseded, see docs/FOUNDRY-PLAN.md §3–5*

This is the part the guidelines document got roughly right in spirit and wrong in every detail.

### Microsoft Foundry is two different things

| Thing | What it is | Fits QuoteDesk? |
|---|---|---|
| **Foundry model provider** (direct inference) | Your app owns the agent definition, tools and orchestration. You call a model deployed in a Foundry project. Produces a plain `ChatClientAgent`. | **Yes — this is the drop-in.** |
| **Foundry Agent Service** (Prompt / Hosted agents) | Foundry owns the agent definition, or you ship a container to Microsoft-managed per-session sandboxes. Produces a `FoundryAgent`. | Optional, bigger job, stronger "Architect" story. |

### The direct-inference path — the small version

```csharp
// dotnet add package Azure.Identity
// dotnet add package Microsoft.Agents.AI.Foundry --prerelease

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

AIAgent agent = new AIProjectClient(
        new Uri("<foundry-project-endpoint>"),
        new DefaultAzureCredential())
    .AsAIAgent(model: "gpt-4o-mini", name: "Resolve", instructions: "...");
```

The result is a **standard `AIAgent`** — it supports sessions, function tools, middleware, tool
approval and streaming, exactly like the `AIAgent` QuoteDesk already builds from an `IChatClient`.
`Microsoft.Agents.AI` is already a dependency at 1.19.0 (`src/QuoteDesk.Agents/QuoteDesk.Agents.csproj`).

**So the migration is far smaller than the guidelines document implies.** It does not require
rewriting the tools against `Azure.AI.Projects.Agents`' tool-calling definitions. It is one new branch
in `QuoteDesk.Agents.Llm.ChatClientFactory` next to the existing `"gemini"` and `"github"` profiles.
`EnquiryPipeline`, `QuoteDeskWorkflow`, all seven typed tools, `TracedAIFunction`, `SqlCheckpointStore`,
the approval `RequestPort` and every test stay exactly as they are.

**Resolved (was: "one thing to verify"):** the Foundry resource endpoint speaks the same
OpenAI-compatible `/openai/v1` surface QuoteDesk's existing `CreateOpenAiCompatible` already builds
against, and it accepts an API key. The `IChatClient` seam, `BudgetedChatClient`, and every existing
test survive untouched — see `docs/FOUNDRY-PLAN.md` §Phase 4a.

### Foundry tracing — how the "cloud proof" story gets told

Foundry has first-class agent observability built on OpenTelemetry GenAI semantic conventions, stored
in Azure Monitor Application Insights and viewable in the Foundry portal under
**Observability → Traces**, with spans for model calls, tool invocations and agent steps.

For .NET: set `APPLICATIONINSIGHTS_CONNECTION_STRING`, plus
`OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=SPAN_AND_EVENT`,
`OTEL_SEMCONV_STABILITY_OPT_IN=gen_ai_latest_experimental` and
`AZURE_EXPERIMENTAL_ENABLE_GENAI_TRACING=true`.

This lines up with `tasks/task-11-observability-docs.md`, which already planned OpenTelemetry. Doing
it here means the hackathon work is not throwaway — it is task 11, done early, against a better backend.

### Foundry Hosted Agents — the ambitious version, not taken

Package the agent as a container, push to Azure Container Registry, and Foundry Agent Service runs it
in per-session VM-isolated sandboxes with its own Entra agent identity, a dedicated endpoint,
platform-managed conversations, a durable state store, and Application Insights injected
automatically. **Supports C# and Microsoft Agent Framework explicitly.** Available in South India,
Southeast Asia and 29 other regions.

QuoteDesk already has a Dockerfile and a container that builds and runs. This was considered and
**not taken** — see `docs/FOUNDRY-PLAN.md`'s cut list: it needs Azure Container Registry (a paid line
item), and Foundry's own **external agent registration** gets the same trace-view and evaluation
benefits without hosting the app inside Foundry at all.

---

## 4. Cost — resolved, see docs/FOUNDRY-PLAN.md Step 0

**Microsoft Foundry has no free tier for model usage.** Verified:

- The Foundry *platform* is free; **model tokens bill at standard rates**. `gpt-4o-mini` is
  $0.15/1M input and $0.60/1M output — a demo's worth of traffic is cents, but it is not zero.
- The Azure free account's **$200 / 30-day credit still exists**, but Microsoft's own docs state that
  serverless/model deployment **requires a subscription with a valid payment method — free or trial
  subscriptions will not work**.
- Foundry Agent Service *hosted* agents bill on CPU + memory across active sessions, on top of tokens.

This runs directly into the standing constraint in the `azure-must-stay-free` memory: Harsh's card is
attached, his 12-month free period is over, and **being charged at all is the failure**, not being
charged a lot.

**Resolved:** pay-as-you-go, capped by a minimum-TPM deployment plus the app's own token budget and
rate limiter, deleting the resource on 25 September. Realistic worst case is a few hundred rupees;
Harsh approved up to ₹1,000 as a last resort. Full mechanism in `docs/FOUNDRY-PLAN.md` Step 0.

---

## 4a. "We'll just set a ₹1,000 limit" — that is not a thing Azure does

This needs stating plainly because the whole cost plan rests on it.

**Pay-as-you-go subscriptions have no spending limit, and one cannot be enabled.** Microsoft's own
documentation: *"The spending limit isn't available for subscriptions with commitment plans or with
pay-as-you-go pricing. For those types of subscriptions, a spending limit isn't shown in the Azure
portal and you can't enable one."* The spending limit exists only for credit-based subscriptions
(free account, Visual Studio), where it equals the credit amount and cannot be customised — and
*"Custom spending limits aren't available"* anywhere.

**An Azure Budget is an alert, not a brake.** It emails you when a threshold is crossed; it does not
stop, throttle or disable anything, and the email can lag by up to 24 hours. Setting a ₹1,000 budget
and assuming spend stops at ₹1,000 is exactly the mistake that produces a surprise bill.

### What actually caps the spend

Four real controls, in descending order of how much they matter:

1. **A minimum-TPM model deployment — the only true hard ceiling.** Quota is assigned per
   subscription, per region, per model in Tokens-Per-Minute, and a deployment cannot exceed the TPM
   assigned to it. The minimum is 1,000 TPM. At 1,000 TPM the arithmetic worst case is
   1,000 × 60 × 24 = 1.44M tokens/day; at `gpt-4o-mini` rates that is roughly **$0.40/day even if
   something ran flat out and unattended**. This is a technical ceiling enforced by the service, not
   a promise to watch the bill.
2. **The application's own governors, which already exist.** `BudgetedChatClient` throws the moment a
   run breaches `Llm:TokenBudget`, the `pipeline` fixed-window rate limiter caps
   `POST /api/enquiries/{id}/process`, and `Llm:MaxToolCalls` bounds the tool loop. QuoteDesk was
   already built so a runaway agent cannot spend without limit — that work pays off here.
3. **Delete the Foundry resource on 25 September.** The only way to guarantee a meter stops is to
   remove the resource.
4. **A budget alert at a low threshold (say ₹200)** — useful as an early warning, and nothing more.
   Treat it as a smoke detector, not a sprinkler.

### A scheduling risk worth knowing now

New Foundry subscriptions frequently show **0 TPM quota for every Azure OpenAI model in every
region**, and the quota has to be requested manually before anything can be deployed. That request is
not instant. **Create the Foundry project and request quota before 17 September**, not on the day —
discovering a zero-quota subscription during the 90-minute live session would cost the whole session.

---

## 5. What the handed-over `agent-a-thon-guidelines.md` gets wrong

The file is confidently written and largely invented. Treating it as authoritative would cost real
work and, in one case, would have broken an actual rule.

### Dangerous — would violate a real rule

| Claim in the file | Reality |
|---|---|
| "3-to-5 minute video" | The Official Rules say **no more than 3 minutes and 150MB**. "Exceeds length specifications" is explicit grounds for rejection. A 4-minute video is a rejected entry. |

### Fabricated — no official source says any of this

| Claim in the file | Reality |
|---|---|
| "Pre-dated repositories will disqualify you; repo history must start on or after 17 Sep" | Not in the rules. Repositories are not mentioned at all. |
| "Deliverable 1: Public GitHub Repo" | Not required. Submission is a video plus a document on the Founderz platform. |
| "Deliverable 4: Architecture Diagram (flowchart)" | Not required. The rules ask for "screen shots and example interactions". |
| "Must use `Azure.AI.Projects` or automatic technical disqualification" | The only stack rule is Copilot Studio Lite / Copilot Studio / **Microsoft Foundry**. No SDK is mandated. |
| "Demo video must show data in the Foundry terminal/console logs or it will be discarded" | Not in the rules. Judging is Innovation / Usability / Impact. (Still a *good idea* — see §3 — just not a rule.) |
| "Maximum one entry for Level 3 per person" | The rules state no per-person entry limit. |
| "Add this exact compliance footnote to README.md" | Invented. No such requirement exists. |
| "Level 3 uses .NET 10 / Microsoft Agent Framework" | Level 3 is defined by Microsoft Foundry. Language and framework are free choices. |
| "Passing raw inputs to external LLM endpoints triggers automatic technical disqualification" | No such clause. |

### Right, or close enough

- 17 September kickoff, 24 September deadline. Correct.
- Level 3 = Architect = Microsoft Foundry. Correct.
- 90-minute live session, asynchronous build window afterwards. Correct.
- Reusing your own prior code is legal. Correct — the rules require original work; nothing bars your own.
- `Azure.AI.Projects` and `Azure.AI.Projects.Agents` are real packages. Correct — though
  `Azure.AI.Projects` is at **stable 2.0.1**, so `--prerelease` is unnecessary, and for our purposes
  `Microsoft.Agents.AI.Foundry` is the better entry point.
- Copy `QuoteDesk.Domain` untouched. Correct — and it genuinely is dependency-free, so this is trivial.

Verify anything else from that file against the Official Rules at https://founderz.com/agentathon-terms/
before it drives a decision.

---

## 6–8. Plan, design decisions and scoring — superseded in full

Everything from here down (the twelve-day plan, the Customer/Catalogue agent split, "try WhatsApp
second", the query-response evaluation workaround, and the scoring estimate) reflects the state of
thinking on 2026-09-12, before the module 5 and module 8 course materials were read in full and
before the Customer/Catalogue split was found to break the worked example's own "same as last time"
disambiguation. **None of it should be followed as-is.** `docs/FOUNDRY-PLAN.md` is the corrected,
current version of all three sections, and states explicitly what changed and why (see its own
"Compliance audit" section). Left below only as the historical record of how the current plan was
reached — legitimate material for the submission document's required "how it was built and refined"
and "key lessons learned" sections.

## Sources

- https://www.microsoft.com/en-us/events/local-events/microsoft-agent-a-thon
- https://founderz.com/agentathon-terms/ — the Official Rules
- https://founderz.com/skilling/agent-a-thon-fz/sign-up/
- https://learn.microsoft.com/en-us/agent-framework/agents/providers/microsoft-foundry
- https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents
- https://learn.microsoft.com/en-us/azure/foundry/observability/concepts/trace-agent-concept
- https://azure.microsoft.com/en-us/pricing/details/foundry-agent-service/
- https://learn.microsoft.com/en-us/azure/cost-management-billing/manage/avoid-charges-free-account
- https://www.nuget.org/packages/Azure.AI.Projects
