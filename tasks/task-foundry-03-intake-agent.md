# Task foundry-03 — Intake Agent

**Depends on:** foundry-02. Plan: `docs/FOUNDRY-PLAN.md` §4b. This is the multi-agent core of the
whole entry — read the plan section in full before writing code, and confirm any Microsoft Agent
Framework signature against the installed package's XML docs first (CLAUDE.md's standing rule).

## Goal

Two agents, split by cognitive role rather than by an invented question. **Intake** perceives — reads
the enquiry (text or, later, a photo) and may check one uncertain term against the catalogue.
**Resolve** decides — unchanged from the original single-agent build, still the one that
cross-references catalogue and customer history and refuses to guess.

**Read this before starting:** an earlier draft split Resolve into "Customer Agent" and "Catalogue
Agent" instead. That was rejected — `get_customer_history` and `check_stock` both need facts only the
*other* half of that split could produce, which would have broken "the 6203 bearings, same as last
time" (the best moment in the worked example). Do not resurrect that design.

## What to build

1. **New tool**, `src/QuoteDesk.Agents/Tools/CatalogTools.cs`:
   `VerifyCatalogueTermAsync(string term)` → `CatalogueTermCheck { Term, Known, Families[],
   ExampleNames[≤3] }`, built on `ICatalogRepository.SearchAsync` plus the existing family-grouping
   logic in `SearchOneAsync`. **Returns no SKU, price or cost** — verify `ToolResultBoundaryTests`
   still passes unmodified. Register as `verify_catalogue_term` in `ReadToolRegistry`.
2. **Rename `ExtractExecutor` → `IntakeExecutor`.** Same pattern as `ResolveExecutor`: it takes an
   `IChatClient` and builds its agent inside `HandleAsync`, because `TracedAIFunction` needs the live
   `IWorkflowContext`. Stage name becomes `"intake"`. Output type stays `ExtractionResult` —
   Resolve, Price, Approve, the contracts and the approval UI are **untouched**.
   - `useSchema: false`, same reasoning as Resolve: a strict response format would also apply to the
     tool-call turns. The existing retry-with-parse-error still guards the final shape.
3. **Prompt** `Prompts/intake.md`, based on `extract.md`, plus explicit instruction to call
   `verify_catalogue_term` only on a genuinely unclear or illegible word, never to guess a product.
   Delete `extract.md` and its `PromptLibrary` property in the same edit (`Load` throws on a missing
   resource, so a half-done rename breaks the DI singleton on first request).
4. `EnquiryPipeline.BuildNodes` — Intake gets only `verify_catalogue_term`; Resolve keeps its four.
   Construct **one shared `ToolCallBudget`** next to `TokenUsageTracker` and pass it to both agents
   (replacing the one currently created inside `ResolveExecutor.HandleAsync`).
5. **Stage union** — `AgentEvent.cs` doc comment; `web/src/api/agentEvents.ts` adds `'intake'` and
   keeps `'extract'` as a legacy value so the fixtures and any stored `TraceJson` rows keep parsing;
   `traceLabels.ts` gets plain-language labels for the new stage and tool.

## Test strategy — read this before touching `WorkedExampleScript`

`WorkedExampleScript` **stays at 6 turns** for the default worked-example test: Extract JSON (now
"Intake JSON") → `resolve_customer` → `get_customer_history` → `search_catalog` → Resolve's final
JSON → Narrate. Intake answers directly here, no tool call — that's the honest default behaviour on a
clean enquiry.

Add a **second, 7-turn script** where Intake's first turn is a `verify_catalogue_term` call before
its JSON answer, proving the tool path actually works. Don't force every test through this path —
only the one that means to.

## Acceptance criteria

- [x] `verify_catalogue_term` implemented, registered, unit-tested (known term, unknown term, empty,
      whitespace), and provably free of SKU/price/cost fields
- [x] `IntakeExecutor` replaces `ExtractExecutor`; stage name `"intake"`
- [x] `intake.md` present, `extract.md` gone, `PromptLibraryTests` updated
- [x] Shared `ToolCallBudget` threaded through `BuildNodes`, `MaxToolCalls` respected across both agents
- [x] The 6-turn worked-example test still passes with Intake making no tool call
- [x] A new 7-turn test proves Intake's tool call is visible in the trace as a `tool_start`/`tool_end`
      pair
- [x] `ResolveAgentToolBoundaryTests` becomes a `[Theory]` covering **both** `IntakeExecutor` and
      `ResolveExecutor` — neither can reach a write tool
