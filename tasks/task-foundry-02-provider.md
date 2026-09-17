# Task foundry-02 — Foundry inference provider

**Depends on:** foundry-01 (done). Step 0's Foundry resource already exists and is confirmed live —
**no model deployments are needed**: `docs/FOUNDRY-PLAN.md` Step 0 records a live-verified finding
(2026-09-14) that Foundry's "instant access" preview feature serves `gpt-5-mini` and `gpt-5-nano` by
name with zero deployment, through the exact `/openai/v1/chat/completions` endpoint this task already
plans to use — both models answered `HTTP 200` in a real test call. Use the model names directly.

## Goal

Swap the LLM provider to Microsoft Foundry with the smallest possible change, because the
OpenAI-compatible `IChatClient` seam this project already has is exactly what Foundry's resource
endpoint speaks.

**Confirmed 2026-09-15 by reading `ChatClientFactory.cs` directly: no new NuGet package is needed for
this task.** `CreateOpenAiCompatible` already uses the plain `OpenAI` package's `OpenAIClient` — not
`Azure.AI.OpenAI`, not `Azure.AI.Projects` — with an arbitrary `Endpoint` and API key, exactly what
the `"github"` arm already exercises. The live `curl` proof in Step 0 used this identical wire
protocol. `Azure.AI.Projects`/`Azure.Identity` are unrelated to inference — they're needed only for
`foundry-07`'s evaluation client, and `Azure.Monitor.OpenTelemetry.AspNetCore` only for `foundry-06`'s
tracing. Neither belongs in this task.

## What to build

1. `src/QuoteDesk.Agents/Llm/ChatClientFactory.cs` — a third arm, one line, reusing the existing
   private method: `"foundry" => CreateOpenAiCompatible(options, model)`. **Keep** the `"gemini"` and
   `"github"` arms as a fallback if Foundry throttles mid-recording later.
2. `LlmOptions` — rename `ExtractModel` → `IntakeModel` (this task only renames the config property;
   `foundry-03` does the actual executor rename). `ChatClientRegistry.Extract` → `Intake`. Fix
   `Endpoint`'s doc comment, which currently claims it's meaningful only for `"github"`.
3. `appsettings.json` — `Provider: "foundry"`, `Endpoint` to the resource's `/openai/v1/` URL (not
   the project endpoint — that 404s), `IntakeModel`/`NarrateModel` to `gpt-5-nano` and `ResolveModel`
   to `gpt-5-mini` (called by name via instant access, no deployment behind them), `TokenBudget`
   20000 → 60000, `MaxToolCalls` 8 → 10.
4. `QuoteDeskApiFactory` — set `Llm__Provider` explicitly in its constructor (it currently falls
   through to the `appsettings.json` default and would silently start needing a real endpoint the
   moment that default changes).
5. `tests/QuoteDesk.Evals/FoundryWorkedExampleEval.cs`, copied from the Gemini one — run it live.

## Acceptance criteria

- [x] `ChatClientFactory` has a working `"foundry"` branch; `"gemini"`/`"github"` unchanged
- [x] `appsettings.json` and `LlmOptions` updated per above
- [x] `FoundryWorkedExampleEval` passes live: the full worked example, three real tool calls, through
      `FunctionInvokingChatClient` — not a hello-world completion
- [x] If a "provider rejected schema-enforced output" warning appears in that eval's log,
      `Llm:UseStructuredOutput` is set to `false` and the eval passes again — no such warning
      appeared, so `UseStructuredOutput` stays `true`
- [x] `dotnet build` (both configs) and `dotnet test --filter "FullyQualifiedName!~Evals"` still pass

## Out of scope

The Intake Agent rename and its new tool (`foundry-03`). Image intake (`foundry-04`). Tracing
(`foundry-06`).

## Notes on completion

Done 2026-09-15. All five "What to build" items landed and all five acceptance criteria are met.

- **`ChatClientFactory`**: added `"foundry" => CreateOpenAiCompatible(options, model)`, reusing the
  identical helper `"github"` already used — no new code path, confirming the task's own premise that
  Foundry's resource endpoint speaks the same OpenAI-compatible wire protocol. `"gemini"`/`"github"`
  are untouched. The unknown-provider exception message now lists all three.
