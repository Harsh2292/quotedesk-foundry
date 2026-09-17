# Tasks

The work queue and **the only place status lives**. Each task is finishable in one sitting and
delivers behaviour through every layer it touches.

**Start here: `/task foundry-04`.** `foundry-00` (the fork itself) is already done — see its notes.

You type `/task foundry-NN` and `/clear`, plus plan mode with `Shift + Tab`. Verification, the
handover note and the status update happen automatically as part of finishing a task. This is the
same convention `Harsh2292/Quote-Desk` used for tasks 00–11 below; `docs/FOUNDRY-PLAN.md` is this
fork's equivalent of that project's `docs/SPEC.md` — the plan each task file is drawn from.

## Foundry migration — the active queue

| # | Task | Status |
|---|---|---|
| foundry-00 | [Fork mechanics](task-foundry-00-fork-and-repo.md) | done |
| foundry-01 | [New GitHub repo, first push, CI](task-foundry-01-new-repo.md) | done |
| foundry-02 | [Foundry inference provider](task-foundry-02-provider.md) | done |
| foundry-03 | [Intake Agent](task-foundry-03-intake-agent.md) | done |
| foundry-04 | [Image intake](task-foundry-04-image-intake.md) | todo |
| foundry-05 | [Quotation Policy grounding](task-foundry-05-policy-grounding.md) | todo |
| foundry-06 | [Observability + agent registration](task-foundry-06-observability-registration.md) | todo |
| foundry-07 | [Evaluation](task-foundry-07-evaluation.md) | todo |
| foundry-08 | [Video and document](task-foundry-08-submission.md) | todo |

**Carry-over fixes from the foundry-03 code review — do these at the start of `foundry-04`** (its photo
intake depends on them): `verify_catalogue_term` (1) cannot recognise a misspelt word ("tming belt",
"spindel tap") because it only accepts exact whole-word matches — suggest the closest catalogue word
within a small edit distance instead; (2) reports family names such as "SpindleTapes" as unknown,
because recall searches only SKU and name. Fix (1) with a catalogue vocabulary and it covers (2).

## Extras — only after the whole plan above is done

Harsh's rule (2026-09-17): nothing in the planned queue is shrunk or cut to make room for these. They
are built only once foundry-04 → foundry-08 all work end to end on Foundry, in this order, as time
allows, and shown in the video if built.

