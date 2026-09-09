# Characteristic-Modifier Caveats

Full requirements: `openspec/changes/classify-characteristic-modifier-caveats/` (original,
retired — see below) and `openspec/changes/unify-characteristic-effect-resolution/` (current
state). `CharacteristicModifierCandidate` is a data-derived classification: recognizes when
BSData's own structured `BsModifier` data (`Type`/`Field`/`Value`/`Conditions`, already read for
`IsGameModeGated` hidden-gating — see "BSData JSON Ingestion") deterministically modifies a
specific `Statline` characteristic for a given granting entry. **As a live, Build-time mechanism
this capability is retired outright** (`unify-characteristic-effect-resolution`, 2026-09-09):
`AttachedUnitAggregator.ApplyCharacteristicModifierCandidates` — the step that used to always
caveat a present classified candidate, described in the now-superseded paragraphs this section
used to carry — is deleted, with no replacement of its own. `CharacteristicModifierCandidate`
itself, and its classifier, are unchanged and still exist as classification-only data — exposed
via `Datasheet.CharacteristicModifierCandidates`, and still consumed by the offline
`RuleEffectClassificationReport` tool's own structural-derivation/cross-validation path (see "Rule
Effect Classification (Text-Only)" in rule-effect-classification.md) — but nothing in the live `/LivePlay` roster-resolution
path reads it anymore.

