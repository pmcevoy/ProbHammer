## Why

`Datasheet.Keywords`/`FactionKeywords` are hardcoded empty in both import pipelines
(`BsdataDatasheetMapper`, `BattleScribeRosterMapper`), and `ModelLine.Keywords` (added by
`add-modelline-keywords` specifically for the case of a keyword scoped to one named individual
within a shared statline, e.g. Masters of the Maelstrom's Garlon Souleater carrying Psyker) is
never populated by either pipeline either. `display-unit-keywords-section` just shipped a
dedicated "Keywords" section on `/LivePlay`, but with no real data behind it, that section can
never render for a real import. Both pipelines' own source data already carries this information —
BSData's `categoryLinks` and BattleScribe/NewRecruit's `categories` — it has simply never been
read.

## What Changes

- `Datasheet` gains a way to look up the Keywords scoped to one of its own named statlines
  (distinct from the Datasheet's own top-level Keywords), defaulting to empty for a name with no
  such data — the missing link that lets `ArmyRosterEnricher` populate `ModelLine.Keywords` when
  building a `ModelLine` from a resolved `Statline` name.
- `BsdataDatasheetMapper` extracts a resolved entry's own `categoryLinks` into `Datasheet.Keywords`
  (unit-wide), and each of its child model entries' own `categoryLinks` into that model's
  per-name Keywords (feeding the new lookup above) — confirmed against the real BSData corpus:
  Masters of the Maelstrom's own `categoryLinks` carry no `Psyker`, but its nested "Garlon
  Souleater" model entry's own `categoryLinks` do.
- `BattleScribeRosterMapper` extracts a unit selection's own `categories` into `Datasheet.Keywords`
  the same way, and a nested `model`-typed selection's own `categories` directly into that
  `ModelLine`'s `Keywords` — confirmed against a real NewRecruit roster JSON export of the same
  Masters of the Maelstrom list: Garlon Souleater's own nested selection carries
  `categories: [{"name": "Psyker"}]`.
- Every `categoryLinks`/`categories` entry becomes one Keyword, with a literal `"Faction: "`
  prefix stripped when present (e.g. `"Faction: Black Templars"` → `"Black Templars"`) and no
  other transformation, exclusion, or Faction-vs-plain-Keyword split — `FactionKeywords` stays
  untouched/unused, matching how it's already read nowhere in the codebase today. A datasheet's
  own name, which BSData/BattleScribe both also surface as one of its own category entries, is
  kept as a real Keyword rather than excluded — real ability text targets units by their own
  datasheet name as a keyword (e.g. "Friendly SWORD BRETHREN SQUAD units have +1 OC").
- No change to `KeywordResolution.EffectiveKeywords`, `AttachedUnitAggregateView.Keywords`, or
  `/LivePlay`'s rendering — all already correctly present-scoped (a fully-removed component or a
  casualty-reduced model-line's Keywords already drop out of the union with no code change
  needed), so populating real data is sufficient on its own to make that behavior visible.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `datasheet-catalogue`: "Multiple Named Statlines" is unchanged; a new requirement adds a
  per-named-statline Keyword lookup alongside it.
- `catalogue-json-ingestion`: new requirements for extracting `categoryLinks` into
  `Datasheet.Keywords` (unit-wide) and per-model Keywords (via the new `datasheet-catalogue`
  lookup).
- `battlescribe-roster-import`: new requirements for extracting `categories` into
  `Datasheet.Keywords` and directly into each `ModelLine.Keywords`.
- `army-roster-enrichment`: "Attached Unit and Unit Graph Assembly" changes so each `ModelLine`
  also carries Keywords, resolved from the Datasheet's per-name Keyword lookup.

`roster-model`'s existing "Attached Unit Keyword Resolution" requirement is unchanged — it already
specs the present-scoped union/model-level-isolation behavior this change relies on (its own
scenarios already use a PSYKER example matching this exact real case), so no delta is needed there.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/Datasheet.cs` — new per-name Keyword lookup, built once at
  construction alongside the existing Statlines dictionary.
- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/BsdataDatasheetMapper.cs` — extract `categoryLinks`
  at both the resolved entry's own level and each child model entry's level.
- `src/ProbHammer.Core/Domain/Import/BattleScribe/BattleScribeRosterMapper.cs` — extract
  `categories` at both the top-level unit selection and each nested `model` selection.
- `src/ProbHammer.Core/Domain/Roster/ArmyRosterEnricher.cs` — populate `ModelLine.Keywords` from
  the Datasheet's new per-name lookup when building each `ModelLine`.
- No change to `Domain/Roster/KeywordResolution.cs`, `AttachedUnitAggregator.cs`, or any
  `ProbHammer.Web` rendering code — all already consume `Keywords` correctly once populated.
