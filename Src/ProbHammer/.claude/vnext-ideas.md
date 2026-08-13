# vNext Ideas — Deferred Features

Ideas and follow-on work that came up during 11e development and were deliberately deferred
rather than built. Nothing here is scheduled — this is a parking lot to draw from when picking
the next `openspec` change. Update this file whenever a new idea gets deferred, and remove an
entry once it's been turned into a change (archived changes remain the historical record).

---

## `/LivePlay` — Attached Unit view

- **Ranged/Melee weapon-contribution breakdown UI.** A merged weapon row today shows only the
  aggregated `TotalAttacks`. The domain already retains per-contribution provenance
  (`WeaponContribution(ComponentName, StatlineName, Count, PerModelAttacks)` on
  `AggregateWeaponEntry.Contributions` — added in `rework-attached-unit-aggregate-view`
  specifically so a future UI could use it) but nothing surfaces it. Idea: a toggle or drill-down
  on a merged row that explodes it back into which component/model-line contributed how much.
  Matters most for **Ranged** weapons — unlike Melee, a single ranged weapon's attacks must all
  be directed at one enemy unit, so knowing the per-source breakdown is sometimes necessary
  (not just informative) when deciding how to split fire.
- **Live casualty mutation/removal.** Tap-to-remove (count=1 lines, e.g. a Leader) or a spinner
  (count>1 lines, e.g. a squad), calling `ModelLine.RemoveCasualties`. The view already carries
  the right granularity (per-`ModelLine` loadout breakdown) to target this correctly. `/LivePlay`
  is currently read-only, rebuilt fresh from `Examples.View.MyArmy()` every request.
- **Session/state persistence for casualty tracking across page loads** — depends on the above;
  currently every request rebuilds a fresh army with no memory of prior removals.
- **Phase-level section toggling across all units at once** — collapse/expand every unit's
  statline/ranged/melee/abilities section together, reusing the same disclosure unit each
  section already uses individually.
- **Core/Faction/Psychic ability source tagging** — distinguish where an ability comes from,
  not just its name/scope.
- **Per-`ModelLine` keywords.** Needed so a leader's personal keyword leaves the unit's effective
  keyword union when that specific model is removed as a casualty — currently keywords are only
  tracked at the component (`Unit.Datasheet.Keywords`) level.
- **Multi-profile-weapon "select one profile" disclaimer** — some weapons have multiple firing
  profiles the player picks between; the view doesn't currently flag this.
- **Ability-driven attack modifiers** — e.g. a unit-wide "+1 Attack to melee weapons" ability
  changing a total from A28 to A35. Not modeled at all yet (consistent with the standing rule
  that ability text is never auto-parsed into behavior).
- **Splitting `Keywords` into unit-wide-union vs. per-component, or adding `FactionKeywords`** —
  currently left as one unioned `IReadOnlySet<string>`.
- **Markup convergence with `_UnitCard.cshtml`** — `/LivePlay`'s GW-style cards and the older 10e
  `ArmyView` unit cards evolved separately; no attempt yet to share markup/partials between them.

## Domain / data pipeline

- **Rewrite `ArmyListParser.cs` for the 11e export format** — the Warhammer App export format
  changed for 11th edition; `/LivePlay` currently runs entirely on hand-built fixture data
  (`Examples/Units.cs`, `Examples/Datasheets.cs`), not a parsed army list. No 11e parser exists.
- **Rebuild `Simulation/*` (CombatSimulator, SimulationAdapter, WoundPool) against the 11e domain
  model.** Paused, not abandoned — the old design assumes one flat Toughness per defender, with
  no notion of Attached Units or multi-statline targets, so it doesn't fit the new shape as-is.
- **Wire `Simulation`/`CombatSimulator` into `/LivePlay`** — today `/LivePlay` is a static
  per-request reference view, not a dice-rolling tool; this is separate from rebuilding the
  simulator itself.

## Bigger picture

- **A full "Gameplay" bounded context** for live/mutable game state beyond casualty counts —
  turn tracking, objective control, battle-shock, and other conditions that rules text sometimes
  references ("while this model is on the battlefield...", "gets Lethal Hits while within range
  of an objective marker"). Some ability text is permanently out of scope for auto-parsing
  because it needs state (positioning, objective control) the domain has no way to represent even
  with this context built — see `.claude/domain-model-11e.md`'s Deliberate Omissions.