- **`LlmOptions`**: `ExtractModel` → `IntakeModel` (property only, per the task's explicit scope —
  `ExtractExecutor`, `PromptLibrary.Extract`, the `"Extract"` stage name and `QuoteDeskWorkflow`'s
  `nodes.Extract` are all untouched, reserved for foundry-03). The class-level `Provider` **default**
  was deliberately left at `"gemini"`, not changed to `"foundry"`: `GeminiWorkedExampleEval` and its
  two siblings build an `LlmOptions` without setting `Provider` at all, relying on that default to
  route through `Google.GenAI`'s native SDK — changing the class default would have silently
  reintroduced the `thought_signature` bug for those evals the next time someone runs them. Foundry
  becomes the actual default the app uses via `appsettings.json`'s `Provider: "foundry"` instead,
  exactly where the task specified it.
- **`ChatClientRegistry`**: `Extract` property → `Intake`. `EnquiryPipeline.cs` (its one consumer) was
  updated at both call sites (`options.IntakeModel`, `chatClients.Intake`) — everything else on that
  line (`prompts.Extract`, `name: "Extract"`) is untouched, same reasoning as above.
- **`appsettings.json`**: `Provider: "foundry"`, `Endpoint` set to the resource's `/openai/v1/` URL
  (confirmed working live, not the project endpoint), `IntakeModel`/`NarrateModel: "gpt-5-nano"`,
  `ResolveModel: "gpt-5-mini"`, `TokenBudget: 60000`, `MaxToolCalls: 10`.
- **`QuoteDeskApiFactory`**: sets `Llm__Provider=foundry` explicitly in the constructor now, so the
  integration-test host no longer depends on `appsettings.json`'s default.
- **`FoundryWorkedExampleEval.cs`**: new, copied from `GeminiWorkedExampleEval`'s structure. Passed
  live: 42s wall time, real multi-turn tool calls through `FunctionInvokingChatClient` against
  `gpt-5-mini` (not a hello-world completion) — `resolve_customer` fired and a single
  `ApprovalRequiredEvent` was reached with no `ErrorEvent`. No schema-rejection warning appeared in
  the run, so `Llm:UseStructuredOutput` stays `true` (item 8/the conditional step was not needed).
- Three sibling Gemini eval files (`GeminiWorkedExampleEval.cs`, `GeminiFlashLiteWorkedExampleEval.cs`,
  `Gemini35FlashLiteWorkedExampleEval.cs`) had one stale inline comment each fixed
  (`ExtractModel` → `IntakeModel`) — a direct, unavoidable consequence of the property rename, not
  scope creep.

**A real bug found and fixed along the way, unrelated to the code:** the live eval's first run failed
with `HTTP 401` straight from a raw `curl` against the Foundry endpoint (bypassing all app code
entirely), which ruled out a code problem immediately. Comparing the `Llm:ApiKey` stored in local
`dotnet user-secrets` (53 chars, an `AQ.`-prefixed value) against the resource's actual live key
(fetched read-only via the already-authenticated `az cognitiveservices account keys list`, 84 chars)
confirmed a straight mismatch — the stored key was stale, not the currently valid resource key
`docs/FOUNDRY-PLAN.md` Step 0 refers to. Updated local user-secrets with the current live key (not a
portal action, so this did not require Harsh directly — see CLAUDE.md's "ask him only when it
genuinely requires him" carve-out); the eval passed immediately after. **Harsh: this confirms the
Step 0 hygiene note about regenerating the key is worth doing soon** — whatever produced the stale
53-char value should be tracked down so it doesn't happen again silently.

**Verification run, all green:**
```
dotnet build QuoteDesk.sln -warnaserror              # 0 warnings, 0 errors
dotnet build QuoteDesk.sln -c Release -warnaserror    # 0 warnings, 0 errors
dotnet test --filter "FullyQualifiedName!~Evals"      # 129 unit + 52 integration, all passed
dotnet test tests/QuoteDesk.Evals --filter "FullyQualifiedName~FoundryWorkedExampleEval"  # passed live, 42s
```
