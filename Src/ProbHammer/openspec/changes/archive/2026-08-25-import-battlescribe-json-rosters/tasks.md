## 1. BattleScribe roster JSON model

- [x] 1.1 Add `System.Text.Json`-backed record types for the subset of the BattleScribe
      `rosterSchema` this pipeline reads (`roster`, `forces[].selections[]` recursively, `profiles[]`
      with `characteristics[]`, `rules[]`, `costs[]`, `categories[]`, `associations[]`) — mirroring
      `Json/BsCatalogueFile.cs`'s existing precedent of modeling only the fields actually needed.
- [x] 1.2 Format recognition: detect a `roster` object whose `xmlns` identifies the BattleScribe
      roster schema, distinct from a GW-app text export.

## 2. Army metadata extraction

- [x] 2.1 PointsSpent/PointsLimit from `roster.costs`/`costLimits`.
- [x] 2.2 Faction from `force.catalogueName`; Detachments from the `Detachment` selection's nested
      selection(s) and their own Detachment Points cost; ForceDisposition from `Force Disposition`;
      BattleSize from `Battle Size`.

## 3. Attachment relationship resolution

- [x] 3.1 Build a map from every top-level selection's outgoing `"Leading"`/`"Supporting"`
      association to its target selection id.
- [x] 3.2 Resolve each target as a Bodyguard with every selection pointing at it as an Attached
      member; resolve every selection with neither an outgoing association nor an incoming one as
      standalone.

## 4. Statline, weapon, and ability extraction

- [x] 4.1 Walk a unit/model selection's `profiles`, classifying by `typeName` into `Statline`
      (`"Unit"`), `WeaponProfile` (`"Ranged Weapons"`/`"Melee Weapons"`), `Ability` (`"Abilities"`) —
      reusing existing characteristic-parsing helpers (`DiceExpression.Parse`, AP sign convention,
      `WeaponKeywordParser`) where the text shape matches.
- [x] 4.2 Walk a unit selection's nested `selections` (already split per loadout) into `ModelLine`s —
      no partition inference needed, since each sub-selection already carries its own count and
      weapon list.
- [x] 4.3 Retain every weapon profile a single wargear selection resolves to (the Sword of the High
      Marshals Sweep/Strike shape).
- [x] 4.4 Invulnerable-save caveat resolution for this format (see design.md's Decisions — deferred
      detail, no BSData-style ancestry chain available here).

## 5. Enhancement and Core Rule extraction

- [x] 5.1 Recognize a selection carrying a `costs` entry named `"Enhancements"` and attach its
      resolved ability to the owning Unit.
- [x] 5.2 Build a roster-scoped `RuleGlossary` by collecting every distinct entry (by id) found in
      any selection's `rules` array across the whole roster.
- [x] 5.3 Extract each selection's own `rules` entries as `AbilityOrigin.CoreRule` abilities, text
      resolved via the roster-scoped glossary from 5.2, with no additional gating.

## 6. Army roster assembly

- [x] 6.1 Assemble every resolved `Unit`/`AttachedUnit` plus the extracted metadata (section 2) into
      one `ArmyRoster`.

## 7. Session storage and provider wiring

- [x] 7.1 Introduce a format-discriminated stored-import type replacing `ParsedArmyList` as what
      `ISessionArmyListStore`/`IArmyRosterProvider` accept and return, per design.md's chosen shape.
- [x] 7.2 `Import.cshtml.cs`: detect submitted format (task 1.2) and route to the corresponding
      pipeline before validating/committing to session — same "validate before Save" ordering the
      existing GW-app text path already follows.
- [x] 7.3 Confirm `/LivePlay` and `LivePlayCasualtyService` need only the parameter-type change, with
      no branching on which format produced the stored import.

## 8. Tests

- [x] 8.1 Fixture-based unit tests for the mapper against a trimmed real excerpt of
      `data/gw-app-export-templars.json` (same "trimmed real excerpt" convention as
      `tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/`), covering: attachment resolution (all three
      real shapes — 2-member, 3-member, Leader-only), the pre-split model-group shape, a
      multi-profile weapon (Sword of the High Marshals), an Enhancement (Oathbound Exemplar), and a
      Core Rule (Templar Vows). **Deviation**: the real sample contains no 3-member attachment
      group (only two real shapes — 2-member and Leader-only — were found; see
      `.claude/domain-model-11e.md`'s new section).
- [x] 8.2 An `ImportFlowTests`-style end-to-end test: paste the full real
      `gw-app-export-templars.json`, confirm redirect to `/LivePlay` and that the rendered page
      matches the equivalent GW-app-text-import rendering for the same army (both pipelines
      producing an `ArmyRoster` for the same real Templars list is exactly the cross-check this
      sample was chosen for).
- [x] 8.3 A format-recognition test: non-JSON text and JSON without the expected `roster.xmlns`
      shape both fall through to the existing GW-app text pipeline (and fail there with its own
      existing diagnostics) rather than being misidentified.

## 9. Docs

- [x] 9.1 Add a "BattleScribe/NewRecruit JSON Import Pipeline" section to
      `.claude/domain-model-11e.md`, parallel to the existing "Army List Import Pipeline" section.
- [x] 9.2 Update `PROGRESS.md` with a summary, the real-sample findings, and the deferred
      caveated-InSv/second-sample verification noted in design.md's Risks.
