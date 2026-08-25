## Context

See `proposal.md` for motivation and scope. This section covers only the technical shape needed to
implement it, plus the decisions worked through during exploration that the specs alone don't
carry (they describe observable behavior, not the mechanism).

Relevant existing shapes, unchanged by this design:
- `BsdataDatasheetMapper.BuildDatasheet` is called fresh per request (no per-`Datasheet` caching);
  only `ResolvedBsdataCatalogue` (the closure + indices) is cached, per faction, app-wide. Adding a
  request-varying input (a resolved Detachment) is architecturally free - no cache-invalidation
  concern.
- `BsdataNameResolver.Resolve` only searches each closure file's own *top-level*
  `SharedSelectionEntries`. A Detachment choice is a nested child of a top-level `"Detachment"`
  `selectionEntryGroup` (confirmed real name, case-sensitive match, in every faction file checked;
  Space Marines alone nests 50 sibling Detachment choices under it) - this resolver cannot reach it
  directly.
- `AttachedUnitAggregator.PromoteArmyRuleAbilities` dedupes an `ArmyRule`-origin ability by Name,
  but only across one `ICombatUnit`'s own `Components` (one `AttachedUnit` or standalone `Unit`).
  There is no existing roster-wide (across every `ArmyRoster.Units` entry) dedup.
- `_UnitBlock.cshtml` builds its popover trigger/panel pairs via local functions
  (`BuildRulePopover`, `NextPopoverId`, `RenderNestedReference`) scoped to that one Razor file, with
  popover ids uniqued by the rendering unit's own page index (`i`).

## Goals / Non-Goals

**Goals:**
- Resolve every selected Detachment's own rule text (whichever of the two real BSData shapes it
  uses) and every roster-wide `ArmyRule`-origin ability, and render both in a new `/LivePlay`
  header.
- Disambiguate a natural-language-joined multi-Detachment line correctly, including both confirmed
  real collision shapes: a Detachment name that itself contains "and" ("Legends of Saga and Song"),
  and one Detachment name that is a literal substring of another ("Warhost"/"Armoured Warhost") -
  see Decisions for the algorithm that handles both without special-casing either.
- Reuse existing popover/rule-text rendering rather than building a second display mechanism.

**Non-Goals:**
- Any per-unit structural attachment of a Detachment rule, any Statline/weapon mutation, any
  general ability-text classification - all deferred, see `proposal.md` and `.claude/vnext-ideas.md`.
- Composition validation of any kind (which Detachments a battle size permits, DP budget
  enforcement) - the export is trusted, unchanged from the project's existing stance.

## Decisions

**Dictionary-based, greedy-longest-match Detachment resolution, not a parser-level split.**
Considered and rejected: splitting in `ArmyListParser` itself (the design this proposal started
with), and a follow-up design of syntactic split-then-validate (try the whole text; on failure,
split rigidly on a plain `"and"` for two items or an Oxford comma for three or more, then validate
each piece against BSData). Both were rejected once corpus data showed two real, confirmed
collision shapes a syntactic split can't handle correctly: a single real Detachment literally named
**"Legends of Saga and Song"** (splitting on `"and"` unconditionally misidentifies it as two
nonexistent Detachments), and, in the same faction, two separate real Detachments **"Warhost"** and
**"Armoured Warhost"**, where one is a literal substring of the other.

The resolved algorithm needs no assumption about which characters separate list items at all: given
the parsed Detachments entry's captured text and the closure's own full set of known Detachment
names (sorted longest-name-first), repeatedly find the longest known name that still matches
somewhere in whatever text currently remains, and "chomp" it — remove that name's own text, plus
any immediately adjacent separator punctuation (comma, `"and"`, whitespace), from the working
string — then loop against what's left. Trying longest names first before shorter ones is what
makes "Warhost" never get a chance to match text already chomped away as part of "Armoured
Warhost" - no separate containment-filtering pass needed, it falls out of the ordering alone. The
loop succeeds once the remaining text is empty and every chomped name has been resolved; it fails
with a diagnostic naming the unresolved remainder, the same "did you mean...?" convention every
other resolution in this capability already uses, the moment a pass finds no further matching name
while non-separator text still remains - this is what keeps a genuine typo or unknown Detachment
name from silently vanishing instead of failing loudly.
`ArmyListParser` itself needs no change at all — it already captures the whole line as one entry
(`ArmyListParserTests.RealExport_CommaBearingDetachmentName_IsCapturedAsOneEntry` stays correct and
unchanged).

