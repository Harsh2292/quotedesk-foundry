# Task foundry-05 — Quotation Policy grounding

**Depends on:** foundry-03. Plan: `docs/FOUNDRY-PLAN.md` §4d.

**No longer blocked.** Confirmed 2026-09-13: module 5 and the module 8 brief are the complete course
material — no module 6 or any other summary is coming. The design below is this project's own
judgment, stated as such in the submission document rather than presented as following a taught
pattern.

## Goal

Module 8 asks how each agent's responses are *grounded* by tools **and knowledge sources**. QuoteDesk
has the tools; it has no knowledge source. Give Narrate one real one — the company's actual quotation
policy — so the narration can cite *why* a discount is within policy instead of asserting it.

## What to build

1. `Prompts/quotation-policy.md` — the real rules already written out in `docs/DOMAIN.md`: the slab
   ladder, tier discounts, the 15% combined cap, the 10% margin floor, freight zones and the waiver
   threshold, 15-day quote validity. Add a version line at the top (`v1`, dated).
2. Add it to `PromptLibrary` alongside `intake.md`/`resolve.md`/`narrate.md`.
3. Narrate's instructions include the policy text and are told to cite it when explaining a discount
   or a margin note — e.g. "the customer's 8% ask is within the 200+ slab plus tier B, per policy."
4. **The numbers still come only from `QuoteDesk.Domain`.** This document grounds the *explanation*;
   it must never become a second source of the arithmetic. If a prompt change ever makes Narrate
   compute rather than cite, that's a rule-1 violation — treat it as a bug, not a style choice.

## Acceptance criteria

- [x] `quotation-policy.md` exists, versioned, matches `docs/DOMAIN.md`'s actual numbers exactly
- [x] `PromptLibraryTests` covers it the same way as the other three prompts
- [x] A live run's narration visibly cites policy for a discount that's within it
- [x] No change to how `PricingTools`/`QuoteDesk.Domain` compute anything
- [x] Both build configs and the full non-eval test suite pass

## Out of scope

Any Foundry-hosted file-search / knowledge-source feature — that belongs to the Foundry Agent Service
runtime, which this app does not run. This is a plain embedded prompt document, and the plan is
explicit that this is a deliberate choice, not an oversight.

## Notes on completion

**Done 2026-09-18.** `Prompts/quotation-policy.md` (`v1 — 2026-09-18`) ships embedded, `PromptLibrary`
exposes it plus `NarrateWithPolicy`, and the Narrate agent is built with the composed text.

**One decision beyond what this file asked for, and the reason for it.** The task's own example
sentence — "the customer's 8% ask is within the 200+ slab plus tier B" — cannot be written without
knowing that 250 units lands on the 200+ rung and that the split is 6% + 2%. Leaving the model to work
that out would have it doing arithmetic on money to explain money, which is rule 1 in everything but
name. So the components are now handed to it: `PricingEngine` already computed `slabPct` and `tierPct`
internally and discarded them, and they are now reported on `PricedLine`
(`SlabDiscountPct`/`TierDiscountPct`/`DiscountCapped`), carried through `PricedQuoteLine`, and joined
by `ResolutionResult.CustomerTier` read from the customer record in code. The model cites; it never
adds. No pricing behaviour changed — `DiscountPct`, `NetUnitPrice` and every total are computed
exactly as before, which the untouched existing `PricingEngineTests`/`WorkedExampleTests` still prove.

**`QuotationPolicyGroundingTests` is the part worth keeping.** A knowledge source that drifts from the
code is worse than none, because the narration cites it with full confidence. So the test reads every
number back out of the shipped Markdown and asserts it against `QuoteDesk.Domain`'s constants — the
slab ladder (including that the document has no rung the code lacks), tier rates, the 15% cap, the 10%
floor, GST, the freight table and the waiver boundary. Change either side alone and it fails.

**Two live runs against Foundry, and what the first one taught.** The first run cited policy correctly
but regressed badly in discipline: nine sentences reading the whole line table back, a mention of the
15% combined cap on a line that was never capped, and unsolicited advice to "ensure" something. Adding
a knowledge source makes a narration wordier unless the prompt pushes back. `narrate.md` was
restructured to lead with the output's shape, cap it at two or three sentences, allow **at most one**
cited rule and only one that actually fired, forbid advice, and carry the bad output itself as a
worked counter-example. The second run: *"Quote for Shreeji Textiles, ₹69,237.68 all in. One line is
unresolved: ring frame spindle tape (12 units) needs a thickness decision before this goes out. The
quote is clean otherwise. The discount is policy-driven: the 200-or-more slab plus tier B yields the
applied discount."* Four sentences, one rule, cited, no advice.

**Known gaps, deliberately not chased here:**
- The narration says "yields the applied discount" rather than naming the 6% and 2% it was handed —
  vaguer than the prompt's own worked example. Restating given components is not arithmetic, so this
  is a wording miss, not a rule-1 risk. Left for `foundry-07`, which evaluates narration quality
  properly, rather than spending further live runs guessing at prompt wording.
- "The quote is clean otherwise" sits oddly next to naming an unresolved line. Same place to fix.

**A process note for the next session:** the live eval was run three times, one of which was pure
waste — the first invocation's `--logger console;verbosity=detailed` output was grepped for the
narration only, so the pass/fail line was filtered out and the run was repeated just to read it. Grep
for `foundry-05|Passed|Failed` in one invocation. Every live run bills real money.
