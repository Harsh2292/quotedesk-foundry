# QuoteDesk — video script (≤ 3:00)

About 420 spoken words at a calm pace. **Bold** = what's on screen. Plain text = what you say.
Record the Desk run first as its own take, then speed up the waiting parts in the edit (show a
"sped up ×4" label while Resolve runs — it's honest and saves ~40 seconds).

---

## Before you record

- App running at http://localhost:8080, signed in, Desk empty.
- Tabs open, in order: Desk · Foundry `quotedesk-resolve` → Traces (one trace open) · Insights (the
  High finding open) · Monitor · Evaluations list · `docs/submission/diagrams/fig1-architecture.svg` ·
  `fig4-evaluation-v1-v2.svg`.
- The small photo ready. Write it clearly, five lines:

  ```
  Mehul bhai —
  1) 250 nos 6203 bearing — same as last time
  2) 40 mtr 25mm PV belt
  3) 12 pcs रिंग फ्रेम spindle tape — જાડી વાળી
  4) 100 nos Module 2 spur gear 40T
  5) 2 nos M3 36T gear
  ```
- Typed text to paste, sender `kiran@shreejitextiles.com`:

  ```
  Mehul bhai, photo ma list mokli chhe. Delivery Sachin unit par karvani chhe, 5 tarikh sudhi joiye.
  Pichhli baar 8% diya tha bearings pe, same rakhna please.
  Kiran - Shreeji Textiles
  ```
- Do one full practice run before recording. Keep the take whose approval card shows the spindle
  tape unresolved.

---

## 0:00 – 0:20 · The problem

**The Desk, empty. Then the handwritten photo, full screen for two seconds.**

> A textile-spares distributor in Surat gets enquiries like this — a phone photo of a handwritten
> list, in English, Hindi and Gujarati, with "same as last time" and "the thicker one" instead of part
> numbers. A salesperson spends twenty minutes turning each one into a quotation. QuoteDesk does it in
> about a minute — and the rule that shapes everything is: the model never decides money.

## 0:20 – 0:40 · Photo and text in

**Attach the photo, paste the text, enter the sender, click Process.**

> The photo and the WhatsApp note are one enquiry. The Intake agent reads both — and because
> handwriting can be misread, it checks every product word against the catalogue before committing
> to it.

## 0:40 – 1:15 · The trace

**The trace panel filling. Expand the catalogue check on "25mm PV belt" — result: not a known term,
suggestion "25mm pu belt". Then the Resolve steps: matched customer, searched catalogue, order
history, stock.**

> There's no such thing as a "PV belt" — the catalogue check says so, and suggests PU. Then the
> Resolve agent takes over and chooses its own tools: it matches the customer from the email domain,
> searches the catalogue once for every line, and reads their order history. "Same as last time"
> resolves to the 6203-2RS they bought before — and it records why.

## 1:15 – 1:45 · The approval card

**The approval card. Point at: the photo thumbnail; the spindle tape line in red with "જાડી વાળી";
the gear line needing a margin override; the 8% line; the totals. Click Approve & send. Cut to the
Quotes list.**

> Here is the whole point. The spindle tape — "the thicker one", written in Gujarati — is left
> unresolved. There are eight widths and nothing decides it, so the agent refuses to guess. The gear
> falls below the margin floor and needs an override. The 8% the customer asked for is exactly what
> policy allows — six percent for quantity, two for their tier — calculated in code, not by the
> model. The photo sits beside the lines so a person can check them. Only when they approve is the
> quote created.

## 1:45 – 2:15 · The decisions

**The architecture diagram (Fig. 1).**

> Four decisions. Two agents, split by role: Intake perceives, Resolve decides. Every price, discount
> and date is deterministic code — the model explains, it never computes. The workflow is fixed and
> checkpointed, built on Microsoft Agent Framework, because it has to pause for a human and survive a
> restart. And nothing is written or sent until a person approves.

## 2:15 – 2:45 · Microsoft Foundry

**Foundry: the trace tree (intake → gpt-5-nano, resolve → gpt-5-mini). Then Insights, the High
finding. Then Fig. 4.**

> Both agents are registered in Foundry as external agents, and every model and tool call is traced.
> Foundry's Insights read those traces and flagged a real defect: the agent had used order history
> to pick a "thicker" tape. We fixed it the same day. The evaluation did the same: the baseline found
> four real bugs, we fixed them, and on the re-run every quality evaluator passed on all eight cases.
> Task completion stayed low — on purpose. Every failure says "stopped for human approval". That's
> the design working.

## 2:45 – 3:00 · Reflection

**Back to the approval card, or the Monitor page.**

> The course's lesson I took furthest: failures can look correct. A confident, plausible answer is
> the dangerous one. So this system is built to trace everything, measure itself, and say "I'm not
> sure" — and to leave the last decision to a person.

---

## Screen hygiene

Your name in the app header and in Foundry is fine (you've decided the entry isn't anonymous). Still
keep off screen: API keys, the App Insights connection string, the subscription id, and the
user-secrets output. Don't open `appsettings` or a terminal showing secrets while recording.
