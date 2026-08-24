## Context

See `proposal.md` - Why for the motivating bug. Relevant current-state facts confirmed directly
against the code and the live BSData clone (not just doc claims):

- `BsdataDatasheetMapper.WalkInfoLink` (line 227) only follows `link.Type == "profile"`; a
  `"rule"`-typed infoLink is explicitly skipped with a comment saying so.
- `BsEntryLink` (`Json/BsCatalogueFile.cs:165`) currently has **no `Modifiers` property at all** -
  only `BsSelectionEntry` and `BsSelectionEntryGroup` carry `Modifiers` today. The real JSON shape
  (`Imperium - Space Marines.json`'s Impulsor entry) puts the display-name-building modifier
  directly on the infoLink itself:
  ```json
  { "name": "Deadly Demise", "type": "rule", "targetId": "b68a-...",
    "modifiers": [{"type":"append","field":"name","value":"D3"}] }
  ```
  So this change needs a JSON-model addition, not just mapper logic.
- The `append` modifier's `value` is mixed-typed in real data: a JSON string for Deadly Demise
  (`"D3"`) but a JSON number for Firing Deck (`6`). `BsModifier.Value` is already typed
  `JsonElement` for exactly this reason (`IsGameModeGated` reads `.ValueKind` today) - the new
  code needs its own `ValueKind` branch to get plain text either way.
- `RuleGlossary.TryResolve(string nameOrAlias) -> RuleDefinition?` (already shipped,
  `rules-glossary-popovers`) is proven to resolve `"Templar Vows"` and `"Oath of Moment"` against a
  Black Templars closure - asserted directly in `openspec/specs/rules-glossary/spec.md`'s "A
  detachment rule defined on an imported library file resolves" scenario. Nothing about resolving
  the *text* is new; only nothing today calls it from the datasheet walk.
- `ResolvedBsdataCatalogue` already builds and holds a `RuleGlossary` (`.Glossary`) alongside the
  closure - `BuildDatasheet` just isn't handed it. This mirrors exactly how `forceEntries` was
  added as an optional trailing parameter for `IsGameModeGated` in a prior change.
- `_UnitBlock.cshtml`'s `AbilityDisplayName` helper checks `ability.Origin == AbilityOrigin
  .Enhancement` specifically (not `!= Enhancement`) - so a new `AbilityOrigin` value renders
  unprefixed through the exact same path with **zero rendering code change needed**. Confirmed by
  reading the helper directly, not assumed.
- `Datasheet.Abilities` already dedups by name via `WalkContext.SeenAbilityNames` (a single
  case-insensitive `HashSet`, shared by every source that adds to `ctx.Abilities`).

## Goals / Non-Goals

**Goals:**
- Extract `infoLink type:"rule"` content into `Ability`s that render on `/LivePlay` exactly where
  `Intrinsic` abilities already render, with no new UI.
- Get the display name right (base rule name + any `append` name-modifier value).
- Close the loop on "which BSData shapes grant an ability" with a corpus-wide check, not just fix
  this one instance.

**Non-Goals:**
- No behavioral interpretation of the resolved rule text (Oath of Moment's actual mechanical
  effect stays untouched, permanent omission per CLAUDE.md).
- No Army Faction / detachment eligibility checking - a `type:"rule"` infoLink present on a
  resolved entry is extracted unconditionally, exactly as every other ability source is today (the
  loader has never evaluated `constraints`/`conditionGroups` as validation logic; see
  `catalogue-json-ingestion`'s "No Composition, Validation, or Points Data" requirement). The rule
  *text itself* states its own Faction condition ("If your Army Faction is Adeptus Astartes...") -
  that's content, not something this loader enforces.
- No change to `ArmyRosterEnricher`, `AttachedUnitAggregator`, or `_UnitBlock.cshtml` - confirmed
  above that the existing Abilities-column path and Enhancement-indicator check already handle a
  new Origin value correctly.

## Decisions

### Where the new ability lives: `Datasheet.Abilities` (Intrinsic-like), not the on-demand index
A Core/faction rule is never something a player "took" or "didn't take" the way an Enhancement or
Impulsor's Shield Dome is - it's always true about the datasheet once reachable, same as an
Intrinsic ability. It shares `ctx.Abilities`/`ctx.SeenAbilityNames` with `Intrinsic`, not
`ctx.OptionalAbilities`.

**Alternative considered:** route through the on-demand `OptionalAbilities` index (mirroring
Enhancement/OptionalGrant) and have `ArmyRosterEnricher` resolve it by name like it resolves
Enhancements. Rejected - there's no "selection" in the export text to resolve a Core rule *from*
(unlike an Enhancement, which the export explicitly lists); a Core rule is reached purely by
walking the datasheet's own tree, with no player choice involved, which is exactly what
`Intrinsic` already models.

### New `AbilityOrigin` value: `CoreRule`
Covers both universal Core rules (Deadly Demise, Firing Deck - defined in the game system's
`sharedRules`) and faction-specific ones (Oath of Moment, Templar Vows, Infiltrators, Scouts -
defined in a faction/library file's `rules`) under one value, rather than splitting them into two
origins. BSData's own JSON draws no distinction between the two here - both are `infoLink
type:"rule"`, differing only in *which* rule pool their `targetId` happens to resolve against, a
distinction this loader has never surfaced anywhere else (`RuleGlossary.Build` already merges both
pools into one glossary, per `rules-glossary`'s existing "Faction and Library Rule Text Extraction"
requirement). Splitting the origin would invent a distinction the source data and the existing
glossary both already decline to make.

### Display name: base infoLink `Name` + `append`/`field:"name"` modifier(s), space-joined
Read every modifier on the infoLink itself (not on its target) where `Type == "append"` and
`Field == "name"`, in array order, and append each one's text value to the base name with a single
space separator. `Deadly Demise` + `D3` -> `Deadly Demise D3`; `Firing Deck` + `6` -> `Firing Deck
6`; `Scouts` + `6"` -> `Scouts 6"`. A modifier `Value` that is a JSON string uses its string value
directly; a JSON number uses `GetRawText()` (its plain decimal text, e.g. `"6"`) - both observed in
real data on the same infoLink shape, so both must be handled, not just the string case.

**Open question the user should confirm, not something I'm deciding silently:** the real rulebook
prints "Scouts 6"" (unit ability, plural "Scouts") while GW's app export/your own phrasing used
"Scout 6"" - this design produces whatever the source data's own base `Name` + modifier says
(`"Scouts" + "6\"" = "Scouts 6\""`), which is presumed correct since it's the authoritative source
text, not a transcription. Flagging so this isn't silently assumed away.

### Text resolution: `RuleGlossary.TryResolve(link.Name)` using the infoLink's own un-appended name
Use the infoLink's own cached `Name` (before any `append` modifier is applied) as the glossary
lookup key - it's the rule's real base name, matching how every `RuleDefinition` is indexed
(`RuleGlossary.Normalize` already strips a trailing value/threshold suffix for exactly this reason,
but since the un-appended name is directly available here, there's no need to rely on that
suffix-stripping at all).

### Unresolvable rule name: silent skip
When `RuleGlossary.TryResolve` returns null (the referenced rule genuinely isn't reachable in this
closure), no `Ability` is added - consistent with this loader's existing, repeated convention for
a dangling reference (`WalkLink`'s "target not found in this closure... skipped rather than failing
resolution", `BsdataClosure.GameSystem`'s own doc comment). The new corpus scan (below) is what
catches this in aggregate, safely, without putting a live import at risk over a data gap unrelated
to the roster being imported.

**Alternative considered:** throw, matching `AmbiguousCharacteristicException`'s "fail loud"
convention used for genuinely malformed characteristic text. Rejected - a characteristic text
shape that doesn't parse is this loader's own responsibility to recognize; a dangling id reference
into a rules pool is `BsData`'s own historical data-completeness situation, the same class of thing
`WalkLink` already tolerates silently.

### `BuildDatasheet` signature: new optional `RuleGlossary?` parameter, default `null`
Mirrors `forceEntries`'s existing precedent exactly - every pre-existing call site (tests, any
future caller that doesn't care about Core rules) keeps compiling unchanged and simply extracts no
`CoreRule` abilities. `ResolvedBsdataCatalogue.ResolveDatasheet` passes its own `Glossary` property,
which already exists and is already built per-closure.

### Weapon-scoped rule references are excluded, not just datasheet-wide ones extracted
**Found during implementation, verified against a real export, not anticipated up front.** A
`"type": "rule"` infoLink also appears nested inside a `"type": "upgrade"` weapon-option entry
(confirmed real shape: the Impulsor's "Ironhail Skytalon Array" wargear option carries its own
`"Sustained Hits"`/`"Anti"` infoLinks describing *that weapon's own* Keywords characteristic) - a
structurally identical shape to a genuine datasheet-wide Core rule reference, resolving against the
glossary just as successfully, but semantically unrelated: it's a cross-reference for a weapon
keyword chip's own popover, redundant with what `WeaponAbilityTags`/`RuleGlossary` already resolve
independently, not an ability grant of any kind. Running the fix against the real Templars export
(before this task list's own verification step) surfaced this immediately: every weapon's own
keyword rule-references leaked into `Datasheet.Abilities` as bogus unit-wide entries ("Sustained
Hits", "Anti", "Melta", "Rapid Fire", "Blast" all appearing on the Impulsor).

**Fix:** reuse the exact same ancestry signal `ProcessAbilityProfile` already uses to distinguish
Intrinsic from optional (`ancestry[^1].Type == "upgrade"`) - a `"type": "rule"` infoLink reached
while the nearest ancestor entry is a `"type": "upgrade"` entry is skipped entirely (not even
routed to `OptionalAbilities`, since it isn't an ability grant to begin with). This is not
speculative future-proofing; it directly fixes a confirmed false-positive found by running the
actual fix against real data, which the openspec proposal/specs process alone (no fixture, no
corpus scan by itself) would not have caught - the corpus scan checks `infoLink.Type` values in
the abstract, not whether a given occurrence should produce a `Datasheet`-level ability. A dedicated
unit test fixture (a weapon-option entry carrying its own resolvable "type: rule" infoLink) now
guards this permanently.

### Corpus scan scope: every distinct `infoLink.Type` value in the corpus, not a broader "any
ability shape" sweep
The bug this change fixes is specifically "an infoLink's `type` value wasn't recognized." The other
three known ability shapes (nested `Abilities` profile, upgrade-nested profile, `infoLink
type:"profile"`) aren't at risk of a *silent* gap the same way: `ProcessProfile` already switches
on every profile it's handed regardless of `TypeName` (an unrecognized `TypeName` is a no-op, not a
crash, and isn't ability-shaped in the first place), and `entryLinks` route structurally (by
whether `targetId` matches the id-index or the group-index), not by a `type` string that could grow
new values BSData authors invented and this loader never learned about. `infoLinks[].type` is the
one place a new value can appear in real data going forward and be *silently* dropped exactly the
way `"rule"` was - so the scan targets that one axis precisely, walking every closure's own
`infoLinks` (at every level - entry, group, nested) across the whole local corpus, collecting the
distinct `Type` values seen, and asserting that set matches a maintained allowlist (`"profile"`,
`"rule"` as handled; anything else recorded and requiring a deliberate allowlist decision) via the
existing bidirectional-allowlist mechanism `bsdata-corpus-scan` already has. This bypasses
`BuildDatasheet` and walks the raw closure directly, the same way `WeaponKeywordScanTests` already
does for `Keywords` tokens - `BuildDatasheet` has no way to report "I saw this infoLink and did
nothing with it," so the scan needs to inspect the JSON shape itself, not the mapper's output.

**Alternative considered:** a broader scan trying to detect any not-yet-modeled way BSData could
express "grants an ability," beyond just infoLink types. Rejected as unbounded and not what
actually caused this bug or the prior Enhancement one - both were narrow, structural discrimination
gaps (a `type` field value; a "which selection group owns this" classification), not a case of
missing an entirely novel container shape. Scoping the scan to the precise axis that has now bitten
this project twice (once via `resolve-enhancement-abilities`'s Enhancement/OptionalGrant split
being a `type`-adjacent classification gap, now via `infoLink.Type`) gives real, checkable
confidence without inventing speculative future-proofing.

## Risks / Trade-offs

- **[Risk]** A Core rule's text is sometimes very long (Templar Vows' full text lists five Vow
  options) and will now render inline in the Unit Abilities column exactly like any other ability,
  with no summarization. → **Mitigation:** none needed structurally - this is the same
  popover-on-tap mechanism (`rules-glossary-popovers`) already used for every other ability's full
  text; nothing about this ability is longer than what that mechanism already handles for other
  abilities with substantial text.
- **[Risk]** Every Adeptus-Astartes-keyword unit in a roster will now repeat the identical Oath of
  Moment text as its own separate ability entry (no cross-component dedup). → **Mitigation:** this
  matches existing, accepted behavior for every other component-scoped ability
  (`AttachedUnitAggregateView`'s documented "No cross-component combination or deduplication"
  stance) - not a new problem this change introduces, and consistent with how the real tabletop
  rule genuinely does apply per-unit, not once per army.
- **[Risk, found during the corpus scan required by this change]** A third, previously-unknown
  `infoLink` type, `"infoGroup"`, was found by the new scan (129 occurrences, e.g. Adeptus Custodes'
  "Talons" - a real ability-granting gap of the same class this change fixes for `"rule"`).
  → **Mitigation:** deliberately NOT fixed here, per user decision - allowlisted in
  `InfoLinkTypeAllowlist.cs` as a confirmed, tracked gap rather than widening this change's scope
  blind, since no real `infoGroup`-carrying export was available to verify a fix against at
  implementation time. Tracked as a dedicated follow-up change once one is (a friend's Adeptus
  Custodes export, expected 2026-08-27) - the same "find, allowlist, defer" discipline
  `WeaponKeywordAllowlist`'s 45 unfixed tokens already established for this project.
- **[Trade-off]** `CoreRule` lumping universal and faction-specific rules into one origin means
  `/LivePlay` cannot later distinguish "Core Ability" from "Faction Ability" sections (matching
  GW's own app's two-tier display) without a further change. Accepted for now per the user's own
  stated framing that these are conceptually just abilities; splitting can be revisited later
  without a breaking model change if GW-app-parity becomes a goal (see `.claude/vnext-ideas.md`
  candidate).
