## Context

See proposal.md for why. Current state this design builds on:

- `ScalarCharacteristicView`/`InvulnerableSaveCharacteristicView` already carry `OriginalValue`,
  `DerivedValue`, `IsCaveated` and `ContributingAbilities`. Scalar Statline fields and weapon S/AP/D
  are first-applied-wins, so at most one contributing ability per scalar field; InSv `Merge` can
  record two.
- Attacks never mutates the profile: each `WeaponContribution` records `AttacksContributions`, and
  `TotalAttacks` folds them in.
- Conditional effects reach the page only for weapons, as `WeaponContribution.UnresolvedAbilities`
  (ability only, no characteristic or amount). Conditional Statline effects are dropped in
  `ApplyStatlineFlagRules`.
- `LivePlayModel.AssignFlagMarkers` builds one marker registry per unit block; `_UnitBlock.cshtml`
  renders markers, legend lines/rows (`RenderLegendLine`) and the full-cell `weapon-value-flagged`
  tint.
- `live-play.js` `recomputeWeaponRow` overwrites the A cell's `textContent` on selection changes.
- `RulePopoverRenderer.BuildRulePopover` builds a trigger/panel pair whose title is the trigger HTML
  and whose body is rendered rule text; nested panels are emitted as siblings, and the Popover API
  keeps a parent open while a child opened from inside it is shown.

Real-data cases, confirmed live 2026-10-03 at 667×315 across every list in `data/`: modified
(Crusade of Wrath S and A, Faith-Fuelled Resolve OC, Ancient's Banner OC), resolved footnote
(Impulsor InSv via Refractor Field), not-added (Chance for Glory on a Chaos Lord's Daemon hammer,
Martial Honour OC, Ork Waaagh! InSv). A caveated-unresolved tile exists only in
`verification-roster-ability-effects-custodes.json`.

## Goals / Non-Goals

**Goals:**
- One provenance view model and one renderer serve every highlighted value, Statline and weapon.
- The Core aggregate carries everything the popover shows; the page computes display text only.

**Non-Goals:**
- Showing a second applicable ability that first-applied-wins skipped (see Risks).
- Any change to which effects apply, to target scoping, or to the breakdown's row layout.

## Decisions

### D1. Reversal: caveated values render amber
`classify-characteristic-modifier-caveats` ruled that a still-caveated value must not be amber,
because amber read as "already adjusted for you". Amber now means "an ability has something to say
about this value; tap to judge", and the popover states which case applies (modified, caveated,
not added), so a caveated value is highlighted like the rest. User decision, 2026-10-03.
`CaveatedScalarTile_RendersMarkedLabelAndLegendButNoAmberBackground` and the caveated case in
`LivePlayInvulnerableSaveRenderingTests` flip; design-tokens.md's "Flagged Statline Legend" section
is rewritten.

### D2. Not-applied effects are recorded in Core, with their condition
- New `EffectCondition(UsageLimit?, GameTurn?, string? ConditionText, bool IsChoiceBranch)`, built
  from the record and the `ClassifiedEffect`.
- Weapons: `WeaponContribution.UnresolvedAbilities` and `AggregateWeaponEntry.UnresolvedAbilities`
  become `NotAppliedEffects` of `NotAppliedWeaponEffect(Ability SourceAbility, string Characteristic,
  int Amount, EffectCondition Condition)`. `Amount` is the signed per-model change (AP improve 1 is
  -1), computed the same way an applied effect's would be. The entry-level list is distinct by
  (ability name, text, characteristic).
- Statline: `AggregateStatlineEntry` gains `NotAppliedEffects` of `NotAppliedStatlineEffect(Ability
  SourceAbility, RuleEffect Effect, EffectCondition Condition)` for conditional `Scalar` and
  `InvulnerableSave` effects. `ApplyStatlineFlagRules` already finds the bearer-matched records; it
  records the conditional effects in the same pass, using the same `IsBearerOf`/target rule and
  Detachment-Rule exception.

Alternative: keep `UnresolvedAbilities` and look the classification up again in the page. Rejected:
it duplicates bearer/selector matching in Web, and the popover needs the characteristic and amount
per value.

### D3. One provenance view model, built in `LivePlayModel`
`ValueProvenance(string Title, string OriginalText, IReadOnlyList<ProvenanceLine> Lines, string?
ResultLabel, string? ResultText)` with `ProvenanceLine(Ability? Source, string Label, string
ChangeText, string? Note, bool Applied)`. `Source` null means a fixed line (Battle-shocked).
Built per highlighted value and attached to `StatlineBlockViewModel` (keyed by field, plus InSv) and
`WeaponRowViewModel` (keyed by A/S/AP/D). Replaces `ScalarFlagSources`, `ScalarMarkers`,
`InvulnerableSaveFlagSource`, `InvulnerableSaveMarker`, `ValueMarkers`, `ValueFlagSources`,
`NameMarker`, `NameMarkerSource`, both `FlagLegend`s and `AssignFlagMarkers`.

