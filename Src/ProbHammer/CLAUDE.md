# CLAUDE.md — wh40k-army-enricher

## Documentation Maintenance

After every implementation change — feature, bug fix, or design decision — update the relevant file in `.claude/`. Keep this root file lean: it describes intent and architecture, not implementation detail.

---

## Project Purpose

A live-game tool for use on a phone or tablet at the Warhammer 40K table. The user pastes two army list exports (attacker and defender), the server enriches them against BSData catalogue data, and the resulting page lets them select weapons and run instant Monte Carlo simulations to get expected damage and expected kills.

---

## Solution Structure

```
wh40k-army-enricher/
  ProbHammer.Web/        ASP.NET Core web application (Razor Pages + JS) — /LivePlay only
  ProbHammer.Core/       Domain logic — the live 11e model (Domain/Catalogue, Domain/Roster,
                          Domain/Examples) plus Parsing/ArmyListParser.cs (kept, pending an
                          11e rewrite)
  ProbHammer.Tests/      xUnit test suite
  legacy/10e-pipeline/   Archived 10th-edition pipeline (Contracts/Catalogue/Enrichment/
                          Simulation + the old Index/ArmyView Web pages, plus REGENERATION.md,
                          .claude/features/*.feature, and data/) — excluded from compilation,
                          kept for reference only; see .claude/domain-model.md,
                          .claude/web-app.md, .claude/simulation-engine.md,
                          .claude/bsdata-parsing.md
```

`/Index` and `/ArmyView` are no longer live routes — the app serves `/LivePlay` only, as of
the `archive-10e-pipeline` change.

- **Language:** C# 12, `net8.0`, nullable reference types enabled, implicit usings enabled
- **Key dependencies:** `FuzzySharp` (name matching), `xunit` + `FluentAssertions` + `Moq` (tests)
- **No third-party XML library** — use `System.Xml.Linq` (XDocument / LINQ to XML)

---

## Architecture Overview

> **Archived — describes the retired 10e pipeline, not the live `ProbHammer.Core`/`Web`.**
> See `legacy/10e-pipeline/` and `.claude/domain-model.md`. The live app today is
> `/LivePlay`, built on the 11e domain model (`.claude/domain-model-11e.md`) with no
> BSData/simulation wiring yet.

```
Army list text
      │
      ▼
 ArmyListParser          Parses plain-text export into ArmyList domain records
      │
      ▼
 Enricher                Resolves each unit/model/weapon against BSData catalogues
      │                  via CatalogueStore (eagerly loaded on startup, disk-cached)
      ▼
 List<UnitProfile>       Stored in ASP.NET Core session (JSON)
      │
      ▼
 SimulationAdapter       Bridges UnitProfile → SimulationConfig
      │
      ▼
 CombatSimulator         Monte Carlo engine: N iterations of the 40K attack sequence
      │
      ▼
 SimulationResponse      Mean damage, expected kills, P(kill ≥ 1), stddev, pipeline stats
```

Full domain model, schema, and enrichment rules (10th edition, archived): @.claude/domain-model.md  
11th edition domain model (Attached Units — in progress, not yet wired to Web/Simulation): @.claude/domain-model-11e.md  
Simulation engine detail: @.claude/simulation-engine.md  
Combat rules and attack sequence: @.claude/rules/combat-rules.md  
BSData parsing and catalogue loading: @.claude/bsdata-parsing.md  
Web application, UI flow, and session handling: @.claude/web-app.md  
Implementation gotchas and defensive notes: @.claude/implementation-notes.md  

---

## User Flow

> **Archived — describes the retired `/Index`/`/ArmyView` flow, not the live app.** See
> `legacy/10e-pipeline/` and `.claude/web-app.md`. The live app today is `/LivePlay` only.

1. **Index page** — paste attacker and defender army list text, submit
2. Server enriches both lists and stores `List<UnitProfile>` in session
3. **ArmyView page** — two columns of collapsed unit cards; catalogue versions shown at top
4. Expand an attacker card → click a weapon variant row (highlights red; attacker unit auto-selected)
5. Click a defender unit card (highlights blue)
6. **Combat panel** appears at the bottom — configure modifiers, click **Run Simulation**
7. Results appear inline: mean damage, expected kills, P(kill ≥ 1), stddev, attack pipeline funnel
8. **Re-download catalogues** button refreshes BSData cache for catalogues used in the current session

The tool must feel fast on a phone screen. Simulation runs server-side; results update without page reload.

---

## Running Locally

```bash
docker compose up --build   # first run: downloads ~35 MB BSData cache
docker compose up           # subsequent runs: cache volume present, starts fast
# browse to http://localhost:8080
```

`Enricher:CachePath` in `appsettings.json` is dead config as of `archive-10e-pipeline` —
nothing reads it now that `CatalogueFetcher`/`CatalogueStore` moved to `legacy/10e-pipeline/`.
Left in `appsettings.json` untouched (harmless) rather than edited as part of that change.

---

## Key Design Constraints

> The first four constraints below describe the archived 10e `Enricher`/`Simulation`/`ArmyView`
> implementation (`legacy/10e-pipeline/`) — kept as a design record, since a future rebuild on
> the 11e model will likely want to honor the same principles again. The fifth (AP sign
> convention) is still live today.

- **Never hard-code statlines.** All stat values come from BSData XML. Unresolved units emit a structured warning and are skipped.
- **Shooting and melee are mutually exclusive per simulation run.** The UI enforces this — selecting the first weapon locks the phase type; opposite-type rows are disabled until selections are cleared.
- **No damage spillover between models** in the wound pool — excess damage on a model is lost, not carried to the next.
- **All simulation modifiers are user-controlled.** Ability text is not auto-parsed into simulation parameters.
- **AP is stored as a negative integer** throughout (e.g. AP-2 → `-2`) — a convention that
  originated in the archived pipeline (`UnitProfile`/`SimWeaponProfile`) and carries over
  unchanged into the live 11e domain model's `WeaponProfile.Ap`. Any save-resolution logic
  built on either model uses `effectiveSave = save - ap`. See `.claude/implementation-notes.md`.

---

## Build State

See `PROGRESS.md` for current session state, spec gaps discovered during
generation, and the resume prompt for the next session.
