# Task foundry-01 — New GitHub repo, first push, CI

**Depends on:** foundry-00. Plan: `docs/FOUNDRY-PLAN.md` Step 2.

## Goal

The fork exists as a real, public, independently-buildable GitHub repo with CI passing, so every
task after this one has somewhere to land.

## Before this can run

`gh auth login` — not done as of foundry-00. This whole task is blocked until it is.

## What to build

1. `git init -b main`, `git switch -c development`. All work happens on `development`, matching the
   source project's convention.
2. First commit, **run by Harsh** (never Claude — standing rule):
   `chore: import QuoteDesk baseline from Harsh2292/Quote-Desk@fb45bdb`
3. Create the remote — Claude proposes the exact command, Harsh runs it and pushes:
   `gh repo create quotedesk-foundry --public --source=. --remote=origin`
4. In the new repo's GitHub settings: add repo **Variable** `VITE_GOOGLE_CLIENT_ID` (Settings →
   Actions → Variables) — the existing `.github/workflows/ci.yml` web job needs it and it is
   intentionally not a secret (sent to the browser by design).
5. Confirm CI goes green on the first push: `build-test` (needs no API key or network — a stubbed
   `IChatClient`/`IGoogleIdTokenValidator` and a SQL Server service container), `web`, `image`.
6. README top line already states provenance (`foundry-00`) — confirm it renders correctly on GitHub.

## Acceptance criteria

- [ ] `gh auth login` completed
- [ ] Public repo `quotedesk-foundry` exists, `development` is the default working branch
- [ ] First commit is exactly the import message above, run by Harsh
- [ ] `VITE_GOOGLE_CLIENT_ID` repo Variable set
- [ ] All three CI jobs green on the pushed commit
- [ ] `codebase-memory-mcp cli index_repository --repo-path .` run against the new repo

## Out of scope

Anything requiring the Foundry resource itself (`foundry-02` onward) — this task is purely repo and
CI mechanics.

## Notes on completion

*(fill in once run)*
