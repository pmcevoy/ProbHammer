## Context

See proposal.md for why. Current state this design builds on:

- Every applied-or-not decision goes through `AbilityClassification.IsUnconditional(effect)`:
  `ApplyStatlineFlagRules` (Statline scalar and InSv), `MatchedWeaponEffects(..., conditional)`
  (S/AP/D, Attacks and keyword grants), and the `FindNotApplied*` helpers that build
  `NotAppliedStatlineEffect`, `NotAppliedWeaponEffect` and `NotAppliedKeywordGrant`.
- Unit status (`IsBattleShocked`, `IsHalfStrengthOverride`) is a settable property on
  `ICombatUnit`, replayed by `LivePlayModel.RebuildRosterWithStatus` from the request before
  `AttachedUnitAggregator.Build`. The server keeps casualty and status state for no request;
  phase/turn is the exception, held in Session by `PhaseTurnStore`.
- `syncPhaseTurn` posts empty casualty and status lists, and `LivePlayCasualtyService` then
  re-renders every unit block from a roster rebuilt with those empty lists.
- Unit-block ability popovers are built by one `RulePopoverRenderer` per block (`_UnitBlock.cshtml`
  line 21): directly in the Abilities column, and nested at depth 1 inside provenance and granted
  chip popovers. The Army Header uses its own renderer.
- Provenance lines are per source ability, not per effect (`ValueProvenanceBuilder`).
- Corpus facts (live classification file): choice groups are `1-1` ×62, `0-1` ×7, `1-2` ×4, `2-2`
  ×3, `0-3` ×1; 24 records with a conditional value or keyword effect carry more than one distinct
  condition text; option labels run from `[LETHAL HITS]` to 237-character sentences.

## Goals / Non-Goals

**Goals:**
- One Core seam decides applied, not added or suppressed, with activation state as an input, so
  every existing consumer picks it up unchanged.
- No new server state: activations travel with the full-state sync like casualties.

**Non-Goals:**
- The caveated-InSv path (`ResolveCaveatedInvulnerableSaves`) stays unconditional-only.
- No validation of a selection against its group's minimum; "None" is always allowed.
- The Army-Rule applicability fix and unit-fact conditions (vnext-ideas.md).

## Decisions

### D1. Activation state is a property of the combat unit
`ICombatUnit` gains `ConditionActivations` (default empty), set by `RebuildRosterWithStatus` from the
request exactly as `IsBattleShocked` is. `Build` keeps its signature. Shape:
`ConditionActivations(IReadOnlyDictionary<string, AbilityActivation>)` keyed by ability name
(case-insensitive), `AbilityActivation(IReadOnlySet<string> Conditions,
IReadOnlyDictionary<int, IReadOnlySet<int>> Choices)`. A condition with no text is the empty string.

Alternative: a `Build(combatUnit, classifications, activations)` parameter. Rejected: every caller
(`SortRoster`, `BuildUnitBlocks`, `RebuildRosterWithStatus`, `Examples/View.cs`) would thread it,
and the status precedent already puts player-set unit facts on the unit.

### D2. One three-way effect state replaces the `IsUnconditional` call sites
A Core helper classifies each classified effect for an ability on a unit as Applied (unconditional,
or activated per the spec), Suppressed (an unselected option of a group with a selection), or
NotApplied. The Statline and weapon matching passes take Applied effects where they took
unconditional ones, and the `FindNotApplied*` helpers take NotApplied ones. Suppressed effects are
dropped by both. `IsUnconditional` stays as the catalogue-level definition the helper uses.

### D3. Activatable conditions are reported by Core, from matching
`AttachedUnitAggregateView` gains `ActivatableConditions`: `ConditionToggle(Ability,
string ConditionText, UsageLimit?, GameTurn?, bool IsActive)` and `ChoiceToggle(Ability,
int GroupIndex, ChoiceGroup Group, IReadOnlySet<int> Selected)`. While matching (before the
applied/not-applied split) Core records which (ability, condition) slots had any effect reach an
entry or contribution; only those are reported. This keeps "reaches something" exact, including for
a group whose options are currently suppressed.

### D4. Controls are rendered by the unit block's popover renderer
`RulePopoverRenderer` takes an optional function from `Ability` to Apply-section HTML. The unit block
supplies one built from the view's `ActivatableConditions`; the Army Header supplies none. Every
ability popover the renderer builds, including the depth-1 ones inside provenance and chip popovers,
gets the section, so a chip's toggle is one nested tap away. Controls carry `data-unit-index`,
`data-ability`, and `data-condition` or `data-group`/`data-option`. Radio `name`s include the popover
id, since the same ability's controls can appear in several popovers of one block.

Labels: a condition's text; with no text, the usage limit ("Once per battle"), else the turn
ownership, else "In effect". A text condition shows its usage limit as a dim caption beside it.
Option labels are the classified option text, unaltered, so the player can compare them with the
rule text above. A switch is a checkbox with `role="switch"`; rows are full-width tap targets
(the touch-target lesson from the unit toolbar).

### D5. Storage, wire format and close-triggered sync
localStorage key `probhammer.livePlay.activations`: `{"<unitIndex>::<abilityName>": {"conditions":
["..."], "choices": {"0": [1]}}}`, pruned when an entry is empty. The sync request gains
`ActivationAdjustments: [{unitIndex, abilityName, conditions, choices}]`; their unit indexes join
`partialUnitIndexes` in `LivePlayCasualtyService`. A control change writes storage and marks the
page dirty; a `toggle` event closing any unit-block popover syncs if dirty. Closing a parent also
closes a nested child, so one close event is enough.

Alternative: sync on each change and re-open the popover. Rejected: popover ids come from a
per-render counter (`RulePopoverRenderer.NextPopoverId`), and activating a grant adds or removes
chips, which shifts every later id.

### D6. Phase/turn sync sends the full state
`syncPhaseTurn` builds the same casualty, status and activation lists as `syncLivePlayState`. The
server already re-renders every block for a phase change, so the blocks then render the recorded
state.

### D7. "Activated" note on provenance lines
An Applied provenance line (value or granted chip) whose source ability has an active condition on
that unit notes "activated". Lines are per ability, so no per-effect flag is needed in Core.

## Risks / Trade-offs

- [A mixed record (28 in the corpus) whose unconditional part reaches a value its activated part
  does not is noted "activated" there too] → Accepted; the line still names the ability, whose text
  is one tap away.
- [Branches that exclude each other by condition (Advanced Firepower's two targets) can both be
  switched on] → Accepted; the player judges, and target-type conditions can't be checked until the
  attacker/defender half exists.
- [Activations keyed by unit index go stale after a re-import] → Same as casualty keys: an
  out-of-range index or unknown ability is ignored. Reset Casualties does not clear activations.
- [A forgotten switch stays on] → The value stays amber with an "activated" line, which is the
  visible reminder.
- [Wrong classifications now change values when activated] → Only on the player's action, with the
  original text directly above the control.

## Migration Plan

Single deploy; new localStorage key only. Rollback is a revert; a stale key is ignored.
