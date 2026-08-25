## Why

`/LivePlay` renders none of the army-level metadata `ArmyRoster` already carries — the page header
is a bare "Live Play" heading, even though `ArmyRoster.Name`/`Faction`/`Detachments`/
`ForceDisposition`/`BattleSize`/`PointsLimit`/`PointsSpent` have been populated by the parser since
`import-army-list-for-live-play`. The most consequential gap is `Detachments`: in 11th edition every
selected Detachment grants an army-wide rule (sometimes several) that materially changes how the
army plays, and none of that text is visible anywhere in the app — a player has to go back to the
rulebook or the official app mid-game to remember what their own Detachment does. BSData already
carries this text (either a local `rules[]` array on the Detachment's own catalogue entry, or a
`"type": "rule"` infoLink into the same shared-rule glossary `resolve-core-rule-abilities` already
built for chapter-wide Core rules), so resolving and displaying it is additive over existing
infrastructure, not a new subsystem.

Explored in detail before proposing: a corpus-wide scan of the live BSData clone found 280 distinct
Detachments; the large majority express their rule as free-form, often multi-paragraph, frequently
phase-conditional text (a Command-phase choice between named Doctrines, a mutable "Favoured
Champions" tracked unit, etc.) with no structural signal tying the effect to a specific unit or
characteristic. A minority (~8%, 22 Detachments) also carry a genuine structural modifier
(characteristic increment/decrement/multiply, or a Keywords append) directly on a specific unit's
own profile, gated by the same Detachment-selection condition. This change deliberately scopes to
the first case only — capture and display the rule text, for every Detachment, verbatim — and holds
back the structural/per-unit-attachment case for a later, `resolve-core-rule-abilities`-shaped
follow-on (see `.claude/vnext-ideas.md`).

## What Changes

- `ArmyListParser` itself is **unchanged** — it keeps capturing a `(N Detachment Points)`-suffixed
  line's full text as one entry, exactly as `ArmyListParserTests
  .RealExport_CommaBearingDetachmentName_IsCapturedAsOneEntry` already asserts (an earlier draft of
  this proposal wrongly assumed the split had to happen here; splitting a multi-Detachment line
  turns out to need BSData knowledge to do safely - see the next bullet - which `army-list-parsing`
  deliberately never has, per its own Purpose statement).
- `ArmyRosterEnricher`'s new Detachment resolution step resolves a Detachments entry that may name
  more than one Detachment (e.g. `"Fulguris Task Force, Marshal's Household, and Subversion
  Assets"`, confirmed real in the already-bundled `data/gw-app-export-3-dp.txt`, and again in a
  second real sample, `data/gw-app-export-templars-multiple-detachments.txt`, using the two-item
  "A and B" form) via a greedy, longest-known-name-first "chomp" over the closure's own full
  Detachment name list — not a syntactic split on `"and"`/commas. This isn't incidental: a real
  Space Wolves Detachment is itself named **"Legends of Saga and Song"**, and, separately, a real
  Aeldari Detachment **"Warhost"** is a literal substring of another real one, **"Armoured
  Warhost"** - a syntactic split gets both wrong; matching against the known name set, longest
  first, gets both right for free. See `design.md` for the full algorithm.
- New `BsSelectionEntry.Rules` DTO field (mirrors the existing `BsRule` shape already used for
  `BsCatalogue.Rules`/`SharedRules`) so a Detachment's own locally-declared rule text deserializes -
  previously silently dropped like every other unmapped BSData field.
