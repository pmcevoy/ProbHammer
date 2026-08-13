## Why

`/LivePlay` is meant to be scanned quickly on a phone/tablet mid-game, but its current rendering
doesn't hold up against that goal: unit blocks are headed by a generic "Unit N" ordinal instead of
the unit's actual name, statlines and weapons are dense always-expanded tables, and weapon
abilities are buried in their own column. Comparing it against GW's own datasheet cards and the
official app's mobile "Battle Forge" view (both proven, in-production references for exactly this
"look something up fast during a game" problem) surfaced a coherent set of layout and information-
hierarchy improvements. This change carries those improvements into `/LivePlay`, without yet
taking on live state mutation (casualty removal stays a future change).

## What Changes

- Add a computed `Name` to `ICombatUnit` (`Unit` and `AttachedUnit`) and thread it onto
  `AttachedUnitAggregateView`, replacing the page's ordinal "Unit N" headers with real names:
  a `Unit`'s name is its Datasheet's name; an `AttachedUnit`'s name is
  `"{Bodyguard} with {Leader}"` for a single attached Leader, or an Oxford-comma list
  (`"{Bodyguard} with {A}, {B}, and {C}"`) for more than one. Duplicate-named attached leaders are
  a known unhandled edge case, not solved by this change.
- Rewrite `/LivePlay`'s statline rendering as one header+tile block per statline entry (matching
  the GW mobile app's pattern), never merging entries that share identical stat values — this
  reinforces the existing no-merge-by-value behavior already locked into the `live-play-view` spec,
  now with a presentation shape that also leaves room for a future per-loadout removal control
  (not built in this change).
- Split each unit's weapons into **Ranged** and **Melee** sections (GW datasheet convention,
  shared/aligned columns since `WeaponProfile.Skill` is already unified across both types), each
  ordered by descending expected value of the aggregated `TotalAttacks`. `DiceExpression` has no
  natural total order today, so this introduces an expected-value calculation used only for sort
  ordering.
- Move weapon ability flags from a separate table column to inline bracketed tags next to the
  weapon name (e.g. `Storm bolter [RAPID FIRE 2]`).
- Make each unit block's sections (statline / ranged weapons / melee weapons / abilities)
  independently collapsible, default collapsed — the atomic disclosure unit a future phase-specific
  view could reuse in bulk, though that bulk toggle is not part of this change.
- Restyle `/LivePlay` toward a GW-datasheet-inspired light theme and typography. Scoped to this
  page only — `.claude/design-tokens.md` and every other page keep the existing dark tactical theme
  unchanged.

**Out of scope (tracked for a later change, not part of this one):** phase-level section toggling
across all units at once; Core/Faction/Psychic ability source tagging; per-`ModelLine` keywords
(needed so a leader's personal keyword leaves the unit's effective set when that specific model is
removed); a multi-profile-weapon "select one profile" disclaimer; and actual live casualty
mutation/removal — this change only shapes the layout to support that interaction later, the page
remains read-only and is rebuilt fresh from `Examples.View.MyArmy()` on every request.

## Capabilities

### New Capabilities
(none — this change extends existing capabilities only)

### Modified Capabilities
- `roster-model`: `ICombatUnit` gains a computed `Name` (`Unit.Name` from its Datasheet;
  `AttachedUnit.Name` composed from its Bodyguard's and Attached units' Datasheet names).
- `live-play-view`: statline, weapon, and section-disclosure rendering requirements change —
  name-based unit headers, tile-block statline rendering, Ranged/Melee weapon grouping with
  expected-value ordering, inline weapon ability tags, and per-section collapse/expand.

## Impact

- `ProbHammer.Core.Domain.Roster`: `ICombatUnit`, `Unit`, `AttachedUnit`,
  `AttachedUnitAggregateView` / `AttachedUnitAggregator` (new `Name` plumbing).
- `ProbHammer.Core.Domain.Catalogue`: `DiceExpression` (new expected-value helper used for sort
  ordering only).
- `ProbHammer.Web.Pages.LivePlay` (`.cshtml` + `.cshtml.cs`): rendering rewrite.
- `wwwroot/css`: new/expanded styles scoped to `.live-play-page`.
- Domain and Web test suites covering the above.
- No change to `Simulation/*`, `ArmyListParser`, BSData wiring, or session persistence — all remain
  untouched/paused per existing scope boundaries in `.claude/domain-model-11e.md`.
