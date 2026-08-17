## 1. File Access, Deserialization Shape, and Fixtures

- [ ] 1.1 Implement the "get JSON content for this filename" boundary (design.md) as a small
      interface/seam, with a local-disk implementation reading from a configuration-supplied root
      directory (e.g. `appsettings.json` / environment variable) — never a hardcoded path. Point
      local dev config at `C:\Users\Pete\wh40k-11e` (this machine's clone; not part of the repo).
- [ ] 1.2 Define minimal deserialization types for the BattleScribe `catalogueSchema` JSON shape
      using `System.Text.Json`: `catalogue` root (`catalogueLinks`, `sharedSelectionEntries`,
      `entryLinks`, `categoryEntries`, `id`, `name`), `selectionEntry`/`sharedSelectionEntry`
      (`type`, `name`, `id`, `profiles`, `categoryLinks`, `selectionEntries`,
      `selectionEntryGroups`, `entryLinks`, `costs`), `profile` (`name`, `typeName`,
      `characteristics`), `characteristic` (`name`, `$text`).
- [ ] 1.3 Deliberately omit deserialization of `constraints`, `modifiers`, `conditionGroups`, and
      `associations` — confirm they can be left unmapped without breaking deserialization of a
      real file (`Imperium - Black Templars.json` has all four).
- [ ] 1.4 Load and deserialize a real sample file end-to-end (`Imperium - Imperial Fists.json`,
      the smallest) as a smoke test before tackling the larger files.
- [ ] 1.5 Copy the specific real-data excerpts this change's automated tests depend on (e.g. the
      `Crusader Squad` entry, a handful of `Keywords` examples, a closure-relevant slice of
      `Imperium - Space Marines.json`) into `tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/`, so
      tests don't depend on the external local clone's presence. Sections 2–8's "verify against
      real data" items are exploratory checks to run against the live local clone during
      implementation; promote the ones worth keeping as regression coverage into fixture-backed
      tests here rather than leaving them as manual-only steps.

## 2. Faction Catalogue Closure Resolution

- [ ] 2.1 Implement closure resolution: given a starting file, follow every `catalogueLinks` entry
      with `importRootEntries: true`, recursively, collecting the full set of files.
- [ ] 2.2 Verify against real data: closure starting from `Imperium - Black Templars.json`
      includes `Imperium - Space Marines.json` plus that file's own further imports
      (`Imperium - Agents of the Imperium.json`, `Library - Titans.json`,
      `Imperium - Imperial Knights - Library.json`).
- [ ] 2.3 Verify against real data: closure starting from `Chaos - Chaos Space Marines.json`
      (single, unsplit faction line) includes its own imports
      (`Chaos - Chaos Knights Library.json`, `Chaos - Daemons Library.json`,
      `Library - Astartes Heresy Legends.json`).
- [ ] 2.4 Guard against import cycles (not observed in the sampled data, but not ruled out either).

## 3. Name Resolution

- [ ] 3.1 Implement name resolution over a closure: check the starting file's own
      `sharedSelectionEntries` first; only on a miss, check each imported file in declared
      `catalogueLinks` order.
- [ ] 3.2 Verify against real data: resolving `"Crusader Squad"` against the Black Templars
      closure returns the locally-defined entry, not anything from the Space Marines import.
- [ ] 3.3 Verify against real data: resolving `"Assault Intercessor Squad"` and `"Scout Squad"`
      against the Black Templars closure falls through to the Space Marines import (neither name
      appears locally at all).
- [ ] 3.4 Trace whether `entryLink`s always carry a `targetId`, or whether some name lookups need
      to go through `entryLinks` rather than `sharedSelectionEntries` directly — do not assume an
      `entryLink`'s shape alone tells you whether it resolves locally or via import (see
      design.md's "entryLink target location" risk).

## 4. Statline Extraction

- [ ] 4.1 Implement single-model detection and extraction: a `Unit`-typeName profile on the
      resolved entry itself becomes one named `Statline`.