Case rules:
- Applied lines from `ContributingAbilities` (non-caveated): result label "Total", result the
  displayed value.
- Caveated: original label "Datasheet", one "may be modified by" line with no change text, no
  result row.
- Not-added lines from `NotAppliedEffects`, note = condition summary + "not added". Only not-added
  lines: result label "Shown", result = original.
- Resolved InSv footnote: original is `OriginalValue` (the printed save).
- Battle-shocked OC: applied lines as normal, then a fixed "Battle-shocked" line with change "→ 0",
  result "Total" 0. BSData has no Battle-shock rule, so this line has no nested popover.
- Residue: `UnclassifiedResidue` from the source ability's catalogue record, as the line's note.
  `BuildUnitBlock` takes the `AbilityClassificationCatalogue` to look it up (every caller already has
  it).

Change text: signed delta for M, T, W, OC, A, S, D and AP ("+1", "-1"); the resulting value for
roll-threshold characteristics Sv and Ld ("2+") and for InSv ("5+", with the melee/ranged icons when
split); "= N" for a Set verb. Pending Statline deltas are computed with
`CharacteristicModificationResolver` against the original value, the same resolver applied effects
use.

### D4. Total A provenance
Original = sum of every contribution's base `PerModelAttacks.Scale(Count)` (a `DiceExpression`, so
dice totals still render). One line per distinct Attacks source across the entry, change text "+N"
(sum of Count × Amount), note "+a per model × c models". Not-added A effects come from D2. Result
"Total" = `TotalAttacks`. The breakdown keeps its ability lines (user decision); the popover and the
breakdown are two on-demand views of the same numbers.

`ShowsBreakdownTrigger` drops its marker terms: multiple model lines, or any recorded Attacks
contribution. A single-model unit's S/AP/D change no longer needs the breakdown to be reachable,
because the value itself is the trigger.

### D5. Rendering
- `RulePopoverRenderer` gains `BuildProvenancePopover(triggerHtml, triggerClass, titleHtml,
  ValueProvenance)`: same id scheme, close button and sibling-panel trailer as `BuildRulePopover`,
  but a separate title and a provenance table body. Each line's ability is built with
  `BuildRulePopover(..., depth: 1)` inside the body, so its panel nests above the provenance panel
  and the parent stays open.
- Statline: the tile itself becomes the trigger (`<button class="stat-tile provenance-tile">`), so
  the whole tile is the tap target. InSv shapes (uniform, melee-only, ranged-only, split) keep their
  value markup inside the button. A highlighted InSv with no save renders "–".
- Weapons: the `<td>` keeps the row's zebra background; an inner `<button class="provenance-tile">`
  carries the amber, and its panel trails it inside the `<td>`.
- `RenderLegendLine`, legend rows, `.statline-flag-legend` and `.flag-legend-marker` go.
  `RenderAttacksContributionLine` keeps its flex wrapper, renamed off `flag-legend-line`.

### D6. Inset tile with a corner tick (mock V3)
`.provenance-tile`: `--amber-tint` fill, 1px `--amber` border, 3px radius (the existing
`.stat-tile-flagged` treatment), plus a 5px `--amber` corner triangle bottom-right via `::after`. No
new colour tokens. `.weapon-value-flagged` and the full-cell rule go. Chosen from four mocks at
667×315: the border plus tick reads as "there's a note about this" and as tappable on a 6-column
table, and the row colour frames it so a column of highlighted values no longer reads as a tint.
The user noted the tick might suit ability buttons generally; that's not part of this change.

### D7. Selection recompute
`recomputeWeaponRow` writes the sum into a `.weapon-attacks-number` span inside the trigger instead
of the cell's `textContent`. The A popover carries `data-prov-original` and `data-prov-total` spans:
the first gets the selected base sum, the second the sum plus ability lines, using the numbers
`recomputeWeaponRow` already computes. When a value is dice-based, both keep their server-rendered
text, as the cell does today.

## Risks / Trade-offs

- [More amber on real lists: Chance for Glory highlights four Daemon hammer values, Martial Honour
  highlights Helbrecht's OC] → Accepted by the user as the point: the highlight reminds the player.
  A lighter style for not-added-only values can follow if it proves busy.
- [First-applied-wins hides a second applicable ability on the same scalar] → No real list collides
  today; the popover lists what was applied. Recording skipped abilities is a later change.
- [Residue is classifier-written prose] → Shown as a note, not a value; the nested ability popover
  always has the real rules text.
- [Two `attached-unit-tracker` scenario titles still say "unresolved ability reference"] → Strict
  validation forbids renaming a MODIFIED scenario; their bodies describe not-applied effects.
- [Popover content can go stale if `live-play.js` fails to update it] → The test suite has no JS
  harness, so this is verified live via chrome-devtools: deselect a contributor, then read the A
  cell and both popover spans from the DOM.

## Migration Plan

Single deploy, no persisted state changes: casualty, selection and status storage keys are untouched.
Rollback is a revert.
