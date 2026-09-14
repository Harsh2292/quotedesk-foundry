# Task foundry-02 — Foundry inference provider

**Depends on:** foundry-01, and **Step 0 of `docs/FOUNDRY-PLAN.md` (Harsh's Azure portal work)** —
the Foundry resource, its `intake`/`resolve`/`judge` model deployments, and a successful `curl`
against `/openai/v1/chat/completions` confirming real TPM quota. Do not start this task until that
`curl` has returned a real completion.

## Goal

Swap the LLM provider to Microsoft Foundry with the smallest possible change, because the
OpenAI-compatible `IChatClient` seam this project already has is exactly what Foundry's resource
endpoint speaks.

## What to build

1. `src/QuoteDesk.Agents/Llm/ChatClientFactory.cs` — a third arm:
   `"foundry" => CreateOpenAiCompatible(options, model)`. **Keep** the `"gemini"` and `"github"` arms
   as a fallback if Foundry throttles mid-recording later.
2. `LlmOptions` — rename `ExtractModel` → `IntakeModel` (this task only renames the config property;
   `foundry-03` does the actual executor rename). `ChatClientRegistry.Extract` → `Intake`. Fix
   `Endpoint`'s doc comment, which currently claims it's meaningful only for `"github"`.
3. `appsettings.json` — `Provider: "foundry"`, `Endpoint` to the resource's `/openai/v1/` URL (not
   the project endpoint — that 404s), the three model settings to the **deployment names** from
   Step 0, `TokenBudget` 20000 → 60000, `MaxToolCalls` 8 → 10.
4. `QuoteDeskApiFactory` — set `Llm__Provider` explicitly in its constructor (it currently falls
   through to the `appsettings.json` default and would silently start needing a real endpoint the
   moment that default changes).
5. `tests/QuoteDesk.Evals/FoundryWorkedExampleEval.cs`, copied from the Gemini one — run it live.

## Acceptance criteria

- [ ] `ChatClientFactory` has a working `"foundry"` branch; `"gemini"`/`"github"` unchanged
- [ ] `appsettings.json` and `LlmOptions` updated per above
- [ ] `FoundryWorkedExampleEval` passes live: the full worked example, three real tool calls, through
      `FunctionInvokingChatClient` — not a hello-world completion
- [ ] If a "provider rejected schema-enforced output" warning appears in that eval's log,
      `Llm:UseStructuredOutput` is set to `false` and the eval passes again
- [ ] `dotnet build` (both configs) and `dotnet test --filter "FullyQualifiedName!~Evals"` still pass

## Out of scope

The Intake Agent rename and its new tool (`foundry-03`). Image intake (`foundry-04`). Tracing
(`foundry-06`).

## Notes on completion

*(fill in once run)*
