# The course modules — one place, so nothing is pasted twice

The Microsoft Agent-a-Thon "Agent Architect" track ships **five** module PDFs. This file holds what
each one actually teaches, read from the PDFs themselves on 2026-09-18. The marking contract is a
separate file: `docs/SUBMISSION-BRIEF.md` (verbatim, and it outranks everything here).

> **Why this file exists.** `docs/FOUNDRY-PLAN.md` spent five days asserting that only two course
> documents existed and that no others would be supplied. That was wrong, and two of the missing
> modules covered ground the plan had designed from first principles. Everything is written down here
> so a future session — or a `/clear` — never has to reconstruct it from a chat window again.

**Source PDFs** (in `C:\Users\Lazy Boy\Downloads`, not committed — they are course material, not ours):

| File | Module | Read |
|---|---|---|
| `M1-03. Agent Architect - Introduction to Microsoft Foundry.pdf` | Introduction to Microsoft Foundry | pending |
| `M1-04. Agent Architect - Monitor and trace agent behavior.pdf` | Monitor and trace agent behaviour | ✅ 2026-09-18 |
| `M1-05. Agent Architect - Evaluate agent quality.pdf` | Evaluate agent quality | summarised in FOUNDRY-PLAN 2026-09-13; re-reading |
| `M1-06. Agent Architect - Orchestrate multi-agent workflows.pdf` | Orchestrate multi-agent workflows | ✅ 2026-09-18 |
| `M1-07. Agent Architect - From prototype to production.pdf` | From prototype to production | ✅ 2026-09-18 |

---

## M1-04 — Monitor and trace agent behaviour

A 7-page **concepts deck, not a walkthrough**. It names no RBAC roles, no `gen_ai.*` attributes, no
portal click paths, nothing about external agents, and nothing about capturing message content. Our
technical design for tracing is therefore our own judgement — neither confirmed nor contradicted.

**Its central claim, and our strongest alignment in the whole course — "Failures Can Appear Correct":**
*"a conventional API may return a recognizable error code, while an agent may produce a **confident but
incorrect response**. This makes terminal output alone insufficient for diagnosing many agent
failures."* This is exactly the foundry-04 finding — a handwritten "20mm" read as "25mm", a real SKU,
priced green past every code check. Lead with it.

**Monitoring vs tracing — terminology to use exactly:**

| Practice | Primary question | Typical evidence |
|---|---|---|
| Monitoring | *"What happened?"* | Runs, errors, token usage, costs, latency, operational trends |
| Tracing | *"Where did it happen, and why?"* | Inputs, model calls, tool calls, intermediate results, retries, outputs |
| Combined | *"How can the issue be resolved and prevented?"* | Performance trends connected to detailed execution evidence |

**Its stated "Core principle", worth quoting:** *"Monitoring identifies the presence and scale of a
problem; tracing reveals the execution details needed to explain its cause."*

**A complete trace "may include":** user inputs and system instructions · model calls and generated
outputs · tool invocations and returned values · latency, token consumption and retries · errors.

**Four operational benefits:** Faster Diagnosis · Performance Optimization · Production Readiness ·
Governance Evidence.

**Its five-step observability workflow:** prepare the environment (App Insights connected) → generate
telemetry → inspect agent traces → review monitoring metrics (costs, token usage, run counts,
tool-call activity, latency, error rates) → analyse aggregated operations in App Insights' **Agents
Preview**.

**"Where to Find Observability Evidence" — four tiers. Organise submission screenshots this way:**
Foundry **Traces** (one execution) → Foundry **Monitoring** (one agent) → App Insights **Agents
Preview** (aggregated) → **Connected Foundry Resources** (cross-resource).

**Its diagnostic template, for writing up incidents:** *Reconstruct the Decision Path → Locate the
Failure Point → Connect Detail to Operational Impact.* Use this shape for our three real incidents
(the 49.6s Resolve, the 342-candidate catalogue flood, the Gemini `thought_signature` 400).

---

## M1-06 — Orchestrate multi-agent workflows

**The module that creates this entry's biggest framing risk.** It teaches exactly two orchestration
surfaces, and **both are Foundry-native**: the **visual Workflow builder** in the portal (Build →
"Agents and workflows": start node, agent nodes, end node, saved workflow variables, published
versions, YAML export) and the **Microsoft Foundry SDK in Python**. The Microsoft Agent Framework and
`Microsoft.Agents.AI.Workflows` (.NET) are **never mentioned**. Its production checklist assumes the
workflow is a *published Foundry object*.

QuoteDesk uses neither surface — see `docs/FOUNDRY-PLAN.md` §5a-1 for the decision (name the choice
and justify it; do not build a Foundry visual workflow).

**"Sequential Execution" is the only orchestration pattern the whole track names.** No concurrent,
hand-off, group-chat, magentic or supervisor/worker vocabulary anywhere. Our fixed pipeline maps
directly onto the taught vocabulary — do not invent comparisons against untaught patterns.

