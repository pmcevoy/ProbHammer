## Why

The ability-classification catalogue records 433 weapon keyword grants ("melee weapons equipped by
this model have [LETHAL HITS]", "ranged attacks have [HEAVY]"), but nothing reads them, so
`/LivePlay` never shows a keyword an ability gives a weapon. A forgotten Sustained Hits or Lance
changes the dice at the table as much as a forgotten +1 Strength, and value-provenance popovers just
gave the page a way to show what an ability adds and why. Detachment rules make the gap worse: the
weapon path ignores keyword-targeted abilities entirely, even though Detachment rules arrive already
matched to the units they reach.

## What Changes

- A weapon keyword grant reaching a weapon renders as a keyword chip beside the weapon's own chips.
  An applied (unconditional) grant is an amber chip; a not-added (conditional) grant is a chip in a
  new "conditional" colour. The chip's popover lists the granting ability above the keyword's rule
  text, with the condition for a not-added grant.
- **Native keywords win**: a grant the weapon already has (or has at an equal or better value) shows
  nothing extra, and the native chip's popover stays as it is.
- **A better value replaces the native keyword** when applied (Sustained Hits 2 over a native
  Sustained Hits 1): the chip renders amber and its popover says what it replaces. Better means a
  higher number for Sustained Hits, Rapid Fire and Melta, and a lower threshold for Anti-X, where
  the target type is part of the keyword. A not-added better value sits beside the native chip.
- An applied grant becomes part of the weapon's identity, so a grant reaching only some carriers
  splits the merged weapon row, as a bearer-scoped Strength change already does. A not-added grant
  never splits a row.
- **The conditional colour also applies to value tiles** whose popover holds only not-added lines
  (today's Chance for Glory, Martial Honour, Waaagh!). Tiles with an applied change, a caveat, or a
  Battle-shocked OC stay amber.
- **Detachment-rule effects reach weapons**: the Statline path's exception (a Detachment-rule
  ability's keyword target was already evaluated, so it applies to the whole unit) extends to every
  weapon effect, grants and Strength/AP/Damage/Attacks alike. On the captured lists this changes no
  displayed number; Empyric Wellspring adds a not-added Strength line on the Masters of the Maelstrom
  Legionaries' ranged weapons.
- Out of scope: activating a conditional grant (condition toggles), and renaming the `--amber`
  tokens to semantic names (a later style and themes change).

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `weapon-characteristic-effect-resolution`: adds resolving a weapon keyword grant against a weapon's
  own keywords (native precedence, better-value replacement, keyword identity).
- `attached-unit-tracker`: "Aggregate Weapon Count View" applies matched keyword grants to each
  contribution's keywords (splitting on an applied grant), records not-added grants, and treats a
  Detachment-rule ability as reaching every component's weapons.
- `live-play-view`: "Weapon Section Rendering" adds granted keyword chips and their popovers, and
  drops its stale reference to the retired weapon markers; "Value Provenance Highlight" gives a
  not-added-only value the conditional colour.

## Impact

- Core: `AttachedUnitAggregator` (`MatchedWeaponEffects` target filter, grant application in
  `ResolveContributionProfile`, not-added grant records), `AttachedUnitAggregateView`
  (`WeaponContribution`/`AggregateWeaponEntry` grant records), a keyword-identity parser beside
  `WeaponCharacteristicEffectResolver`.
- Web: `LivePlay.cshtml.cs` (chip view model, conditional-only flag on `ValueProvenance`),
  `_UnitBlock.cshtml` (chip rendering in both weapon tables), `RulePopoverRenderer` (source section
  in a keyword chip popover), `site.css` (`--cond`/`--cond-tint` tokens, chip and tile rules).
- Tests: aggregator grant tests, keyword-identity tests, chip rendering tests, real-corpus checks.
- Docs: `.claude/design-tokens.md`, `.claude/domain-model/roster-context.md`,
  `ability-classification-catalogue.md` (consumer table), `.claude/vnext-ideas.md`.
