# CLAUDE.md — wh40k-army-enricher

## Documentation Maintenance

After every implementation change — feature, bug fix, or design decision — update the relevant file in `.claude/`. Keep this root file lean: it describes intent and architecture, not implementation detail.

---

## Project Purpose

A live-game tool for use on a phone or tablet at the Warhammer 40K table. The end goal: paste
two army list exports (attacker and defender), enrich them against catalogue data, and use the
resulting page to select weapons and run instant Monte Carlo simulations for expected damage and
kills. Today's app implements one slice of this — `/LivePlay`, a read-only reference view with
live casualty tracking, built on hand-authored fixture data rather than a parsed export, with no
simulation wired up yet.

---

## Solution Structure

```
wh40k-army-enricher/
  ProbHammer.Web/        ASP.NET Core web application (Razor Pages + JS) — /LivePlay only
  ProbHammer.Core/       Domain logic — the live 11e model (Domain/Catalogue, Domain/Roster,
                          Domain/Examples) plus Parsing/ArmyListParser.cs (kept, pending an
                          11e rewrite)
  ProbHammer.Tests/      xUnit test suite
  legacy/10e-pipeline/   Retired 10th-edition pipeline — excluded from compilation, kept only
                          for reference, including its own .claude/ docs (domain-model.md,
                          web-app.md, simulation-engine.md, bsdata-parsing.md, rules/combat-
                          rules.md)
```

- **Language:** C# 12, `net8.0`, nullable reference types enabled, implicit usings enabled
- **Key dependencies:** `xunit` + `FluentAssertions` + `Moq` (tests)
- **No third-party XML library** — use `System.Xml.Linq` (XDocument / LINQ to XML)

---

## Architecture Overview

`ProbHammer.Core` holds the live 11e domain model — `Domain/Catalogue` (Datasheet, Statline,
WeaponProfile), `Domain/Roster` (Unit, AttachedUnit, per-model-line remaining-count tracking),
`Domain/Examples` (hand-authored fixture army, standing in for a parsed export). No BSData or
simulation wiring exists yet. `ProbHammer.Web` serves one page, `/LivePlay`: a read-only reference
view over that fixture army with live casualty tracking (browser `localStorage` + a full-map POST
that rebuilds and re-renders server-side — no session state).

Full domain model detail: @.claude/domain-model-11e.md  
Implementation gotchas and defensive notes: @.claude/implementation-notes.md  
`/LivePlay` visual design tokens: @.claude/design-tokens.md  

Archived 10e docs, kept only as reference material, not auto-loaded:
`legacy/10e-pipeline/.claude/domain-model.md`, `web-app.md`, `simulation-engine.md`,
`bsdata-parsing.md`, `rules/combat-rules.md`, `design-tokens.md`,
`implementation-notes.md`.

---

## Running Locally

```bash
docker compose up --build   # first run
docker compose up           # subsequent runs
# browse to http://localhost:8080/LivePlay
```

---

## Key Design Constraints

- **AP is stored as a negative integer** throughout (e.g. AP-2 → `-2`), matching the game value —
  see `WeaponProfile.Ap` and `.claude/implementation-notes.md`.

---

## Build State

See `PROGRESS.md` for current session state, spec gaps discovered during
generation, and the resume prompt for the next session.
