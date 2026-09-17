# Extra 01 — Resolve an unclear line on the approval card

**Depends on:** the whole original plan (foundry-04 through foundry-08) being done first — this is an
**extra**, built only if time remains. Added 2026-09-17 after a judging-criteria review: the worked example's
climax — the salesperson picks the 8mm spindle tape the agent refused to guess — cannot happen in the
app today. The approval card offers approve or reject only, and shows an unresolved line in red with no
way to choose. That is the single biggest Usability gap in the entry.

## Goal

On the approval card, each unresolved line shows a short list of catalogue candidates. The salesperson
picks one (or leaves the line out), the quote is **re-priced in code**, and they approve the re-priced
quote. The model plays no part in this step — rule 1 (the model never decides money) and rule 3 (nothing
leaves without a human) both still hold.

## What to build — the smallest version

1. **Candidates, computed in code, not by the model.** `UnresolvedLine` gains `Candidates[]` (SKU, name,
   distinguishing attribute — no price, no cost). Filled in `ResolveExecutor`'s reconcile step by running
   the existing catalogue search for that line's description. Resolve's model output is unchanged.
   (docs/SPEC.md §8 already names this shape as the deferred design.)
2. **Decision carries selections.** `ApprovalDecisionRequest` gains optional
   `LineSelections[{ originalDescription, sku }]`. The server rejects a SKU that was not one of that
   line's candidates (ProblemDetails 400) — the browser is untrusted too.
3. **Re-price before creating the draft.** On approve with selections, the server builds the new line
   list and prices it through the existing pricing tools — the same deterministic path the Price stage
   uses — and that priced quote is what gets created. No second pricing implementation.
4. **Card UI.** A select per unresolved line ("Choose an item…" / "Leave out"), the re-priced total shown
   before the Approve button is enabled. Loading, empty and error states per CLAUDE.md.
5. `AgentEvent` / TypeScript mirror updated in the same commit if the payload shape changes.

## Out of scope

Editing quantities or prices, adding new lines, free-text SKU entry, re-running any agent.

## Acceptance criteria

- [ ] Worked example: the spindle tape line offers the ring-frame tape variants as candidates, with no
      price or cost in the payload (reflection/boundary test)
- [ ] Approving with a selection creates a quote whose lines include the chosen SKU, priced by the same
      code path as the Price stage (integration test comparing totals)
- [ ] A selection outside the line's candidates is rejected with 400
- [ ] Approve with no selections behaves exactly as today
- [ ] Card renders candidates, shows the re-priced total, handles loading/error
- [ ] Both builds, non-eval tests, `npm run build` pass

## Notes on completion

*(fill in once run)*
