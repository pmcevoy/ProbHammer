# Proposal

## Why

An Army Rule's classified effects never reach the units that carry it when its classification
targets a keyword (Waaagh! targets `ORKS`, Templar Vows `ADEPTUS ASTARTES`, Synapse `TYRANIDS`) or
the bearer itself (Harbingers of Dread). Keyword targets are rejected because no roster-wide
keyword evaluation exists. Self targets never match because an Army Rule is promoted to a
whole-unit entry that belongs to no single component. Yet an Army Rule ability is only ever present
on a unit whose own datasheet or roster entry carries it, so it has already been scoped. The
visible gap: Waaagh!'s conditional 5+ InSv and `[ASSAULT]` never show on Ork units (confirmed live
2026-10-04). It is also the prerequisite for the planned Riled Up work.

## What Changes

- An ability whose Origin is Army Rule applies its classified effects whatever its classification's
  target kind, to every row of the unit carrying it - the same WholeUnit treatment the Detachment
  Rule exception already gives - on both the Statline and weapon paths.
- That brings in Waaagh! (conditional InSv 5+ and `[ASSAULT]` on ranged weapons), Synapse
  (conditional +1 S on melee weapons) and Templar Vows (its vow choice, plus a conditional
  `[PRECISION]` on melee weapons). All of their effects are conditional, so they show as
  not-added values and chips, with condition controls in the ability's popover. None changes a
  printed value until the player activates it.
- Templar Vows' choice options carry a corrupted apostrophe (`attacker\ractes1 Strength`) that
  would now be shown in its popover. The v2 batch run wrote a `\r` escape plus junk in place of an
  apostrophe in 3 of 3,828 records (Templar Vows' options and residue; Mindlock's and Radiant
  Mantle's residue, which isn't displayed). All three are hand-corrected in the classifier's source
  data and the catalogue re-exported. A reminder to check the prompt v3 resubmit for the same
  corruption goes into `vnext-ideas.md`.
- Army Rules already applied today (attached-unit-targeted Dark Pacts, Doctrina Imperatives,
  Martial Ka'tah) are unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `statline-flag-rules`: Target-Scoped Application gains an Army Rule exception beside the
  Detachment Rule one.

## Impact

- `ProbHammer.Core`: `AttachedUnitAggregator.TryGetApplicableClassification` and `IsBearerOf`.
- Data: `tools/AbilityPipeline/data/classifications.json` (Templar Vows, Mindlock and Radiant
  Mantle records, each with a `ReviewerNote`), re-exported to
  `src/ProbHammer.Web/Data/ability-classifications.json`.
- Docs: `.claude/domain-model/statline-flag-rules.md`, `.claude/vnext-ideas.md` (remove the entry;
  add a corruption check to the prompt v3 resubmit).