**Quotable, and it describes what we already built:** *"Saving outputs and defining schemas are
architectural decisions. They establish a clear contract between agents and reduce ambiguity in
downstream processing."* Also: *"A multi-agent system is more than a sequence of prompts. It is an
executable business process in which specialized capabilities, context, and decisions are deliberately
coordinated."*

**Individual agent call vs orchestrated workflow:** scope (one task vs a business process) ·
information transfer (manual handoff vs automatic) · decision logic (one prompt vs distributed) ·
output (task-specific vs consolidated operational result).

**Production-ready workflow capabilities:** Version Control · Traceability · Monitoring · Evaluation ·
Portable Definitions (YAML for CI/CD) · **Flexible Hosting** — which names hosted agents, Azure App
Service, **Azure Container Apps** and Azure Functions. Cite that directly for our deployment section.

**Operational readiness checklist:** validate inputs and outputs → inspect execution traces → evaluate
workflow quality → manage versions → select a deployment model.

**Not covered at all:** human-in-the-loop approval, checkpointing, restart-survivable state. Our
approval gate and SQL checkpointing go beyond the curriculum — say so plainly rather than implying
they are taught practice.

---

## M1-07 — From prototype to production

**The document's skeleton.** Its framing is the answer to the brief's "prototype → production"
reflection.

**Its definition:** *"A production-ready agent is defined by the reliability and inspectability of the
complete system, not by a single successful response."* And: *"The central production question is not
whether an agent can answer once, but whether the complete system can run reliably, be inspected,
measured, improved, and reused."*

**Best single line in the course:** *"Trustworthy agents are engineered, not prompted into existence."*

**Prototype vs production priorities:**

| Dimension | Prototype | Production |
|---|---|---|
| Agent behaviour | Produces a useful response once | Performs consistently across repeated cases |
| Architecture | May combine several tasks in one agent | Specialised agents with explicit responsibilities |
| Visibility / quality | Relies on manual inspection | Tracing and monitoring to inspect execution |
| Execution | Judged informally | Measured through systematic evaluation |
| Improvement | Ad hoc, isolated observations | Repeatable orchestrated workflow, evidence-led |

**Six "Core Production Design Principles" — and what we show for each:**

| Principle | Their words | Our evidence |
|---|---|---|
| **Clear Ownership** | "Each agent has a focused role" | Intake perceives, Resolve decides |
| **Relevant Grounding** | "Tools and knowledge sources connect responses to the information required" | Typed tools per agent + the Quotation Policy |
| **Observable Execution** | "Tracing and monitoring reveal agent interactions, execution paths, and potential failure points" | Trace panel, OpenTelemetry spans, three real incidents |
| **Measured Quality** | "Evaluation replaces intuition with evidence" | 10-case dataset, thresholds fixed before baseline |
| **Repeatable Orchestration** | "Defined workflows coordinate specialized agents consistently rather than relying on improvised execution" | A pipeline that never reorders or skips |
| **Operational Governance** | "Oversight and controls support responsible operation" | The human approval gate, write tools unreachable |

**"Operational Governance" / "oversight and control" is the course's only vocabulary hook for a human
approval gate.** Use those words for it.

**"Design for Trustworthiness":** architecture establishes responsibilities · observability exposes
behaviour · evaluation provides evidence of quality · governance establishes oversight.

**Six-stage lifecycle — structure the document on this:** define the operational scenario → assign
agent responsibilities → connect tools and knowledge → instrument agent behaviour → evaluate output
quality → orchestrate the workflow.

**Six-step "Solution Readiness Review" — close the document with this as a self-assessment:** confirm
role clarity → verify grounding → inspect execution evidence → assess evaluation results → test
workflow repeatability → review governance.

**Not named in M1-07 at all:** cost, security, CI/CD. It prescribes **no** particular Azure deployment
approach — hosting options live in M1-06's "Flexible Hosting" as options, not a prescribed path.

---

## M1-03 — Introduction to Microsoft Foundry

*Pending — being read. This section will be filled in.*

---

## M1-05 — Evaluate agent quality

*Being re-read from the PDF. The 2026-09-13 summary lives in `docs/FOUNDRY-PLAN.md`'s compliance
audit: six evaluation dimensions, a six-step configuration flow, and a six-stage lifecycle
(baseline before deployment → acceptance thresholds → re-evaluate after a change → detect regressions
by comparing runs → monitor drift on a schedule → expand risk coverage). This section will carry the
full version.*

---

## Neither M1-06 nor M1-07 says anything about grading

Checked explicitly. Nothing in those two modules describes how work is judged or what a submission
must contain. The only sources for that are `docs/SUBMISSION-BRIEF.md` and the Founderz competition
rules — summarised in `docs/FOUNDRY-PLAN.md`'s compliance audit.
