# The final activity brief — verbatim

> **This is the contract, reproduced exactly as issued.** Everything in `docs/FOUNDRY-PLAN.md` is a
> plan for satisfying it; where the two disagree, this file wins. Pasted into the repo on 2026-09-18
> because earlier sessions were working from a paraphrase, and a paraphrase of a marking scheme is a
> liability. Do not edit the quoted text below — add commentary under "Reading notes" instead.

## Final activity — Design and deliver a multi-agent solution

**Activity Description:**

Throughout this program, you have explored the complete lifecycle of building enterprise AI solutions
with Microsoft Foundry. You learned how to create specialised agents, connect them to tools and
knowledge sources, monitor and trace their behaviour, evaluate their quality using datasets and
metrics, and orchestrate them into scalable workflows.

Now it is your turn to apply these concepts by designing a production-ready multi-agent solution that
addresses a real business challenge. The goal is not simply to build a working agent, but to
demonstrate how you would move from prototype to production using the architectural patterns covered
in the course.

### Step 1: Design your AI solution

Choose a realistic business scenario that could benefit from an AI-powered workflow. Examples include
operational monitoring, customer support, claims processing, service desk automation, manufacturing
diagnostics, or another professional use case relevant to your experience.

Define:

1. The business problem you want to solve.
2. The intended users of the solution.
3. At least two specialised agents with distinct responsibilities.
4. The tools, data sources, or knowledge bases that each agent will use.
5. Why a multi-agent approach is more effective than a single-agent solution.

Your design should clearly show how information will flow between agents and how they contribute to
solving the overall problem.

### Step 2: Create a production-readiness plan

Using the concepts from the course, explain how you would make your solution reliable, measurable,
and trustworthy.

Include:

**1. Observability strategy**
- Describe what traces and monitoring metrics you would collect.
- Explain how these insights would help diagnose issues and improve performance.

**2. Evaluation strategy**
- Define an evaluation dataset or testing approach.
- Select evaluation criteria such as coherence, relevance, groundedness, task adherence, safety,
  fluency, or tool usage.
- Explain how evaluation would be integrated into the ongoing development lifecycle.

**3. Governance and reliability**
- Describe how you would ensure consistent outputs, safe behaviour, and maintainability.
- Explain how knowledge sources and tools help ground agent responses.

### Step 3: Design the end-to-end workflow

Create a workflow that demonstrates how your agents collaborate to deliver a business outcome.

Your workflow should include:

1. The sequence of agent interactions.
2. The information passed between agents.
3. Any tool calls or knowledge retrieval steps.
4. The final output delivered to the user or business process.
5. How the workflow could be deployed, monitored, evaluated, and improved over time.

### Present your work

Submit:

- A document (PDF or Word) describing your solution design, agent architecture, observability
  strategy, evaluation approach, and workflow.
- A short recorded presentation demonstrating your proposed solution, explaining the decisions you
  made, and reflecting on how the course concepts helped you move from a prototype idea to a
  production-ready architecture.

Use diagrams, workflow illustrations, screenshots, or architectural sketches where appropriate to
support your explanation.

**Important:** If your use case is based on your real work, you may use genuine organisational
information. However, take care not to display confidential, personal, or sensitive information in
your video, screenshots, or shared materials. If your agent contains internal knowledge sources,
avoid sharing it publicly unless it complies with your organisation's policies. Always follow your
organisation's data protection and information-sharing guidelines.

---

## Reading notes (ours, not the brief's)

**The brief asks for a design, not a build.** Step 2 and Step 3 are written almost entirely in the
conditional: "explain how you *would* make your solution reliable", "how the workflow *could be*
deployed". A submission that only describes an architecture satisfies it completely. QuoteDesk is a
working system, evaluated against a real model, with tests and CI — considerably beyond what is
asked. That is the single largest advantage this entry has, and it is only an advantage if the video
and document make it obvious that the screens are a running system rather than a mock-up. Say it
early and show it working.

**It also means the cut deployment is fully compliant.** Step 3.5 asks how the workflow *could be*
deployed, monitored, evaluated and improved. Describing the Container Apps recipe, and citing the
original Quote-Desk as evidence the recipe works, answers the question as asked. Deploying this fork
would earn nothing extra against this wording.

**Two agents is the floor, not the target.** "At least two specialised agents with distinct
responsibilities." Intake and Resolve are genuinely distinct in *kind* — one perceives, one decides —
which is a better answer than two agents split by subject matter. Make the distinction explicit;
"distinct responsibilities" is the phrase being marked.

**Every Step 1 item is a question to answer in its own words.** Items 1–5 map to five short sections,
not to a narrative that happens to contain the information. Same for Step 2's three areas and Step
3's five items. Structure the document to the brief's own numbering so a marker can find each one.

**The evaluation criteria are named in the brief itself** — "coherence, relevance, groundedness, task
adherence, safety, fluency, or tool usage". Use those exact words for the evaluators chosen, and say
why each was picked, rather than substituting near-synonyms.

**Confidentiality applies to us too.** The screen-hygiene checklist (no signed-in email, no
subscription id, no key, no connection string in any frame) is not optional politeness — it is in the
brief. The customer data here is seeded and fictional, which is worth one sentence in the document.

**"Record yourself now"** appears at the end of the brief. Treat the presentation as a presentation —
a person explaining decisions — rather than a silent screen capture with captions.
