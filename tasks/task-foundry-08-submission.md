# Task foundry-08 — Video and document

**Depends on:** foundry-04, foundry-05 (if it landed), foundry-06, foundry-07 — everything the video
and document reference must already work live. Plan: `docs/FOUNDRY-PLAN.md` Step 6. This is the task
that decides the score; do not compress it to make room for more code.

## Goal

A ≤3-minute, ≤150 MB video and a PDF document, submitted to the Founderz platform, that together
demonstrate the system, explain the decisions behind it, and reflect on how the course concepts moved
it from prototype to production — the three things module 8 explicitly asks for, not just a demo.

## Before recording — fix latency

The last live run measured Resolve at 49.6 seconds; two agents plus a vision call may be worse. Pick
faster deployments or trim tool results *before* recording, then re-run `foundry-07`'s evaluations as
`-v2` per its own task — don't record around a problem that a five-minute config change would fix.

## Before recording — rehearse, and mind TPM contention

Do at least one full **dry run** of the entire video script, off-camera, timed, before the real
recording — not a first-attempt read-through on camera. This is also when you'll find out if the
photo enquiry, the trace, and the approval card actually flow as smoothly as the script assumes.

**Watch for token contention on recording day specifically.** Dev testing, `foundry-07`'s evaluation
runs, the dry run, and several real recording takes can all land on the same low-TPM `intake`/
`resolve` deployments in a short window — the same class of problem that caused real rate-limit
failures earlier in this project's history on a different provider. If a take gets a `429` mid-record,
that's a config/timing problem to fix, not a reason to lower the deployment's TPM back down.

## What to build

### Video (≤3:00, ≤150 MB — over-length is explicit grounds for rejection)

- 0:00–0:15 — hook: a two-person sales team losing an evening to manual quoting, then "the model is
  forbidden from the part that matters."
- 0:15–0:35 — the crafted photo goes in; Intake visibly calls `verify_catalogue_term`.
- 0:35–1:20 — the live trace: two named agents on two deployments; the 6203 resolved with its recorded
  reason from order history; the spindle tape flagged red for a human.
- 1:20–1:45 — a human approves; the quotation is created.
- 1:45–2:15 — **the decisions**, over the blueprint diagram: two agents split by cognitive role;
  pricing kept in code; no vector database; a human gate before any write tool.
- 2:15–2:40 — the Foundry Traces tab for each registered agent, then the evaluation results against
  their thresholds.
- 2:40–3:00 — the reflection: how the course concepts (tracing, evaluation, the lifecycle) took this
  from a Gemini prototype with no measured quality to something with a number attached.
- **Screen hygiene, checked on every frame before final export:** no signed-in Google email, no API
  keys, no subscription ids, no connection strings. Use a demo account; crop portal headers.

### Document (exported as PDF)

Sections, in this order:
1. Purpose and intended users, stated explicitly (not implied). Impact framed as "any distributor
   quoting from a catalogue," not only Surat textile spares.
2. **Design** (module 8 Step 1): the two agents, their tools, data and the policy knowledge source,
   why multi-agent beats single-agent, the information flow — blueprint Fig. 01.
3. **Production readiness** (Step 2): observability (traces, the metrics list, the three real
   diagnosis incidents from `docs/FOUNDRY-PLAN.md` §5a); evaluation (dataset + trace runs, criteria,
   thresholds, the v1-vs-v2 comparison, token usage, the drift job); governance and reliability
   (schema + retry, deterministic pricing, the approval gate, write-tool unreachability,
   prompt-injection wrapping, the reconcile step, tests + CI, the sensitive-data trade-off).
4. **End-to-end workflow** (Step 3): sequence, contracts passed, tool calls, the final quote; how it
   would be deployed (Container Apps + Azure SQL free tier + a GHCR image, citing the original
   Quote-Desk deployment as evidence the recipe works), monitored, evaluated, improved.
5. **How it was built and refined** — true history: Gemini → Foundry; the 342-candidate catalogue
   flood → a two-stage ranker; a post-hoc token count → a live budget governor; the Customer/Catalogue
   split rejected in favour of Intake/Resolve.
6. Screenshots and example interactions: the Desk, the trace panel, the approval card, both Foundry
   Traces tabs, both evaluation result screens.
7. Key lessons learned: a model skips formatting instructions, so parse defensively; split agents by
   capability, not by an invented question; measure quality, don't assume it; the interesting agentic
   decision is knowing when *not* to decide.
- Updated blueprint diagrams (Fig. 03 needs updating to show dataset + trace evaluation, not the
  earlier query-response-only design). Repo URL included **only if the anonymity check below allows it**.

### One check that has to happen before submitting

Look at the actual Founderz upload form for whether a name or repo URL may appear anywhere in the
video or document — the rules say judging is anonymous, and this hasn't been confirmed either way.

## Acceptance criteria

- [ ] Latency fixed and re-evaluated (`foundry-07`'s v2 runs) before recording starts
- [ ] Video is ≤3:00 and ≤150 MB, covers all six segments above, passes the screen-hygiene check
- [ ] Document covers all seven sections, exported as PDF
- [ ] Every diagram is current (Fig. 03 updated for dataset + trace evaluation)
- [ ] The anonymity check on the Founderz form has been done, and the document/video match its answer
- [ ] Submitted to the Founderz platform with time to spare before 24 Sep 23:59 PDT

## Out of scope

Any further feature work — if something here reveals a bug, fix the minimum needed to make the demo
honest, don't scope-creep back into `foundry-04`/`05`.

## Notes on completion

*(fill in once run)*