**Detachment name resolution: a dedicated nested-name index, not a change to `BsdataNameResolver.
Resolve`'s existing contract.** Considered: widening `Resolve` itself to search nested entries.
Rejected — that resolver's top-level-only contract is what keeps unit-name resolution from
accidentally matching a nested wargear-option entry sharing a name; widening it for Detachments
would widen it for every other caller too. Instead, a new, narrowly-scoped index is built once per
closure: locate the closure's own `"Detachment"` `selectionEntryGroup` (name match, local-over-
imported precedence per the existing convention), then index its own direct `selectionEntries` by
name. Exposed as a new method alongside `ResolveDatasheet` on `ResolvedBsdataCatalogue` (e.g.
`ResolveDetachment(string name)`), built lazily the same way the existing id/group/profile indices
are.

**Detachment rule text extraction reuses `RuleGlossary`, not a new text-resolution mechanism.**
`BsSelectionEntry` gains a `Rules: List<BsRule>` field (identical shape to the already-modeled
`BsCatalogue.Rules`/`SharedRules`, just nested one level differently) for the locally-declared
shape. The infoLink shape reuses the existing `RuleGlossary.TryResolve` lookup `Core Rule Ability
Extraction` already established — but does *not* reuse `ProcessRuleInfoLink`'s ancestry-based
"nested inside a `type: upgrade` entry" guard, since that guard exists specifically to exclude a
weapon's own keyword cross-reference from leaking into a datasheet's abilities; a Detachment
entry's own direct infoLinks carry no equivalent ambiguity.

**`ArmyRoster.Detachments` new shape**: two new small records in `Domain.Roster`, mirroring this
codebase's existing "named record over raw tuple" convention (`Ability`, `InvulnerableSave`, etc.):
```
public sealed record DetachmentRule(string Name, string Text);
public sealed record ResolvedDetachment(string Name, IReadOnlyList<DetachmentRule> Rules);
```
`ArmyRoster.Detachments` becomes `IReadOnlyList<ResolvedDetachment>`. **BREAKING** for every
existing producer/consumer of the bare-string shape:
- `ArmyRosterEnricher` (text-export pipeline) - updated as this change's own core work.
- `BattleScribeRosterMapper` (JSON-roster pipeline) - updated to populate `ResolvedDetachment.Rules`
  directly from the roster JSON's own `selections[].rules[]` on each selected Detachment selection
  (confirmed real shape: Death Guard's "Virulent Vectorium" carries a nested "Worldblight" rule with
  `name`/`description` inline). No BSData involvement, consistent with that pipeline's existing
  "synthesizes everything from the JSON's own already-resolved data" design.
- Every test currently asserting `Detachments.Should().Equal("Companions of Vehemence")`-style bare
  strings (`ArmyRosterTests`, `ViewTests`, `ArmyRosterEnricherTests`, `BattleScribeRosterMapperTests`
  - see proposal.md Impact) needs updating to the new shape.

**Roster-wide `ArmyRule` aggregation is a new, separate step from `AttachedUnitAggregator`'s
existing per-unit dedup — not a generalization of it.** The existing dedup
(`PromoteArmyRuleAbilities`) is deliberately scoped to one `ICombatUnit`'s own components, and
stays exactly as-is (per the proposal, the per-unit box keeps rendering unchanged). The header
needs a second, roster-wide view: walk every `ArmyRoster.Units` entry's components' `Datasheet
.Abilities`, filter to `Origin == ArmyRule`, dedupe by Name. This is computed independently of
casualty/`RemainingCount` state — an `ArmyRule` ability is a fact about the army's chapter
identity, not about which models are currently alive, so (unlike the per-unit box, which
disappears once its contributing component is fully dead) the header's own entry for it never
disappears over the course of a game. Lives as a small new helper (e.g. on `LivePlayModel`,
alongside `BuildUnitBlocks`), not inside `AttachedUnitAggregator` itself, since it operates over
the whole roster rather than one `ICombatUnit`.

