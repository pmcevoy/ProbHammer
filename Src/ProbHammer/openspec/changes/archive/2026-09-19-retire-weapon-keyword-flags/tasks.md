## 1. Domain model

- [x] 1.1 Remove every typed ability flag and the `Anti` dictionary from `WeaponProfile`
  (`Torrent`, `Blast`, `Melta`, `RapidFire`, `SustainedHits`, `LethalHits`, `DevastatingWounds`,
  `TwinLinked`, `IndirectFire`, `Pistol`, `IgnoresCover`, `Assault`, `Anti`), keeping `KeywordsText`
  as the only ability-keyword representation.
- [x] 1.2 Update `WeaponProfileEqualityKey` to drop the same fields and `NormaliseAnti`, replacing
  them with one normalized representation of `KeywordsText` (case-folded, trimmed, deduplicated,
  order-independent — reuse `RuleGlossary.Normalize`'s own normalization where practical, per
  design.md's Risks, so keyword-equality and glossary-resolution can't drift apart on what counts
  as "the same token").
- [x] 1.3 Update `WeaponProfile.EqualityKey()` to build the new `WeaponProfileEqualityKey` shape.

## 2. Keyword tokenization

- [x] 2.1 Reduce `WeaponKeywordParser` to tokenization only: split `Keywords` on `,`, trim each
  token, drop empty entries, treat `"-"`/blank as an empty list — remove `TryRecognize`, the
  `Anti`/`Melta`/`RapidFire`/`SustainedHits` regex patterns, and the flag-mapping switch.
- [x] 2.2 Remove `WeaponKeywordParser.UnrecognizedTokens` (superseded by the corpus-scan's own
  glossary-resolution check — see Section 5).
- [x] 2.3 Confirm both call sites (`BsdataDatasheetMapper`, `BattleScribeRosterMapper`) still
  compile against the slimmed parser with no behavioral change to their own tokenization inputs.

## 3. Rendering

- [x] 3.1 In `_UnitBlock.cshtml`, replace `WeaponAbilityTags(weapon)` with a read of
  `weapon.KeywordsText` directly at both weapon-tag render sites (Ranged and Melee tables) — the
  existing `glossary.TryResolve(tag)` / resolved-vs-unresolved chip rendering loop is unchanged,
  only its input source changes.
- [x] 3.2 Remove the now-unused `WeaponAbilityTags` method.
- [x] 3.3 Re-verify against the earlier real-world repro (`data/nr-export-orks-close-quarters-cleave.json`,
  loaded via `/Import` → `/LivePlay`): the Slugga's `CLOSE-QUARTERS` and `LETHAL HITS:
  non-MONSTER/VEHICLE` tokens and the Beastchoppa - Standard's `CLEAVE 1` token all now render as
  chips (resolved or unresolved per their own glossary match), none silently missing.

## 4. Consumer updates

- [x] 4.1 Search `src/` and `tests/` for any remaining reference to the removed flags/`Anti`
  dictionary/`WeaponKeywordParser.TryRecognize`/`UnrecognizedTokens` and update or remove each one.
  Known from exploration: `AttachedUnitAggregatorTests.cs` (one assertion reads
  `.Profile.LethalHits` to distinguish two grouped copies — switch to inspecting `KeywordsText`),
  `BsdataDatasheetMapperTests.cs` (one assertion reads `.DevastatingWounds` — switch to
  `KeywordsText`), `WeaponKeywordParserTests.cs` (rewrite entirely around tokenization only, per
  the new `catalogue-json-ingestion` "Weapon Keyword Tokenization" requirement).

## 5. Corpus-scan repurposing

- [x] 5.1 Update `WeaponKeywordScanTests.Full_corpus_weapon_keyword_token_scan` to build a
  `RuleGlossary` per catalogue file's own `BsdataClosure` (mirroring how the test already builds a
  per-file `WalkContext`) and record a token when `glossary.TryResolve(token)` returns no match,
  instead of calling the now-removed `WeaponKeywordParser.UnrecognizedTokens`.
- [x] 5.2 Delete every existing entry in `WeaponKeywordAllowlist.cs` — none of them describe a
  glossary-resolution gap, all describe the retired flag vocabulary.
- [x] 5.3 Run the scan against the real local BSData clone and seed `WeaponKeywordAllowlist.cs`
  fresh from whatever tokens genuinely don't resolve against `RuleGlossary` (expected to be a
  different, likely shorter list than before — see design.md's "bsdata-corpus-scan" decision).

## 6. Verification

- [x] 6.1 `dotnet build` the whole solution.
- [x] 6.2 `dotnet test` the whole solution (excluding the explicit corpus scans by default, as
  today) — confirm a clean pass with no reference to the removed members remaining.
- [x] 6.3 Run the corpus scan explicitly once (`dotnet test --filter
  FullyQualifiedName~WeaponKeywordScanTests`) to confirm the repurposed scan and its freshly-seeded
  allowlist agree.
- [x] 6.4 Rebuild and restart the local `docker compose` container; re-import
  `data/nr-export-orks-close-quarters-cleave.json` at `/Import` and confirm `/LivePlay` renders
  every keyword chip from Section 3.3 correctly.
