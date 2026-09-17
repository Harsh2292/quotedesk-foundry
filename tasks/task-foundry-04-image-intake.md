# Task foundry-04 — Image intake

**Depends on:** foundry-03. Plan: `docs/FOUNDRY-PLAN.md` §4c. This is the Innovation differentiator
for the video — the one thing that should look like magic on camera — but it is the amputable task:
cut it before cutting foundry-02/03/06/07 if the schedule slips.

## Goal

A photo of a handwritten enquiry goes in; structured lines come out; Intake's `verify_catalogue_term`
tool fires on the illegible word. No multipart upload, no blob storage — base64 riding the existing
JSON POST, deliberately not persisted past the moment it's used.

## Start here: carry-over fixes from the foundry-03 code review

Photos are exactly where an unclear word appears, so fix these first, test-first:
1. `verify_catalogue_term` rejects misspellings ("tming belt", "spindel tap") — it only accepts exact
   whole-word matches. Build a catalogue vocabulary (words from SKUs, names, categories, attributes) and,
   for a word not in it, return the closest vocabulary word within a small edit distance as a suggestion.
   Still never a SKU, price or cost.
2. It reports family names ("SpindleTapes") as unknown — recall searches only SKU and name. The vocabulary
   in (1) covers categories, which fixes this.

**Also check early, before building the UI:** whether `gpt-5-nano` (Intake's model) reads a real
handwritten list accurately — one live call with the crafted demo photo, line-by-line accuracy, input
tokens and latency recorded. Correctness outranks speed on this project: if nano misreads quantities,
bring the numbers to Harsh before changing any model choice.

**Scope (decided 2026-09-17):** one photo per enquiry, as written below. Several photos/messages per
enquiry and several enquiries at once are designed but deferred to the extras queue (extra-06, extra-07)
because of the deadline.

## The constraint that shapes everything

The image arrives on `POST /api/enquiries` but is needed on `POST /api/enquiries/{id}/process`, a
different request reading from `enquiries.GetByIdAsync`. So it must be persisted somewhere between
the two calls — "don't persist it at all" breaks Retry, the SSE shape and the resume path.

## What to build

1. `PasteEnquiryRequest.ImageDataUrl` (a `data:image/...;base64,...` string). Reject over ~2 MB
   decoded with a 400 before anything touches the database.
2. One EF migration: `Enquiries.ImageDataUrl nvarchar(max) null`. `NewEnquiry` gets the new parameter
   **with a default value**, so `DeterministicSeeder` and every other caller compiles unchanged.
   `Database:MigrateOnStartup` is `false` — apply this migration to the dev database by hand; the
   test fixtures call `MigrateAsync()` and pick it up automatically.
3. `PasteAdapter.FromPastedTextAndImage(...)` as a second static factory on the existing class.
   **Do not create an `ImageAdapter`** — `IntakeBoundaryTests` fails the build if the literal
   `EnquiryChannel` appears anywhere in `QuoteDesk.Api` or `QuoteDesk.Agents`.
4. `EnquiryStatusRule`: `needs_manual_entry` when the body is blank **and** there's no attachment
   (populate `IncomingEnquiry.Attachments` with one `EnquiryAttachment` — the shape already exists
   and has been unused until now). Same guard on the endpoint's 400 check.
   **`PasteAdapterTests.cs`'s existing blank-body-with-no-attachment case must still pass unchanged**
   — add a new image-only case, don't weaken the old one.
5. `EnquiryInput.ImageDataUrl = null` default (positional, with a default — every existing
   construction site compiles). **It is embedded in `ExtractionResult` and every downstream record,
   all checkpointed to SQL on every superstep** — strip it the moment it's done its job:
   ```csharp
   return new ExtractionResult { Enquiry = message with { ImageDataUrl = null }, Extracted = extracted };
   ```
   in `IntakeExecutor.HandleAsync`, once the image has been read.
6. `StructuredModelCall` — add a `RunAsync<T>(AIAgent, ChatMessage prompt, ...)` overload; make the
   existing `string` overload delegate to it. The image rides in as `new DataContent(dataUri,
   mediaType)`. **Verify `AIAgent.RunAsync(ChatMessage, ...)` and the `DataContent` constructor
   against the installed package XML docs first** — this is exactly the kind of signature CLAUDE.md
   says to confirm before writing, not recall.
7. Web: `FileReader` → `<canvas>` downscale to ~1280px → `toDataURL('image/jpeg', 0.8)` before the
   image ever enters React state. **Deliberately do not persist it to `sessionStorage`** — three
   touch points only (`useState` in `DeskSessionProvider`, `submitDraft`, `reset`), left out of the
   persisted trio. A refresh losing a pending photo is honest; silently blowing the ~5 MB quota and
   losing the whole session's trace is not.
8. Do **not** add the image to `EnquiryDetailResponse` (fetched on every Desk navigation). If it
   needs to render back, add a dedicated `GET /api/enquiries/{id}/image`.

## The demo photo, deliberately crafted (not incidental)

For `foundry-08`'s recording: the photo must contain **one genuinely hard-to-read word** — e.g. "PU
belt" written so it could pass for "PV" — so `verify_catalogue_term` visibly fires in the trace. On a
perfectly legible photo, Intake behaves exactly like a plain non-agentic extraction call and the
"two agents" claim is true but invisible. Craft this photo before recording, not on the day.

## Acceptance criteria

- [x] Image rides the existing JSON POST; oversized images rejected with 400
- [x] Migration applied; `NewEnquiry` compiles at every existing call site
- [x] `PasteAdapter.FromPastedTextAndImage` exists; no new `EnquiryChannel` reference anywhere in
      Api/Agents (verify `IntakeBoundaryTests` still passes)
- [x] Blank-body-with-attachment → `needs_manual_entry` still correctly *not* triggered; old test
      unchanged, new case added *(with one refinement — see notes)*
- [x] `EnquiryInput.ImageDataUrl` never reaches a persisted checkpoint past `IntakeExecutor`
- [ ] A live run with the crafted photo shows `verify_catalogue_term` firing in the in-app trace
      — **open: needs Harsh's crafted photo and a signed-in browser run** (see notes)
- [x] The image is never in `sessionStorage`, never in `EnquiryDetailResponse`
- [x] Both build configs and the full non-eval test suite pass

## Out of scope

Rendering the photo back in a detail view (a `GET /api/enquiries/{id}/image` endpoint) — only build
this if the demo script actually needs it.

## Notes on completion

**Built 2026-09-17.** Both builds clean, 182 unit + 69 integration tests green, `npm run build` passes.

- **Carry-over fixes.** `verify_catalogue_term` reads the whole catalogue (new
  `ICatalogRepository.GetAllAsync`) and returns `Suggestions[≤3]` for an unknown term. Guard rails:
  suggestions come only from item names, families and attributes, never SKU codes; a word containing a
  digit is never corrected; a reading must appear together in one real item. Family names are now known.
- **The attachment rule, refined.** The task said a blank body with an attachment should not be
  `needs_manual_entry`, but the existing test `IngestAsync_AttachmentOnlyEnquiry_...` asserts that it
  *is*, for a JPEG attachment carrying only metadata. Both are right: the rule is now "blank body and
  no **readable** image (an image whose content is present)". The old test passes unchanged, the new
  image-only case is `pending`, and a voice note still needs a human (docs/SPEC.md §5).
- **Unreadable quantities.** `intake.md` tells Intake to write quantity 0 with "(quantity unreadable)"
  rather than guess or drop the line. `ResolveExecutor.ReconcileAsync` turns quantity ≤ 0 into an
  unresolved line in code, test-first, so such a line can never be priced.
- **Early live check** (`tests/QuoteDesk.Evals/FoundryImageIntakeEval.cs`, synthetic image
  `Fixtures/handwritten-enquiry-synthetic.jpg` rendered in a handwriting font — not a real photograph):
  - Reading accuracy: 3 of 3 quantities exactly right on all 3 runs, plus company and ship-to.
  - Intake call: 3.8–7.2 s, ~3,000 input tokens (the image dominates), with reasoning `None`.
  - The date "need by 5th" was missed (`requiredBy` null) with reasoning `None`, and read correctly
    with `Low`. `Low` also took the whole run from ~40 s to 66 s, so production stays at `None`.
    A photo-specific date line was added to `intake.md`; its effect is not yet measured.
  - **`verify_catalogue_term` did not fire** on a cleanly rendered "PV", at either reasoning setting.
    One prompt change ("check every short type code") had no effect and was reverted. The model reads
    a clear "PV" as PV and has no reason to doubt it; the belt then goes to the human as unresolved,
    which is safe. **For the demo, the photo must make a letter genuinely ambiguous**, which is what
    the task's "crafted photo" section already asks for.
  - **Found in passing, not this task's code:** on one run Narrate (`gpt-5-nano`) wrote "discount ₹20%
    applied" for a line priced at 8%. The numbers on the card are right (they come from
    `QuoteDesk.Domain`), but the sentence explaining them was wrong. That is exactly the failure
    foundry-05's policy grounding and foundry-07's evaluation should catch. It is recorded here so it
    is not lost.
