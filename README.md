# QuoteDesk on Microsoft Foundry

> An agentic RFQ-to-quotation service. Two agents, one human approval gate, and a rule the code
> enforces rather than promises: the model never decides money.

Domain, data layer and pipeline adapted from my earlier project,
[Quote-Desk](https://github.com/Harsh2292/Quote-Desk) — the Microsoft Foundry integration, the
two-agent design, observability, evaluation and image intake were built for the
[Microsoft Agent-a-Thon](https://www.microsoft.com/en-us/events/local-events/microsoft-agent-a-thon)
(Level 3, Architect track).

## What it does

A customer sends a messy enquiry — by paste, email or WhatsApp — nicknames instead of part numbers, a
remembered discount, a photo of a handwritten list. **Intake** reads it (text or image) and can check
an uncertain word against the catalogue before committing to it. **Resolve** cross-references the
catalogue and this customer's own purchase history, checking stock and lead time, and refuses to
guess when it genuinely cannot tell. Pricing is computed by deterministic C#, never by a model. A
salesperson sees one card — resolved lines, flagged lines, the policy note — and approves, edits or
rejects it. Only then is a quote created.

## Architecture in one line

```
Enquiry → Intake Agent → Resolve Agent → Price (code) → Approve (human) → Create · Send
```

The sequence is fixed and never reorders. Intake and Resolve are the two autonomous nodes — each
chooses its own tool calls. Everything with consequences is code. See `docs/FOUNDRY-PLAN.md` for the
full design and why it's shaped this way, and `docs/DOMAIN.md` for the worked example the whole
project is built around.

## The four rules

1. The model never decides money — pricing lives in a dependency-free domain project.
2. No raw SQL from the model. It calls typed tools; data access is EF Core with LINQ.
3. Nothing leaves without a human. Write tools are unreachable from either agent.
4. Every stage and tool call is traced — to the live UI, and to Microsoft Foundry.

## Microsoft Foundry, four ways

- **Inference** — both agents and the narration step call a model deployed in a Foundry project.
- **Tracing** — every model call and tool invocation becomes an OpenTelemetry span in Foundry
  Observability, alongside the live in-app trace a salesperson watches.
- **Agent registration** — both agents are registered as external agents in the Foundry portal, so
  their traces are attributable and browsable per-agent.
- **Evaluation** — a ten-case golden set (resolvable / needs-a-flag / must-refuse) scored against six
  named criteria by a judge model that is never the model being judged, plus trace-based evaluation
  on live runs. Full detail in `docs/FOUNDRY-PLAN.md` Step 5c.

## Development

Start here: `docs/FOUNDRY-PLAN.md` (the fork's own plan) and the last entry in
`docs/SESSION-LOG.md` — nothing else carries context across a `/clear`. `CLAUDE.md` holds the rules
this repo is built under; `docs/SPEC.md` and `docs/DOMAIN.md` are the inherited contract and business
rules. `tasks/README.md` is the work queue — open the folder, run `claude`, and type
`/task foundry-00`.
