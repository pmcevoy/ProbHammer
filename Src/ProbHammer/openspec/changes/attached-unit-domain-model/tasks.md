## 1. Project Structure

- [ ] 1.1 Add new `Domain/Catalogue` and `Domain/Roster` folders under `ProbHammer.Core` for the new domain model, kept separate from the existing `Contracts/`, `Catalogue/`, and `Enrichment/` code (which remains in place, untouched, until a later change reconciles `ArmyListParser`, `Web`, and `Simulation` with the new model)
- [ ] 1.2 Add a matching test folder structure under `ProbHammer.Tests` for the new domain namespaces, with a `Fixtures/` subfolder for hand-built static test data

## 2. Catalogue Context — Datasheet

- [ ] 2.1 Implement `Statline` (Movement, Toughness, Save, Wounds, Leadership, Objective Control) as a value object
- [ ] 2.2 Implement `WeaponProfile` (Range, Attacks, Skill, Strength, AP, Damage, Abilities) as a value object, including its structural equality key (Type, Skill, Strength, AP, Damage, Abilities — excluding count/attacks)
- [ ] 2.3 Implement `Ability` (Name, Text, optional Choices, Scope: Model|Unit)
- [ ] 2.4 Implement `Datasheet` (Name, Faction Keywords, Keywords, unit-wide Abilities, named Statlines collection) with on-demand weapon-profile resolution by name — no materialized full options menu
- [ ] 2.5 Write fixture Datasheets covering: a single shared statline (Assault Intercessor Squad-style), multiple distinct statlines (5-statline Chaos Space Marine-style), and a shared statline with divergent weapon eligibility (Sergeant-style)
- [ ] 2.6 Write tests for all `datasheet-catalogue` spec scenarios

## 3. Roster Context — Unit

- [ ] 3.1 Implement `ModelLine` (statline name reference, weapon selections, count)
- [ ] 3.2 Implement `Unit` (Datasheet reference, Enhancements, model-lines)
- [ ] 3.3 Write fixture Units covering: uniform loadout, and mixed loadout within one shared statline (e.g. 2× Power Fist / 3× Master-crafted Power Weapon)
- [ ] 3.4 Write tests for the Unit-composition scenarios in the `roster-model` spec

## 4. Roster Context — Attached Unit

- [ ] 4.1 Define the common `Unit`/`AttachedUnit` abstraction (Composite-pattern shape) so aggregate-view logic can treat both uniformly
- [ ] 4.2 Implement `AttachedUnit` (Bodyguard Unit, open Attached collection) without tracking Leader/Support origin
- [ ] 4.3 Implement the live keyword-union computation (over currently-present components) as a pure function, not stored state
- [ ] 4.4 Implement the Toughness-resolution rule for attacks against an AttachedUnit (highest T among present Bodyguard models, falling back to present Leader/Support models) as a domain-level computation, with no wiring to `Simulation/*`
- [ ] 4.5 Write fixture AttachedUnits covering: default one-Leader/one-Support, the two-Leaders-plus-one-Support exception, and the Bodyguard-destroyed fallback case for Toughness resolution
- [ ] 4.6 Write tests for all `roster-model` AttachedUnit spec scenarios (composite structure, keyword union including live recomputation, model-level vs unit-level keyword checks, Toughness resolution)

## 5. Live Tracking

- [ ] 5.1 Add a decrementing remaining-count to `ModelLine`, initialized from its original Count, clamped at a minimum of 0
- [ ] 5.2 Write tests for remaining-count decrement behavior, including the zero-floor case

## 6. Attached Unit Aggregate View

- [ ] 6.1 Implement the aggregate Statline view (distinct statlines across present components whose model-lines have remaining count > 0)
- [ ] 6.2 Port the weapon-aggregation-by-structural-equality-key algorithm from `SimulationAdapter` (grouping by `WeaponProfile`'s equality key, summing remaining counts) for the aggregate Weapon view
- [ ] 6.3 Implement the aggregate Ability view: combine Unit-scoped Abilities from all present components into one list; keep Model-scoped Abilities attached to their originating model-line; drop a Model-scoped ability once its bearer's remaining count reaches 0
- [ ] 6.4 Wire the aggregate view's keyword display to the live keyword-union computation from 4.3
- [ ] 6.5 Write tests for all `attached-unit-tracker` spec scenarios, including casualty-driven recomputation (weapon counts decreasing, statlines dropping out, Model-scoped abilities disappearing)

## 7. Wrap-up

- [ ] 7.1 Run the full test suite and confirm every `datasheet-catalogue`, `roster-model`, and `attached-unit-tracker` spec scenario has a corresponding passing test
- [ ] 7.2 Update `.claude/domain-model.md` (or add a new `.claude/` doc) to describe the new domain shape, per this repo's documentation-maintenance convention
- [ ] 7.3 Note in `PROGRESS.md` that `Contracts/*`, `Catalogue/*`, and `Enrichment/Enricher.cs` are superseded and `Simulation/*` is paused, and that all four remain in the tree only until a later change rewires `ArmyListParser`, `Web`, and `Simulation` to the new domain model
