## Why

`resolve-core-rule-abilities` (shipped, not yet committed) extracts BSData's `infoLink
type:"rule"` shape into always-exposed `CoreRule` abilities, but never reads the target rule's own
`hidden`/`modifiers` — so a chapter-exclusive faction rule shows regardless of which chapter the
army actually is. Verified two ways: a Black Templars roster shows `Oath of Moment` (a rule its own
BSData definition explicitly excludes Black Templars from) alongside `Templar Vows`; running the
same pipeline against a Salamanders closure produces the identical ability list, meaning a
Salamanders army would incorrectly show `Templar Vows` too. Separately, because each eligible
component of an `AttachedUnit` independently carries its own reference to the same underlying rule
(same `targetId`, confirmed), `Templar Vows` currently renders as three duplicate, textually
identical entries on a three-component attached unit instead of once.

## What Changes

- `BsdataDatasheetMapper` gains the ability to read a `BsRule`'s own `hidden`/`modifiers` and
  exclude a `CoreRule` extraction when they provably resolve true for the current closure —
  generalizing the existing `IsGameModeGated` mechanism (today scoped to `BsSelectionEntry`/
  `BsSelectionEntryGroup` modifiers, `scope: "force"`/`"roster"` only) to also read a rule's own
  modifiers and recognize `scope: "primary-catalogue"` (chapter/sub-faction exclusivity, resolved
  directly against the closure's own starting catalogue id — no name lookup needed, unlike the
  existing force-entry resolution).
- `BsRule` gains `Modifiers` deserialization (currently silently dropped).
- When multiple components of one `AttachedUnit` reference the identical underlying Core rule, the
  aggregate ability view collapses them into one entry belonging to no single component, rendered
  as its own row above the whole attached-unit block (matching the user's own mockup) instead of
  once per component.
- **BREAKING** (internal domain shape only, no public API consumers outside this repo):
  `AggregateAbilityEntry.ComponentName` becomes meaningfully absent for this one new case.

## Capabilities

### New Capabilities
None — this change extends existing capabilities.

### Modified Capabilities
- `catalogue-json-ingestion`: extends the existing `IsGameModeGated`-family gating mechanism to
  also read a `CoreRule` reference's target rule's own `hidden` condition, recognizing
  `scope: "primary-catalogue"` (chapter exclusivity) alongside the existing `scope: "force"`/
  `"roster"` recognition, and applying it (previously force/roster-only, entry/group-scoped) to a
  rule's own modifiers for the first time.
- `attached-unit-tracker`: `Aggregate Ability View`'s existing "no cross-component combination or
  deduplication" rule gains one narrow, identity-based exception for army-wide `CoreRule`
  abilities shared verbatim across multiple components of one `AttachedUnit`.
- `live-play-view`: `Statline Ability Column Rendering` gains a third placement (no single
  component owns the entry — renders as its own row, aligned to none of the block's statline
  rows), alongside the existing row-bound and component-wide placements, plus its own collapse
  rule (hidden only once every component in the attached unit is fully dead).

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/Json/BsCatalogueFile.cs` — `BsRule.Modifiers`
- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/BsdataDatasheetMapper.cs` —
  `IsGameModeGated`/`IsProvablyAlwaysTrueInMatchedPlay` generalization, applied from
  `ProcessRuleInfoLink`; needs the closure's own starting catalogue id threaded in
- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/ResolvedBsdataCatalogue.cs` /
  `BsdataClosure.cs` — expose the starting catalogue's own id to the mapper
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` /
  `AttachedUnitAggregateView.cs` — `AggregateAbilityEntry`'s component-binding becomes optional
  for this one case; `BuildAbilities` gains the identity-matching dedup step
- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` — new rendering branch for a
  component-less ability entry
- No changes to `ArmyRosterEnricher`, `ArmyListParser`, or anything upstream of catalogue
  resolution/aggregation
