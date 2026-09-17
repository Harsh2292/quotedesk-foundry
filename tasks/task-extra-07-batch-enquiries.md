# Extra 07 — Several enquiries at once, one quote each

**Depends on:** the whole original plan being done, and ideally extra-06 (multi-part enquiries). An
**extra** — designed 2026-09-17, deferred by Harsh for lack of time. Motivation: a salesperson's evening is not one enquiry, it is a pile of them — several customers'
photos and messages. The Desk must take several enquiries in one go and turn each into **its own**
quotation and approval card.

## Goal

On the Desk, the salesperson builds several enquiry drafts (each one a foundry-04 multi-part enquiry with
its own sender), presses **Process all**, and watches them run one after another. Each gets its own trace
and lands as its own card on Approvals.

## Design — the smallest version

- **No new server endpoint.** The client loops the existing `POST /api/enquiries` then
  `POST /api/enquiries/{id}/process` per draft. The pipeline, contracts and approval flow are unchanged:
  N enquiries = N ordinary runs.
- **Sequential, not parallel.** One run at a time: it respects the model provider's rate limits and the
  Api's `RateLimiting:PipelinePermitPerDay` cap (15/day — a batch of 5 spends 5; the UI says so when the
  cap would be hit rather than failing mid-batch).
- **One failure doesn't stop the batch.** A draft that errors (including `provider_rate_limited`) is
  marked failed with its message and a Retry, and the batch continues with the next.
- **Desk state becomes a list.** The session provider (CLAUDE.md "The Desk keeps its state") holds a list
  of drafts, each with its own parts, status (draft / running / awaiting approval / failed) and trace; the
  trace panel shows the selected one. Text and traces persist to `sessionStorage`; images still never do.
  **Update CLAUDE.md's Frontend rule in the same commit** — it currently describes a single enquiry.
- A cap of **10 drafts** per batch.

## Acceptance criteria

- [ ] Desk can hold up to 10 drafts, each with its own sender and parts; add/remove drafts
- [ ] Process all runs them sequentially; each produces its own enquiry, run, trace and approval card
- [ ] A failing draft (tested with a stubbed error) is marked failed with Retry; later drafts still run
- [ ] Hitting the daily pipeline cap mid-batch shows a clear message, not a silent stop
- [ ] Switching between drafts shows each one's own trace; navigating to Approvals and back keeps the list
- [ ] A live batch of 3 mixed enquiries (one multi-photo, one text-only, one single photo) produces 3 cards
- [ ] Both build configs, non-eval tests and `npm run build` pass

## Out of scope

Server-side batch endpoints, parallel processing, background queues, bulk approve.

## Notes on completion

*(fill in once run)*