| # | Extra | Status |
|---|---|---|
| extra-01 | [Resolve an unclear line on the approval card](task-extra-01-line-picker.md) — the human picks the item the agent refused to guess, re-priced in code | todo |
| extra-02 | WhatsApp photo intake (Twilio sandbox + Microsoft Dev Tunnel; reuses foundry-04's image intake). Needs Harsh: Twilio account, sandbox join, dev-tunnel login | todo — no task file yet |
| extra-03 | Faster Resolve ("middle version": routine lookups in code, in parallel; Resolve keeps the order-history decision) — ~31 s → ~20 s estimated. Decided **not** to build unless time remains; presented in the video/document as a measured alternative either way (`docs/FOUNDRY-PLAN.md` Step 6) | todo — no task file yet |
| extra-04 | Impact evidence (Harsh, no code): one anonymised real enquiry or one sentence from a real distributor about quoting time; measured cost per quote checked against Azure Cost Management | todo |

Email intake: considered and not queued — low value next to WhatsApp for this business; mention as the next channel.

Status values: `todo` · `in progress` · `done` · `blocked`. Full context, the schedule, the cut list
and the compliance audit against the course materials: `docs/FOUNDRY-PLAN.md`.

**Step 0 of the plan (the Azure portal work — Foundry resource, model deployments, App Insights,
quota check) is Harsh's and sits outside this numbering**, since no task file can do a portal click
or a credential for him. Nothing from `foundry-02` onward can be verified live until it's done — see
`docs/FOUNDRY-PLAN.md`.

---

## Inherited from Quote-Desk — history, not this fork's queue

Everything below is the original project's task history, carried over because it's how the code this
fork builds on came to exist, and because "how it was built and refined" is part of what the
Agent-a-Thon submission has to say. **None of these rows are open work for this fork** — 00–08 are
the baseline this project inherited; 09a onward describe deploying and extending the *original*
Gemini-based QuoteDesk and are superseded here by the Foundry queue above.

| # | Task | Session | Status (as of the fork) |
|---|---|---|---|
| 00 | [Environment, repo, provider spike](task-00-environment.md) | 1 | done — baseline |
| 01 | [Setup and skeleton](task-01-setup.md) | 1 | done — baseline |
| 02 | [Data, EF Core, migrations, seed](task-02-data-efcore.md) | 1 | done — baseline |
| 03 | [Pricing domain](task-03-pricing-domain.md) | 1 | done — baseline |
| 04a | [Google sign-in and a Users table](task-04a-auth.md) | 2 | done — baseline |
| 04 | [Intake abstraction and paste adapter](task-04-intake.md) | 2 | done — baseline |
| 05 | [Typed tools](task-05-tools.md) | 2 | done — baseline |
| 06 | [Agents and workflow](task-06-agents-workflow.md) | 2 | done — baseline (superseded in this fork by `foundry-03`) |
| 07 | [API, streaming, auth, logging](task-07-api.md) | 2 | done — baseline |
| 08 | [React screens](task-08-web.md) | 2 | done — baseline |
| — | Agent-layer rework (retrieval, structured output, ceilings) | 2 | done — baseline |
| 09a | [Deployable — model routing, rate limiting, container, CI](task-09a-deployable.md) | 2 | **superseded** — was in progress on the original repo; out of scope here |
| 09b | [Deploy to Azure — live URL](task-09b-azure.md) | 2 | **superseded** — this fork's app deployment is cut (see `docs/FOUNDRY-PLAN.md`); the original deployment is cited as evidence the recipe works |
| — | Code review + security review + codebase walkthrough | 2 | **superseded** — belongs to the original repo |
| 11 | [Observability, evals, README, demo](task-11-observability-docs.md) | 3 | **superseded by `foundry-06`/`foundry-07`** — this fork's observability and evaluation are Foundry-native, not the OpenTelemetry-generic version this task planned |
| 10 | [Email and WhatsApp channels](task-10-channels.md) | 3 | **cut** — see `docs/FOUNDRY-PLAN.md`'s cut list |

The original project's own notes on why 11 was reordered before 10, and why 09 was split into 09a/09b,
are left in place below — genuine project history, not this fork's decisions.

**Execution order is 11 before 10, reversing the numbering** (Harsh's call, 2026-09-01): the eval
suite, telemetry and README are the actual differentiators for a portfolio repo and don't depend on
extra channels existing; email/WhatsApp are being deliberately saved for last. Task numbers/file
names are unchanged (10 still means channels, 11 still means observability/evals/README) — only the
row order above, reflecting when each is actually done, changed. Neither task depends on the other
(10 depends on 04, 11 depends on 09), so nothing about swapping them is unsafe.

**Tasks 09–11 were re-scoped on 2026-08-31** to absorb the agent-layer rework and the gaps a full
audit turned up — model routing and the sign-in-screen polish moved into task 09; the eval golden
set, prompt-injection test, per-stage token counts and OpenTelemetry are spelled out in task 11's
"Expanded" section. The small correctness bugs the audit found (a streaming-`401` hole, a
deep-link-`404` infinite load, a swallowed Google `onError`) are recorded in task 08's notes and
belong to the review pass between 09 and 10, not to a numbered task.

**Task 09 was split into 09a/09b on 2026-09-01** — ten distinct pieces of work is not one sitting.
09a is everything verifiable on this machine (the model-routing fix that was the actual blocker, rate
limiting, the container, CI); 09b is Azure and the live URL, needing an account and credentials only
Harsh has.

## Why deploy is task 09 and not last (original project's reasoning)

The previous project was never finished, and an unfinished repo is worth nothing to a recruiter. So
the paste path ships to a public URL the moment it works end to end — before extra channels,
telemetry or evals exist.

From task 09 onward there is always something live to click. Tasks 10 and 11 improve a running
product instead of being prerequisites for one. If the project stops after any of them, what remains
is still a working demo.

## Sessions (original project)

**Session 1 (tasks 00–03)** — environment, repo, and a throwaway spike proving tool calling works on
the Gemini key, then schema and the entire pricing engine under test.

**Session 2 (tasks 04–09b)** — the agent layer, the product, and the deploy.

**Session 3 (tasks 11 then 10)** — telemetry, evals, and the README first; extra channels last.