**Popover mechanism: extract, don't duplicate.** `_UnitBlock.cshtml`'s `BuildRulePopover`/
`NextPopoverId`/`RenderNestedReference` local functions are presently usable only within that one
file, scoped by the unit's own page index for id uniqueness. The header needs the identical
trigger/panel behavior with no unit index to scope against. Duplicating a second copy of this logic
in a new header partial is rejected — this codebase has already hit a real bug from popover-related
logic drifting between two call sites once (`rules-glossary-popovers`' chip-nested-inside-toggle
regression), and duplication is exactly the shape that invites a second one. Extract the
trigger/panel-building logic into a shared, reusable location (e.g. a small static
`RulePopoverRenderer` used by both `_UnitBlock.cshtml` and the new header partial), with id
uniqueness keyed by an explicit scope prefix parameter (`"u{i}"` for a unit block, `"hdr"` for the
header) instead of implicitly relying on the unit index.

**Layout**: a new partial (e.g. `_ArmyHeader.cshtml`), rendered once above the `@for` loop over
unit blocks in `LivePlay.cshtml`, fed a small new view-model carrying the roster metadata, the
deduped `ArmyRule` list, and the resolved Detachments. Two-column rules section, `<thead><th>`
column labels reusing the weapon-table convention (labels themselves, and the section's own title
text, are implementation/markup choices, not specified literally in `live-play-view`'s new
requirement - see that spec delta). A Detachment's own name renders as a plain label (never a
popover trigger itself) with one popover-trigger chip per resolved rule beneath it, including
zero chips when it resolved none. No DP cost, no unit points cost, anywhere.

## Risks / Trade-offs

- **[Risk] The `"Detachment"` selection-group name match is unverified across the full corpus.**
  Confirmed real in Space Marines (via `Marshal's Household`'s own ancestry) and structurally
  implied by every BattleScribe roster JSON sample captured so far (`"name": "Detachment"` as the
  top-level force-configuration selection), but not exhaustively checked against every faction file
  in the live clone. → **Mitigation**: add a permanent, explicit-only corpus-scan test (mirroring
  `InfoLinkTypeScanTests`/`WeaponKeywordScanTests`'s existing pattern) asserting every faction's own
  Detachment-choice group resolves via this same name, so a differently-named outlier is caught by
  a manual scan run rather than failing silently in production.
- **[Risk] Two Detachment names could collide such that the algorithm can't tell them apart.**
  Confirmed real example of the milder form already exists and is handled (`"Warhost"`/`"Armoured
  Warhost"`, via greedy longest-match-first). A harder, theoretical form has no real example: two
  same-length names where one's own text is a substring of the other at an ambiguous position, or
  two different valid tokenizations of the same line both fully covering it. → **Mitigation**: none
  specially designed beyond greedy longest-match-first itself, since no real corpus example needs
  more; if the greedy scan ever produces leftover unclaimed text, or (a case not yet seen in real
  data) more than one full-coverage tokenization is possible, resolution fails loudly with a
  did-you-mean diagnostic rather than silently guessing - revisit only if a real export surfaces
  this shape.
- **[Risk] The roster-wide multi-`ArmyRule` path (2+ simultaneous `ArmyRule`-origin abilities) is
  architecturally sound by code inspection but has never been exercised against real data** - no
  captured export in the repo is Drukhari or Aeldari/Craftworlds, the two confirmed real cases.
  → **Mitigation**: add a hand-built fixture test (mirroring `AbilityClassificationTests`' existing
  closure-fixture convention) asserting two distinctly-named `ArmyRule` abilities both render
  independently in the header, since a real corpus example isn't available yet.
- **[Trade-off] Extracting the popover-building mechanism touches working, tested code
  (`_UnitBlock.cshtml`) for a currently-unrelated feature.** Accepted given the duplication-drift
  risk above is a repeat of a real, already-hit bug class in this exact codebase - the extraction
  itself is behavior-preserving (existing `_UnitBlock.cshtml` rendering tests should catch any
  regression) and is a smaller, more mechanical change than building a second popover
  implementation from scratch.
