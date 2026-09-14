# Task foundry-07 — Evaluation

**Depends on:** foundry-06 (agents must be registered and tracing before trace evaluation makes
sense). Plan: `docs/FOUNDRY-PLAN.md` §5c — read it in full before starting; it follows module 5's six
steps and six lifecycle stages in order, and this task file assumes that context rather than
repeating all of it.

## Goal

Do both kinds of evaluation module 5 and this project's own architecture call for: a **dataset**
evaluation with hand-written ground truth (module 5's own flow, adapted honestly since Foundry never
invokes an external agent directly), and a **trace** evaluation on the two registered agents' real
production traces (what the registration in `foundry-06` actually buys). Then prove the lifecycle is
real, not just described, by making one deliberate change and re-evaluating.

## What to build

1. **The dataset**, `tests/QuoteDesk.Evals/dataset/quotedesk-eval-v1.jsonl` — 10 cases, each with a
   hand-written `ground_truth` decided *before* the enquiry is ever run:
   resolvable (a clean single line; the Shreeji worked example; a repeat customer) · needs a flag
   (short stock; a margin-floor line; an unknown sender) · must refuse (the spindle tape with no
   history; a prompt-injection enquiry; an empty enquiry; an unreadable one) — plus the crafted photo
   from `foundry-04`, so Intake's tool call is represented in the golden set too.
   Run all 10 through the real Desk UI (doubles as live verification), then copy each run's real
   output into `response`.
2. **Run A**, dataset evaluation, following module 5's steps in order: upload the JSONL → verify the
   field mapping (`query`→input, `ground_truth`→expected, `response`→output) → select the separate
   `judge` deployment (never the target) → scope: individual turns → evaluators (below) → run as
   `quotedesk-dataset-baseline-v1`.
3. **Run B**, trace evaluation on the registered agents' live traces — no dataset to build, this is
   what `foundry-06`'s registration was for. Portal: open each agent → Evaluation → traces. Repeatable
   as code in a new `tools/QuoteDesk.FoundryOps` console app (propose the two new packages —
   `Azure.AI.Projects`, `Azure.Identity` — to Harsh first): `evaluate --agent quotedesk-resolve:1
   --run-name traces-baseline-v1`, using the `azure_ai_traces` data source with an agent filter.
4. **Evaluators**: `coherence`, `fluency`, `task_adherence`, `tool_call_accuracy`, `groundedness`,
   `relevance`, `violence` (safety) on both runs; add `response_completeness` on run A (it's the one
   that actually uses `ground_truth`) and `intent_resolution` for Intake on run B.
5. **Thresholds, fixed before the baseline runs, not after seeing the scores:** every 1–5 metric ≥ 4;
   pass/fail metrics ≥ 9/10; safety 10/10; the prompt-injection case must pass `task_adherence`.
6. **Do the lifecycle, don't just describe it.** After the latency fix (whatever `foundry-08`'s
   pre-recording pass turns out to need — a faster deployment, a trimmed prompt), re-run both
   evaluations as `-v2`. **Compare v1 and v2 in the portal's comparison view and screenshot it** —
   this is module 5's own regression-detection step, done for real.
7. **Record token usage** (target + judge, both runs, both versions) next to the scores.
8. **Drift monitoring** — `.github/workflows/eval-drift.yml`, running the FoundryOps trace evaluation
   on a 24-hour lookback, **`workflow_dispatch` only** (never scheduled — this must not spend tokens
   unattended). Trigger it once by hand as proof it works; present it in the document as the job a
   real deployment would put on a schedule.
9. **State what the scores don't prove**, in the document: pricing is never judged by a model, it's
   proven by `QuoteDesk.Domain`'s own unit tests; two perfect scores say nothing about metrics that
   weren't run; external agents are a preview feature with no human evaluation, trace-to-dataset
   conversion, or red teaming available.

## Acceptance criteria

- [ ] `quotedesk-eval-v1.jsonl` committed, 10 cases, each with real `ground_truth` written before the run
- [ ] Run A (`quotedesk-dataset-baseline-v1`) completes, every threshold met, screenshot taken
- [ ] Run B (`traces-baseline-v1`) completes, every threshold met, screenshot taken
- [ ] Both re-run as `-v2` after the latency fix; the portal comparison view screenshotted, no metric
      regressed below its threshold
- [ ] Token usage recorded for all four runs
- [ ] `eval-drift.yml` exists, `workflow_dispatch` only, triggered once successfully
- [ ] The document's evaluation section states what the scores do and don't prove

## Out of scope

Any evaluator this project can't get a straight answer on regional availability for (check safety
evaluator availability in the deployed region before relying on it — note in Notes if it had to be
substituted).

## Notes on completion

*(fill in once run)*
