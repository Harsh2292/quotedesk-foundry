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

- [ ] `quotation-policy.md` exists, versioned, matches `docs/DOMAIN.md`'s actual numbers exactly
- [ ] `PromptLibraryTests` covers it the same way as the other three prompts
- [ ] A live run's narration visibly cites policy for a discount that's within it
- [ ] No change to how `PricingTools`/`QuoteDesk.Domain` compute anything
- [ ] Both build configs and the full non-eval test suite pass

## Out of scope

Any Foundry-hosted file-search / knowledge-source feature — that belongs to the Foundry Agent Service
runtime, which this app does not run. This is a plain embedded prompt document, and the plan is
explicit that this is a deliberate choice, not an oversight.

## Notes on completion

*(fill in once run)*