- [ ] 4.2 Implement squad detection and extraction: `Unit`-typeName profiles nested on distinct
      child entries become one named `Statline` each, preserving declared order.
- [ ] 4.3 Map `M`, `T`, `Sv`, `W`, `LD`, `OC` characteristics onto `Statline`'s fields; map `InSv`
      directly from its own characteristic (no text extraction needed, unlike 10e).
- [ ] 4.4 Trace the four `Initiate` occurrences inside `Crusader Squad`
      (`Imperium - Black Templars.json`): confirm whether they are genuinely distinct model
      variants or duplicate/linked references to one shared definition, and handle whichever is
      true before assuming one shape.

## 5. Weapon Profile Extraction

- [ ] 5.1 Implement `Ranged Weapons`-typeName profile mapping to `RangedWeapon`
      (`Range`, `A`, `BS`, `S`, `AP`, `D`).
- [ ] 5.2 Implement `Melee Weapons`-typeName profile mapping to `MeleeWeapon`
      (`A`, `WS`, `S`, `AP`, `D`; `Range` characteristic literal `"Melee"` is not stored, matching
      `MeleeWeapon`'s existing fixed `Range = 0`).
- [ ] 5.3 Confirm AP values pass through unchanged as negative integers (source data already uses
      the same sign convention as `WeaponProfile.Ap`).

## 6. Weapon Keyword Text and Flag Parsing

- [ ] 6.1 Add a verbatim, always-populated keyword-text field to `WeaponProfile`
      (`datasheet-catalogue` delta spec's "Weapon Profile Verbatim Keyword Text" requirement) and
      include it in `EqualityKey()`.
- [ ] 6.2 Implement the `Keywords` free-text split (comma-separated tokens) and populate the new
      verbatim field with every token, in source order, unconditionally.
- [ ] 6.3 Implement per-token pattern match against `WeaponProfile`'s existing flags (`Torrent`,
      `Blast`, `Melta N`, `Rapid Fire N`, `Sustained Hits N`, `Lethal Hits`, `Devastating Wounds`,
      `Twin-linked`, `Indirect Fire`, `Pistol`, `Ignores Cover`, `Assault`, `Anti-<keyword> N+`),
      for exact matches only — no alias/synonym recognition (see design.md's deferred alias
      decision; do not map `"Cleave"` to `Blast` or `"Close Combat"` to `Pistol` in this change).
- [ ] 6.4 Handle `"-"` as "no keywords" (produces no flags set and an empty verbatim list, not a
      parse failure).
- [ ] 6.5 Verify against real data: `"Anti-infantry 4+, Devastating Wounds"`,
      `"Sustained Hits 1"`, `"Ignores Cover, Pistol, Torrent"`, `"Anti-Character 5+, Precision"`
      (all observed in `Imperium - Black Templars.json`) each produce the expected flag set AND
      the expected verbatim text, including `"Precision"` appearing verbatim with no flag set.

## 7. Ability Text Extraction

- [ ] 7.1 Implement `Abilities`-typeName profile mapping to `Ability`, carrying the `Description`
      characteristic through as `Text` verbatim.
- [ ] 7.2 Confirm no attempt is made to parse `Text` for behavioral effect, per existing
      `datasheet-catalogue` stance.

## 8. Cross-Cutting Verification

- [ ] 8.1 Confirm no `constraints`, `modifiers`, `conditionGroups`, or `associations` data leaks
      onto any produced `Datasheet`, `Statline`, `WeaponProfile`, or `Ability`.
- [ ] 8.2 Confirm no points/`costs` data is exposed on any produced object.
- [ ] 8.3 End-to-end test: resolve every named unit/model/weapon that appears in
      `data/gw-app-export.txt`, `data/gw-app-export-3-dp.txt`, and
      `data/gw-app-export-masters-of-the-maelstrom.txt` against the real BSData closure for each
      export's faction, confirming every name resolves to a `Datasheet` (this does not require
      the enrichment step — it's a resolution smoke test, not a graph-construction test).
