## Why

`/LivePlay` now shows every conditional ability effect as a blue "not added" tile or chip, but the
player can never say "this condition holds now". Dark Pacts puts two blue chips (Lethal Hits,
Sustained Hits 1) on every Heretic Astartes weapon with no way to pick one, and Chance for Glory's
+1 S/A/AP/D can never be seen applied. The value-provenance and granted-chip work gave the page
everything needed to show an applied effect; this change lets the player switch one on.

## What Changes

- An ability's popover in a unit block gains an "Apply" section under its unchanged rule text, one
  control per activatable condition of that ability on that unit:
  - a switch per distinct condition (an ability whose only condition is a usage limit or turn
    restriction gets one switch);
  - a radio list, always starting with "None", per choice group with a maximum of one selection;
    capped checkboxes for a group allowing more.
  A condition is offered only when one of its effects reaches a value or weapon of that unit.
- An activated effect is applied exactly like an unconditional one: the value or chip turns amber,
  a partial reach splits weapon rows, and its provenance line notes it was activated by the player.
- Selecting a choice option drops the other options of that group: their effects are neither
  applied nor shown as not added. With no selection, every option shows as not added, as today.
- Activation state is kept per unit and ability in browser localStorage, like casualty and status
  state, and sent with the full-state sync. Changing a control updates storage at once; the sync
  and unit-block re-render happen when the popover closes, so it does not close under the player.
- Activations never reset automatically. Usage limits and turn ownership are shown as labels only.
- The phase/turn sync sends the full casualty, status and activation state. Today it sends empty
  lists while re-rendering every unit block from a pristine roster, which (from reading the code)
  already drops casualty and status display on a phase change, against `live-play-phase-tracker`'s
  "No Rules Interpretation", and would discard activations too.
- Out of scope: unit-fact conditions such as riled up, linking Empyric Wellspring to Dark Pacts, a
  "used" marker, and the Army-Rule applicability fix (all in `.claude/vnext-ideas.md`). Until that
  fix, Waaagh!'s effects never reach a unit, so it gets no controls in this change.

## Capabilities

### New Capabilities

- `condition-activation`: the player-set activation of a conditional ability effect: what can be
  activated on a unit (conditions and choice options), how an activation is applied, and how a
  choice selection suppresses the other options.

### Modified Capabilities

- `ability-classification-catalogue`: "Unconditional Effect Rule" lets a consumer also apply a
  conditional effect the player has activated.
- `statline-flag-rules`: "Conditional Statline Effects Are Recorded Without Being Applied" excludes
  activated effects and the unselected options of a choice group with a selection.
- `live-play-view`: "Conditional Effects Are Shown But Not Added" excludes activated effects; adds
  the popover's Apply controls, activated provenance notes, and activation persistence.
- `live-play-phase-tracker`: "No Rules Interpretation" adds condition activation state to what a
  phase/turn change must leave alone, with a scenario that re-rendered blocks keep recorded state.

## Impact

- Core: `AbilityClassification` (activation-aware applicability), `AttachedUnitAggregator` (Statline
  and weapon paths, grants, not-added records), `ICombatUnit`/`Unit`/`AttachedUnit` (activation
  state), `AttachedUnitAggregateView` (activatable conditions per ability).
- Web: `LivePlaySyncRequest`, `LivePlayModel.RebuildRosterWithStatus`, `LivePlayCasualtyService`,
  `RulePopoverRenderer` and `_UnitBlock.cshtml` (Apply section), `ValueProvenance` (activated line
  note), `live-play.js` (storage, close-triggered sync, full-state phase sync), `site.css`.
- Tests: aggregator activation tests, rendering tests, sync tests, a real-list check (Chaos Lord,
  Dark Pacts).
- Docs: `.claude/domain-model/roster-context.md`, `ability-classification-catalogue.md`,
  `.claude/design-tokens.md`, `.claude/vnext-ideas.md`.
