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
                                       // - see record's own doc comment

Datasheet.CharacteristicModifierCandidates: IReadOnlyList<CharacteristicModifierCandidate>
                                       // on-demand exposure, mirroring OptionalAbilityNames/
                                       // WeaponNames' existing on-demand-list pattern - never part of
                                       // the always-enumerated Statlines/Abilities.

BsdataDatasheetMapper.ClassifyCharacteristicModifierCandidates(entry) -> IEnumerable<...>
                                       // see method's own doc comment (entry-scoped only, never a
                                       // group's; the tier-1/tier-2 discipline). Reads
                                       // entry.Modifiers alongside IsGameModeGated's existing read of
                                       // the same data, keyed by CharacteristicFieldIds - see its
                                       // own doc comment.
BsdataDatasheetMapper.IsTier1OrTier2(modifier, entryId) -> bool
                                       // see ClassifyCharacteristicModifierCandidates' own doc
                                       // comment for the tier-1/tier-2 discipline this implements.
```

**Closed Field allowlist covers the six Statline scalars plus InSv (M/T/Sv/W/Ld/Oc/InSv)** — see
`CharacteristicFieldIds`'s own doc comment for the full corpus-scan provenance, the InSv-rejoin
rationale (real corpus case unlocked: Black Templars' "Consecrating Aura" Enhancement), and why
every `WeaponProfile` characteristic remains excluded.

**Tier 2 is empty in the real corpus** — see `ClassifyCharacteristicModifierCandidates`'s own doc
comment for the full finding (the one self-referencing-condition example found at all, Astra
Militarum's "Deficiency" Battle Scar, and why it's correctly excluded).

**Full-Corpus Scan** (`CharacteristicModifierClassificationScanTests.cs`): see its own class doc
comment for the scan shape and classification-oracle rationale. Only unclassified occurrences are
collected (`CharacteristicModifierClassificationAllowlist.cs` — now just the "condition present"
tier-3+ bucket, since InSv's own allowlist entry was removed once it started classifying
successfully like any other recognized field).

**Explicitly deferred, not part of any change to date** (tracked in `.claude/vnext-ideas.md`):
computing an actual `DerivedValue` for any caveat a future mechanism surfaces; tier 3+ conditions (a
sibling selection, live attachment state, or a different unit entirely); prose-only classification
(an ability whose text describes a characteristic change with no backing `BsModifier` at all, e.g.
Darnath Lysander's "Inspiring Commander").
