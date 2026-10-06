# Design

## Context

`ArmyRosterProvider.BuildFromBattleScribe` builds its glossary with
`BattleScribeRuleGlossaryBuilder.Build(roster)`, which uses only the roster JSON's own `rules`
arrays. The GW-text path uses `ResolvedBsdataCatalogue.Glossary`, built by
`RuleGlossary.Build(closure)`. That method already indexes `closure.GameSystem.SharedRules`, and
`BsdataClosureResolver` already finds the game-system file by id (`ResolveGameSystem`, private).
`nr-orks.json`'s `gameSystemId` (`sys-352e-adc2-7639-d610`) matches the bundled
`Warhammer 40,000.json`, and every weapon keyword in that roster is defined there.

Colon qualifiers: `WeaponKeyword.SplitQualifier` (private) already splits `LETHAL HITS:
non-MONSTER/VEHICLE` at the colon for chip identity. `RuleGlossary.Normalize` has no equivalent
step; only its Anti regex (`^anti[\s\-:].*`) handles a colon. No rule name or alias in the
bundled BSData contains a colon (checked across every file), so dropping a qualifier during
normalization can't merge two real rules onto one key.

## Goals / Non-Goals

**Goals:**
- Reuse existing code for every piece: game-system lookup, glossary indexing, the qualifier split.

**Non-Goals:**
- Faction catalogue lookup for NR imports (proposal.md - What Changes).
- Showing the qualifier's meaning in the popover. The chip already displays it; the popover shows
  the base rule.

## Decisions

**Merge two glossaries' indexes rather than chaining lookups.** Add
`RuleGlossary.WithFallback(RuleGlossary fallback)`, returning a new glossary whose index is this
one's entries plus every fallback key not already present (`TryAdd`, the same first-occurrence-wins
rule `Build`/`BuildFrom` use). `TryResolve` is unchanged. A chained lookup (a fallback reference
inside `RuleGlossary`) was considered, but a merged index keeps one lookup path. Merging into the
same `Dictionary` also avoids exposing the definitions list.

**Build the core-rules glossary with the existing `RuleGlossary.Build`.** Pass it a
`BsdataClosure` with no files and the game-system catalogue. `Build` already skips empty `Files`
and reads `GameSystem.SharedRules`, so no new indexing code is needed. A dedicated
`BuildFromGameSystem` was rejected as a second copy of the same loop.

**Find the game-system file with the existing id scan.** Make
`BsdataClosureResolver.ResolveGameSystem` callable with a source and an id; it builds the file-name
set from `source.ListFileNames()`. The closure resolver keeps calling it as today. Matching by the
roster's `gameSystemId` (added to `BsRoster`) is how a catalogue already finds its game system;
hard-coding `"Warhammer 40,000.json"` was rejected because the id is the declared link.

**Cache the core-rules glossary in `BsdataCatalogueCache`.** Add
`GetGameSystemGlossary(string gameSystemId)`, memoized per id like `GetOrBuild`, returning `null`
when no file matches. The id scan reads every bundled file once per process, so it can't run per
request (`/LivePlay` rebuilds the roster on every request).

**Compose in `ArmyRosterProvider.BuildFromBattleScribe`.** Roster glossary
`.WithFallback(coreGlossary)` when the cache returns one, otherwise the roster glossary alone.
`BattleScribeRuleGlossaryBuilder` stays BSData-free, so the BattleScribe mapper keeps no catalogue
dependency.

**Share `WeaponKeyword`'s qualifier split with `RuleGlossary.Normalize`.** Make `SplitQualifier`
`internal static`; `Normalize` keeps the head as its new step 2, ahead of the regex steps. The Anti
regex drops its colon branch (`^anti[\s\-].*`), since step 2 already turns
`anti: non-monster/vehicle 5+` into `anti`. A new colon regex in `RuleGlossary` was rejected as a
second implementation of the same split.

## Risks / Trade-offs

- [The GW-text pipeline changes too: qualified chips there start resolving] → Intended; the
  rules-glossary delta covers both pipelines, and existing glossary tests run against both.
- [A future rule name containing a colon would index under its head only] → None exist in the
  bundled data today. A collision falls back to first-occurrence-wins, the same as any duplicate
  name.
- [First NR import per process pays the game-system id scan] → One-off and cached; it's the same
  scan a GW-text import already pays when building its closure.
- [Roster and BSData data versions differ (`nr-orks.json` is game-system revision 16, the bundled
  file 14)] → The roster's own text wins for any rule it defines; core rule text drifts rarely.
