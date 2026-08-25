## Context

Analyzed `data/gw-app-export-templars.json` (the existing Templars fixture, round-tripped through
NewRecruit: import → JSON export) directly against its real structure — see proposal.md's Why for
the headline findings. Key facts this design leans on:

- `roster.forces[0].selections` is a flat top-level list mixing army-configuration selections
  (`Battle Size`, `Detachment`, `Force Disposition`) with real unit/model selections. A unit
  selection's own `profiles` array carries every characteristic/weapon/ability it needs, already
  resolved, tagged by `typeName` (`"Unit"`, `"Ranged Weapons"`, `"Melee Weapons"`, `"Abilities"`).
  A unit's own nested `selections` carry its wargear/model-group choices, each already its own
  separately-counted entry — there is no flat weapon list requiring partition inference anywhere in
  this format.
- Attachment is encoded via `associations: [{type: "outgoing", to: <selection id>, name: "Leading" |
  "Supporting"}]` on the Leader/Support selection, pointing at the Bodyguard selection's own `id`.
  Confirmed against all three attachment groups in the sample (2-member, 3-member, Leader-only) and
  both standalone units (`associations` absent).
- A selection's own `rules` array (distinct from its `profiles`) carries army-wide/Core-rule-style
  abilities (`"Templar Vows"`, `"Scouts 6\""`) with full text already resolved — this is the JSON
  equivalent of `AbilityOrigin.CoreRule`.
- An Enhancement selection (`"Oathbound Exemplar"`) carries a `costs` entry named `"Enhancements"`
  with value 1 — a distinct, reliable structural tag, separate from and more direct than the
  BSData pipeline's group-name substring match.
- The rule/ability text markup (`**bold**`, `^^small-caps^^`, `[BRACKET]`) matches what
  `RuleTextTokenizer`/`RuleTextEmphasisRenderer` (`rules-glossary-popovers`) already parse.
- `roster.xmlns` is `"http://www.battlescribe.net/schema/rosterSchema"` — the standard, documented,
  cross-tool BattleScribe roster schema, not a bespoke NewRecruit format.

Current pipeline shape (`army-list-import`, `army-list-parsing`, `army-roster-enrichment`):
`ExportText → IArmyListParser.Parse → ParsedArmyList` (session-stored) `→ IArmyRosterProvider.Build
(→ BsdataFactionResolver → BsdataCatalogueCache → ArmyRosterEnricher) → ArmyRosterBuildResult
(ArmyRoster, RuleGlossary)`. `/LivePlay` and `LivePlayCasualtyService` both call
`IArmyRosterProvider.Build` fresh on every request from the session's stored `ParsedArmyList` —
`ISessionArmyListStore`/`IArmyRosterProvider` are both hard-typed to `ParsedArmyList` today.

## Goals / Non-Goals

**Goals:**
- Resolve a BattleScribe roster JSON directly into `ArmyRoster`, with no BSData catalogue
  involvement.
- Share `/LivePlay` and casualty-sync unchanged — both should stay unaware of which format was
  originally submitted.
- Reuse existing generic building blocks where the underlying text shape matches (rule-text markup
  rendering, weapon-keyword parsing, dice-expression parsing, the `RuleGlossary`/`RuleDefinition`
  container types), rather than re-deriving parallel copies.

**Non-Goals:**
- Supporting NewRecruit's other export formats (its own "GW Short" text variant) — JSON only.
- Supporting raw `.ros`/`.rosz` (zipped XML) — this pipeline takes pasted JSON text, matching
  `/Import`'s existing paste-box UX; a file-upload path is not in scope.
- Any change to the existing GW-app text pipeline, BSData ingestion, or its own capabilities.
- Validating army composition/legality — same standing omission as the rest of this project (the
  exporting tool is trusted).

## Decisions

### Bypass BSData entirely; synthesize `Datasheet`-shaped objects directly from the JSON

