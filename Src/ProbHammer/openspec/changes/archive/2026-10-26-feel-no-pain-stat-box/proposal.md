# Proposal

## Why

Feel No Pain is a defensive value players check constantly at the table, but `/LivePlay` only shows
it as an ability name, and the 113 classified Feel No Pain grants in the catalogue are loaded and
then ignored. Living Fortress (Custodian Wardens: once per battle, FNP 4+ until the end of the
phase) can't be activated because there is no FNP value for it to change, and the Wardens' bare,
valueless "Feel No Pain" entry (which BSData includes to point at Living Fortress) is noise.

## What Changes

- A Statline gains a Feel No Pain characteristic, shown as an "FNP" tile below the stat row (beside
  InSv), in the same tile family. GW prints FNP as an ability, not a characteristic; it is promoted
  here because it is common and checked as often as a save.
- A run's FNP comes from the "Feel No Pain X+" abilities its bearer carries, on the datasheet or
  through wargear. FNP is always ability-granted, so the tile is always highlighted (amber, or the
  conditional colour) and its popover names the giving ability. A footnoted or compound name
  ("Feel No Pain 3+*", "Feel No Pain 4+ 5+") gives a caveated value, like a footnoted InSv. No FNP
  and nothing reaching it: no tile.
- Classified Feel No Pain effects apply like invulnerable-save effects: an unconditional grant keeps
  the better threshold and highlights the tile amber; a conditional one (Living Fortress) is recorded
  as not added, shows the tile in the conditional colour (a "–" tile when the run has no FNP), and
  gets an Apply control in its ability's popover.
- A Feel No Pain effect whose record carries a qualifier (against mortal wounds, Psychic Attacks,
  CHARACTER models only, ...) never changes the value. It appears in the popover as a caveat line
  naming the qualifier. Qualifier display beyond that stays out of scope.
- A valueless "Feel No Pain" ability entry is hidden from a component's ability list while that
  component's runs show an FNP tile.
- Living Fortress's classification is hand-corrected: its "Psychic Attacks" qualifier is not in its
  text.

## Capabilities

### New Capabilities

- `feel-no-pain`: the Statline Feel No Pain characteristic - its value shape, the value
  derived from a bearer's Feel No Pain abilities, and the better-threshold merge.

### Modified Capabilities

- `statline-flag-rules`: Feel No Pain effects are applied and recorded like invulnerable-save
  effects; qualified ones never change the value.
- `live-play-view`: the FNP tile, its provenance highlight and popover lines, and hiding the
  valueless Feel No Pain entry.

## Impact

- `ProbHammer.Core`: `Statline`, `AttachedUnitAggregator` (the bearer's own FNP, effect application,
  not-applied recording, `Reaches`), a Feel No Pain merge helper beside `InvulnerableSaveEffectResolver`.
- `ProbHammer.Web`: `_UnitBlock.cshtml` (FNP tile), `ValueProvenance.cs`, `LivePlay.cshtml.cs`
  (ability-list filter), `site.css` (tile placement).
- Data: Living Fortress record in `tools/AbilityPipeline/data/classifications.json`, re-exported.
- Docs: `.claude/domain-model/statline-flag-rules.md`, `.claude/design-tokens.md`,
  `.claude/vnext-ideas.md` (remove the FNP-box and valueless-entry items).
