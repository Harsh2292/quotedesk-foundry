# Task foundry-04 — Image intake

**Depends on:** foundry-03. Plan: `docs/FOUNDRY-PLAN.md` §4c. This is the Innovation differentiator
for the video — the one thing that should look like magic on camera — but it is the amputable task:
cut it before cutting foundry-02/03/06/07 if the schedule slips.

## Goal

A photo of a handwritten enquiry goes in; structured lines come out; Intake's `verify_catalogue_term`
tool fires on the illegible word. No multipart upload, no blob storage — base64 riding the existing
JSON POST, deliberately not persisted past the moment it's used.

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

- [ ] Image rides the existing JSON POST; oversized images rejected with 400
- [ ] Migration applied; `NewEnquiry` compiles at every existing call site
- [ ] `PasteAdapter.FromPastedTextAndImage` exists; no new `EnquiryChannel` reference anywhere in
      Api/Agents (verify `IntakeBoundaryTests` still passes)
- [ ] Blank-body-with-attachment → `needs_manual_entry` still correctly *not* triggered; old test
      unchanged, new case added
- [ ] `EnquiryInput.ImageDataUrl` never reaches a persisted checkpoint past `IntakeExecutor`
- [ ] A live run with the crafted photo shows `verify_catalogue_term` firing in the in-app trace
- [ ] The image is never in `sessionStorage`, never in `EnquiryDetailResponse`
- [ ] Both build configs and the full non-eval test suite pass

## Out of scope

Rendering the photo back in a detail view (a `GET /api/enquiries/{id}/image` endpoint) — only build
this if the demo script actually needs it.

## Notes on completion

*(fill in once run)*