The roster JSON is already fully resolved, so there is nothing to look up. A new mapper walks each
top-level selection's `profiles` (classified by `typeName`, mirroring `BsdataDatasheetMapper`'s own
classification approach) directly into `Statline`/`WeaponProfile`/`Ability` instances, and each
selection's own `rules` into `AbilityOrigin.CoreRule` abilities — never touching
`Domain.Catalogue.Bsdata`, `BsdataCatalogueCache`, or `BsdataFactionResolver`. This is the
foundational decision the rest of this design follows from; the alternative (resolving JSON-derived
names back against BSData, the way the text pipeline does) would reintroduce exactly the class of
name-mismatch problems (`[[project_bsdata_name_mismatch_patterns]]`) this pipeline exists to avoid,
for no benefit — the data needed is already sitting in the source.

Characteristic-text parsing (dice notation, AP sign convention, threshold values) reuses the
existing helpers where the source text shape matches BSData's own (`DiceExpression.Parse`,
`WeaponKeywordParser`) — confirmed the same conventions apply from the sample (e.g. Ferocity's
Keywords text `"Anti-infantry 4+, Devastating Wounds"` is byte-for-byte the same shape
`WeaponKeywordParser` already handles). Invulnerable-save caveat resolution
(`BsdataDatasheetMapper.ResolveInvulnerableSave`/`ResolveCaveatAbility`) is the one exception: its
BSData version depends on walking an ancestry chain across `entryLinks` that has no equivalent in
this format. A caveated InSv here needs its own, simpler resolution (the linked ability is most
likely a sibling `profiles`/`rules` entry on the same selection) — deferred to implementation, and
flagged in Risks below since the one sample doesn't happen to include a caveated invulnerable save.

### Roster-scoped `RuleGlossary`, reusing the existing type

Rather than invent a new glossary type, build a `RuleGlossary` (existing type, from
`rules-glossary-popovers`) by walking the whole roster JSON once, collecting every distinct entry
found in any selection's `rules` array (deduped by id, first occurrence wins — matching
`RuleGlossary.Build`'s own existing precedent for a BSData closure) into `RuleDefinition`s. This
gives `[BRACKET]` cross-reference resolution and nested-popover rendering for free, with zero
changes to `RuleTextTokenizer`/`RuleTextEmphasisRenderer`/`_UnitBlock.cshtml`'s existing popover
wiring — `/LivePlay` only ever needs *a* `RuleGlossary`, not specifically a BSData-sourced one.

### Attachment resolution via `associations`, not role text

The Bodyguard is whichever top-level selection is the *target* of one or more `"Leading"`/
`"Supporting"` associations; every selection with such an outgoing association is an Attached
member. This sidesteps needing a Leader/Support role distinction at all for domain purposes
(`AttachedUnit.Attached` is already an open, unordered-by-role collection — see
`.claude/domain-model-11e.md`), though the association's own `name` is available if a future need
for the distinction arises.

### Enhancement recognition via the `costs["Enhancements"]` tag

More reliable than the text pipeline's `"Enhancements"`-substring group-path match, and — because a
roster JSON only ever contains selections the player actually made — this pipeline cannot reproduce
the original `resolve-enhancement-abilities` bug class (an available-but-unselected Enhancement
leaking onto every eligible unit) by construction. Likewise, `Core Rule Extraction`'s deliberate
"no additional gating" (see specs) is safe for the same reason: `gate-and-dedupe-core-rule-abilities`
exists because BSData's raw catalogue content includes rules that are chapter/mode-excluded and
must be filtered out; a resolved roster has already had that filtering applied by whatever tool
produced it.

### Format-discriminated session storage and shared `Build`