**Why retiring it is safe, not a regression**: a present ability whose granting selection also
happens to classify as a `CharacteristicModifierCandidate` now resolves (or doesn't) purely through
`statline-flag-rules`' own `ApplyStatlineFlagRules` pass — the exact same present-ability,
baseline-Text-match mechanism every other characteristic-affecting ability already goes through.
`unify-characteristic-effect-resolution`'s own corpus check confirmed every real tier-1
characteristic-modifier candidate in the live BSData clone has real ability text reachable through
the ordinary present-ability walk (a local profile or an infoLink) — none depended on a
raw-value-only resolution path that only `CharacteristicModifierCandidate`'s own application step
could reach. The one confirmed real overlap this retirement had to get right — Adeptus Custodes'
"Vexilla," whose own wargear entry carries both a tier-1 structural Oc-increment modifier *and* is
matched by the baseline's own fully-resolving Effect — now lands on the correct, resolved value
because `ApplyStatlineFlagRules` is the only thing touching it at all, not because of an ordering
accident between two coordinating mechanisms (the retired guard's original job). A present
candidate with no matching baseline entry now produces no flagged value at all, rather than the
old mechanism's always-caveated fallback.

```
CharacteristicModifierCandidate(EntryName, Characteristic, RawValue, RawType)   // Domain/Catalogue
                                       // - a classified, data-derived candidate: "EntryName, if
                                       // actually selected on a resolved unit, structurally
                                       // modifies Characteristic." Characteristic is one of
                                       // "M"/"T"/"Sv"/"W"/"Ld"/"Oc"/"InSv" (Statline's own scalar
                                       // property names) - the only fields the classifier's closed
                                       // Field allowlist recognizes (see below for the one
                                       // remaining exclusion and why). RawValue/RawType are the
                                       // source BsModifier's own unparsed Value/Type text - not
                                       // read by anything at Build time (see above); RawType feeds
                                       // only the offline report tool's own structural-derivation
                                       // path. Never applied to a Datasheet's own Statline fields -
                                       // selection-blind catalog data, identical for every roster
                                       // resolving that Datasheet.

Datasheet.CharacteristicModifierCandidates: IReadOnlyList<CharacteristicModifierCandidate>
                                       // on-demand exposure, mirroring OptionalAbilityNames/
                                       // WeaponNames' existing on-demand-list pattern - never part of
                                       // the always-enumerated Statlines/Abilities.

BsdataDatasheetMapper.ClassifyCharacteristicModifierCandidates(entry) -> IEnumerable<...>
                                       // entry-scoped only (never a BsSelectionEntryGroup's own
                                       // Modifiers - a full-corpus check found every real group-level
                                       // characteristic modifier is either itself tier 3+ or belongs
                                       // to a group with no own Profiles/SelectionEntries to ever
                                       // resolve a matching name against, so classifying one would
                                       // only ever produce an unreachable candidate). Reads
                                       // entry.Modifiers alongside IsGameModeGated's existing read of
                                       // the same data, keyed by a closed Field id -> characteristic
                                       // allowlist (CharacteristicFieldIds) built from a full-corpus
                                       // scan of the live clone, not guessed.
BsdataDatasheetMapper.IsTier1OrTier2(modifier, entryId) -> bool
                                       // closed-world condition-tier classifier: tier 1 (no
                                       // Conditions/ConditionGroups at all) or tier 2 (every
                                       // condition is a "selections" count of this same entry, by
                                       // id, evaluated from a "self" or "parent" scope) classify;
                                       // any other condition shape - a sibling entry's id, an
                                       // "associations" (live attachment) check, a self-reference
                                       // scoped broader than "self"/"parent" (e.g. "roster") - is
                                       // left unclassified, never guessed as safe.
```

**Closed Field allowlist covers the six Statline scalars plus InSv (M/T/Sv/W/Ld/Oc/InSv)** — built
from a full-corpus scan of the live BSData clone's real `BsModifier.Field` values (resolved against
the game system's own `profileTypes["Unit"].characteristicTypes` id table), not guessed. InSv
(`55a7-5b54-c60d-11dc`) rejoined this allowlist via `unify-characteristic-effect-resolution` — it
was excluded originally only because no downstream consumer could resolve a structurally-derived
InSv candidate safely; that reason no longer holds now that `AttachedUnitAggregator`'s
`ResolveCaveatedInvulnerableSaves` step (see statline-flag-rules.md) is a single,
safe consumer for every characteristic-affecting present ability, InSv included. Real corpus case
this unlocks: Black Templars' "Consecrating Aura" Enhancement (tier 1, unconditional, previously
discarded entirely). One exclusion remains, confirmed by the same scan:
- **Every `WeaponProfile` characteristic** (Ranged/Melee Weapons' own A/S/AP/D/BS/WS/Range/Keywords
  ids) is excluded — the scan confirmed zero real `entry.Modifiers`/`group.Modifiers` anywhere in the
  corpus target a `WeaponProfile` field; every real occurrence of those ids lives inside a
  Crusade-only `modifierGroups` block this loader doesn't read at all (unmapped, per
  `BsCatalogueFile.cs`'s own doc comment), never in the directly-modeled `modifiers` array. Building
  an untested, unreachable WeaponProfile-targeting classification/application path would be exactly
  the kind of premature behavior on an unconsumed shape this codebase avoids elsewhere.

**Tier 2 is empty in the real corpus** — implemented per spec regardless (an empty bucket is an
honest, acceptable outcome, not a design failure). The one self-referencing-condition example found
at all (Astra Militarum's "Deficiency" Battle Scar) uses `scope: "roster"` with a roster-wide
`affects` path and is Crusade-mode-only content, correctly excluded by requiring a recognized tier-2
condition's own `scope` be "self" or "parent" only.

**Full-Corpus Scan** (`CharacteristicModifierClassificationScanTests.cs`, same permanent
`[Fact(Explicit = true)]` pattern as the other CorpusScan tests): reuses the real, public
`BuildDatasheet` as its own classification oracle (each interesting entry re-rooted as its own
Datasheet's starting entry) rather than re-deriving a second copy of the classifier's own predicate,
so the scan can never share an undetected bug with the classifier it checks. Only unclassified
occurrences are collected into the allowlist-checked results (`CharacteristicModifierClassificationAllowlist.cs`
— now just the "condition present" tier-3+ bucket, since InSv's own allowlist entry was removed
once it started classifying successfully like any other recognized field); a separate sanity
assertion confirms real tier-1 classifications are still found (never silently zero).

**Explicitly deferred, not part of any change to date** (tracked in `.claude/vnext-ideas.md`):
computing an actual `DerivedValue` for any caveat a future mechanism surfaces; tier 3+ conditions (a
sibling selection, live attachment state, or a different unit entirely); prose-only classification
(an ability whose text describes a characteristic change with no backing `BsModifier` at all, e.g.
Darnath Lysander's "Inspiring Commander").
