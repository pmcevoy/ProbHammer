## Context

ProbHammer's current codebase (`ProbHammer.Core`) is a small, pipeline-shaped implementation built for 10th edition: `ArmyListParser` → `Enricher` (resolves against BSData catalogue XML) → a flat, anemic `UnitProfile` stored in session → `SimulationAdapter` → `CombatSimulator`. The `Contracts/*` types are pure data bags with no behavior; all rules logic lives in separate static processors. The codebase was itself produced by regenerating code from prose specs in `.claude/*.md` (see `PROGRESS.md`), so there is precedent for spec-first development here.

11th edition formalizes **Attached Units**: a Leader or Support ability lets a unit join a "bodyguard" unit to form "a single unit for all rules purposes." Nothing in the current shape can represent this. Extended discovery (captured across several rounds of domain exploration) surfaced that:
- A datasheet can have more than one named statline (e.g. a Chaos Space Marine datasheet has 5), and weapon-loadout variation is independent of statline grouping (a squad's Sergeant can share the squad's statline but have different wargear options).
- BSData's constraint system (wargear min/max, composition rules) does not need modeling — the exporting app (official Warhammer 40,000 app, or eventually NewRecruit.eu) is trusted to have already produced a valid, legal list. ProbHammer enriches; it does not validate.
- Ability/rule effects have a `Scope` (`Model` vs `Unit`) that determines whether they apply only to their bearer or to a whole (attached) unit, per 11e rules text on Enhancements and Attached Units.
- An Attached Unit's keyword set is the union of its components' keywords for unit-level rule checks, but individual models never gain each other's keywords for model-level checks — and that union is a **live derivation over currently-present components**, not stored data.
- The concrete, motivating feature is an **Attached Unit aggregate view**: the official app has no view showing all statlines and all weapons (with live counts) across an attached unit's components. This is explicitly why the user is building ProbHammer as its own app rather than just a combat-math tool.
- The existing `Simulation/*` engine (CombatSimulator, SimulationAdapter, WoundPool, DiceExpression) has real, reusable pieces — notably a weapon-aggregation-by-structural-equality-key algorithm — but its core assumption (one flat Toughness/Save/Wounds per defender) does not accommodate Attached Units or multi-statline datasheets. The user has explicitly asked to pause reworking it until the new domain model is proven.

## Goals / Non-Goals

**Goals:**
- Establish a `Datasheet` (Catalogue context) domain type supporting N named statlines and on-demand (not materialized) weapon-profile resolution.
- Establish `Unit` and `AttachedUnit` (Roster context) domain types, with `AttachedUnit` as an open Bodyguard + attached-units collection (not fixed Leader/Support slots).
- Give `Ability` a real shape (`Name`, `Text`, optional structured `Choices`, `Scope: Model | Unit`) replacing today's flattened free-text/sub-ability-as-joined-string approach.
- Encode the Attached Unit keyword-union rule as a live derivation, not stored state.
- Deliver the Attached Unit aggregate view: combined statlines, weapon counts aggregated by profile, combined Unit-scoped abilities (Model-scoped abilities stay per-model), and the live keyword union.
- Add the minimal live-state layer the aggregate view needs: a decrementing remaining-count per model-line (alive/dead granularity only).
- Prove all of the above against hand-written static fixtures, independent of any parser.

**Non-Goals:**
- Rewriting `ArmyListParser.cs` for the new (or eventual multiple) 11e export format(s).
- Reworking or wiring up `Simulation/*` (CombatSimulator, SimulationAdapter) — remains untouched and disconnected; a later change reconciles it with the new domain model (or replaces it).
- Modeling or enforcing wargear option constraints, unit-composition rules, or points costs.
- Partial wound-count tracking per model (only alive/dead per model-line — physical wound markers already cover this at the table).
- Any positional, objective-control, turn-sequence, or other live gameplay state beyond "is this model-line's count still > 0."
- Distinguishing *which* ability (Leader vs Support) formed an attachment — confirmed unnecessary.

## Decisions

**1. Datasheet supports an arbitrary number of named statlines, not "1 or sometimes 2."**
Evidence forced this: a Chaos Space Marine datasheet has 5 distinct statlines; Black Templars Crusader Squad's Initiate/Neophyte differ only in Save. Alternative considered: a single statline plus an "overrides" mechanism for exceptions — rejected, since the exceptions are common enough (and independent enough from wargear grouping) that a first-class collection is simpler than an escape hatch.

**2. Weapon profiles resolve on demand; no full options menu is materialized.**
Matches an explicit product principle: ProbHammer never shows wargear that wasn't actually chosen (a deliberate contrast with the official app's confusing dual-view of datasheets). Alternative considered: pre-materialize every weapon profile per datasheet to support a future "browse this unit's options" feature — rejected as premature; nothing in this change's scope needs it, and it would work against the "only show what's chosen" principle if surfaced by accident.

**3. `Ability { Name, Text, Choices?, Scope }` — `Text` is retained even when `Choices` exists.**
Sub-ability "choose one of the following" groups often have meaningful intro prose (e.g. conditions like "While on an objective marker..."). Alternative considered: drop `Text` once `Choices` exists and treat the sub-ability list as the entire ability — rejected, this would silently lose conditions attached to the choice itself.