`ISessionArmyListStore`/`IArmyRosterProvider` change from being hard-typed to `ParsedArmyList` to a
small discriminated wrapper (two variants: the existing `ParsedArmyList` for the text pipeline, and
a parsed BattleScribe roster DTO for this one). `/Import`'s `OnPost` performs format recognition
once (attempt `JsonDocument.Parse`; check for `roster.xmlns`) and constructs the appropriate
variant; `IArmyRosterProvider.Build` dispatches internally to either the existing
`BsdataFactionResolver → BsdataCatalogueCache → ArmyRosterEnricher` sequence or the new mapper.
`/LivePlay` and `LivePlayCasualtyService` are unchanged beyond the parameter type, since both
already only call `Load` → `Build` and never branch on what's inside.

**Alternative considered and rejected**: two entirely separate session keys, two `IArmyRosterProvider`
implementations, and format-specific routing duplicated at every call site (`/Import`, `/LivePlay`,
casualty-sync). Rejected because it would triple the number of places that need to know both formats
exist, for no benefit over a single small discriminated wrapper — `/LivePlay` genuinely doesn't care
which format produced its `ArmyRoster`.

**Alternative considered and rejected**: resolving JSON-derived selections back into `ParsedArmyList`
so the existing `ArmyRosterEnricher`/BSData path could be reused unchanged. Rejected — the whole
point is to avoid BSData name-matching; forcing the already-resolved JSON data back through a
name-based re-resolution step would throw away the reliability this format offers and reintroduce
exactly the failure modes it doesn't have.

## Risks / Trade-offs

- **[Risk]** Verified against exactly one real sample: one faction (Black Templars), one detachment,
  one battle size, no vehicles-with-crew, no split-fire, no caveated invulnerable save. Field-level
  extraction rules (especially statline/weapon characteristic text shapes) may need broadening once
  more samples exist. → **Mitigation**: same discipline this project has followed throughout (see
  `.claude/domain-model-11e.md`'s "BSData JSON Ingestion" and its full-corpus scan tests) — treat
  the first implementation as provisional until run against a second, independently-sourced roster
  JSON (a different faction, ideally with a caveated invulnerable save and a vehicle), and land a
  fixture-based regression test from that second sample too, not just the one already in hand.
- **[Risk]** `roster.name` in the sample is the generic `"Imported List"`, not the original army
  name (`"Forever Crusade"`) — NewRecruit's own import-then-export round-trip doesn't preserve a
  user-entered list name from a re-imported GW-app text export. → **Mitigation**: none needed at the
  design level; this is cosmetic (Army Name has no other behavior riding on it) and is whatever the
  user names their list directly in NewRecruit for a list built there natively.
- **[Risk]** `force.catalogueName` (e.g. `"Imperium - Adeptus Astartes - Black Templars"`) doesn't
  cleanly map to the existing 1-or-2-entry Faction convention used for `BsdataFactionResolver`
  suffix matching in the text pipeline. → **Mitigation**: not a real risk for this pipeline
  specifically — `ArmyRoster.Faction` is confirmed (via a repo-wide search) to be read only by the
  text pipeline's own `BsdataFactionResolver` call site, never rendered on `/LivePlay`; this
  pipeline can populate it with whatever segments result from splitting `catalogueName` without
  needing to match that convention exactly.

## Migration Plan

Additive to the domain/mapping layer; the `ISessionArmyListStore`/`IArmyRosterProvider` signature
change is an internal API change with no external/persisted-data migration (session state is
in-memory, per-process, and already cleared on every restart). Existing GW-app text imports continue
to work unchanged — regression-covered by the existing `ArmyListParserTests`/`ImportFlowTests`
suites, which this change must not need to modify beyond the type signature at the call sites they
exercise.

## Open Questions

- Exact field-by-field extraction rules (which `typeId` values are stable across a broader corpus,
  how a caveated invulnerable save's linked ability is actually positioned relative to its
  characteristic) are deferred to implementation, to be verified against a second real sample rather
  than guessed now — consistent with this project's established practice, and explicitly not
  expected to change the architecture or task breakdown above, only the mapper's internal detail.
