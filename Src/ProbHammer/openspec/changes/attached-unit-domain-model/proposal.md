## Why

ProbHammer is being rewritten to support Warhammer 40,000 11th edition, and the existing codebase blends catalogue data, army-list composition, and combat math into flat, anemic DTOs (`UnitProfile`, `ArmyList`) with no real domain boundaries. 11th edition also formalizes **Attached Units** (Leader/Support abilities joining a unit to a "bodyguard" unit to form one composite unit), which the current shape has no way to represent at all. The official Warhammer 40,000 app lets you mark a datasheet as attached to another but has no useful aggregated view of the result — you can't see, in one place, every statline and every weapon (with live counts) that applies while you're playing that unit. Building that view properly requires a real domain model first; this change establishes that foundation and delivers the aggregate view as its first concrete payoff.

## What Changes

- Introduce a **Catalogue context** rooted on `Datasheet` (reference/rules data resolved from BSData): named statlines (supporting more than one per datasheet), weapon profiles resolved by name on demand (never a materialized full options menu), and a richer `Ability` shape (`Name`, `Text`, optional structured `Choices`, and a `Scope` of `Model` or `Unit`).
- Introduce a **Roster context** rooted on `Unit` (a Datasheet selected into a specific army list, with model-lines carrying statline reference, chosen weapons, and count) and a new composite **`AttachedUnit`** (one Bodyguard `Unit` plus an open collection of attached Leader/Support `Unit`s — 11e's "single unit for all rules purposes").
- Add an **Attached Unit aggregate view**: a live-computed read model showing all distinct statlines across an attached unit's components, all weapons aggregated by profile with live counts, all Unit-scoped abilities combined into one list, Model-scoped abilities kept against their own model entry, and the live keyword union across currently-present components.
- Add minimal **live tracking** needed to drive that view: a decrementing "remaining count" per model-line (alive/dead granularity only, no partial-wound tracking), adjustable via tap-to-remove (count-1 lines) or a spinner (count>N lines).
- **BREAKING**: `Catalogue/*`, `Contracts/*` (`ArmyList`, `UnitEntry`, `ModelEntry`, `WeaponEntry`, `UnitProfile`), and `Enrichment/Enricher.cs` are being replaced by the new domain model. `Simulation/*` (CombatSimulator, SimulationAdapter, etc.) is untouched code but is **paused** — not wired to the new domain model in this change, since its assumptions (one flat Toughness per defender, no Attached Unit concept) don't fit and reconciling them is deferred to a later change.
- Out of scope for this change: rewriting `ArmyListParser.cs` for the new (and eventually multiple) export formats — the new domain model is built and proven against static, hand-built fixtures, not live parsing. Also out of scope: wargear constraint/composition-rule validation, points-cost modeling, partial wound-count tracking, and any positional/objective/turn-tracking gameplay state — the exporting app is trusted to have already produced a valid, legal list.

## Capabilities

### New Capabilities
- `datasheet-catalogue`: the Catalogue-context reference model — `Datasheet` with named statlines, on-demand weapon profile resolution, and the `Ability` shape (`Text` + `Choices` + `Scope`).
- `roster-model`: the Roster-context model — `Unit` (Datasheet reference, model-lines, Enhancements) and the `AttachedUnit` composite (Bodyguard + open collection of attached units), including the keyword-union rule (live derivation over currently-present components, not stored data) and the Toughness-resolution rule for attacks against an attached unit (captured as a domain fact even though no simulation code consumes it yet).
- `attached-unit-tracker`: live remaining-count tracking per model-line (alive/dead granularity, tap/spinner adjustment) and the aggregate view built from it (combined statlines, aggregated weapon counts, combined Unit-scoped abilities, live keyword union).

### Modified Capabilities
- None — this is a from-scratch domain model; there are no pre-existing specs in `openspec/specs/` to modify.

## Impact

- **Removed/replaced**: `src/ProbHammer.Core/Contracts/*`, `src/ProbHammer.Core/Catalogue/*`, `src/ProbHammer.Core/Enrichment/Enricher.cs` — superseded by the new Catalogue/Roster domain model.
- **Untouched but disconnected**: `src/ProbHammer.Core/Simulation/*` — remains in the tree (its weapon-aggregation-by-equality-key algorithm is worth reusing conceptually for the aggregate view's weapon aggregation) but is not wired to the new domain model in this change.
- **Untouched**: `src/ProbHammer.Core/Parsing/ArmyListParser.cs` — the 11e export format has changed; this parser is superseded by future work, not this change. Static fixtures stand in for parsed input here.
- **Web/UI**: not addressed in this change; the aggregate view's data model is the deliverable, not its rendering surface.
- **Tests**: new test suite exercising `Datasheet`, `Unit`, `AttachedUnit`, and the aggregate view against hand-built fixtures (no BSData or export-parsing dependency).
