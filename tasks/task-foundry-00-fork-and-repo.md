# Task foundry-00 — Fork mechanics

**Depends on:** nothing — this is the first task. Plan: `docs/FOUNDRY-PLAN.md` Step 1.

## Goal

Turn a plain `git clone` of `Harsh2292/Quote-Desk@development` into a repo that is safe to build on
independently, with no risk of it ever touching the original deployed project's secrets, database or
git history.

## What to build

1. `git clone --branch development "<source>" "<destination>"`, then remove `.git` for a fresh history.
2. Fix `.gitignore`: `docs/`, `tasks/` and `CLAUDE.md` were only tracked in the source repo because
   they predate its ignore rules — in a fresh repo they would silently never commit. Remove those
   lines (and, deliberately, the `.claude/` line too — see Notes).
3. Copy by hand the files a clone cannot bring because they were never tracked in the source:
   `docs/AGENT-A-THON.md`, `src/QuoteDesk.Web/.env.local`.
4. Generate one new `UserSecretsId` GUID and set it in both `src/QuoteDesk.Api/QuoteDesk.Api.csproj`
   and `tests/QuoteDesk.Evals/QuoteDesk.Evals.csproj` (they deliberately share one id with each
   other, but must not share the source repo's).
5. Write `docs/FOUNDRY-PLAN.md` — the durable, in-repo copy of the fork's plan.
6. Update `CLAUDE.md`'s architecture line and top pointer, and `README.md`, for the new project.
7. Write this task queue (`tasks/README.md` plus the `task-foundry-NN-*.md` files).

## Acceptance criteria

- [x] New repo exists with no `.git` history shared with the source
- [x] `.gitignore` no longer excludes `docs/`, `tasks/`, `CLAUDE.md`
- [x] `docs/AGENT-A-THON.md` and `.env.local` present
- [x] `UserSecretsId` changed in both csproj files, to the same new value
- [x] `docs/FOUNDRY-PLAN.md`, updated `CLAUDE.md`, `README.md`, `tasks/README.md` all written
- [x] `dotnet build QuoteDesk.sln -warnaserror` passes in the new repo (Debug, 0 warnings, 0 errors)
- [x] `dotnet build QuoteDesk.sln -c Release -warnaserror` passes (0 warnings, 0 errors)
- [ ] `dotnet test` passes — **not yet run**, needs a database connection string first (see Notes)

## Out of scope

Creating the GitHub remote (`foundry-01`), anything requiring the Foundry resource (`foundry-02`
onward).

## Notes on completion

**A deliberate deviation from the plan, worth stating:** `docs/FOUNDRY-PLAN.md` Step 1's table says
"keep `.claude/` ignored." That was reconsidered while executing this task — the whole point of this
task queue is that a fresh Claude Code instance can pick this project up and continue exactly where
it stopped, and that only works if the `.claude/skills/` and `.claude/settings.json` this session
depends on are actually committed, not left to whatever happens to be in a future clone's working
directory. `.claude/settings.local.json` stays specifically excluded (already a separate `.gitignore`
line) since it's genuinely per-machine.

**Blocked on Harsh, not done in this task:**
- `dotnet user-secrets set` for `ConnectionStrings:QuoteDesk` (a new database name — proposed
  `QuoteDeskFoundry` — needs deciding), `Auth:Google:ClientId`, `Auth:Jwt:SigningKey`,
  `Auth:AdminEmails:0`. None of these can be set without Harsh's values, and the full build/test
  verification in the acceptance criteria above is blocked on them.
- `Llm:ApiKey` and `Foundry:ProjectEndpoint` — blocked on Step 0 (the Azure portal work), not started.

**`.env` for docker-compose was not recreated** — same reasoning: it needs a real (or deliberately
different) `MSSQL_SA_PASSWORD` and port choice, and running both projects' compose stacks
simultaneously would collide on ports 1433/5080/8080, so this needs a decision, not a copy.

**Next:** `foundry-01` — create the GitHub repo and push the first commit, once `gh auth login` is
done.
