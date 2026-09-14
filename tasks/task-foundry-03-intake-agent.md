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

- [ ] `verify_catalogue_term` implemented, registered, unit-tested (known term, unknown term, empty,
      whitespace), and provably free of SKU/price/cost fields
- [ ] `IntakeExecutor` replaces `ExtractExecutor`; stage name `"intake"`
- [ ] `resolve.md`... `intake.md` present, `extract.md` gone, `PromptLibraryTests` updated
- [ ] Shared `ToolCallBudget` threaded through `BuildNodes`, `MaxToolCalls` respected across both agents
- [ ] The 6-turn worked-example test still passes with Intake making no tool call
- [ ] A new 7-turn test proves Intake's tool call is visible in the trace as a `tool_start`/`tool_end`
      pair
- [ ] `ResolveAgentToolBoundaryTests` becomes a `[Theory]` covering **both** `IntakeExecutor` and
      `ResolveExecutor` — neither can reach a write tool
- [ ] Stage-order assertions updated (`extract` → `intake` everywhere it appears)
- [ ] Both build configs and the full non-eval test suite pass

## Out of scope

Image intake (`foundry-04`) — this task's Intake Agent handles text only; the tool and prompt are
written so a photo slots in without further change to this stage's shape.

## Notes on completion

*(fill in once run)*