- **Later the same day — real photo, model routing, review fixes.**
  - Harsh's real notebook photo (messy, crossed-out words, Gujarati-English, "2₹" meaning 2RS): nano
    misread most of it and invented "the thicker one" (copied from the prompt's worked example); mini
    read far more but still read "20mm" as "25mm" — a real SKU, so it was priced green — and "2₹" as
    "2F". Claude Opus also read "2₹" as "2Z". Decisions, recorded in docs/SPEC.md §5: photos read on
    `gpt-5-mini` (`Llm:IntakeImageModel`), text stays on `gpt-5-nano`; the approval card shows the
    photo; prompt examples no longer share any item or size with the demo; a "re-read the photo" agent
    loop was considered and not built.
  - Three reviews (code review, security review, code simplifier) on the first version, then one more
    code review and security review on everything after. All findings fixed except the accepted demo
    risks listed in SPEC §5. Final state: both builds clean, 201 unit + 80 integration tests, web build
    and lint pass, live Foundry text and photo evals pass.
  - **For the demo:** use clearly handwritten photos. The feature stays for messy photos, with the
    photo on the approval card as the check.
- **Still open, needs Harsh:** write and photograph the crafted demo list, then run it through the Desk
  signed in, and check the trace for `verify_catalogue_term`. If it doesn't fire on a genuinely
  ambiguous photo either, the decision is Harsh's: accept it (the tool is for illegible words), or try
  a different Intake model or reasoning setting (measured on one run: `Low` added ~5–10 s across Intake and Narrate; that run was ~25 s slower overall, but the other ~17 s was Resolve's normal run-to-run variation — Resolve's reasoning is not affected by this setting).
