## Why

On `/LivePlay`, a value an ability touches is explained by footnote markers (`5*`, `OC**`, `Daemon
hammer*`) and legend rows that repeat the source ability once per run or weapon row. Helbrecht's
Crusade of Wrath appears in a legend on every melee row plus again in the expanded Attacks lines. The
full-cell amber tint on weapon S reads as a column tint, not a signal. And the ability effects behind
these values come from LLM classifications that may simply be wrong, so the page should draw the
player's attention to a value and let them judge it, always showing the original value, rather than
quietly trusting the classification.

## What Changes

- **Amber means "an ability has something to say about this value; tap to judge."** Every affected
  value (Statline tile, weapon A/S/AP/D) renders as an inset amber tile with a corner tick, and that
  tile is itself a popover trigger.
- The popover shows the original value, each contributing ability as an `.ability-name-line` button
  (its own nested popover holds the ability text), and the resulting value. Content by case:
  - **modified**: original, each applied ability with its change, total;
  - **modified with residue**: as above, plus the classification's unmodelled-residue note;
  - **resolved datasheet footnote** (a caveated InSv the catalogue resolved): original as printed,
    resolving ability, total;
  - **caveated, unresolved**: the datasheet value and "may be modified by [Ability]", no total;
  - **conditional ("not added")**: an effect the app can't evaluate (usage limit, turn restriction,
    residual condition, choice branch) is listed with its condition text and is not added; the value
    shown is unchanged. This is also how a qualified value appears (Ork Waaagh!: 5+ InSv while riled
    up), including an InSv tile that otherwise wouldn't exist.
- **Total A becomes a modified value**: its popover's original is the total without ability
  contributions (Power fist: 6 + Crusade of Wrath 2 = 8). The expanded breakdown keeps its ability
  lines. Selection-driven recompute keeps the popover's original and total in step.
- **Battle-shocked OC tile becomes a trigger**: original, any abilities, "Battle-shocked → 0".
- **BREAKING (page markup)**: footnote markers on Statline labels, weapon values and weapon names,
  every flag-legend line/row, and the "unresolved ability" weapon-name marker are removed.
- **Reversal, deliberate**: a caveated value now renders amber. This reverses
  `classify-characteristic-modifier-caveats`' "a caveated value must not be amber" rule; the popover
  now says which case applies, so no separate "possibly modified" style is needed.
- Out of scope: granted weapon keywords as chips, condition toggles, roll modifiers.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `live-play-view`: "Flagged Statline Characteristic Rendering" and "Flagged Weapon Characteristic
  Rendering" (markers + legends) are replaced by value-provenance highlight and popover
  requirements; "Battle-Shocked Objective Control Rendering" gains the popover; "Weapon
  Ability-Contribution Row Rendering" drops its reference to the marker convention.
- `attached-unit-tracker`: "Aggregate Weapon Count View" — the unresolved ability reference becomes a
  not-applied effect record carrying the characteristic, signed amount and condition, not just the
  ability.
- `statline-flag-rules`: a conditional Statline scalar/InSv effect is recorded on the entry it would
  reach (same target scoping as an applied one), still without being applied.

## Impact

- Core: `AttachedUnitAggregator` (record conditional Statline effects; weapon unresolved abilities
  become not-applied effect records), `AttachedUnitAggregateView` (`AggregateStatlineEntry`,
  `WeaponContribution`, `AggregateWeaponEntry`).
- Web: `LivePlay.cshtml.cs` (retire `AssignFlagMarkers`, `FlagLegend`, `ValueMarkers`, `NameMarker`,
  `ScalarMarkers`, `InvulnerableSaveMarker`; build per-value provenance view models),
  `_UnitBlock.cshtml` (`RenderScalarTile`, `RenderWeaponValueCell`, the OC/Battle-shock and InSv
  branches, A cell; delete `RenderLegendLine` and legend rows), `site.css` (tick tile, provenance
  popover body; drop legend/flagged-cell rules), `live-play.js` (A recompute writes into the trigger
  and updates the popover numbers).
- Tests: Statline/weapon/InSv/Battle-shock rendering tests and roster tests asserting markers,
  legends or `UnresolvedAbilities` change; `CaveatedScalarTile_RendersMarkedLabelAndLegendButNoAmberBackground`
  and the caveated case in `LivePlayInvulnerableSaveRenderingTests` flip.
- Docs: `.claude/design-tokens.md` (Colour Palette amber rows, Flagged Statline Legend, Popovers),
  `.claude/domain-model/statline-flag-rules.md`, `ability-classification-catalogue.md`,
  `.claude/vnext-ideas.md` (remove the shipped entry).
