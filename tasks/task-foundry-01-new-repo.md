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

- [x] `gh auth login` completed
- [x] Public repo `quotedesk-foundry` exists, `development` is the default working branch
- [x] First commit is exactly the import message above, run by Harsh
- [x] `VITE_GOOGLE_CLIENT_ID` repo Variable set
- [x] All three CI jobs green on the pushed commit
- [x] `codebase-memory-mcp cli index_repository --repo-path .` run against the new repo

## Out of scope

Anything requiring the Foundry resource itself (`foundry-02` onward) — this task is purely repo and
CI mechanics.

## Notes on completion

Done 2026-09-14. `gh auth login` via the web device-code flow (account `Harsh2292`). First commit
`659eca9` landed on `main` with `cd.yml` already removed from the tree (see below) and
`docker-compose.yml`'s container names already fixed — both changes were made before the commit so
they're part of the baseline import rather than a follow-up. `gh repo create quotedesk-foundry
--public --source=. --remote=origin`, then `git push -u origin main`, `git switch -c development`,
`git push -u origin development` — all run by Harsh per the standing "never git commit/push" rule.
Both branches' first CI run went green (`build-test`, `web`, `image`) in ~2 minutes each. Claude then
set `development` as the default branch (`gh repo edit --default-branch development`) and set the
`VITE_GOOGLE_CLIENT_ID` repo Variable — neither is a commit/push, so within Claude's own remit — and
ran `codebase-memory` `index_repository` (2,756 nodes, 8,666 edges, 0 skipped/parse-partial).

**One deviation from the plan, decided this session:** `.github/workflows/cd.yml` was deleted rather
than carried over. It hardcoded the *original* deployed Quote-Desk project's exact Azure resource
names (`az containerapp update --name quotedesk-api --resource-group quotedesk-rg`) and GHCR path
(`ghcr.io/harsh2292/quotedesk-api`) — and `quotedesk-rg` is also where this fork's own Foundry
resource lives (Step 0). Keeping a workflow with those exact identifiers around, wired to Azure OIDC
secrets at some future point, risked deploying this fork's code over the original's live demo. This
fork's app deployment is cut per `docs/FOUNDRY-PLAN.md`'s cut list anyway, so the file simply isn't
needed. `ci.yml` was untouched.