- [x] Stage-order assertions updated (`extract` → `intake` everywhere it appears)
- [x] Both build configs and the full non-eval test suite pass

## Out of scope

Image intake (`foundry-04`) — this task's Intake Agent handles text only; the tool and prompt are
written so a photo slots in without further change to this stage's shape.

## Notes on completion

**Done 2026-09-17.** Debug and Release builds clean with `-warnaserror`; 140 unit + 54 integration tests
green; `npm run build` + `npm run lint` pass (two pre-existing `only-export-components` warnings in
untouched files). `FoundryWorkedExampleEval` passed live in 28s on the production model split
(Intake/Narrate `gpt-5-nano` with reasoning off, Resolve `gpt-5-mini`): stages in order
intake → resolve → price, `BRG-6203-2RS` resolved, spindle tape unresolved.

**What was built**
- `verify_catalogue_term` (`CatalogTools.VerifyCatalogueTermAsync`) → `CatalogueTermCheck`. Recall is one
  `SearchAsync` on the term's longest word; `Known` requires every meaningful word to appear as a
  **whole word** in an item, reusing `search_catalog`'s own tokenizer and stop-words — otherwise "ring"
  would be "known" via "bea*ring*", the bug the two-stage ranker already fixed once. Seven unit tests,
  plus a reflection test that the result type has no Sku/Price/Cost property.
- `IntakeExecutor` replaces `ExtractExecutor` (built like `ResolveExecutor`: agent assembled in
  `HandleAsync`, tools wrapped in `TracedAIFunction`, `useSchema: false`). Stage `"intake"`, executor id
  `"Intake"`. `intake.md` replaces `extract.md`, adding the one-tool rules.
- `BuildNodes`: one `ToolCallBudget` per run shared by both agents; **tool lists named explicitly** per
  agent via a `ToolsNamed` helper that throws on an unknown name. This replaced the old
  `Where(t => t.Name != "price_quote")` filter, which would have silently handed Resolve the new tool
  too — the plan did not call this out.
- Tests: `ResolveAgentToolBoundaryTests` is a `[Theory]` over both executors; the 6-turn worked example
  passes unchanged; a 7-turn script proves the tool pair lands between the `intake` and `resolve` stage
  events; a shared-budget test (limit 2) proves Resolve's second call is refused because Intake spent one.
- Web: `'intake'` added to the stage union with `'extract'` kept as legacy; labels "Read the enquiry" /
  "Checked a word against the catalogue"; badge "Intake" for both values.

**Left out / deliberate**
- Resolve's reasoning effort is untouched — testing None/Low there needs several live runs and belongs
  to the latency step (`docs/FOUNDRY-PLAN.md` Step 6), not this task.
- Contract type names (`ExtractedEnquiry`, `ExtractionResult`) kept, per the task.

**Surprises / open decisions for Harsh**
- **`Llm:UseStructuredOutput` is now read by nothing.** Extract was its only consumer; Intake needs schema
  mode off because it calls a tool, and Narrate returns plain text. Intake therefore lost provider-side
  schema enforcement — the parse-retry layer still guards it, and the live run parsed cleanly. Options:
  delete the setting, or give Intake schema enforcement only on a no-tool final turn (more code).
  Documented in `LlmOptions` for now, not removed.
- Whether real Intake ever calls the tool on a clean enquiry is not asserted by the live eval; it is
  expected not to. foundry-04's illegible-photo case is where the live tool call should show up.
- A run suspended at approval **before** this change has checkpoints referencing executor id `Extract`;
  resuming it after the rename may fail. Local demo data only.
- `SearchAsync` matches SKU and Name, not `Attributes`, so a term that only lives in an attribute (e.g. a
  thickness like "8mm") reports `Known = false`. Fine for "is this a product word"; revisit if foundry-04's
  photos need it.

**Post-review follow-up (same day):** a code review found Intake could spend the run's shared tool-call
budget before Resolve started. Fixed with `Llm:IntakeMaxToolCalls` (default 2): Intake draws from a
capped child `ToolCallBudget` of the run's budget, so Resolve always keeps at least
`MaxToolCalls - IntakeMaxToolCalls`; 5 unit tests in `ToolCallBudgetTests`. The review's other two
findings (`verify_catalogue_term` cannot handle misspellings or family names) are carried over to the
start of foundry-04. 145 unit + 54 integration tests green after the fix.

**Next task should know:** Intake's constructor already takes the prompt as a string built in
`HandleAsync` — foundry-04 changes that to a `ChatMessage` carrying `DataContent`, with no change to the
stage's shape or the shared budget.
