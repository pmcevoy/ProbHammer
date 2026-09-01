## Why

Two related gaps make `/LivePlay` under-report what a unit's stats actually are. First, a footnoted
invulnerable save (`"4+*"` or `"5+ / 4+*"`) always renders as an unresolved caveat with a prose
paragraph underneath, even though the linked ability's text is, in the overwhelming majority of real
cases, one of a handful of fixed sentences a player would rather see resolved into a real
melee/ranged split. Second, an ability or Enhancement that actually *changes* a characteristic (an
Impulsor's Shield Dome granting a 5+ invulnerable save; a Custodian Guard's Vexilla adding 1 to OC)
today only ever shows up as a name in the Abilities list — the player has to read it and do the
arithmetic themselves at the table, the exact manual step this app otherwise exists to remove.
Separately, every Leader/Support-eligible unit currently carries one or two abilities ("Leader",
"Support", "Attached Unit") whose text only repeats the export's own already-parsed and already-
rendered attachment relationship — pure noise in the Abilities column.

This change resolves a small, closed set of previously-catalogued ability phrasings into real stat
values wherever it's safe to do so, always leaving the source ability visible for the player to
double-check, and drops the three attachment-eligibility abilities that never carry new information.

## What Changes

- **Invulnerable-save caveat resolution**: when a footnoted InSv's linked ability text is an exact,
  whole-string match against one of four known templates ("This model has a {N}+ invulnerable save
  against {ranged|melee} attacks." / "Models in this unit have a {N}+ invulnerable save against
  {ranged|melee} attacks."), resolve it to a real, non-caveated melee/ranged split instead of a
  uniform caveated value. Anything else (including the one confirmed real anomaly, Orks' Makari)
  keeps today's conservative fallback unchanged. Implemented once, shared by both the BSData and
  BattleScribe/NewRecruit import pipelines, replacing each pipeline's own near-duplicate digit logic.
- **New ability-driven Statline-flag rule mechanism**: a small, closed-vocabulary set of rules, each
  matching one specific ability by exact name and text, that derive a flagged Statline-characteristic
  value for a specific resolved unit (not the shared catalogue Datasheet) when that ability is
  actually present. Two seed rules: Shield Dome (grants a 5+ invulnerable save) and Vexilla (adds 1
  to Objective Control for the bearer's whole unit). A matched ability is never removed or suppressed
  from the unit's normal Abilities/Enhancements list — it always renders in both places.
- **Drop "Leader"/"Support"/"Attached Unit" abilities**: these three exact ability names — sourced
  either from a local BSData ability profile or from a shared Core Rule reference — never carry
  information the export's own attachment parsing hasn't already surfaced, and are excluded from a
  Datasheet's Abilities regardless of source. Applies to both import pipelines.
- **Unified flagged-stat display**: replaces the InSv-only always-visible italic caveat paragraph
  with a general per-statline-run footnote marker (`InSv*`, `OC**`, ...) plus a small legend line
  beneath that run's own tiles (`* [Ability Name]`), where the ability name reuses the existing
  ability-popover trigger already used everywhere else on the page. Applies uniformly to an
  unresolved invulnerable-save caveat and to a statline-flag-rule match alike. Deliberately repeated
  per statline run rather than hoisted to one shared location, so a unit-wide-scoped flag (e.g.
  Vexilla, which affects every component of an attached unit) is never invisible from any one run.

## Capabilities

### New Capabilities
- `statline-flag-rules`: the closed-vocabulary ability-to-stat-mutation rule mechanism (Shield Dome,
  Vexilla), and the requirement that a matched source ability always remains visible in the unit's
  normal ability listing.

### Modified Capabilities
- `invulnerable-save`: adds caveat-text classification for the bare and split footnote shapes,
  resolving to a non-caveated melee/ranged split on an exact whole-text template match.
- `datasheet-catalogue`: excludes abilities named exactly "Leader", "Support", or "Attached Unit"
  from a Datasheet's Abilities, regardless of which BSData extraction path produced them.
- `battlescribe-roster-import`: applies the same "Leader"/"Support"/"Attached Unit" exclusion to the
  BattleScribe/NewRecruit JSON pipeline's own ability extraction.
- `live-play-view`: replaces the InSv-specific always-visible caveat paragraph with a general
  per-run footnote-marker-and-legend mechanism, driven by both an unresolved invulnerable-save
  caveat and a `statline-flag-rules` match.

## Impact

- `ProbHammer.Core.Domain.Catalogue.Bsdata.BsdataDatasheetMapper` (`ResolveInvulnerableSave`,
  `ResolveCaveatAbility`, `ProcessAbilityProfile`, `ProcessRuleInfoLink`)
- `ProbHammer.Core.Domain.Import.BattleScribe.BattleScribeRosterMapper` (its own
  `ResolveInvulnerableSave`/`ResolveCaveatAbility`, and its ability extraction)
- `ProbHammer.Core.Domain.Catalogue.Datasheet` and `ProbHammer.Core.Domain.Catalogue.InvulnerableSave`
- `ProbHammer.Core.Domain.Roster.AttachedUnitAggregator` and its render-facing output shapes
  (`AggregateStatlineEntry` or equivalent) — new home for a resolved unit's flagged-characteristic
  values
- `ProbHammer.Web.Pages.Shared._UnitBlock.cshtml` and its `RulePopoverRenderer` usage — new
  marker/legend rendering, removal of `.insv-caveat-text`
- `.claude/domain-model-11e.md`, `.claude/design-tokens.md`, `.claude/vnext-ideas.md` (remove the
  now-addressed portion of the "Ability-text interpretation pass" idea)
- New test fixtures/corpus-scan coverage mirroring the project's existing `[Fact(Explicit = true)]`
  pattern for both the InSv classifier's closed vocabulary and the Leader/Support/Attached-Unit
  exclusion
