# The evaluation dataset

`quotedesk-eval-v1.jsonl` — ten cases, one JSON object per line, for task foundry-07's **Run A**
(dataset evaluation in Microsoft Foundry). `docs/FOUNDRY-PLAN.md` §5c is the full plan; this file
covers only what someone editing the dataset needs to know.

## The one rule

**`ground_truth` is written before the case is ever run, and is never edited to match what the system
actually did.** That is the entire difference between ground truth and a transcript. If a run
disagrees with the ground truth, one of two things is true — the system is wrong, or the expectation
was wrong — and deciding which is the point of the exercise. Quietly rewriting the expectation to
match the output turns the whole dataset into an expensive way of proving that the system does what
it does.

If an expectation genuinely was wrong, change it deliberately, say so in the task file's notes, and
re-baseline: a changed expectation invalidates comparison against earlier runs.

All ten `ground_truth` values in v1 were written on 2026-09-18, before any of them had been run.

## Fields

| Field | Foundry mapping | Notes |
|---|---|---|
| `query` | input | The enquiry body exactly as it would be pasted into the Desk. |
| `ground_truth` | expected outcome | Hand-written. Describes the *outcome* — which SKU resolves and why, what must stay unresolved, what must be flagged — not an exact rupee figure. |
| `response` | output | **Empty until filled from a real run.** See below. |
| `id`, `band`, `sender`, `why` | — | Ours, not Foundry's. `sender` matters: `POST /api/enquiries` falls back to the signed-in Google account when the sender box is blank, which matches no seeded customer, so a case run without its sender silently becomes the unknown-sender case. |

Verify the field mapping in the portal after uploading — module 5 makes this its own step, and a
dataset whose ground truth is mapped to the wrong column scores nonsense rather than failing.

## Why `response` is empty

Foundry never invokes an external agent, so it cannot generate the responses itself. Each case is run
through the real Desk UI — which doubles as live verification — and its actual final output pasted
into `response`. This is the one honest adaptation of module 5's flow, and the submission document
says so rather than glossing it.

**Run each case with its own `sender`.** Ten runs cost real model calls against the shared Foundry
key, so run them deliberately, once, not in a loop.

## The three bands

Cases are spread across severity deliberately, so the suite cannot be passed by a system that is
merely agreeable:

- **`resolvable` (3)** — the agent should get a definite answer, including one that needs order
  history to disambiguate.
- **`needs-a-flag` (3)** — priced, but something (stock, margin, an unknown sender) must be surfaced
  to a human rather than quietly absorbed.
- **`must-refuse` (4)** — the agent must decline to decide: an ambiguity with no evidence, a prompt
  injection, an empty enquiry, and a photograph with an unreadable word.

A dataset of only resolvable cases would score beautifully and prove nothing. The refusal cases are
where this architecture's actual claim lives.

## Every factual claim was read from the database

Customer ids and tiers, SKUs, on-hand quantities, how many variants a family carries, and which
customers have purchase history were all queried against the running seeded database while writing
the cases — never derived from `DeterministicSeeder`'s source by hand. `docs/SESSION-LOG.md` records
a real off-by-one between the index used for a customer's tier and the index used for everything
else, which is exactly the trap that costs an afternoon. Two findings worth keeping:

- **No customer has ever bought a spindle tape.** That is *why* the worked example's spindle tape
  cannot be resolved from history — it is a property of the data, not an accident of the prompt.
- The catalogue carries **eight** Doubling Frame Spindle Tape widths (4mm–11mm), not two.

## What these scores do not prove

Worth stating here as well as in the submission document, because a green dashboard is persuasive out
of proportion to what it measures:

- **Pricing is never judged by a model.** Every rupee is proven by `QuoteDesk.Domain`'s own unit
  tests. A model scoring well on `groundedness` says nothing about whether a discount is correct.
- Two perfect scores say nothing about the metrics that were not run.
- External agents are a preview feature: no human evaluation, no trace-to-dataset conversion and no
  red teaming are available for them.
