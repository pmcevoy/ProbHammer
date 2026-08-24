## Why

`/LivePlay` is missing an entire category of ability text that GW's own app calls "Core"/"Faction
Abilities" and NewRecruit lists as "Rules" — `Oath of Moment`, `Templar Vows`, `Deadly Demise D3`,
`Firing Deck 6`, `Infiltrators`, `Scouts 6"`, and their equivalents on virtually every datasheet in
the corpus. BSData represents these as `infoLinks` with `"type": "rule"`, pointing by id into the
`rules`/`sharedRules` text pool `rules-glossary-popovers` already resolves — a shape
`BsdataDatasheetMapper.WalkInfoLink` explicitly and deliberately skips today (it only follows
`"type": "profile"` infoLinks). Confirmed via a live pipeline run against a real Black Templars
export: every unit in the roster is missing at least its faction's core rule, and units carrying
BSData's other "Core ability" shapes are missing those too. This is the second time a whole
ability-sourcing shape has surfaced only via a user bug report (after `resolve-enhancement-
abilities`), so this change also adds a corpus-wide scan whose job is to confirm no further shape
is still lurking, rather than fixing this one and waiting for a fifth report.

## What Changes

- Extend `BsdataDatasheetMapper`'s walk to follow `infoLink type:"rule"`, resolving:
  - a display name — the linked rule's base name, with any `append`/`field:"name"` modifier value
    on that same infoLink applied in order (e.g. `"Deadly Demise"` + `"D3"` → `"Deadly Demise D3"`)
  - `Ability.Text` via `RuleGlossary.TryResolve` against the closure's glossary (this is new: every
    other `Ability` extraction path reads a `BsProfile`'s `Description` characteristic directly;
    this one reads the same `rules`/`sharedRules` pool the glossary already indexes for popover
    cross-references)
- Add a new `AbilityOrigin` value distinguishing this source (a datasheet-wide rule reference) from
  `Intrinsic`, `Enhancement`, and `OptionalGrant`. Threaded through `Datasheet.Abilities` exactly
  like `Intrinsic` — always exposed, never on-demand-only — since a Core/faction rule is, like an
  Intrinsic ability, simply true about the datasheet rather than one of several optional choices.
- An unresolvable rule reference (the linked id isn't present anywhere in the closure) is skipped
  silently, consistent with this loader's existing convention for a link that was always meant to
  dangle (`BsdataClosure.GameSystem`'s own precedent).
- `/LivePlay` renders the new Origin through the existing Model/Unit Abilities columns — no new UI
  section — with no visual indicator distinguishing it from an Intrinsic ability (unlike
  `Enhancement`'s leading "✦ ", which stays Enhancement-only).
- Add a new full-corpus scan (`bsdata-corpus-scan`'s existing manually-triggered,
  `Explicit = true` pattern) that walks every datasheet-reachable entry/group across the whole
  local BSData clone and asserts every ability-granting shape it can find (nested `Abilities`
  profile, upgrade-nested profile, `infoLink type:"profile"`, `infoLink type:"rule"`) is one of
  these four known, handled shapes — closing the loop on "discovered one at a time via bug
  reports" for ability extraction specifically.

## Capabilities

### New Capabilities
None — this change extends existing capabilities; it introduces no new domain concept beyond one
additional, already-precedented `AbilityOrigin` value.

### Modified Capabilities
- `datasheet-catalogue`: `Ability Shape`'s Origin classification gains a fourth value (a
  datasheet-wide Core/faction rule reference), alongside Intrinsic, Enhancement, and Optional
  Grant.
- `catalogue-json-ingestion`: a new extraction requirement for `infoLink type:"rule"` — display
  name construction from the linked rule name plus any `append` name modifier, and `Ability.Text`
  resolution via `RuleGlossary` instead of a `BsProfile`.
- `bsdata-corpus-scan`: a new scan requirement verifying the complete, closed set of BSData
  ability-granting shapes across the full corpus.
- `live-play-view`: clarifies that the new Origin renders through the existing Abilities columns
  with no visual indicator, and updates the Enhancement Visual Indicator requirement's scenario to
  reflect the now-complete set of non-Enhancement origins.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/BsdataDatasheetMapper.cs` — `WalkInfoLink`,
  `BuildDatasheet` (likely needs the closure's `RuleGlossary` threaded in, mirroring how
  `forceEntries` was added for `IsGameModeGated`)
- `src/ProbHammer.Core/Domain/Catalogue/Ability.cs` (or wherever `AbilityOrigin` is declared) —
  new enum value
- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/ResolvedBsdataCatalogue.cs` — `ResolveDatasheet`'s
  call into `BuildDatasheet` gains the glossary argument
- `src/ProbHammer.Web/Pages/LivePlay/_UnitBlock.cshtml` — no rendering change expected (the new
  Origin flows through the existing Abilities column path), but the Enhancement indicator's
  conditional is worth a read-through to confirm it still only fires for `Enhancement`
- `tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/` — new scan test + allowlist file
- No changes to `ArmyRosterEnricher`, `AttachedUnitAggregator`, or the parser — this is entirely
  within catalogue resolution
