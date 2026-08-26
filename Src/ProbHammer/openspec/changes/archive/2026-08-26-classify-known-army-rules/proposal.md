## Why

`/LivePlay`'s army header only ever shows an "Army" rule for a faction whose army-wide rule
happens to be gated by BSData's `primary-catalogue` scope — a mechanism that exists specifically
for a handful of shared-library sub-faction families (Space Marine chapter books' Oath of
Moment/Templar Vows, the Aeldari Library's Battle Focus/Power from Pain), not as a general "this
rule applies to the whole army" signal. A real Death Guard import (via the BattleScribe/NewRecruit
JSON pipeline) surfaced the gap this leaves: Nurgle's Gift is exactly as army-wide as Templar Vows
in the real game, but resolves with `CoreRule` Origin, not `ArmyRule`, so it never reaches the
header. The same gap applies to Custodes (Martial Ka'tah), Grey Knights (Gate of Infinity), and
every faction imported through the JSON pipeline, which never produces `ArmyRule` Origin at all
today.

Investigating that gap surfaced a second, more serious problem with the `primary-catalogue`-scope
signal itself: it is **not reliable even where it does fire**. Checked directly against the bundled
BSData clone, the identical gating shape is used for `Assigned Agents` (Agents of the Imperium),
`Disparate Paths` (Aeldari), and `Corsairs and Travelling Players` (Drukhari) — all three are
mustering/composition rules ("you can include these units even though they don't have your
Faction's keyword"), not gameplay rules, yet each is referenced from real datasheets and each
carries `primary-catalogue`-scoped gating on its own rule definition. Today's classification would
misclassify all three as `ArmyRule`, showing e.g. "Assigned Agents" in the header alongside a
roster's genuine army rule whenever the roster includes an allied Agents of the Imperium unit —
already a live bug, independent of this change. No reliable structural or frequency-based signal
distinguishes a genuine army-wide gameplay rule from this shape of content (see the conversation
history that produced this change for the alternatives investigated and rejected). A small,
curated, per-faction lookup — verified against the bundled corpus and guarded by a permanent scan
against staleness — is the only mechanism found to be both correct and general enough.

## What Changes

- New curated table mapping a faction to zero or more known army-wide rule names, each verified to
  exist as a real rule in the bundled BSData corpus and referenced by that faction's own datasheets
  (e.g. Death Guard → "Nurgle's Gift", Custodes → "Martial Ka'tah", Grey Knights → "Gate of
  Infinity", Black Templars → "Templar Vows", every other Space Marine chapter → "Oath of Moment").
- **The Core-vs-Army Rule Origin classification becomes driven entirely by this table.** The
  existing `primary-catalogue`-scope structural check is removed from Origin classification — it no
  longer decides `ArmyRule` on its own, because that same shape reliably produces false positives
  (Assigned Agents, Disparate Paths, Corsairs and Travelling Players). It keeps its existing,
  separate role in `Core Rule Chapter and Game-Mode Gating` (deciding whether a hidden reference
  produces an Ability at all) completely unchanged — only its use as an Origin signal is removed.
- The BattleScribe/JSON pipeline's Core Rule Extraction gains the identical name-match
  classification — a new capability for that pipeline, which produces only `CoreRule` Origin today
  and never had any structural signal of its own to fall back on.
- Because the structural check no longer classifies Origin on the BSData side either, the curated
  table must include every Space Marine chapter's own rule up front — not deferred — so no existing
  BSData-pipeline roster regresses. Verified directly against the bundled corpus for the whole
  family (see the conversation history): Black Templars → Templar Vows, Grey Knights → Gate of
  Infinity, every other named chapter (Blood Angels, Dark Angels, Deathwatch, Imperial Fists, Iron
  Hands, Raven Guard, Salamanders, Space Wolves, Ultramarines, White Scars) plus generic/unsplit
  Space Marines → Oath of Moment.
- Classification happens once, at ingestion time, where `Ability.Origin` is first decided. Both
  existing consumers that already key strictly off `Origin == ArmyRule` — the army header's rule
  list and the per-`AttachedUnit` shared-ability dedup (`attached-unit-tracker`'s "promoted,
  unattached row" behavior) — pick this up automatically. **No changes to either consumer.**
- Allied/off-faction content is unaffected by construction: the lookup is keyed to the roster's own
  primary Faction, so an ally's own rule (e.g. an Imperial Knight inside a Custodes list) won't
  match that Faction's table entries and stays `CoreRule`.
- The `primary-catalogue`-scope structural check is **repurposed as a scan-time discovery aid**
  rather than discarded: a new permanent, manually-triggered corpus scan walks every rule
  definition in the bundled clone carrying that gating shape and requires each one to be
  explicitly triaged — present in the inclusion table, or in a new, small, curated exclusion list
  documenting confirmed non-army-rule names (seeded with Assigned Agents, Disparate Paths, Corsairs
  and Travelling Players) — failing the scan on anything neither list accounts for. This preserves
  the auto-generalization benefit the structural check originally offered (a future codex's own
  shared-library rule gets flagged for a human to triage) without ever trusting it at runtime. It
  does not, and cannot, discover a rule that lives in its own dedicated, ungated file (e.g. Gate of
  Infinity, Nurgle's Gift) — those are only ever found by the same manual/frequency verification
  already used to populate the table's non-Space-Marines rows, and stay covered only by the
  existing "does each table entry still resolve" scan.

## Capabilities

### New Capabilities
- `army-rule-name-lookup`: the curated per-faction table of known army-wide rule names, plus
  Faction-scoped resolution (using the same "most specific Faction entry" convention already used
  for catalogue file resolution) into the name set a given roster should check against.

### Modified Capabilities
- `catalogue-json-ingestion`: "Core Versus Army Rule Origin Classification" gains the name-match
  path to Army Rule Origin, alongside its existing structural check.
- `army-roster-enrichment`: resolves the roster's known army-rule name set once, from its parsed
  Faction, and threads it into every Datasheet resolution performed for that roster.
- `battlescribe-roster-import`: "Core Rule Extraction" gains the same name-match reclassification,
  using the Faction the mapper already derives from the roster's own `catalogueName`.
- `bsdata-corpus-scan`: gains a new full-corpus scan verifying every lookup-table entry against the
  live BSData clone, following the existing permanent/manually-triggered/allowlisted scan pattern.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/` — new lookup type; `BsdataDatasheetMapper`'s
  ability-classification step gains the caller-supplied known-name-set parameter.
- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/ResolvedBsdataCatalogue.cs` — `ResolveDatasheet`
  threads the known-name set through to `BuildDatasheet`.
- `src/ProbHammer.Core/Domain/Roster/ArmyRosterEnricher.cs` — resolves the known-name set once per
  roster from `parsedArmyList.Faction`.
- `src/ProbHammer.Core/Domain/Import/BattleScribe/BattleScribeRosterMapper.cs` — same
  reclassification applied to its own Core Rule extraction.
- `tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/` — new scan test + allowlist file.
- No changes to `AttachedUnitAggregator`, `LivePlayModel`, or any `/LivePlay` rendering code.
