# Domain Model — 11th Edition (Attached Units)

Full requirements: `openspec/changes/attached-unit-domain-model/`. This file summarizes the shape
that landed in code.

**Status:** implemented and tested (`src/ProbHammer.Core/Domain/`), proven against hand-built
fixtures plus a real BSData JSON loader (`Domain/Catalogue/Bsdata/` — see
`.claude/domain-model/bsdata-json-ingestion.md`). `ProbHammer.Web`/`/LivePlay` is fully wired: a
pasted GW-app 11e export is parsed
(`Domain/Import/`), resolved against BSData (`ArmyRosterEnricher`), and rendered — `Examples/`
fixture data is unreferenced by `Web` but kept in-tree for future domain-model exploration.
`/Import` also recognizes a BattleScribe/NewRecruit roster JSON export and routes it through a
second, independent pipeline (`Domain/Import/BattleScribe/` — see
`.claude/domain-model/battlescribe-import-pipeline.md`) that
synthesizes this same model directly from the JSON's own already-resolved data, no BSData
involved. Coexists with the untouched, fully-superseded 10th-edition model
(`.claude/domain-model.md`) — see `PROGRESS.md` for that history.

---

## Namespaces

- `ProbHammer.Core.Domain.Catalogue` — reference/rules data (Catalogue context)
- `ProbHammer.Core.Domain.Roster` — army-list composition and live game state (Roster context)

---

## Topic Files

This file used to hold every subsystem inline; it exceeded the 150k-character auto-load limit and
was split (2026-09-09, mechanical reorg only) into topic files under `.claude/domain-model/`. Each
is loaded on demand — reference by path, not by `@`-include, the same convention this project
already uses for the archived 10e docs.

- `.claude/domain-model/catalogue-context.md` — `Datasheet`/`Statline`/`WeaponProfile`/`Ability`
  shapes, `AbilityOrigin` classification, `DiceExpression`.
- `.claude/domain-model/characteristic-value-domain-model.md` — `CharacteristicValue`/
  `CharacteristicView` abstract hierarchies (unconsumed except `InvulnerableSaveCharacteristicView`).
- `.claude/domain-model/characteristic-modification-kind.md` — the `RollThreshold`/
  `ArmourPenetration`/`Plain` sign-and-clamp resolver for applying an `Improve`/`Worsen`/`Set`
  verb to a characteristic.
- `.claude/domain-model/bsdata-json-ingestion.md` — the BSData catalogue JSON loader: closure
  resolution, `Datasheet` mapping, ability/core-rule/army-rule extraction, `InvulnerableSave`
  resolution, weapon keyword parsing, and the full-corpus scan tests.
- `.claude/domain-model/rules-glossary-and-popovers.md` — `RuleGlossary`, `[BRACKET]` token
  resolution, `/LivePlay`'s popover wiring, and the Army Header.
- `.claude/domain-model/army-list-import-pipeline.md` — the GW-app text export pipeline: parsing,
  BSData enrichment, Detachment resolution, session-backed storage.
- `.claude/domain-model/battlescribe-import-pipeline.md` — the independent BattleScribe/NewRecruit
  JSON import pipeline (no BSData involvement).
- `.claude/domain-model/roster-context.md` — `Unit`/`AttachedUnit`/`ModelLine`/`ICombatUnit`,
  `AttachedUnitAggregator`'s aggregate view, and `ArmyRoster`.
- `.claude/domain-model/statline-flag-rules.md` — how a matched `RuleClassificationBaseline` entry
  derives a flagged, per-unit Statline value at roster-Build time.
- `.claude/domain-model/characteristic-modifier-caveats.md` — `CharacteristicModifierCandidate`,
  the structural (`BsModifier`-based) classifier; retired as a live mechanism, kept for the
  offline report tool.
- `.claude/domain-model/rule-effect-classification.md` — `RuleEffectClassifier`, the text-only
  Target/Effect extractor, its regex patterns, the corpus report tool, and the checked-in
  verified-classification baseline.
- `.claude/domain-model/invulnerable-save-effect-resolution.md` — resolving a classified
  `InvulnerableSaveCharacteristicEffect` into a real `InvulnerableSaveCharacteristicView`.
- `.claude/domain-model/phase-turn-tracker.md` — the player-set phase/turn control and the
  section-relevance table driving which unit-block sections render open.
- `.claude/domain-model/deliberate-omissions.md` — what this app permanently does not model, and
  why.

Archived 10e docs, kept only as reference material, not auto-loaded:
`legacy/10e-pipeline/.claude/domain-model.md`, `web-app.md`, `simulation-engine.md`,
`bsdata-parsing.md`, `rules/combat-rules.md`, `design-tokens.md`,
`implementation-notes.md`.