**4. `Scope: Model | Unit` is a property of the ability, not of its source (Enhancement, wargear, datasheet-intrinsic).**
11e rules text treats Enhancement and wargear as examples of sources, not special cases — the same binary scope applies regardless of source. Alternative considered: give `Enhancement` its own hardcoded "always affects the attached unit" behavior — rejected; this was an earlier assumption that turned out to be wrong once the actual rule text was read.

**5. `AttachedUnit { Bodyguard: Unit, Attached: IReadOnlyList<Unit> }` — an open collection, not fixed Leader/Support slots.**
The default is one Leader and one Support, but "unless otherwise stated" exceptions exist (a real datasheet allows 2 Leaders + 1 Support = 3 attached). Since ProbHammer does not validate attachment legality, there's no benefit to modeling the common case as fixed slots plus an exception path — a flat collection handles both uniformly. Which ability (Leader/Support) produced each entry is explicitly not tracked (confirmed unneeded).

**6. `AttachedUnit`'s effective keyword set is a computed function, never stored data.**
Per 11e rules, the union is over components *currently present* — if the Support unit is destroyed, the effective set changes with no change to any component's own keywords. Storing and invalidating a cached union was considered and rejected as unnecessary complexity; a pure function over "currently present components" is simpler and can't go stale.

**7. `Unit` and `AttachedUnit` should satisfy a common abstraction (Composite-pattern shape).**
"An attached unit is a single unit for all rules purposes" is a direct textual cue for this. The aggregate view (and anything built later) should be able to treat a plain `Unit` and an `AttachedUnit` uniformly wherever the rules don't distinguish them. Alternative considered: keep them as two unrelated types and duplicate view logic per case — rejected, duplicates work and risks the two views drifting apart.

**8. Reuse the weapon-aggregation-by-structural-equality-key concept from `SimulationAdapter` for the aggregate view's weapon counts.**
The existing algorithm (group by `(Type, Skill, Strength, AP, Damage, Abilities)`, sum counts/attacks) is already proven and solves the same shape of problem — grouping weapon instances by profile and summing quantities — just across an attached unit's components instead of across selected weapon groups. Reimplementing from scratch was considered and rejected; this is exactly the kind of "good code, keep it" piece identified early in scoping this rewrite.

**9. Ability-text is never auto-parsed into behavioral/simulation effects — this is a permanent exclusion, not a deferral.**
Some abilities are conditional on live game state the domain has no way to represent even in principle (e.g. "gets Lethal Hits while within range of an objective marker" needs positional/objective-control state, which is out of scope entirely). Alternative considered: parse the subset of abilities that don't have such conditions — rejected as inconsistent (there'd be no reliable way to know in advance which abilities are safe to parse without already understanding their full conditions).

**10. The Toughness-resolution rule for attacks against an Attached Unit is captured as a domain fact but not implemented against any simulator.**
`Simulation/*` is paused in this change, so there is no consumer for this rule yet. It's recorded (in the `roster-model` spec) so it isn't lost, and because it validates the multi-statline decision (the "highest T among bodyguard models" computation needs per-model-line stats).

## Risks / Trade-offs

- **[Risk]** Building fixtures ahead of the real 11e export-format study means fixtures may not match the eventual parser's actual output shape. → **Mitigation**: keep the parser/domain boundary explicit (fixtures feed the domain the same way a future parser would); expect to revise fixtures, not the domain shape, once the format is studied.
- **[Risk]** Pausing `Simulation/*` means no combat math is available end-to-end during this change. → **Mitigation**: deliberate and accepted — this change's payoff is the aggregate view, not combat resolution.
- **[Risk]** `Ability.Scope` may not be an explicit field in the real 11e BSData schema, requiring heuristic classification later (similar in spirit to the existing `ExtractInvulnFnp` regex approach). → **Mitigation**: fixtures hardcode `Scope` directly for now; how to source it from real catalogue data is an open question for a later change, not blocking this one.
- **[Risk]** The Unit/AttachedUnit common-abstraction shape (Decision 7) could be over-engineered if no other consumer besides the aggregate view ever needs it. → **Mitigation**: keep the abstraction minimal (only what the aggregate view actually needs); expand later if a second consumer appears.

## Migration Plan

This is a personal, session-based app with no persisted cross-version data to migrate. `Contracts/*`, `Catalogue/*`, and `Enrichment/Enricher.cs` are replaced outright by the new domain model and its tests. `Simulation/*` and `Parsing/ArmyListParser.cs` are left in place, untouched and temporarily disconnected, pending later changes. No rollback strategy beyond normal source control is needed.

## Open Questions

- How will `Ability.Scope` actually be sourced once the real 11e BSData catalogue files are parsed — an explicit schema field, or text-pattern inference?
- Does the eventual export-parser rewrite need to detect Attached Unit groupings (Bodyguard ↔ Leader/Support) from day one, or can that be layered in after basic parsing works?
- Do the official Warhammer 40,000 app export and NewRecruit.eu's export encode Attached Units the same way? Unknown until both are studied.
- What's the right C# shape for the `Unit`/`AttachedUnit` common abstraction (interface vs abstract base vs discriminated union) — left to the tasks/implementation phase.