- A new resolve-by-name path for a Detachment entry: Detachment choices are nested children of a
  "Detachment" `selectionEntryGroup` (50+ siblings on some factions), not top-level
  `sharedSelectionEntries`, so the existing `BsdataNameResolver.Resolve` (which only searches each
  file's own top-level entries) doesn't reach them directly.
- Detachment rule text resolution covering both real shapes confirmed in the corpus: a local
  `rules[]` entry, or an existing `"type": "rule"` infoLink resolved via `RuleGlossary` - reusing
  `resolve-core-rule-abilities`'s machinery, rooted at the Detachment entry instead of a datasheet
  entry.
- `ArmyRosterEnricher.Enrich` resolves each entry in `ParsedArmyList.Detachments` (per the
  whole-first-then-split algorithm above) against the faction's `ResolvedBsdataCatalogue` and
  attaches each resolved Detachment's own name + resolved rule text (not DP cost - see below) to
  `ArmyRoster`.
- `ArmyRoster.Detachments` shape changes from `IReadOnlyList<string>` (bare names) to
  `IReadOnlyList<ResolvedDetachment>`, each carrying its own Name and zero-or-more resolved
  `(Name, Text)` rule pairs (**BREAKING** - every existing reader of the bare-string shape needs
  updating, including `BattleScribeRosterMapper`, the JSON-roster pipeline's own independent
  `ArmyRoster` producer - conveniently, a BattleScribe roster JSON already carries a selected
  Detachment's own rule text inline on its own `selections[].rules[]`, e.g. Death Guard's
  "Virulent Vectorium" → "Worldblight", so that pipeline's own resolution needs no new BSData-style
  machinery, just reading a field it already has access to).
- `/LivePlay` gains a header, above the unit blocks, in two parts: (1) the roster's Name, Faction,
  BattleSize, PointsSpent/PointsLimit, and ForceDisposition; (2) a rules section, under its own
  section heading, laid out as two columns - one for every distinctly-named `ArmyRule`-origin
  ability present anywhere in the roster (deduplicated army-wide, a NEW aggregation distinct from
  the existing per-`AttachedUnit` dedup - see design.md), one for each selected Detachment. Each
  column uses the same lightweight `<thead><th>` column-label convention the weapon-table sections
  already use, rather than a new visual pattern. A Detachment renders its own name once, as a plain
  (non-interactive) label, with one popover-trigger chip beneath it per resolved rule (usually one;
  confirmed real cases exist with up to 5). Every rule - Army or Detachment - is a popover trigger
  showing its full text on tap, exactly like every other ability/Core-rule already on the page,
  rather than rendering text inline. **No DP cost and no unit points cost render anywhere on the
  page** - consistent with the fact that `/LivePlay` doesn't show any unit's own points cost today
  either; only the roster's own total PointsSpent/PointsLimit does.
- The existing per-unit rendering of an `ArmyRule`-origin ability (its own dedup'd box on each
  `AttachedUnit`, from `gate-and-dedupe-core-rule-abilities`) is unchanged and kept alongside the
  new header - a player looking at one specific unit still sees its own applicable rules without
  scrolling to the header.
- A rule's own composition-restriction sentences (e.g. a trailing "Restrictions: ...") render
  verbatim as part of its popover text - informational only, no enforcement, consistent with the
  project's existing no-composition-validation stance.
- Explicitly **not** in scope, deferred to `.claude/vnext-ideas.md`: any per-unit structural
  attachment of a Detachment rule (the ~8%-of-corpus "orphaned unit ability, Templar-Vows-style"
  pattern found during exploration - e.g. Black Templars' "Faith-Fuelled Resolve" as a Sword
  Brethren Squad ability, mutating its OC stat), any mechanical mutation of a unit's Statline/weapon
  characteristics, and any general ability-text classification.

## Capabilities

### New Capabilities

None - this change extends four existing capabilities rather than introducing a new subsystem.

### Modified Capabilities

- `catalogue-json-ingestion`: new Detachment Rule Text Extraction requirement (local `rules[]` +
  `"type": "rule"` infoLink, mirroring the existing Core Rule Ability Extraction requirement) -
  given an already-resolved Detachment entry. The nested-by-name resolution mechanism itself (how
  that entry is located in the first place) is specified as part of `army-roster-enrichment`'s own
  new requirement below, matching how that capability's existing Faction Catalogue File Resolution
  requirement already owns its own catalogue-structural resolution detail.
- `army-roster-enrichment`: new Detachment Name Resolution requirement - a greedy longest-name-
  first match against the closure's own known Detachment names, per the "Legends of Saga and Song"/
  "Warhost" findings above - following the same "resolve by name, fail loud with a did-you-mean
  suggestion on a miss" contract as every other resolution in this capability.
- `roster-model`: the Army Roster Container requirement's `Detachments` field shape changes from
  bare names to resolved rule entries.
- `live-play-view`: new Army Header Rendering requirement covering the new header section.

`army-list-parsing` is explicitly **not** a modified capability - see "What Changes" above for why
the split moved out of the parser and into `army-roster-enrichment`.

## Impact

- `ProbHammer.Core.Domain.Catalogue.Bsdata`: `Json/BsCatalogueFile.cs` (new `BsSelectionEntry.Rules`
  field), a new Detachment-rule text extractor, `ResolvedBsdataCatalogue` (new nested-name
  resolution path for Detachment entries, plus the whole-first/split-fallback resolution logic).
- `ProbHammer.Core.Domain.Roster`: `ArmyRoster.cs` (new `ResolvedDetachment`/rule-pair types,
  `Detachments` shape change - **BREAKING**), `ArmyRosterEnricher.cs` (new resolution step).
- `ProbHammer.Core.Domain.Import.BattleScribe`: `BattleScribeRosterMapper.cs` (produces the new
  `ResolvedDetachment` shape directly from the roster JSON's own inline `rules[]`, no BSData
  involvement).
- `ProbHammer.Web`: `LivePlayModel.cs`/`LivePlay.cshtml`/a new shared partial for the header (new
  header rendering, exposing `ArmyRoster`'s metadata; a new whole-roster `ArmyRule` aggregation
  step distinct from `AttachedUnitAggregator`'s existing per-unit one; reuses or extracts the
  existing `_UnitBlock.cshtml` popover-building mechanism rather than duplicating it - see
  design.md).
- Tests: new fixtures/tests across every touched layer, including a hand-built fixture covering two
  simultaneous `ArmyRule`-origin abilities (e.g. a Drukhari-shaped fixture with "Power from Pain"
  and "Corsairs and Travelling Players"), since no real captured export in the repo is Drukhari or
  Aeldari; verification against real captured exports covering both single- and multi-Detachment
  armies - `data/gw-app-export-3-dp.txt` (3 Detachments, Oxford-comma form) and
  `data/gw-app-export-templars-multiple-detachments.txt` (2 Detachments, plain "and" form) - plus a
  Detachment using the local `rules[]` shape (e.g. Black Templars' "Marshal's Household") and one
  using the `"type": "rule"` infoLink shape (e.g. Necrons' "Awakened Dynasty") for the two
  rule-text resolution shapes - per this project's established "verify BSData-mapper changes
  against a real export" practice. `ArmyListParserTests
  .RealExport_CommaBearingDetachmentName_IsCapturedAsOneEntry` is unaffected by this change and
  needs no update.
