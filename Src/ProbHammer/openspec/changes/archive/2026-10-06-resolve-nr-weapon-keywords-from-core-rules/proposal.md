# Proposal

## Why

A New Recruit roster JSON carries rule text only for the rules its own selections link to, so
the roster-scoped glossary a BattleScribe import builds usually has no entry for a standard
weapon keyword. On `data/nr-orks.json` (12 rule entries, none a weapon keyword) every weapon
keyword chip renders dimmed and unclickable. GW-app Android exports often fail to parse, so the
NR JSON round-trip is now the main import path for some players, and this gap is the most
visible thing it lacks next to the GW-app text pipeline.

## What Changes

- A BattleScribe import's rule glossary falls back to the BSData game-system file's shared rules
  (`Warhammer 40,000.json`, the file whose id matches the roster's own `gameSystemId`) for any
  name the roster's own rules don't define. Weapon keywords such as Lethal Hits, Cleave,
  Close-quarters, Assault and Rapid Fire, and core rules such as Deadly Demise and Firing Deck,
  become resolvable popovers.
- The roster's own rule text wins whenever both define the same name.
- Glossary lookup drops a colon qualifier, so `LETHAL HITS: non-MONSTER/VEHICLE` and
  `SUSTAINED HITS 2: MONSTER/VEHICLE` resolve to their base rule. Today they stay dimmed in both
  import pipelines, and most of `data/nr-orks.json`'s weapons carry the first. The qualifier
  split already in `WeaponKeyword` is shared with the glossary rather than reimplemented, and it
  replaces the colon branch of the Anti special case.
- If no bundled game-system file matches the roster's `gameSystemId`, the import still succeeds
  with the roster-only glossary, as today.
- No faction catalogue lookup: NR already exports the faction rules a roster uses.
- `data/nr-orks.json` is committed as a real capture for the regression test.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `battlescribe-roster-import`: adds a requirement for how the roster's rule glossary resolves a
  name the roster itself doesn't define (fallback to the game system's shared rules, roster text
  first).
- `rules-glossary`: Glossary Lookup By Normalized Name Or Alias gains a qualifier-dropping step;
  the Anti exception no longer needs its colon boundary.

## Impact

- `ProbHammer.Core`: `RuleGlossary` (compose a primary glossary with a fallback; qualifier step
  in normalization), `WeaponKeyword` (its qualifier split becomes shared),
  `BsRoster` (read `gameSystemId`), game-system lookup by id (today private inside
  `BsdataClosureResolver`), `BsdataCatalogueCache` (cache the game-system glossary).
- `ProbHammer.Web`: `ArmyRosterProvider.BuildFromBattleScribe` composes the two glossaries.
- No rendering change: `/LivePlay` already resolves chips and `[BRACKET]` references through
  whatever `RuleGlossary` the build result carries.
- Docs: `.claude/domain-model/battlescribe-import-pipeline.md`'s "only as complete as that
  roster's own exported rules" note.
