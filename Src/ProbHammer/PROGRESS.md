# Work Journal

## Active Work

Nothing in progress.

---

## Recently Completed

- OpenSpec change `import-battlescribe-json-rosters` implemented (22/22 tasks) — a second,
  independent import pipeline for BattleScribe/NewRecruit roster JSON exports
  (`roster.xmlns == "http://www.battlescribe.net/schema/rosterSchema"`), for players who use
  NewRecruit instead of the (paywalled-per-book) official GW app. Unlike the GW-app text pipeline,
  this JSON is already fully resolved — statlines, weapon profiles, ability/rule text, and the
  Leader/Support/Bodyguard attachment relationship are all present inline — so the new mapper
  (`Domain.Import.BattleScribe.BattleScribeRosterMapper`) synthesizes `Datasheet`/`Statline`/
  `WeaponProfile`/`Ability` directly from the JSON's own `profiles`/`rules`, bypassing
  `Domain.Catalogue.Bsdata` entirely — no BSData catalogue lookup, no name-mismatch risk. Analyzed
  against one real captured sample (`data/gw-app-export-templars.json`, the existing Templars
  fixture round-tripped through NewRecruit) in detail before implementing; see
  `.claude/domain-model-11e.md`'s new "BattleScribe/NewRecruit JSON Import Pipeline" section for
  the full field-mapping detail. Key findings from that analysis: attachment resolves via a
  structural `associations: [{to, name: "Leading"|"Supporting"}]` field (no role-text parsing);
  multi-model squads are already split into per-loadout sub-selections, each carrying its own count
  and weapon list (eliminating the text pipeline's partition-ambiguity bug class entirely); a
  wargear selection is tagged as a selected Enhancement via a distinct `costs["Enhancements"]`
  entry rather than the text pipeline's group-name substring match — and, since a roster JSON only
  ever contains selections the player actually made, this pipeline structurally cannot reproduce
  `resolve-enhancement-abilities`' original bug (an available-but-unselected Enhancement leaking
  onto every eligible unit). `ISessionArmyListStore`/`IArmyRosterProvider` changed from being
  hard-typed to `ParsedArmyList` to a small format-discriminated wrapper (`StoredArmyImport`, with
  `TextArmyImport`/`BattleScribeArmyImport` variants) so `/Import` can route to either pipeline
  while `/LivePlay`/`LivePlayCasualtyService` stay unchanged beyond the parameter type — validated
  by a real end-to-end test asserting both pipelines render the same real Templars list
  equivalently. **Deferred / unverified, both flagged in the change's own design.md Risks**: (1)
  invulnerable-save caveat resolution for this format was implemented by analogy to the BSData
  mapper's own two naming conventions, but the one real sample in hand has no caveated InSv to
  verify it against; (2) the real sample's own attachment groups are only 2-member and Leader-only
  shapes — no 3-member group exists in it, so that specific shape (mentioned as an aspiration in
  the change's own tasks.md) remains unverified. Both should be confirmed against a second,
  independently-sourced roster JSON (a different faction, ideally with a caveated invulnerable save,
  a vehicle, and a 3-member attachment group) before this pipeline is considered fully robust — see
  `.claude/vnext-ideas.md` if a further follow-up change is warranted once such a sample turns up.

- OpenSpec change `harden-army-list-parsing-for-android-exports` implemented (12/12 tasks) —
  `ArmyListParser` was only ever verified against iOS-captured exports; two real Android exports
  (`data/gw-android-export-custodes.txt`, `data/gw-android-export-deathguard.txt`, both relayed
  over WhatsApp) failed to parse at all. Investigated against both real files and fixed three
  independent, mechanical causes: **(1) case sensitivity** — the `Points` cost suffix and the
  `ATTACHED UNITS`/`Attached unit N` markers are now matched case-insensitively (`NamePointsRegex`,
  `AttachedUnitGroupRegex`); the four top-level section headers and `Detachment Points` were left
  as-is since both samples already render those identically to iOS. **(2) bullet-continuation
  rule** — Android drops the nested `◦` glyph entirely: a weapon list's own first item (at any
  depth) re-adopts `•`, and every later item in that same list carries no bullet at all, relying
  purely on indentation. `CollectBulletBlocks` now reads each line's own leading-indent count
  (tokenizing no longer fully strips it — see `ArmyListParser.Line`) to tell a nested item, a
  continuation, and a genuine next unit/section header apart, applied uniformly with no
  depth-specific branching (full reasoning in `CollectBulletBlocks`' own doc comment and this
  change's design.md). **(3) `ForceDisposition` is now optional** — a third finding, surfaced only
  while verifying the Death Guard file end-to-end after (1)-(2): its preamble omits that line
  entirely (goes straight from the Detachment line to the BattleSize line). Confirmed with the user
  before implementing, since it fell outside the two causes this change was originally scoped to.
  Deliberately deferred: a real, confirmed partition ambiguity (Adeptus Custodes' Custodian Guard —
  once the continuation rule flattens its three weapon-count lines into one list, the existing
  "counts must sum to the total" partition rule still can't resolve `1x Guardian spear` / `3x
  Praesidium Shield` / `3x Sentinel blade` against a total of 4, since the correct reading pairs the
  latter two as one loadout and text structure alone can't distinguish that from Company Veteran's
  genuinely-independent same-count alternatives) — left failing loudly, same diagnostic as before,
  not guessed at. Also open: whether the bullet-continuation shape and the missing
  `ForceDisposition` line are native to the Android app or introduced by WhatsApp's own text
  handling — pending a directly-saved (non-WhatsApp) Android export (~2026-08-27).
- OpenSpec change `gate-and-dedupe-core-rule-abilities` implemented (all 21 tasks) — fixes two real
  problems found while discussing `resolve-core-rule-abilities` (below) with the user: **(1)
  missing chapter/game-mode gating.** A `CoreRule` ability's own target rule can carry chapter/
  sub-faction exclusivity (`scope: "primary-catalogue"`, confirmed real: Oath of Moment's rule
  definition is hidden unless the army is one of 11 named chapters, Black Templars deliberately
  excluded; Templar Vows' is hidden unless it specifically IS Black Templars, by catalogue id) or
  game-mode exclusivity (`scope: "force"`, e.g. Scouts hidden during Boarding Actions missions) -
  `resolve-core-rule-abilities` never read a rule's own `hidden`/`modifiers` at all, so a Black
  Templars roster showed both `Oath of Moment` and its own chapter-exclusive replacement `Templar
  Vows` together; running the same pipeline against a Salamanders closure produced the identical
  (and therefore equally wrong) list. Fixed by generalizing `BsdataDatasheetMapper
  .IsGameModeGated`'s existing condition-evaluator (previously force/roster-only, entry/group-
  scoped) to also recognize a `primary-catalogue` condition (evaluated with its own comparison
  type against the closure's own starting catalogue id - no name lookup needed, the condition's
  `childId` already IS the target catalogue's own literal id) and to accept a rule's own modifiers
  as input (`BsRule.Modifiers` was previously silently dropped). Found and fixed a real bug while
  wiring this up: the method's pre-existing early-return guard short-circuited before ever
  reaching the new check whenever no `forceEntries` were supplied - existing `GameModeGatingTests`
  confirmed no regression to the original behavior. **(2) Army-wide Core rules duplicated per
  component instead of deduplicated.** Every component of an `AttachedUnit` independently
  references the same underlying rule (confirmed: identical `targetId` across every reference), so
  `Templar Vows` rendered as 3 separate, textually-identical entries on a 3-component attached
  unit. Fixed with a narrow, identity-matched exception (by `Name`, `CoreRule`-origin only) to
  `attached-unit-tracker`'s existing "no cross-component combination" rule (itself a deliberate
  choice from `add-live-play-ability-columns` - this change's design.md explicitly addresses why
  the narrow exception here doesn't reopen that change's original problem, unconditional same-
  Scope merging). Rendered per the user's own hand-drawn ASCII mockup: the deduped ability gets its
  own dedicated grid row above every component's statline rows (a third placement, distinct from
  row-bound and component-wide), implemented via a `rowOffset` shifting every other row down by
  one when present. Collapse-on-death falls out for free from the view's existing per-request
  rebuild (a fully-dead component simply stops contributing on the next rebuild) rather than
  needing separate state tracking. Verified: new `PrimaryCatalogueGatingTests` (fixture-based, both
  `instanceOf`/`notInstanceOf` directions plus an unrecognized-shape safety check),
  `ChapterExclusiveCoreRuleRegressionTests` (explicit-only, live clone, the exact real Black
  Templars-vs-Salamanders case), new `AttachedUnitAggregatorTests`/`LivePlayAbilityRenderingTests`
  coverage for the dedup+span behavior; full suite green (356 total, 350 run + 6 explicit-only);
  `docker compose up` + a real Firefox session against the real Templars export confirmed the
  rendered layout matches the user's own mockup exactly (screenshot compared side-by-side), `Oath
  of Moment` gone, everything else unaffected. Docs: `.claude/domain-model-11e.md`'s Core Rule
  Ability Extraction and `IsGameModeGated` sections updated; `.claude/vnext-ideas.md`'s "Ability-
  text interpretation pass" entry gained detailed notes on the related but explicitly out-of-scope
  "classifier" ideas discussed alongside this change (Model/Unit scope classification, dropping
  Leader/Support/Attached Unit, Shield Dome-to-InSv conversion, a toggleable rule-pipeline
  architecture) so they aren't relitigated from scratch when that work starts.
  **Post-delivery correction, found via user feedback after the above was verified**: two real
  gaps, fixed within this same change. (1) The "shared by 2+ present components" dedup trigger was
  the wrong signal — `Templar Vows` got its own box on a multi-component `AttachedUnit` but
  rendered as a plain inline entry on a standalone `Unit` (Impulsor/Scout Squad), even though it's
  just as much an army-wide fact there. Fixed by adding `AbilityOrigin.ArmyRule` (chapter-exclusive
  Core rule, distinguished from plain `CoreRule` by a structural presence check —
  `HasPrimaryCatalogueScope` — on the rule's own gating, not a contributor headcount);
  `AttachedUnitAggregator`'s promotion now triggers on `Origin == ArmyRule` unconditionally, even
  for a single contributor. This also revisits, correctly given new evidence, `resolve-core-rule-
  abilities`' own earlier argument against splitting `CoreRule` by chapter-exclusivity. (2) Shield
  Dome was **still** invisible on Impulsor — the original single-row-component collision (found
  earlier this session, its fix-or-defer question lost when the conversation moved to the mockup
  discussion) was never actually addressed by this change's own `rowOffset` work, which only added
  a *new* row for the multi-component case. Fixed by having `BuildComponentAbilitySpans` absorb a
  single-row component's row-bound abilities into its own component-wide span instead of rendering
  both as separate, identically-coordinated grid cells. Verified: updated
  `AttachedUnitAggregatorTests`/`PrimaryCatalogueGatingTests`/`LivePlayAbilityRenderingTests`; full
  suite green (360 total, 354 run + 6 explicit-only); `docker compose up` + Firefox against the
  real export confirmed both fixes (Impulsor now shows `Templar Vows` in its own box *and* `Shield
  Dome` alongside `Transport`/`Assault Vehicle`/etc., all visible) with no regression to the
  already-verified `AttachedUnit` rendering. This change's own `design.md`/`specs/` deltas updated
  to reflect the corrected behavior rather than leave the superseded language in place.

- OpenSpec change `resolve-core-rule-abilities` implemented (all tasks): a fourth,
  previously-unhandled BSData ability-sourcing shape — an `infoLink` with `"type": "rule"`
  (Oath of Moment, a Chapter's own Vows, Deadly Demise, Firing Deck, Infiltrators, Scouts; GW's
  own app shows these as "Core"/"Faction Abilities", NewRecruit as "Rules") — was silently
  dropped by `BsdataDatasheetMapper.WalkInfoLink`, deliberately, per its own prior comment. Found
  via a real user-reported bug against `data/gw-app-export-templars.txt`: every unit in a Black
  Templars roster was missing at least `Templar Vows`. `WalkInfoLink` now resolves the linked rule's
  text via `RuleGlossary` (the same glossary `rules-glossary-popovers` already built) and builds the
  display name from the infoLink's own name plus any `"append"`/`field:"name"` modifier value
  (string- and number-valued, both real shapes — `"Deadly Demise"` + `"D3"`, `"Firing Deck"` + `6`).
  New `AbilityOrigin.CoreRule` — always-exposed like `Intrinsic`, never on-demand, since a Core rule
  is never a player selection. **Real defect found and fixed during implementation, not anticipated
  by the plan**: a `"type": "rule"` infoLink also appears nested inside a weapon's own
  `"type": "upgrade"` wargear-option entry (the Impulsor's "Ironhail Skytalon Array" carries its own
  "Sustained Hits"/"Anti" keyword cross-references) — the first working version leaked these into
  `Datasheet.Abilities` as bogus unit-wide entries; fixed by reusing the same ancestry signal
  `ProcessAbilityProfile` already uses to exclude an optional ability (nearest ancestor's own
  `Type == "upgrade"`), caught only by running the fix against the real export before shipping, not
  by any planned test. New `InfoLinkTypeScanTests` (full-corpus scan, same `[Fact(Explicit = true)]`
  pattern as the other `CorpusScan/` tests) walks every `infoLink` in the local clone and asserts its
  `Type` value is one of a maintained allowlist — found a third, genuinely new type, `"infoGroup"`
  (129 occurrences, e.g. Adeptus Custodes' "Talons" — confirmed real ability-granting content, same
  class of gap), deliberately allowlisted rather than fixed (no `infoGroup`-carrying export was
  available to verify against) — see Known Issues below. Verified: 9 new `CoreRuleAbilityTests`
  (fixture-based, including a dedicated regression test for the weapon-nested false-positive); full
  suite green (339 total, 335 run + 4 explicit-only); `InfoLinkTypeScanTests` and every other
  explicit-only corpus scan re-run manually and clean; `docker compose up` + a real Firefox session
  against the real Templars export confirmed every expected ability renders correctly (including
  `Templar Vows`'s popover showing the real, correct rule text) with `Shield Dome` and the
  Enhancement `✦` indicator both unaffected. One genuine BSData data fact surfaced along the way,
  not a bug: Black Templars' own local "Impulsor" override declares Deadly Demise/Firing
  Deck/Templar Vows but not Oath of Moment, unlike the generic Space Marines Impulsor it overrides.
  Docs: `.claude/domain-model-11e.md`'s "BSData JSON Ingestion" section gained "Core Rule Ability
  Extraction" and "Full-Corpus InfoLink-Type Scan" subsections.

- OpenSpec change `restyle-rule-popover-titlebar` implemented (6/6 tasks): every rule/ability
  popover (ability names, weapon-keyword chips, nested `[BRACKET]` references) gained a title bar
  above its body text, naming whatever was tapped to open it. `_UnitBlock.cshtml`'s
  `BuildRulePopover` reuses its existing `triggerHtml` parameter verbatim for the new
  `.rule-popover-title` `<div>` — no new plumbing, and the `✦` prefix from
  `display-resolved-enhancements` carries through into the title automatically. The title bar's
  CSS matches `.lp-section` summary's own section-title style (dark filled bar, uppercase, bold);
  `.rule-popover`'s own padding moved onto `.rule-popover-text` so the title bar can sit flush at
  the panel's top edge, and the title is `position: sticky` so it stays visible while a long
  popover's body scrolls. Verified: full suite green (297 total); manually confirmed via `dotnet
  run` + the real templars export that ability, weapon-tag, and Enhancement popovers all show a
  correctly-styled title bar (the Enhancement's showing "✦ Oathbound Exemplar"). Docs:
  `.claude/design-tokens.md`'s Popovers section updated.

- OpenSpec change `display-resolved-enhancements` implemented (11/11 tasks): `Unit.Enhancements`
  (resolved by `resolve-enhancement-abilities`, archived below) was never read by any rendering
  code — an applied Enhancement was invisible on `/LivePlay`. `AttachedUnitAggregator.BuildAbilities`
  now reports each present component's own `Enhancements` the same way it already reports
  `Datasheet.Abilities` (component-wide, `StatlineName: null`) — no new domain shape needed, since
  `Ability.Origin` already distinguishes an Enhancement from every other ability. `_UnitBlock.cshtml`
  gained an `AbilityDisplayName` helper prefixing an Enhancement-classified ability's rendered name
  with `✦ ` wherever it renders (both ability columns; the popover title bar, once
  `restyle-rule-popover-titlebar` lands, reuses the same prefixed name for free). An Enhancement
  renders in the Unit Abilities column unconditionally for now — `Ability.Scope` is already always
  `Unit` for every BSData-resolved ability (no per-ability scope signal in the source data), so this
  isn't new scope logic, just the pre-existing default. A genuine per-Enhancement Model/Unit scope
  is deferred to the future ability-text classification pass, noted in `.claude/vnext-ideas.md`.
  Verified: new `AttachedUnitAggregatorTests` (Enhancement reported/disappears-when-dead/absent-when-
  none-applied) and new `LivePlayAbilityRenderingTests` (real-Razor-path render confirming the `✦`
  prefix appears only on an Enhancement-origin ability); full suite green (297 total); manually
  re-verified via `dotnet run` + the real templars export — `/LivePlay` now shows "✦ Oathbound
  Exemplar" on the Marshal. Docs: `.claude/domain-model-11e.md`'s Aggregate Ability View description
  updated.

- Post-implementation bug fix on `resolve-enhancement-abilities` (no new OpenSpec change at the
  time - the underlying `IsGameModeGated` method predates that change; this fix generalized it,
  folded into the same change before it was archived): user added a real Enhancement (`Enhancement(s): Oathbound
  Exemplar`, on a Marshal using the "Companions of Vehemence" Detachment) to
  `data/gw-app-export-templars.txt` and importing it failed with "Datasheet 'Marshal' has no
  Enhancement named 'Oathbound Exemplar'." Traced (via the same instrument-and-print approach used
  throughout this investigation) to `IsGameModeGated`'s original design: "a plain existence check
  across nested conditionGroups, not real AND/OR boolean evaluation." Black Templars' "Companions of
  Vehemence Enhancements" - a real, always-available Detachment Enhancement pool (Oathbound
  Exemplar, Incendiary Animus, Merciless Denunciation, Zealous Vanguard) - is hidden by `AND(forces
  (Crusade Force) < 1, selections(some other id) < 1)`, i.e. "hidden UNLESS you have a Crusade Force
  OR this Detachment selected" - the existence check treated the mere presence of the Crusade Force
  reference as sufficient to exclude the whole pool, regardless of the AND's other, unevaluated
  branch, incorrectly hiding a real matched-play Enhancement for exactly the players using that
  Detachment.
  Fixed generally rather than allowlisted around: `IsGameModeGated` now calls a new
  `IsProvablyAlwaysTrueInMatchedPlay`, real (if still narrow) 2-value boolean evaluation over the
  condition tree - a gate-id condition combined via AND with an unrelated, unevaluated condition is
  no longer treated as gated (that other condition could make the whole AND false); combined via OR,
  it still is (OR only needs one always-true branch). Both previously-confirmed real shapes (Chaos
  Boons' single flat condition, Mark of Chaos' single-condition "and" group) still evaluate
  correctly under the new logic - neither was a coincidence of the old check, both are genuinely
  "every branch provably true."
  Verified: a new unit test confirms `Marshal.TryResolveAbility("Oathbound Exemplar", ...)` now
  succeeds; full test suite (292 tests) and all 3 permanent full-corpus scans re-run against the
  live BSData clone still pass unchanged - confirming the stricter evaluation doesn't silently
  un-gate any of the content the original check correctly excluded. Manually re-verified via
  `dotnet run` + `curl`: the updated export now imports successfully with no error. Note: at the
  time of this fix, the resolved Enhancement didn't yet visibly render on `/LivePlay` - wiring
  `Unit.Enhancements` into the page's rendering was the explicitly deferred future step from
  `resolve-enhancement-abilities`'s own design.md; `display-resolved-enhancements` (above) has
  since closed that gap. Docs:
  `.claude/domain-model-11e.md`'s `IsGameModeGated` description updated to describe the new
  evaluation and this second real-world finding.

- OpenSpec change `resolve-enhancement-abilities` implemented (all 23 tasks) and archived: a
  real user-reported bug — after importing a real Black Templars export
  (`data/gw-app-export-templars.txt`), `/LivePlay` showed "Thirst for Glory" on the Crusade Ancient
  (a Space-Wolves-only Enhancement, never selected) and both "Fervent Exemplars" and "Inheritors of
  Sigismund" on the Sword Brethren Squad (two real Black Templars Enhancements, neither selected,
  on a unit that isn't even a Character eligible for an Enhancement at all). Root cause:
  `BsdataDatasheetMapper.BuildDatasheet`'s walk materialized *every* `"Abilities"`-typeName profile
  it found into the public, always-rendered `Datasheet.Abilities` — with no distinction between an
  intrinsic fact about a datasheet and an optional, player-selectable ability nested inside its own
  `"type": "upgrade"` selection entry (the exact same shape a weapon option uses).
  `WeaponProfile` already had on-demand-only protection against this (`ResolveWeaponProfile`/
  `TryResolveWeaponProfile`, never enumerated in bulk); `Ability` never got the same treatment.
  **A first hypothesis (BSData "primary catalogue"/detachment gating) was investigated and
  explicitly rejected** once the Sword Brethren Squad case surfaced: that content is genuine,
  correctly-scoped Black Templars material on the right catalogue — it simply wasn't selected in
  the export. The real, general fix had to be "never surface an optional ability unless the export
  actually chose it," not "filter by which catalogue/detachment defines it."
  Fix: `Datasheet` gained a second, on-demand-only ability index (`TryResolveAbility`/
  `OptionalAbilityNames`, mirroring the weapon-profile shape exactly) alongside the now
  intrinsic-only public `Abilities` list. `BsdataDatasheetMapper` classifies an ability during the
  walk by the nearest owning entry's own `Type` (`"upgrade"` → optional, routed to the new index;
  anything else → intrinsic, as before) and, for optional abilities, further classifies
  `Enhancement` vs. a plain `OptionalGrant` by whether the nearest enclosing selection group's Name
  contains `"Enhancements"` (a case-insensitive *substring* match, confirmed necessary against a
  real nested sub-pool — "Legends of Saga and Song Enhancements" — that isn't an exact-name match).
  New `AbilityOrigin` enum (`Intrinsic`/`Enhancement`/`OptionalGrant`) added to `Ability`, laying
  domain groundwork for a future `/LivePlay` visual distinction (not built in this change).
  `ArmyRosterEnricher`'s existing wargear-name-falls-back-to-an-ability resolution (how Impulsor's
  "Shield Dome" has always resolved) repoints onto the new on-demand index — confirmed via a real
  regression this exposed mid-implementation: a hand-built test fixture had "Shield Dome" sitting
  directly on its owning entry's own `Profiles` (not nested inside a `"type": "upgrade"` child the
  way the real corpus always shapes it), so under the new intrinsic/optional split it briefly
  reclassified as intrinsic and broke that fixture's own test — fixed by correcting the fixture to
  match the real shape, not by loosening the new rule. New `ArmyRosterEnricher.ResolveEnhancements`
  step resolves each parsed Enhancement name against the same on-demand index and attaches the
  resolved `Ability` to the `Unit`, replacing the previously inert raw-string
  `Unit.Enhancements: IReadOnlyList<string>` (**BREAKING**, confirmed unused by any other code path
  before this change) with `IReadOnlyList<Ability>`.
  A pre-existing `GameModeGatingTests` test (`GatedContent_IsIncluded_WhenNoForceEntriesAreProvided`)
  needed updating, not because it was wrong, but because its fixture's gated abilities are — like
  real Chaos Boons/Mark of Chaos options — *also* genuinely `"type": "upgrade"`-shaped: once not
  gated out, they now correctly land in the on-demand index rather than the public `Abilities` list
  regardless of gating, which is orthogonal to what that test file is actually about.
  292 tests, all passing (11 new, across a new `AbilityClassificationTests.cs` — a hand-built
  closure fixture confirming all four classification outcomes including the substring-match nested
  case — new `ArmyRosterEnricherTests` Enhancement-resolution cases, new `DatasheetTests` on-demand
  ability cases, and a new `EnhancementAbilityLeakRegressionTests.cs` reading the real captured
  export against the real bundled BSData snapshot end-to-end), including all 3 permanent full-corpus
  scan tests re-run explicitly against the live BSData clone (confirms the ancestry/group-name
  threading change didn't regress anything corpus-wide). Manually verified via `dotnet run` + `curl`
  (cookie jar + antiforgery token, mirroring `ImportFlowTests`'s own approach): re-imported the real
  export, confirmed zero occurrences of all three leaking ability names in the rendered `/LivePlay`
  HTML, while Crusade Ancient, Sword Brethren Squad, Impulsor's Shield Dome, and the genuine
  intrinsic abilities all still render correctly. Docs: `.claude/domain-model-11e.md` updated
  (`Datasheet`'s on-demand ability API, the new `AbilityOrigin` shape, `BsdataDatasheetMapper`'s
  classification mechanism, `ArmyRosterEnricher`'s repointed wargear fallback and new Enhancement
  resolution step, `Unit.Enhancements`'s new element type).

- OpenSpec change `rules-glossary-popovers` implemented (all 32 tasks), archived, and synced to
  main specs (new `rules-glossary` capability spec; `live-play-view` modified). `/LivePlay` now
  surfaces the ability/weapon-keyword rule text BSData carried all along but nothing ever
  read — every ability name and resolvable weapon-keyword chip is a native `popover="auto"` tap
  trigger showing that rule's full text, with a resolved `[BRACKET]` cross-reference inside that
  text itself becoming a further nested trigger (confirmed live to 3 levels deep on real data).
  New domain types (`ProbHammer.Core.Domain.Catalogue.Bsdata`): `RuleDefinition`/`RuleGlossary`
  (built from BSData's previously-unread `sharedRules`/`rules` arrays, exposed as
  `ResolvedBsdataCatalogue.Glossary`), `RuleTextTokenizer` (`[BRACKET]` extraction with
  markup-char/Unicode-whitespace-hyphen normalization), `RuleTextEmphasisRenderer` (a pure,
  delegate-based parser rendering `*italic*`/`**bold**`/`^^small-caps^^` markup, including the
  nested `***bold+italic***` split-close case, as styling only). Full architecture documented in
  `.claude/domain-model-11e.md`'s new "Rules Glossary & Popovers" section.
  - `RuleGlossary`'s resolution went through two superseded designs (exact-match-only, then an
    `Alias`-only fallback) before landing on a single normalized-key index built from both `Name`
    *and* `Alias` — the full-corpus bracket-token-resolution scan (new
    `BracketTokenResolutionScanTests`) dropped from 1,964 unresolved occurrences to exactly 2, both
    confirmed one-off BSData authoring anomalies.
  - Real bugs found and fixed during implementation, not just in planning: a weapon-keyword chip
    nested inside `.weapon-name-toggle` let a chip tap bubble up and also toggle the weapon's
    contribution breakdown (user-flagged during review, confirmed live, fixed by making the chip a
    sibling instead — the only valid fix, since a `<button>` can't validly contain another); several
    real BSData rules (`Sustained Hits`, `Anti`, `Cleave`) reference their own name in their own
    description text, which stack-overflowed an early version of the nested-popover renderer (fixed
    with an ancestor-chain guard, documented as a new spec scenario since real data uncovered a gap
    neither the proposal nor design.md anticipated).
  - CSS anchor positioning (`anchor-name`/`position-anchor`, `@supports`-gated) opens/dismisses
    correctly on the real target browser tested (Firefox 154) but doesn't yet position adjacent to
    its trigger there — a genuinely incomplete implementation (`CSS.supports()` says yes, the rule
    matches in the CSSOM, but `getComputedStyle` still resolves `top:0;left:0`), not an absent one;
    left as-is per this change's own explicit no-polyfill policy, since the popover still opens and
    is fully usable either way.
  - 275 tests passing (9 in a new `RuleTextEmphasisRendererTests`, plus new `RuleGlossary`/
    `RuleTextTokenizer` test suites and fixtures), full solution build clean.

- Post-implementation bug fix on `import-army-list-for-live-play` (no new OpenSpec change): a real
  user-reported bug after importing the Masters of the Maelstrom export - `/LivePlay` showed a pile
  of unrelated abilities (Chaos Boons like "Warp Stalker"/"Mutant Form", all four "Mark of Chaos"
  options) on every unit's ability list, none of them real matched-play rules. Root cause:
  `BsdataDatasheetMapper`'s tree walk collects every "Abilities"-typeName profile it finds,
  regardless of whether BSData itself marks that content hidden outside "Army Roster" (matched
  play) mode - a shape this loader had never needed to recognize before, since no prior fixture or
  captured export happened to reach this kind of content. Traced by hand through the live BSData
  clone (confirmed via NewRecruit's own UI too: "Mark of Chaos" only appears when building a
  "Crusade Force," never an "Army Roster") down to the exact modifier shape BSData uses -
  `IsGameModeGated` in `BsdataDatasheetMapper.cs` (see `.claude/domain-model-11e.md`'s "Army List
  Import Pipeline" section for the full mechanism) now recognizes it, resolved by name (never a
  hardcoded id) against the game system's `ForceEntries`. New `GameModeGatingTests.cs` (5 tests,
  hand-built fixture) locks in the fix; first draft of this fix used name-matching on the "Crusade"
  entryLink specifically, caught two related-but-differently-named cases ("Crusade Relic Upgrades,"
  "Mark of Chaos") it missed, and was replaced with the general structural check instead of adding
  more names to a list. 237 tests, all passing.

- OpenSpec change `import-army-list-for-live-play` implemented (all 27 tasks, not yet archived):
  `/LivePlay` now renders a real, user-pasted GW-app 11e army-list export instead of the
  `Examples/View.Roster()` fixture — the project's stated core purpose finally closes. New pipeline:
  `text → IArmyListParser → ParsedArmyList → ArmyRosterEnricher (against a ResolvedBsdataCatalogue)
  → ArmyRoster → /LivePlay`, with per-session storage (ASP.NET Core Session, reintroduced) holding
  only the `ParsedArmyList` - every request (including every casualty tap) re-enriches fresh, one
  level up from `casualty-tracking`'s existing "rebuild from source of truth" precedent.
  New `ProbHammer.Core.Domain.Import` namespace: `ParsedArmyList`/`ParsedAttachmentGroup`/
  `ParsedUnit`/`ParsedModelGroup` records plus `ArmyListParser` (line-based, classifies by bullet
  character - `•` top-level, `◦` nested - not indentation depth). The model-group weapon-count
  partition algorithm (shared-vs-alternating split) turned out to need one refinement beyond the
  literal spec text, confirmed against a second real squad (`gw-app-export-3-dp.txt`'s Company
  Veteran): each non-common weapon is its own partition, never merged with another weapon that
  happens to share the same count - "1x Master-crafted bolt rifle" / "1x Master-crafted heavy
  bolter" are two distinct 1-model sub-groups, not one sub-group carrying both.
  New `ArmyRosterEnricher` (`Domain.Roster`) plus `BsdataFactionResolver`/`BsdataCatalogueCache`/
  `ResolvedBsdataCatalogue`/`BsdataNameSuggestion`/`BsdataNameNormalization` (`Domain.Catalogue.
  Bsdata`, alongside the existing `catalogue-json-ingestion` types). **The design.md decision that
  a model-group name resolves against BSData "directly, trivially" was verified only against one
  squad and did not generalize** - the one-off manual verification (task 2.8, against the live
  clone at `C:\Users\Pete\wh40k-11e`) surfaced five independent real mismatch patterns between
  export text and BSData names, each requiring a user decision mid-implementation (see
  `[[project_bsdata_name_mismatch_patterns]]` in auto-memory for the full writeup): a Datasheet's
  own name is sometimes itself one of several Statline names (Scout Squad, Legionaries, Eradicator
  Squad); a single-Statline Datasheet's one Statline is sometimes named nothing like the export
  label or the Datasheet name at all (Impulsor → "Black Templars Impulsor", Emperor's Champion →
  "The Emperor's Champion", Sword Brethren Squad → "Sword Brethren") - resolved via a two-tier
  fallback (Datasheet-name-as-Statline, then sole-Statline-if-only-one, both independently
  necessary); one wargear line can resolve to more than one BSData weapon profile (Helbrecht's
  "Sword of the High Marshals" → "➤ ... - Sweep" / "➤ ... - Strike"); some "weapon" lines are
  actually BSData Ability profiles, not weapons at all (Impulsor's "Shield Dome", a granted 5+
  invulnerable save) - now attached to the `ModelLine`'s own `Abilities` instead of thrown as
  unresolved; and one genuine BSData spelling difference in the corpus itself (export's "Absolvor
  bolt pistol" vs. BSData's own "Absolver bolt pistol", Garreon the Corpsemaster) - deliberately
  *not* special-cased (a curated mismatch map doesn't scale to the corpus size); instead every
  resolution failure now carries a Levenshtein-based "did you mean...?" suggestion
  (`BsdataNameSuggestion`) in its exception message, diagnostic only, never auto-applied.
  New `army-list-import` capability: `/Import` Razor Page (paste box + submit), `ISessionArmyListStore`
  (JSON round-trip of `ParsedArmyList` through `ISession`), `IArmyRosterProvider` (shared
  faction-resolve → cache-fetch → enrich orchestration, used by both `/Import`'s pre-commit
  validation and `/LivePlay`'s per-request rebuild). `Program.cs` reintroduces
  `AddDistributedMemoryCache`/`AddSession`/`UseSession` (removed by `archive-10e-pipeline`, brought
  back for this new purpose) and registers a `BsdataCatalogueCache` singleton over the bundled
  `src/ProbHammer.Web/BsData/` snapshot (path from new `Bsdata:RootDirectory` config, default
  `"BsData"`). `LivePlayModel`/`LivePlayCasualtyService` repointed off `Examples.View`
  entirely - `Examples/` stays in the tree, unreferenced by `Web`, as design.md anticipated.
  232 tests (up from 224), including a new `ImportFlowTests.cs` (real end-to-end HTTP flow:
  import→redirect, parse/enrichment failure reporting, a failed import leaving a prior successful
  one untouched, two concurrent `HttpClient`-cookie-jar sessions staying isolated, no-import→
  redirect, rebuild-fresh-per-request) using the real captured exports and the app's own bundled
  BsData snapshot rather than synthetic fixtures. Manually verified via `firefox-devtools-mcp`
  (`dotnet run`, no Docker) against all three captured exports end to end, rendering correctly.
  **Tooling note**: `WebApplicationFactoryClientOptions.HandleCookies` defaults to `true` in this
  package version, so a single `HttpClient` from `factory.CreateClient()` persists the ASP.NET Core
  Session cookie automatically across an `/Import` POST and a following `/LivePlay` GET within one
  test - no manual cookie-container wiring needed. The `/Import` page's `<form>` needed an explicit
  `asp-page="/Import"` attribute for the `FormTagHelper` to attach at all and auto-inject the
  antiforgery hidden field (a bare `<form method="post">` with no `asp-*` attribute isn't recognized
  by the tag helper, and Razor Pages' global `AutoValidateAntiforgeryTokenAttribute` convention
  rejects a POST with no token regardless).

- OpenSpec change `redesign-invulnerable-save-display` implemented and archived (all 26 original
  tasks plus two post-review refinement rounds — 7 tasks, then 8 more, 41 total — synced to
  `openspec/specs/live-play-view/spec.md` before archiving): `/LivePlay`'s
  invulnerable save now renders as its own tile in the same visual family as M/T/Sv/W/Ld/Oc (label
  "InSv"/"InSv*" on top, value below, inside one bordered box), positioned below the main stat-tile
  row and aligned under the Sv tile specifically via a `.statline-tiles` flex-wrap→CSS Grid change
  (`grid-column: 3`, matching Sv). No domain-model change at all —
  `InvulnerableSave`/`BsdataDatasheetMapper.ResolveInvulnerableSave` untouched throughout; this is a
  `_UnitBlock.cshtml`/`site.css`-only change. Five render shapes, the label always plain "InSv" (or
  "InSv*" caveated) and any attack-type icon riding inline next to the value it qualifies instead:
  uniform (bare value, no icon); melee-only/ranged-only (value + one icon); differing-both-known
  (two stacked rows inside the tile, ranged above melee, no separator — settled on this shape, over
  an earlier single-line-joined-by-`/` version, specifically because it needs no separator at all
  and because ranged-above-melee mirrors the page's own Ranged Weapons-before-Melee Weapons section
  ordering, itself following the game's shooting-before-melee phase order; no real BSData text
  produces this shape today, traced during design: a footnoted split always collapses to one
  duplicated digit, a parenthetical restriction always zeroes the other side, so this is
  forward-looking coverage of a value the type supports rather than real data); and caveated
  (off-color `--amber-tint` tile background, the linked `CaveatAbility`'s full text rendered in
  italics beneath the tile — the first and only place on this page rendering an ability's text
  rather than only its name, a deliberately scoped exception, not a step toward the still-deferred
  general full-text-on-demand idea in `vnext-ideas.md`).
  **Icon choice took several live iterations, then a direct re-confirmation**: ⚔ (melee) reads fine
  as plain Unicode text; the ranged glyph did not — ⌖, ◎, and ⊙ were each tried live and each read
  ambiguously in this browser/OS font stack (a diamond, an "info" icon, a plain dot), so ranged uses
  a small inline SVG crosshair instead, matching this file's own filter-flag icon precedent for the
  same underlying reason (font/emoji glyphs can't be trusted to render as intended across
  platforms). Re-proven a third time via a live side-by-side (⌖ vs. the SVG, identical tile styling)
  after the user asked why the SVG was needed at all rather than just taking the earlier reasoning
  on faith — the diamond reading held up.
  **Two real bugs found only by looking at the rendered page, both from the same root cause**: the
  first implementation's `.insv-row` spanned multiple grid columns to fit a horizontal
  "InSv vs ⚔ / [icon]" label, which (1) silently clipped the caveated case's ability-text line off
  the bottom of the card (`.statline-tiles`' pre-existing `max-height: 6rem`, sized for the old
  single-line layout, confirmed via a live DOM bounding-box check — fixed by raising it to `12rem`)
  and (2) bled its own width into every column it spanned, visibly bloating the W/Ld/Oc tiles'
  padding above it. Moving the label inside the tile and confining it to `grid-column: 3` alone
  fixed both at once. A third round of feedback, after even closer visual inspection, tightened
  `.stat-tile`'s own `padding`/`min-width` ("very spacious" — applies to all six stat-tiles, not
  InSv-specific) and replaced the differing-both-known case's single joined line with the final
  two-stacked-rows shape, chosen over an "icon-over-value + `/`" alternative by mocking both up live
  via temporary DOM injection (no source changes) before committing to either.
  Two new fixtures added to `Examples/Datasheets.cs`/`Units.cs`, both wired into
  `View.Roster()` (now nine units, up from seven): `HowlingBanshee()`, using real verified BSData
  values (`M8" T3 Sv4+ W1 Ld6+ OC1`, real InSv text `"4+* / 5+"`, real linked-ability text "Models
  in this unit have a 4+ invulnerable save against melee attacks." — traced to the base rules file's
  shared `"Invulnerable Save (4+*)"` ability, id `9fa6-3128-6c5b-55f6`) — a second, independently-
  sourced real caveated example alongside `CanisRex()` (left unchanged); and `TwinWardSentinel()`,
  an explicitly synthetic fixture (doc comment states plainly it is hand-constructed, not from real
  BSData) exercising the differing-both-known case no real corpus text can produce today. New test
  file `tests/ProbHammer.Tests/Web/LivePlayInvulnerableSaveRenderingTests.cs` covers all five render
  shapes by rendering `_UnitBlock.cshtml` directly through the real `IRazorPartialRenderer` path
  against hand-built `AttachedUnitAggregateView` input (**adjusted from the original task plan**,
  which assumed the existing `internal BuildUnitBlock` testing approach would suffice — it doesn't,
  since the InSv branching lives entirely in the `.cshtml`, not in any C# method `BuildUnitBlock`
  calls; its assertions were updated twice more across the refinement rounds, though the final
  stacked-rows change needed no assertion changes at all — the text content it asserts on was
  unaffected by the row-vs-line layout). Two pre-existing tests needed straightforward updates for
  the roster's new unit count/order (`ViewTests.Roster_UnitsMatchesMyArmyRoster`, `LivePlayModelTests
  .OnGet_OrdersAttachedUnitsBeforePlainUnits...`) — both confirmed the existing sort rule placed the
  two new units correctly, not a behavior gap. 180 tests, all passing (178 run + 2 explicit-only
  corpus scans skipped as expected) after every round. Visually verified via `firefox-devtools-mcp`
  against the real example army repeatedly across all three rounds: uniform (High Marshal
  Helbrecht), both caveated fixtures, the synthetic differing-both-known fixture, run-collapse
  (marked a casualty, watched the InSv tile collapse and reappear correctly alongside the stat-tile,
  no JS changes needed), plus two live mocked-up comparisons that needed no source changes at all —
  the glyph re-confirmation and a bare-uniform-vs-ranged-only side-by-side (raised by the user
  thinking ahead to a possible future where the still-deferred ability-text-interpretation pass
  flips a caveated save like Canis Rex's to genuinely ranged-only; already correctly distinct today,
  nothing to change). Docs: `.claude/design-tokens.md` updated (`--amber`'s row description, new
  `--amber-tint` token, and the invulnerable-save tile's typography folded into the existing scale
  description). `.claude/domain-model-11e.md` deliberately not touched — it covers the domain layer
  only and doesn't catalog individual `Examples/` fixtures, and this change has no domain-model
  surface at all.
- OpenSpec change `structured-invulnerable-save` implemented and archived (all 24 tasks):
  `Statline.InSv: int` replaced with a dedicated `InvulnerableSave { MeleeInSv, RangedInSv,
  Caveated, CaveatAbility }` value (`src/ProbHammer.Core/Domain/Catalogue/InvulnerableSave.cs`),
  resolving the parked "Known Issues" InSv representation gap. Self-contained raw text (a plain
  digit, a `"(Ranged)"`/`"(Melee)"` parenthetical) resolves directly; footnoted/ability-linked text
  (a bare `"*"`, or a `"/"`-split with one footnoted side) always resolves `Caveated = true` with a
  captured `Ability` reference — `BsdataDatasheetMapper` deliberately never reads what that
  ability's Description text *means*, only which specific one is linked (by id, never name alone —
  the real base catalogue has two different profiles both named `"Invulnerable Save (4+*)"` with
  opposite meanings). Classifying the caveated ability's text is a new, explicitly deferred vNext
  idea (`.claude/vnext-ideas.md`'s "Ability-text interpretation pass").
  **Two real architectural gaps found only by re-running the permanent corpus scan** (not
  anticipated by the change's own design — investigated, paused, and fixed generally rather than
  allowlisted around, per explicit user direction): (1) the base rules game-system file
  (`Warhammer 40,000.json`) — where the shared `"Invulnerable Save (X+*)"` abilities actually live
  — was never reachable at all, since every catalogue references it via a separate `GameSystemId`
  field, not a `catalogueLinks` entry `BsdataClosureResolver` follows; fixed by making one
  `BsCatalogue` type model both real JSON root shapes (`{"catalogue":...}` and `{"gameSystem":...}`)
  and resolving `GameSystemId` into a new `BsdataClosure.GameSystem` property, kept deliberately
  separate from the general `Files` list (a first, broader attempt — folding it into `Files`
  directly — regressed T'au weapon parsing, since it made previously-dangling `"Warlord"`/
  `"Enhancements"`/`"Crusade"` entryLinks newly resolve into generic template wargear this loader
  was never designed to map). (2) "owning entry" needed to become a full ancestry chain, not the
  immediate entry: Howling Banshees' squad carries its ability infoLink on the top-level entry
  while the footnoted `InSv` characteristic sits on a nested child model entry one level down.
  **A second real ability-naming convention was also found this way**: alongside the
  digit-parameterized `"Invulnerable Save ({digit}+*)"`, a generic `"*Invulnerable Save"` (no
  digit) is also real (Space Marines' Judiciar/Astraeus, locally; Chaos Knights' War Dog family,
  linked). One genuine data anomaly remains allowlisted, not fixed: Aeldari's Archon/Ynnari Archon
  carry a footnoted InSv with no matching ability anywhere in the entry under either convention —
  confirmed by direct inspection to be a real BSData authoring gap, correctly throwing via the "no
  candidate ability found" path rather than silently guessing.
  New fixture: `Examples.Datasheets.CanisRex()`/`Units.CanisRex()`, using real verified BSData
  values, added as a 7th roster unit specifically so `/LivePlay` has a caveated invulnerable save
  to render (the existing six-unit example army has none) — visually verified via
  `firefox-devtools-mcp`: the Sv stat-tile shows a `5++*` amber sub-value with a tooltip pointing at
  the unit's own abilities, and the linked `"Invulnerable Save (5+*)"` ability correctly appears in
  the adjacent abilities column. 174 tests, all passing (172 run by default + both permanent
  corpus-scan tests, explicit-only, also passing against the live clone). Docs:
  `.claude/domain-model-11e.md`'s "BSData JSON Ingestion" section rewritten for the new shape and
  resolution mechanism; `.claude/design-tokens.md` updated (`--amber`'s existing role now also
  covers the caveat indicator, no new token).
- OpenSpec change `bsdata-corpus-scan-tests` implemented and archived (all 20 tasks): two
  permanent, manually-triggered xUnit v3 `[Fact(Explicit = true)]` tests
  (`tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/`) replace the earlier one-off
  full-corpus checks — a characteristic-resolution scan (every catalogue file gets a turn as its
  own closure's starting file; every one of that file's own top-level entries attempted through
  `BsdataDatasheetMapper.BuildDatasheet`, catching any exception) and a weapon-keyword-token scan
  (walks each file's closure directly, collecting every `"Ranged Weapons"`/`"Melee Weapons"`
  profile's `Keywords` text and running it through a new `WeaponKeywordParser.UnrecognizedTokens`).
  Both check their results against a maintained, per-pattern allowlist (`AllowlistEntry<T>` +
  `AllowlistCheck.AssertClean`) **bidirectionally** — an unmatched result or a stale (unhit)
  allowlist entry both fail the test — and both `Assert.Skip` (not a silent pass) on a machine
  without the local BSData clone. `WeaponKeywordParser.Apply` was refactored (behavior-preserving,
  existing test suite unchanged) to share its token-recognition logic with the new
  `UnrecognizedTokens` method via a private `TryRecognize` helper, so the scan can't silently drift
  from what `Apply` actually recognizes. Docs: `.claude/domain-model-11e.md` updated ("Full-Corpus
  Scan Tests" subsection; `ParseThreshold`'s existing paragraph corrected — see below).
  **Two real findings from the first live-clone runs, neither anticipated going in:**
  (1) The characteristic-resolution scan surfaced 54 failures, not just the 2 seeded multi-value-
  InSv patterns — ~43 were a single trailing-`*` InSv footnote (e.g. `"5+*"` on Aeldari Rangers,
  most Imperial/Chaos Knights, Judiciar, Astraeus) that `CharacteristicPattern`'s own doc comment
  had incorrectly described as an accepted shape. Investigated properly rather than just "fixing
  the regex to match the docs" (paused for explicit user review first, per the apply workflow's
  guardrails, since the fix under consideration would have changed production parsing behavior):
  resolving the linked ability behind several real examples showed the footnote almost always
  restricts the save to one attack type (`"This model has a 5+ invulnerable save against ranged
  attacks"`, confirmed on Aeldari Rangers and every sampled Imperial Knight) — the same
  value-changes-by-context gap as the two already-known forms, just spelled differently in source
  data (one exception, Ork's Makari, uses the same `*` for an unrelated "no re-rolls" caveat).
  Conclusion: `ParseThreshold` throwing here is correct, working-as-intended behavior, not a bug —
  only the (previously inaccurate) doc comments were corrected, parsing behavior is unchanged. The
  3 known Ork dice-Strength weapons were confirmed to already throw `FormatException` via
  `ParsePlainInt`, exactly as predicted by design-time code inspection — no separate flagging
  mechanism was needed. (2) The weapon-keyword scan found 45 distinct unrecognized tokens on its
  first run (the old `scratchpad/keywordscan/` prototype no longer exists, so this was a fresh
  triage against new corpus content, not a re-check of the previously-found 43) — all added to the
  allowlist, none fixed in `WeaponKeywordParser` (deliberately out of scope for this change): 3
  already-documented no-flag tokens (Hazardous/Precision/Heavy), 7 case/punctuation variants of an
  already-recognized token (a parser case-sensitivity gap, flagged below as a future fix
  candidate), 4 dice-valued variants of an int-only mechanic (`Rapid Fire D3`/`D6`/`D6+3`,
  `Sustained Hits D3`), and 31 genuinely unmodeled mechanics — most one-off weapon-specific flavor
  abilities, but `Extra Attacks` (150+ occurrences), `Psychic`/`PSYCHIC` (~280 combined), `Lance`
  (~70), and `One Shot` (~30+) are common enough to be real gaps worth a dedicated future flag.
  Also found: `ANTI-non‑MONSTER/VEHICLE 2+` (Aeldari's Stone Stave) uses a U+2011 non-breaking
  hyphen, not ASCII `-`, plus a dual slash-separated exclusion target — a different targeting shape
  from the modeled Anti-X mechanic, not just a spelling variant.
  **Tooling gotcha found during implementation**: on this machine, `dotnet test -- --help` (and
  `Explicit`-passthrough via `dotnet test`'s own CLI) crashes the MTP wrapper outright; running the
  built `ProbHammer.Tests.exe` directly with `-explicit only` is the reliable way to execute
  `[Fact(Explicit = true)]` tests here — added alongside the existing `dotnet test`
  no-`-v`/`--nologo` gotcha.

- Parser-hardening follow-up on `catalogue-json-ingestion` (no new OpenSpec change): ran
  `BsdataDatasheetMapper` against every real file in the live BSData clone via a scratch scan tool
  (`scratchpad/keywordscan/`, not committed) and fixed the real bugs it found —
  `ParseMeasurement`/`ParseThreshold`/`ParsePlainInt` now handle `"-"`/`"N/A"` markers, trailing
  `*`/`+` footnote/threshold marks, `"12+"`-style flyer minimum-move values, and a `"Melee"` literal
  on one mis-typed Ranged Weapons profile. Reduced corpus-wide parse failures from 166 to 4. All
  fixes and their justifying real-data examples are documented inline on each helper in
  `BsdataDatasheetMapper.cs`. 157 tests, all still passing.

- OpenSpec change `catalogue-json-ingestion` implemented (all 30 tasks, not yet archived): new
  `ProbHammer.Core.Domain.Catalogue.Bsdata` namespace reads real BSData 11th Edition JSON catalogue
  files (a local clone at `C:\Users\Pete\wh40k-11e`, this machine only) and resolves them into
  `Datasheet`/`Statline`/`WeaponProfile`/`Ability` objects, queryable by name across a faction's full
  transitive `catalogueLinks` import closure. Not wired into `ProbHammer.Web`/`/LivePlay` (still runs
  on `Examples/` fixtures) and doesn't build the `Unit`/`AttachedUnit` graph — both deliberately
  deferred to a future change. Full shape: `.claude/domain-model-11e.md`'s new "BSData JSON
  Ingestion" section.
  **Three real-data findings surfaced only by running against the live clone, none anticipated by
  the design docs going in**: (1) a `catalogueLinks` entry's cached `name` can drift from its
  target's actual current file name (`"Chaos - Daemons Library"` links to
  `"Chaos - Chaos Daemons Library.json"`) — fixed by falling back to matching every available file's
  own top-level catalogue `id` against the link's `targetId` when the name-based guess isn't present,
  which is what `IBsdataCatalogueSource.ListFileNames()` exists for. (2) A weapon's `BS`/`WS`
  characteristic can be the literal text `"N/A"` (observed on Torrent weapons, which auto-hit) rather
  than a numeric threshold — parsed as `0`, matching the existing `Examples/Datasheets.cs` fixture's
  own precedent for Torrent weapons. (3) Not every model statline is nested inside a
  selectionEntry's own `profiles` array — Chaos Space Marines' "Legionaries" squad reaches its troop
  model's `Unit` profile only through a `"profile"`-type `infoLink` into the catalogue's separate
  top-level `sharedProfiles` list; the mapper now follows both `entryLinks` (into
  `sharedSelectionEntries`/`sharedSelectionEntryGroups`) and this second `infoLinks`-into-
  `sharedProfiles` path. `WeaponProfile` gained a verbatim, always-populated `KeywordsText` field
  (included in `EqualityKey()`) so a keyword whose meaning isn't yet recognized (`Hazardous`,
  `Precision`) or that's a context-dependent alternate spelling of an already-modeled flag
  (`Cleave` for `Blast`) is never silently lost or misrendered — rendering itself
  (`_UnitBlock.cshtml`) is *not* switched over to it in this change. Automated tests never touch the
  live clone: `tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/` holds trimmed real excerpts
  (generated by deserializing a real file through the loader's own reader types and re-serializing
  just the entries needed, which strips every unmapped field automatically as a side effect) plus a
  few small hand-built closure fixtures for the import-precedence scenarios. 157 tests, all passing
  (34 new). A one-off end-to-end check — every named top-level unit across all three captured
  exports (`data/gw-app-export*.txt`) resolves against its faction's real BSData closure — was run
  once against the live clone during implementation and passed (not kept as a permanent test, since
  it needs the external clone); surfaced one more finding not worth chasing here: the captured
  export text uses a typographic apostrophe where BSData's JSON uses a plain ASCII one
  (`Emperor's Champion`), a normalization problem for the future export-parser/enrichment step, not
  this loader.
- OpenSpec change `add-modelline-keywords` implemented (all 10 tasks, not yet archived):
  `ModelLine` gains its own `Keywords` (`IReadOnlySet<string>`, `StringComparer.OrdinalIgnoreCase`,
  matching `Datasheet.Keywords`'s convention, optional constructor parameter so every existing
  call site keeps compiling), and `KeywordResolution.EffectiveKeywords` now unions the `Keywords`
  of every currently-present (`RemainingCount > 0`) `ModelLine` alongside the existing per-
  component `Datasheet.Keywords` union. Motivated by a real datasheet ("Masters of the
  Maelstrom") whose printed card scopes a `Psyker` keyword to one named individual
  (`GARLON SOULEATER: PSYKER`) among three sharing an identical statline — this project's own
  `ChaosSpaceMarineSquad` fixture has the same five-distinct-named-model shape and the same gap,
  now fixed by a new `UnitFixtures.ChaosSpaceMarineSquadWithPsyker()` (one model-line per named
  type, `Psyker` on only the Gunner's). **A related assumption from this change's own proposal
  draft turned out wrong and was corrected before implementation**: BSData does carry `Leader`/
  `Support` as static keywords on most datasheets (confirmed present on Emperor's Champion even
  while unattached in an export) — not "never," as first assumed. Masters of the Maelstrom itself
  has no static `Support` keyword despite always requiring attachment in practice (both apps
  reject it standing alone), so whether our own model should ever synthesize a role keyword for
  that exceptional case is genuinely unresolved and stays out of scope, deferred to whenever the
  export parser exists to make it concrete. 133 tests, all passing (3 new, in a new
  `KeywordResolutionTests.cs`: unit-level union includes a present model-line's own keyword,
  model-level check doesn't leak to a sibling model-line, and the keyword drops from the union
  once its model-line has no models remaining). Docs: `.claude/domain-model-11e.md` updated
  (`ModelLine`'s shape, `KeywordResolution.EffectiveKeywords`'s pure-function description).
- OpenSpec change `add-army-roster-container` implemented and archived (all 10 tasks): new
  `ArmyRoster` class in `ProbHammer.Core.Domain.Roster` wrapping the existing per-unit roster
  (`Units`) with army-level metadata that no `Unit`/`AttachedUnit`/`Datasheet` had anywhere to
  carry — `Name`, `PointsSpent`, `Faction` (ordered list, parent codex before sub-faction),
  `Detachments` (ordered list, one per selected detachment), `ForceDisposition`, `BattleSize`,
  `PointsLimit`. Surfaced during exploratory design of a future GW-app export parser: real
  exports (`data/gw-app-export.txt` and its `-3-dp`/`-masters-of-the-maelstrom` variants,
  captured this session) carry all of this, and `Examples/View.MyArmyRoster()` had nowhere to
  put it, returning a bare `List<ICombatUnit>`. `Faction`/`Detachments` are plain ordered
  `IReadOnlyList<string>` rather than fixed named fields — real samples showed 1-2 Faction
  entries and 1-3 Detachment entries, and a fixed-arity shape would break the moment a deeper
  hierarchy or larger battle size showed up. `PointsSpent` and `PointsLimit` are deliberately
  separate fields (975 spent vs. 1000 limit in the sample list) and neither is derived from or
  validated against `Units`, since no points value is tracked anywhere below `ArmyRoster`.
  `Examples/View.cs` gained `Roster()`, populated with literal values transcribed from
  `data/gw-app-export.txt` (`"For the Emperor"`, `Space Marines`/`Black Templars`, `Companions
  of Vehemence`, `Purge the Foe`, `Incursion`, 1000-point limit); `MyArmyRoster()` is now a thin
  `Roster().Units.ToList()` projection, so its `List<ICombatUnit>` contract and both
  `LivePlay.cshtml.cs` call sites are unchanged. 130 tests, all passing (4 new — two on
  `View.Roster()` against the sample data, two on directly-constructed `ArmyRoster`s covering
  the single-Faction/no-sub-faction case and a three-Detachment case, neither exercised by the
  `View.cs` example itself). Doc: `.claude/domain-model-11e.md` updated.
- Fourth follow-up on `archive-10e-pipeline`, correcting a reasoning error from the third:
  `design-tokens.md` had been left in `.claude/` on the assumption that `site.css` being
  unsplit meant the doc couldn't cleanly split either (its "Live Play Page Exception" section
  was written as a contrast against the dark-theme content above it). Checked `site.css`
  directly rather than continuing to assume: `.live-play-page`'s CSS block turned out to be
  fully self-contained — its own independently-declared border-radius/spacing/hover values,
  and the file's one `@media` breakpoint targets only the archived
  `.army-columns`/`.army-inputs`/`.catalogue-bar` selectors, not `.live-play-page` — so
  nothing in the "dark theme" sections was actually load-bearing for `/LivePlay`, contrary
  to the earlier assumption. Rewrote `.claude/design-tokens.md` from scratch as a standalone,
  current-state `/LivePlay` visual design doc (palette, typography, spacing, interaction
  states — all read directly off the live CSS, not inferred by contrast); moved the original
  file's full content to `legacy/10e-pipeline/.claude/design-tokens.md`. Updated the
  `site.css` comment above `.live-play-page` to match (dropped "documented exception this
  page is" framing).
  Also pruned `.claude/implementation-notes.md`'s two `ArmyListParser.cs` iOS/Android
  format-gotcha entries to the legacy copy, prompted by explicit confirmation that the
  planned 11e `ArmyListParser` work is a full reimplementation rather than a refactor — these
  notes describe internals of the current 10e-format parser that won't carry forward, unlike
  the AP sign convention and generic Razor/C# gotchas that remain. `CLAUDE.md`'s doc-pointer
  list updated to match (design-tokens.md restored to an `@`-import, now that it's fully
  live). `dotnet build` reconfirmed clean.
- Third follow-up on `archive-10e-pipeline`: moved the remaining fully-archived `.claude` docs
  (`domain-model.md`, `web-app.md`, `simulation-engine.md`, `bsdata-parsing.md`,
  `rules/combat-rules.md`) from banner-noted-but-in-place to
  `legacy/10e-pipeline/.claude/`, prompted by the user asking whether banner-only docs should
  physically move too. **Surfaced and deliberately accepted a real gap along the way**: this
  session's earlier archive pass had moved `ArmyListParserTests.cs` +
  `ArmyListParserAndroidTests.cs` (38 tests) to `legacy/` even though `ArmyListParser.cs`
  itself stays live and unmodified in `src/ProbHammer.Core/Parsing/` — meaning the live
  parser currently has zero test coverage. Raised this with the user explicitly; decision was
  to leave the tests archived rather than restore them, accepting the coverage gap until the
  planned 11e `ArmyListParser` rewrite replaces them anyway. **`domain-model.md` moved
  wholesale despite documenting some still-live behavior** (its "Core Domain Records"/"Army
  List Export Formats" sections describe the exact live `ArmyListParser.cs` parsing rules,
  not just the archived `Enricher`/`UnitProfile` schema) — user's explicit call, preferring
  simplicity over a doc split, on the basis that `ArmyListParser.cs` is short enough to read
  directly when needed.
  `design-tokens.md` was **not** moved, unlike the others — its "Live Play Page Exception"
  section documents the still-live `.live-play-page` CSS token-override mechanism (in the
  still-unsplit `site.css`), which isn't written down anywhere else; moving the whole file
  would have deleted that documentation, unlike the parser docs the user just chose to let go
  of. `CLAUDE.md` updated to point at the new `legacy/10e-pipeline/.claude/...` paths.
  `dotnet build` reconfirmed clean.
- Second follow-up on `archive-10e-pipeline`, prompted by direct feedback: banner-then-old-
  content in `CLAUDE.md` (added in the prior follow-up) still meant the file described
  archived behavior, which defeats the point — `CLAUDE.md`'s own stated job is describing
  *current* intent and architecture. Rewrote rather than banner-annotated: removed the
  "Architecture Overview" pipeline diagram and the entire "User Flow" section (both 100%
  archived-flow narrative with no live equivalent); trimmed "Key Design Constraints" to just
  the one constraint still true (AP sign convention, reworded to cite the live
  `WeaponProfile.Ap` instead of the archived `SimWeaponProfile.Ap`); dropped the dead
  `Enricher:CachePath` note from "Running Locally" entirely rather than explaining that it's
  dead; removed `FuzzySharp` from "Key dependencies" (no longer a live dependency once
  `Enricher.cs` moved). "Architecture Overview" now describes the actual live architecture
  (11e domain model + `/LivePlay`) instead of the retired pipeline.
  **Real side effect, not just tidying**: `CLAUDE.md` used `@`-import syntax
  (`@.claude/domain-model.md` etc.) on four now-archived docs, which force-loads their full
  content into *every* session's context automatically regardless of relevance — confirmed
  by their appearance in this session's own system prompt. Dropped the `@` on all four
  archived docs plus `combat-rules.md` (also archived-flagged), replaced with a plain
  "read on demand" pointer list. `domain-model-11e.md` and `implementation-notes.md` (already
  trimmed to current-state entries in the prior follow-up) keep their `@`-imports since
  they're genuinely live and commonly needed.
- Follow-up documentation cleanup on `archive-10e-pipeline` (no separate OpenSpec change —
  doc-only, zero build/behavior risk, landed directly): archived the remaining legacy
  documentation that the code-only move left behind. `REGENERATION.md` (a full build journal
  from an earlier "regenerate the app from spec files only" experiment, predating this
  project's OpenSpec adoption) and `.claude/features/*.feature` (its Gherkin acceptance
  criteria — referenced only by that journal) both moved wholesale to
  `legacy/10e-pipeline/`, mirroring their original root-level/`​.claude/` paths. `data/`
  (10e-format sample army list exports, referenced from nowhere in code) moved the same way.
  `.claude/implementation-notes.md` was split by triage rather than moved wholesale: entries
  tied purely to the archived `Enrichment`/`Catalogue`/`Simulation` code (Session JSON
  serialisation, BSData XML parsing gotchas, fuzzy-matching/`name_overrides.json`,
  sub-ability/multi-profile weapon parsing, `Cover`/`SimDefenderProfile.Save`) moved to
  `legacy/10e-pipeline/.claude/implementation-notes.md`; entries confirmed still live were
  kept — the two `ArmyListParser.cs` iOS/Android gotchas (that parser stays unmodified in
  `src/`), the generic Static-Classes-and-`ILogger` and three Razor gotchas (confirmed still
  exercised: `/LivePlay`'s `_UnitBlock.cshtml` already uses the same `@(expr)` wrapping the
  Razor-email-heuristic/WH40K-notation notes describe), and the AP sign-convention note
  (reworded — confirmed the 11e domain model's `WeaponProfile.Ap` uses the same
  negative-integer convention as the archived `SimWeaponProfile.Ap`, so the rule carries
  forward rather than being purely historical).
  `.claude/design-tokens.md` was **not** split — its "Live Play Page Exception" section
  (documenting `/LivePlay`'s light-theme token override) is live and necessary, but has no
  clean physical boundary from the rest of the file's archived dark-theme content, mirroring
  the earlier decision to leave `site.css` itself unsplit. Got a banner note instead.
  `CLAUDE.md` gained banner notes on its "Architecture Overview" and "Key Design Constraints"
  sections (matching the "User Flow" banner from the original `archive-10e-pipeline` session),
  a correction to its `.claude/domain-model.md` pointer line ("current Web app" → "archived"),
  an updated `Solution Structure` tree (folds `REGENERATION.md`/`.claude/features/`/`data/`
  into the `legacy/10e-pipeline/` entry instead of listing `data/` separately), and a note
  that `Enricher:CachePath` in `appsettings.json` is now dead config (left in `appsettings.json`
  itself, untouched, consistent with the original change's decision not to touch app config).
  `dotnet build` reconfirmed clean (0 errors/warnings) after the moves.
- OpenSpec change `archive-10e-pipeline` implemented (all 34 tasks, `skip_specs: true` — the
  archived code predates this project's spec-tracked capabilities and none of them describe
  it): the entire live 10th-edition pipeline (`Contracts/UnitProfile.cs`+`ScalarValue.cs`,
  `Catalogue/*`, `Enrichment/*`, `Simulation/*`, plus the dependent `Web` pages —
  `Index`/`ArmyView`, `_UnitCard.cshtml`, `SimulationService`, `CatalogueStartupService`,
  `SessionJson`, `army-view.js`) moved from `src/` to `legacy/10e-pipeline/` (mirroring
  original relative paths), excluded from all three `.csproj` files' compilation. Done ahead
  of rewiring `ArmyListParser` for the 11e export format, per `.claude/domain-model-11e.md`'s
  standing plan to eventually rewire `ArmyListParser`, `Web`, and `Simulation` onto the 11e
  domain model rather than patch the old `Contracts`-based pipeline in place — moving the
  orphaned code aside first avoids it silently rotting half-broken mid-rewrite while still
  keeping it intact as reference material.
  **BREAKING**: `/Index`, `/ArmyView`, `POST /api/simulate`, and `POST /api/refresh-catalogues`
  are gone from the running app — it serves `/LivePlay` only now. `Program.cs` lost the DI
  registrations, ASP.NET Session wiring (`AddDistributedMemoryCache`/`AddSession`/
  `UseSession` — confirmed as exclusively a 10e-pipeline consumer; `/LivePlay`'s casualty
  persistence uses client-`localStorage` + a full-map POST instead, per
  `.claude/domain-model-11e.md`), and the `github` named `HttpClient`, all for the same
  reason.
  Mechanism turned out simpler than the proposal first assumed: both `.csproj` files use
  the SDK's default file glob rooted at each project's own directory, so moving files
  outside `src/` excludes them from compilation with **zero** `<Compile Remove>` edits — the
  only `.csproj` change was dropping the now-unused `FuzzySharp` package reference (used only
  by the moved `Enricher.cs`; the pre-existing, unrelated unused `YamlDotNet` reference was
  left alone, out of scope).
  Two things were deliberately kept in `src/`, not archived, found by actually reading the
  files rather than assuming: `Contracts/ArmyList.cs` (the `ArmyList`/`UnitEntry`/
  `ModelEntry`/`WeaponEntry` records) stays, since it's `ArmyListParser.cs`'s own return type
  and that parser is explicitly untouched by this change, kept as the 11e rewrite's starting
  point; `wwwroot/css/site.css` stays unsplit, since its 10e-only and `/LivePlay`-only
  (`.live-play-page`-scoped) rules are interleaved with no clean physical boundary and
  splitting it risked a real visual regression on the one page still in active use, for a
  change whose entire point was archival safety, not a redesign (flagged as a
  `.claude/vnext-ideas.md` follow-up instead). `wwwroot/js/army-view.js`, by contrast, had no
  such mixing (100% ArmyView-only logic) and moved wholesale, with its dead `<script>` tag
  removed from `_Layout.cshtml`.
  Verified via `dotnet build` (0 errors/warnings, nothing under `legacy/` touched — it has no
  `.csproj`/`.sln` of its own) and `dotnet test` (126/126 passing, down from 276 — the exact
  drop expected from moving `Simulation/*`'s and `Catalogue/*`'s and the 10e-format
  `Parsing/*Tests.cs`' test files). Live-checked via `dotnet run` + `curl` rather than
  `firefox-devtools-mcp` (not connected this session): `/LivePlay` → 200 with real content;
  `POST /api/live-play/casualties` round-tripped a casualty adjustment correctly; `/Index`,
  `/ArmyView`, and `/` all → 404 as expected, not a crash. `.claude/domain-model.md`,
  `.claude/web-app.md`, `.claude/simulation-engine.md`, `.claude/bsdata-parsing.md`,
  `.claude/rules/combat-rules.md`, and `CLAUDE.md` (Solution Structure + User Flow sections)
  all gained banner notes pointing readers at the new `legacy/` location instead of being
  rewritten or deleted — matching `domain-model.md`'s pre-existing pattern for flagging
  superseded-but-retained code.
- Follow-up patch on `casualty-tracking` (no separate OpenSpec change — small enough to land
  directly): a "reset casualties" control per unit block, requested after hands-on play surfaced
  that reverting several taps' worth of casualties one at a time was tedious. Lives in the
  Statline `<summary>` bar alongside the existing Clear-filter funnel, wrapped in a new
  `.statline-flags` flex container so the two push right *together* (a single `margin-left: auto`
  on the wrapper, not on each button — two auto-margined siblings would have split the bar's free
  space between them instead of sitting a fixed distance apart) with a deliberate `0.75rem` gap
  between them, so clearing a filter and resetting casualties can't be fat-fingered for each other.
  Ordering was flipped after review: Clear-filter sits left, casualty-reset sits rightmost, since
  casualties persist far longer than a per-turn filter and the rightmost slot won't visually shift
  every time the filter toggles on/off. Confirms via a native `window.confirm()` before doing
  anything — no custom modal, matching this project's zero-dependency JS approach. On confirm,
  `live-play.js` collects every casualty coordinate currently rendered for that unit (deduped
  across each pair's dec/inc buttons), resets each to its own `data-initial` value through the
  same `syncCasualties()`/`swapUnitBlock()` path a normal casualty tap already uses (no new
  server-side verb needed), and prunes those now-pristine keys back out of `localStorage` once the
  sync confirms success — `syncCasualties()` gained a boolean return for exactly this. Only
  visible while the unit actually has a casualty (`UnitBlockViewModel.HasCasualties`, computed
  server-side from `RemainingCount != InitialCount` across the unit's statline entries), server-
  rendered the same way the run-collapse "dead" state already is, so no client-side visibility
  scan is needed either.
  **Icon went through two iterations, both tried live in-browser rather than guessed at**: first
  an inline SVG skull built from an `<svg><mask>` (white cranium path, black circular eye-socket
  and triangular nose cutouts) — same reasoning that already ruled out a Unicode glyph for the
  Ranged/Melee funnel icon (a mask composites correctly against `currentColor` regardless of what's
  behind it, where a font/emoji glyph varies by platform and can't punch true holes). Scaled 6× in
  a live Firefox session to confirm the shape actually read as a skull before settling on it — it
  did. The user then asked to compare against the U+1F480 (💀) emoji directly; tried it the same
  way (scaled 6× live), confirmed it renders as a full-color platform glyph that ignores
  `--amber`/`currentColor` entirely (as expected, but worth seeing rather than assuming), and after
  reviewing both side by side the user kept the emoji anyway — small enough of a style
  inconsistency to accept for a control this size. The SVG mask code was removed rather than kept
  dead/commented-out once the emoji was chosen.

- Post-implementation fix on `casualty-tracking`, reported from hands-on play: a casualty tap on
  one statline entry was resetting *every* entry's weapon-selection filter in the same unit block
  back to fully-selected, not just the adjusted entry's — jarring mid-combat (deselect several
  entries to focus on one ongoing fight, then an unrelated casualty tap on a different entry
  reselects them all). Root cause: `live-play.js`'s `initUnitSelection` built a fresh, empty
  `deselected` `Set` on every call, including the re-init `swapUnitBlock` triggers after a casualty
  round-trip — this directly contradicted the already-written "Casualty Tracking Is Independent of
  Selection Filtering" requirement's own "Marking a casualty does not change the current filter"
  scenario, not a new judgment call. Fixed by moving the `Set` into a module-scope
  `deselectedByUnit` `Map` keyed by `data-unit-index`, reused (not recreated) across a swap of the
  same unit — safe because a casualty adjustment never changes a select-key's shape
  (`ComponentName`/`StatlineName`/`LoadoutIndex`), only `RemainingCount`. Page-load-ephemeral filter
  behavior (`live-play-selection-scoped-weapons`) is unchanged — only in-session swaps now preserve
  selection state. Also fixed in the same session, from a separate contrast-nit report: the
  casualty `−`/`+` buttons' text was near-black (`.mod-step-btn`'s inherited `color: var(--text)`)
  on a bottle-green background, unreadable — overridden to white for `.casualty-btn` specifically.
  `openspec/changes/casualty-tracking/design.md` updated (Decisions/Risks corrected to describe the
  fix instead of the old, spec-contradicting "accepted trade-off" framing).
- OpenSpec change `casualty-tracking` implemented and applied (all 29 tasks): `/LivePlay` gains its
  first mutation and first persistence — a `−`/`+` casualty control on every single-loadout
  statline entry's header line and on every loadout line of a multi-loadout entry (never on a
  multi-loadout entry's own summed header, which has no single `ModelLine` to adjust). Architecture
  decided over several rounds of discussion (initially explored, then backed away from, a Blazor
  WASM rewrite; settled on keeping the page server-rendered): the browser persists a coordinate →
  remaining-count map to `localStorage`, POSTs the *entire* current map to a new
  `POST /api/live-play/casualties` endpoint on every load and every tap (never just the newest
  change — a stateless-server bug where a tap-only-adjustment request would discard earlier-session
  casualties was caught mid-implementation and fixed by always resending the full map), and the
  server rebuilds the roster from scratch (`View.MyArmyRoster()` — a new raw-`ICombatUnit` export
  alongside the existing pre-aggregated `View.MyArmy()`), reapplies every adjustment via
  `ModelLine.SetRemainingCount` (a new clamped `[0, Count]` setter both directions reduce to;
  `RemoveCasualties` is now a thin wrapper over it), and returns rendered `_UnitBlock.cshtml`
  fragments (a new partial extracted from `LivePlay.cshtml`'s per-unit markup, rendered to a string
  via a new `IRazorPartialRenderer` — the standard `IRazorViewEngine`/`ITempDataProvider`/hand-built
  `ViewContext` pattern for rendering a partial outside an MVC action) for whichever units the
  batch addresses — the client only ever swaps pre-rendered HTML, no aggregation logic duplicated
  in JS. The casualty coordinate `(UnitIndex, ComponentName, StatlineName, LoadoutIndex)` reuses
  the existing `LivePlayModel.SelectKey` convention unchanged, adding only a `UnitIndex` qualifier
  (`LivePlayModel.CasualtyKey`); `UnitIndex` is stable across a pristine GET and an adjusted rebuild
  because `LivePlayModel.SortRoster`'s sort keys (`IsAttachedUnit`, initial model count, `Name`) are
  all pristine, remaining-count-independent properties — factored out of `OnGet()` into a shared
  method precisely so the rebuild path can't drift from it.
  **A real domain-model behavior reversal, exposed by trying to build the "revert a mis-tap"
  story**: `AttachedUnitAggregator.BuildStatlines` used to drop a statline entry from the aggregate
  view entirely once every one of its model-lines reached 0 remaining — with nothing rendered,
  there'd be no row left for the new casualty control to revert from. Fixed by narrowing the drop
  condition from "no survivors" to "never referenced at all" (`lines.Count == 0`), so an entry
  persists at 0/`InitialCount` once it's ever had model-lines — this is now a documented,
  MODIFIED-and-tested requirement (`attached-unit-tracker`'s "Aggregate Statline View"), not a
  quiet implementation change. `AttachedUnitAggregator.BuildWeapons`/`BuildAbilities` needed no
  equivalent fix — both already correctly excluded `RemainingCount == 0` lines via the existing
  `presentLines` filter; that behavior only gained explicit spec scenarios, confirmed via a live
  browser check (killing a shared loadout dropped a weapon's total from 10 to 7 and removed its
  breakdown row entirely, never showing a `0×` row).
  A statline run now collapses to header-only under **two independent triggers**: the pre-existing
  client-only "every entry fully deselected" one, and a new server-computed "every entry fully
  dead" one (`StatlineBlockViewModel.IsFullyDead`, and `ComponentAbilitySpanViewModel.IsFullyDead`
  for component-wide ability cells) — baked into the initial server-rendered markup (`data-dead`
  attribute + `.run-collapsed` class) so a fully-dead run renders already collapsed after a reload,
  not something a player must re-trigger. `live-play.js`'s `updateRunCollapse()` now ORs its
  existing deselection-based per-run boolean with each run's `data-dead` marker rather than
  resetting the class from scratch, so reselecting a dead entry cannot un-collapse it — only
  reverting the casualty (increasing `RemainingCount` back above 0) does. `live-play.js` also
  consolidates what used to be two independent `DOMContentLoaded`-time passes (a page-wide
  `.weapon-name-toggle` query, and a per-unit-block `initUnitSelection` call) into one
  `initUnitBlock(unitEl)`, now re-run on a casualty-triggered swap's replacement node — a gap
  caught during design review (a swap that only re-ran `initUnitSelection` would leave the swapped
  unit's provenance-breakdown toggles with no click handler at all). A second gap surfaced only by
  hands-on browser testing, not the design discussion: replacing a whole `.unit-block`'s markup
  also discarded every `<details>` section's open/closed state, so tapping a casualty control
  visually snapped an expanded Statline section shut — fixed by having `swapUnitBlock` carry
  forward which sections were open across the swap.
  Casualty controls sit inside their row's existing whole-row selection-toggle click target, so
  their click handlers call `event.stopPropagation()` first (same shape as the pre-existing
  Clear-filter button's `event.preventDefault()` inside `<summary>`) and are visually built from
  the same `.mod-step-btn` step-control class ArmyView's combat panel already uses — safe to reuse
  directly because `.live-play-page` locally redeclares the same `--bg3`/`--text`/`--border` custom
  properties `.mod-step-btn` reads, so it automatically renders in this page's light theme rather
  than ArmyView's dark one.
  Verified via `firefox-devtools-mcp` against the real example army end to end: marked High Marshal
  Helbrecht as a casualty, watched its run collapse and its "LEADER" ability cell collapse with it;
  reloaded the page and confirmed both the mark and the collapse persisted with no re-marking
  needed; reverted via `+` and watched both expand again; reduced the Crusader Squad's
  Astartes-chainsword Initiate loadout from 3/3 to 0/3 across three separate taps (each its own
  round trip) and watched the parent "Initiate" entry correctly re-sum to 2/5, the Bolt pistol
  weapon row's total drop from 10 to 7, its breakdown lose the dead loadout's row entirely, and its
  merged "Initiate" breakdown row correctly demote to a single raw "Initiate w/ Power fist (2×1)"
  row now that only one contribution remained in that group. 276 tests, all passing — includes a
  new `Microsoft.AspNetCore.Mvc.Testing`-based integration test suite (`WebApplicationFactory`,
  the project's first) exercising the casualty endpoint end to end, including the Razor-partial-to-
  string rendering path. Docs: `.claude/domain-model-11e.md` updated (`ModelLine.SetRemainingCount`,
  the `Statlines`-persist-at-zero behavior change); this entry is the `PROGRESS.md` record.
- OpenSpec change `add-live-play-weapon-provenance` implemented: weapon rows on `/LivePlay` now
  expand on click to show which `(ComponentName, StatlineName)` groups contributed how much,
  instead of only ever showing the aggregated total. Contributions sharing a group collapse to one
  row (`Name (Count×PerModelAttacks)`, product as the Attacks subtotal) when their
  `PerModelAttacks` agree; if they disagree — possible since `WeaponProfileEqualityKey` excludes
  Attacks by design, though no real fixture exercises it — each contributing `ModelLine` renders
  separately instead, so no subtotal is ever shown that isn't verifiable from the numbers beside
  it. Grouping/formatting (`LivePlayModel.BuildContributionBreakdown`) is page-layer view-model
  work over data `AttachedUnitAggregator` already computed correctly (`WeaponContribution`, added
  in `rework-attached-unit-aggregate-view` specifically for this), matching the page's existing
  `StatlineBlockViewModel` precedent — no domain-model change. This is `/LivePlay`'s first
  JavaScript (`live-play.js`, click-to-toggle): native `<details>` can't span additional `<tr>`s
  below the row that triggers it, so a small script toggles pre-rendered, hidden breakdown rows
  via a shared `data-weapon-id` attribute.
  **Refined after hands-on layout feedback, twice.** First: zebra striping broke, because
  `:nth-child(even)` counts the always-present-but-`[hidden]` breakdown rows too, silently shifting
  every later primary row's parity (confirmed live — a unit's 4th weapon row, which should read as
  even/grey, rendered white because three hidden rows ahead of it shifted its DOM position to odd).
  Fixed by driving `.weapon-row-alt` from each weapon's own list index instead of DOM position, and
  giving a breakdown row the same class as its parent so an expanded row reads as one continuous
  block rather than a colour gap; also increased the breakdown label's indent for a clearer
  nested-section look. Second, a genuine scope change: the trigger was originally per-weapon
  (`Contributions.Count > 1`), but using the page surfaced that a *single* contributor is still
  worth naming — e.g. a Pyre pistol carried only by a unit's Sword Brother — as long as the unit has
  other `ModelLine`s to distinguish it from. Trigger is now unit-level: `totalModelLinesInUnit > 1`,
  computed once via `view.Statlines.Sum(s => Math.Max(s.Loadouts.Count, 1))` (`Loadouts` is empty
  when exactly one `ModelLine` shares a statline name, so `Max(.,1)` recovers the true per-name
  count). This can only ever exclude a single-`ModelLine` plain `Unit` (e.g. Impulsor) — an
  `AttachedUnit` always totals two or more `ModelLine`s by construction (Bodyguard + at least one
  Attached component, each contributing at least one line), confirmed by a dedicated test asserting
  no weapon on Impulsor ever shows a breakdown. Visually verified via `firefox-devtools-mcp` against
  the real example army: the Crusader Squad's Bolt pistol (A10) expands to Neophyte (4×1), Initiate
  (5×1, collapsed across its two differently-loadout model-lines), and the attached Crusade
  Ancient's own Bolt Pistol (1×1) — a genuine cross-component merge, confirming
  `WeaponProfileEqualityKey`-equal weapons from different components do combine as designed; Pyre
  pistol expands to a single "Sword Brother (1×D6)" row; the Melee table's Astartes chainsword
  breakdown (Neophyte 4×4, chainsword-loadout Initiate 3×4, Power-fist-loadout Initiates correctly
  absent since they don't carry that weapon) confirmed the loadout-differentiation scenario
  discussed during design needed no special-casing. 253 tests, all passing. `.claude/vnext-ideas.md`
  updated: this item removed (now implemented), two new complementary ideas added — an
  ability/weapon-keyword definitions popup, and selection-scoped weapon aggregation (checkbox-driven
  filtering of the Statline section, deliberately sequenced after this change since it reuses the
  same grouping logic).
- OpenSpec change `add-live-play-ability-columns` implemented and archived: the Statline section
  on `/LivePlay` is now a 3-column CSS Grid — STATLINES | MODEL ABILITIES | UNIT ABILITIES —
  filling the previously-unused horizontal space beside each statline block and putting each
  ability directly beside the model(s) it depends on. `AttachedUnitAggregateView.Abilities`
  replaces the old `UnitScopedAbilities`/`ModelScopedAbilities` fields with a single per-component,
  optionally row-bound `AggregateAbilityEntry(ComponentName, StatlineName?, Ability)` list: `null`
  `StatlineName` means Datasheet-sourced (renders beside the first statline row of its component,
  visually spanning every row belonging to that component via CSS Grid's `grid-row` span — not an
  HTML `<table>`/`rowspan`, since the page's existing `<div>`-based cards and rich nested statline
  content don't fit a literal table); a set `StatlineName` means ModelLine-sourced, Enhancement-
  conferred (renders beside that one row only). Column placement is decided purely by
  `Ability.Scope`; row placement purely by ability source — this also surfaces a previously-hidden
  gap, since Datasheet-level `Scope: Model` abilities (`LEADER`, `SUPPORT`) were never rendered
  anywhere before this change. Ability rendering is name-only for now; full-text-on-demand is
  explicitly deferred. The old standalone "Abilities" collapsible section is removed. Visually
  verified via `firefox-devtools-mcp` against the real example army, including the rowspan effect
  spanning a merged statline run through a separate one. 246 tests, all passing.
- OpenSpec change `merge-adjacent-live-play-statlines` implemented and archived: on `/LivePlay`,
  adjacent statline rows within the same component that share an identical Statline value
  (M/T/Sv/W/Ld/Oc, and InSv when present) now render under one shared stat-tile, with each
  contributing entry's own name/count kept as its own header line above it (and its own nested
  loadout breakdown, unaffected by sharing a tile). The merge is adjacency-scoped (only a
  contiguous run in the already-established component/declared-order sequence) and never crosses a
  component boundary, even when values happen to match — this is a rendering-only grouping, not a
  data-level merge; the underlying `AggregateStatlineEntry` list is unchanged, so per-name
  remaining/initial counts and future casualty tracking stay independently addressable per entry.
  `AggregateStatlineEntry` gained a `ComponentName` field (same convention as
  `WeaponContribution.ComponentName`), populated in `AttachedUnitAggregator.BuildStatlines`. The
  grouping itself (a single forward scan starting a new run whenever `ComponentName` or `Statline`
  differs from the current run) lives entirely at the page layer in a new `StatlineBlockViewModel`,
  matching this page's established precedent of rendering-only decisions living in
  `LivePlayModel`. `BuildUnitBlock` was made `internal` (with `InternalsVisibleTo` for
  `ProbHammer.Tests`) so a hand-built `AttachedUnitAggregateView` could exercise a
  same-component/equal-value merge, a value-change split, and a component-boundary split in one
  precise unit test — the real example army doesn't naturally contain the last case adjacently.
  Visually verified via `firefox-devtools-mcp`: Crusader Squad shows Sword Brother + Initiate under
  one shared tile with Neophyte separate; Assault Intercessor Squad shows Sergeant + rank-and-file
  under one shared tile. 243 tests, all passing.
- OpenSpec change `order-live-play-unit-cards` implemented and archived: `/LivePlay` unit-card
  blocks now render attached-sourced units (`AttachedUnit` source) before plain-`Unit`-sourced
  ones; within each group, descending total model count (Bodyguard + every Attached unit's models
  for an AttachedUnit), ties broken by ascending `Name` (ordinal, case-insensitive). An
  `AttachedUnit` with zero attached units still counts as attached-sourced, since the rule keys on
  source type, not rendered content. `AttachedUnitAggregateView` gained a new `IsAttachedUnit`
  field (`combatUnit is AttachedUnit`, set in `AttachedUnitAggregator.Build`) so the page layer can
  tell the two source types apart — the sort itself happens in `LivePlayModel.OnGet()`, matching
  the existing precedent of weapon ordering being a page-layer concern. `ProbHammer.Tests` gained
  its first `ProbHammer.Web` project reference and a `Web/LivePlayModelTests.cs` exercising
  `LivePlayModel.OnGet()` directly against the real example army (no DI needed) — the exact
  expected order was hand-computed from `Examples/Units.cs`'s model counts and confirmed both by
  the test and visually via `firefox-devtools-mcp`. 241 tests, all passing.
- OpenSpec change `order-statlines-by-declaration` implemented and archived: `Datasheet.Statlines`
  changed from `IReadOnlyDictionary<string, Statline>` to an ordered
  `IReadOnlyList<(string Name, Statline Statline)>` — declaration order (Sergeant/leader-equivalent
  entry first, matching the real NewRecruit/GW app export convention) is now a documented,
  load-bearing property instead of an accident of `Dictionary` enumeration. `GetStatline(name)`
  stays O(1), backed by an internal dictionary built once in the constructor. All 9 sites in
  `Examples/Datasheets.cs` and 12 test-fixture sites migrated (all already declared Sergeant/leader
  first, so no reordering was needed beyond the syntax change). `AttachedUnitAggregator
  .BuildStatlines` rewritten to walk components in display order (an `AttachedUnit`'s `Attached`
  list, then `Bodyguard`; a plain `Unit` is just itself) and, within each, that component's
  Datasheet-declared statline order — matching each declared name only against that component's
  own `ModelLine`s. **Behavior change**: per-name statline merging is now scoped to a single
  component, not merged globally across an `AttachedUnit` as before; confirmed acceptable since
  real 40k attachment rules can't produce two identically-named Leader/Support units on one
  Bodyguard. Visually verified via `firefox-devtools-mcp`: an AttachedUnit card renders Attached
  units before its Bodyguard, sergeant-first within each; a plain Unit card renders its Sergeant
  statline first. 237 tests, all passing.
- OpenSpec change `drop-melee-range-column` implemented and archived: the Melee Weapons table on
  `/LivePlay` no longer shows a Range column (always read "Melee", pure noise). Two further
  `/LivePlay` display tweaks landed alongside it (no spec impact — pure CSS/markup): the
  invulnerable save now renders as a smaller sub-line nested under the Sv stat-tile instead of its
  own separate tile (`.stat-subvalue`, matching GW card style more closely), and both weapon
  tables now use `table-layout: fixed` with a shared `<colgroup>` column-width scheme (`.col-weapon`
  / `.col-weapon-melee` / `.col-slot`) so the two independent `<table>` elements' shared columns
  stay pixel-aligned regardless of each table's own content-driven width. All three fixes were
  visually verified against a real Firefox instance via the `firefox-devtools-mcp` MCP server
  (Mozilla's official Firefox DevTools MCP) — Melee Weapons shows no Rng column, the High Marshal
  Helbrecht statline's Sv tile shows a nested `4++` under `2+`, and the Assault Intercessor Squad
  card's Ranged/Melee weapon-table columns (A/S/AP/D) align across both tables.
- OpenSpec change `live-play-gw-layout` implemented (all 28 tasks): `/LivePlay` rewritten around a
  GW-datasheet-inspired light theme, scoped entirely to that page via locally-redeclared CSS
  tokens (`.claude/design-tokens.md` now documents this as an explicit exception). `ICombatUnit`
  gained a computed `Name` (`Unit.Name` from its Datasheet; `AttachedUnit.Name` a humanized join of
  Bodyguard + Attached Datasheet names, Oxford-comma at 3+), threaded through
  `AttachedUnitAggregateView.Name` and rendered as each unit block's header, replacing the old
  "Unit N" ordinal. Statlines render as one header+stat-tile block per `AggregateStatlineEntry`
  (GW mobile-app pattern, never merged by matching value — reinforces the existing no-merge rule
  from `render-aggregate-view`). Weapons split into Ranged/Melee sections ordered by descending
  `DiceExpression.ExpectedValue()` (new helper, added because `DiceExpression` has no natural total
  order for sorting); weapon ability flags render as inline bracketed tags instead of a separate
  column. Each unit block's statline/ranged/melee/abilities sections are independent `<details>`,
  collapsed by default. Still read-only, still rebuilt fresh from `Examples.View.MyArmy()` per
  request — no session/persistence added. Explicitly deferred to a future change: phase-level
  section toggling, Core/Faction/Psychic ability source tagging, per-`ModelLine` keywords (needed
  for a leader's personal keyword to leave the unit when that model is removed), a multi-profile-
  weapon disclaimer, and live casualty mutation itself. Docs: `.claude/domain-model-11e.md` and
  `.claude/design-tokens.md` updated.
- OpenSpec change `promote-dice-expression-to-domain` implemented (all 19 tasks): `DiceExpression`
  moved from `ProbHammer.Core.Simulation` into `ProbHammer.Core.Domain.Catalogue` — it's a
  game-rules value type (`WeaponProfile.A`/`D` already depended on it), not a simulation-owned
  one. `Simulation/*` (`SimulationAdapter`, `CombatSimulator`, `SimWeaponProfile`, `IDiceRoller`,
  plus their test doubles/tests) were repointed via `using` statements only — no behavior change;
  `Simulation/*` remains paused/not wired to the 11e domain model, per `.claude/domain-model-11e.md`.
  Added an implicit `int -> DiceExpression` conversion, `D3`/`D6` static presets, and an
  `operator +(DiceExpression, int)` so fixed weapon stats can be written as plain integers and
  dice notation reads close to game shorthand (`DiceExpression.D6 + 2`), rather than needing
  `DiceExpression.Fixed(n)` wrapping at every call site or a combinatorial set of constructor
  overloads on `RangedWeapon`/`MeleeWeapon`. **Scope addition made mid-implementation** (agreed
  with the user after discovering it during apply): `WeaponProfile.D` (Damage) was also widened
  from `int` to `DiceExpression`, matching `A` — this had been started but abandoned mid-way in
  the prior commit (a `//NOTE: D needs to be able to accept "D6"` comment on a `Fixed(6)`
  placeholder for the Multi-melta), and completing it was necessary to actually fix that weapon's
  damage. `BlackCrusade.cs` and `WeaponFixtures.cs` simplified to drop now-unnecessary
  `DiceExpression.Fixed(...)` wrapping. `DiceExpression` already had a dedicated test suite
  (`tests/.../Simulation/DiceExpressionTests.cs` — the proposal incorrectly claimed it had none);
  relocated it to `tests/.../Domain/DiceExpressionTests.cs` and extended it with coverage for the
  three new additions. Also fixed one pre-existing test bug the build surfaced: `DatasheetTests.cs`
  asserted `profile.A.Should().Be(4)` against a `DiceExpression`-typed property using a bare `int`
  literal — this only started failing at runtime (rather than silently not compiling) once the new
  implicit conversion made it compile; fixed to assert against `DiceExpression.Fixed(...)`
  explicitly. 214 tests, all passing.
- Refinement pass on the `attached-unit-domain-model` change (commits `3cfdfc0`..`d8c53c0`, all
  tasks already closed — no scope change): `WeaponProfile` reshaped to an abstract base with
  sealed `RangedWeapon`/`MeleeWeapon` subtypes (`WeaponAbilities` removed, ability fields
  flattened directly onto `WeaponProfile`); `Skill` is now an abstract *computed* property
  (`RangedWeapon.Skill => Bs`, `MeleeWeapon.Skill => Ws`) rather than a stored value, avoiding a
  shadow-property desync under `with` expressions. `Datasheet`'s constructor now takes
  `weaponProfiles` as `IEnumerable<WeaponProfile>` (keyed internally by `.Name`) instead of a
  caller-built dictionary, removing a key/name-drift bug class found when `WeaponFixtures` was
  introduced. New fixture/test coverage added for two differently-named statlines with identical
  values (`Assault Intercessor` / `Assault Intercessor Sergeant`), proving
  `AttachedUnitAggregator` dedupes statlines by name, not by value. 35 domain tests, all passing.
  Doc: `.claude/domain-model-11e.md` updated to match.
- OpenSpec change `attached-unit-domain-model` implemented (all 28 tasks): new 11th-edition
  domain model under `ProbHammer.Core.Domain.Catalogue` / `ProbHammer.Core.Domain.Roster` —
  `Datasheet` (named statlines, on-demand weapon-profile resolution), `Ability` (`Scope:
  Model|Unit`), `Unit`/`AttachedUnit` (Composite-pattern `ICombatUnit` abstraction), live
  keyword-union and Toughness-resolution rules, per-model-line remaining-count tracking, and
  the Attached Unit aggregate view (`AttachedUnitAggregator`). Proven against hand-built
  fixtures only (`tests/ProbHammer.Tests/Domain/Fixtures/`) — no BSData or export-parser
  wiring. 35 new tests, 207 total, all passing. Doc: `.claude/domain-model-11e.md`.
  **`Contracts/*`, `Catalogue/*`, and `Enrichment/Enricher.cs` are now superseded** by this
  new domain model — they remain in the tree, untouched, only because nothing has rewired
  `ArmyListParser`, `Web`, or `Simulation` onto it yet. **`Simulation/*` is paused** for the
  same reason (its one-flat-Toughness-per-defender assumption doesn't fit Attached Units or
  multi-statline datasheets). A later change is expected to rewire all three onto the new
  model; until then all four old areas keep working exactly as before.
- AP sign convention unified across the sim engine. `SimWeaponProfile.Ap` is now a negative
  integer matching the game value (AP-2 → -2). `AbilityProcessor.EffectiveSave` and
  `CombatSimulator` both use `save - ap`. `SimulationAdapter` passes AP through unchanged
  (removed the negation and `Math.Max(0,...)` guard). All tests updated; 2 new Gherkin tests
  added for the AP-2 → 5+ save boundary. 172 tests, all passing.
- Sub-ability rendering implemented in `_UnitCard.cshtml`.
  Both Abilities and While Leading loops now detect `•` in `ability.Text`,
  split on `\n`, and render sub-ability lines as `.sub-ability` divs with
  `.sub-ability-name` / `.sub-ability-text` spans. Flat abilities unchanged.
  Three CSS rules added to `site.css`. Build: 0 errors, 0 warnings.
- Sub-ability profile parsing implemented in `CatalogueParser.ParseProfiles`.
  Two-pass approach: Pass 1 collects standard types; Pass 2 collects non-standard
  typeNames as sub-ability groups, merging into matching parent abilities or creating
  new ones. 4 new tests added; 170 total, all passing.
- Regeneration v1 complete. Generated UI near-identical to original.
  No spec files were modified during generation — all gaps were minor.
- Sub-ability spec written and merged into `.claude/` docs.

---

## Known Issues

- `harden-army-list-parsing-for-android-exports` deliberately left one real ambiguity unresolved:
  Adeptus Custodes' Custodian Guard (`data/gw-android-export-custodes.txt`) fails to import with
  the existing unpartitionable-weapon-counts diagnostic (`1x Guardian spear` / `3x Praesidium
  Shield` / `3x Sentinel blade` against a total of 4) — the correct reading pairs Shield+blade as
  one 3-model sub-group, which can't be inferred from counts alone without risking a wrong merge on
  a genuinely-independent-alternatives case like Company Veteran's. Needs either a second real
  example to clarify the general pattern, or becomes moot once a NewRecruit/BattleScribe JSON
  import path exists (see `openspec/changes/import-battlescribe-json-rosters/`). Also still open:
  whether the bullet-continuation shape and the missing `ForceDisposition` line that change fixed
  are native to the Android app or an artifact of the WhatsApp relay both captured exports went
  through — a directly-saved Android export is expected ~2026-08-27 to check against.
- `resolve-core-rule-abilities`'s new `InfoLinkTypeScanTests` found a third, confirmed real
  ability-granting `infoLink` shape, `"infoGroup"` (129 occurrences across the corpus) — targets a
  `sharedInfoGroups`/`infoGroups` container holding its own nested `profiles` array of real
  `Abilities`-typeName content (confirmed: Adeptus Custodes' "Talons" holds two genuine,
  mutually-exclusive-by-modifier auras, "Null Aegis (Aura)" and "Deadly Unity (Aura)", currently
  invisible on `/LivePlay` — the same class of bug that change fixed for `"rule"`). Deliberately
  allowlisted rather than fixed blind, since no real `infoGroup`-carrying export was available to
  verify a fix against at the time. Tracked as a dedicated follow-up change once one is (a friend's
  Adeptus Custodes export, expected 2026-08-27).
- A real captured export (`data/gw-app-export-masters-of-the-maelstrom.txt`) names a weapon
  "Absolvor bolt pistol" (Garreon the Corpsemaster); the live BSData clone's own corpus spells it
  "Absolver bolt pistol" - a genuine BSData typo/spelling drift, not a parsing bug. Deliberately not
  special-cased (a curated mismatch map doesn't scale to the corpus size, per explicit user
  direction) - `ArmyRosterEnricher`'s "did you mean...?" suggestion (`BsdataNameSuggestion`,
  Levenshtein-based) surfaces this correctly on import, but the import itself still fails for this
  one unit until BSData's own data is fixed or the user edits their pasted text.
- `import-army-list-for-live-play`'s real-data verification (task 2.8) found several BSData
  naming shapes `ArmyRosterEnricher` now handles via fallback chains rather than a single direct
  match - see that change's summary above and `[[project_bsdata_name_mismatch_patterns]]` in
  auto-memory. Not exhaustively proven against the *full* corpus (only the three captured exports,
  covering ~4 factions) - a future corpus-wide scan (mirroring `bsdata-corpus-scan-tests`'s existing
  pattern) could surface further shapes the three captured exports don't exercise.
- 3 Ork weapons carry Strength as a dice expression (Battlewagon `D6+6`, Big Gunz [Legends] `2D6`,
  Deff Rolla Battle Fortress [Legends] `2D6`), which `WeaponProfile.S: int` can't represent. Needs a
  decision: leave failing / round to `DiceExpression.ExpectedValue()` / widen `S` to
  `DiceExpression`. Tracked (not resolved) by `CharacteristicResolutionScanTests`'s allowlist.
- The weapon-keyword-token scan (`WeaponKeywordScanTests`) found several tokens worth a dedicated
  future `WeaponKeywordParser` fix rather than staying allowlisted forever: a handful of case/
  punctuation variants of an already-recognized token (e.g. `"Devastating wounds"`, `"Ignores
  cover"`, `"Rapid fire 2"` — parser vocabulary is case-sensitive); dice-valued variants of an
  int-only mechanic (`"Rapid Fire D3"`/`"D6"`/`"D6+3"`, `"Sustained Hits D3"` — same class of gap as
  the Ork Strength issue above); and real, widespread core mechanics with no flag at all yet
  (`"Extra Attacks"` 150+ occurrences, `"Psychic"`/`"PSYCHIC"` ~280 combined, `"Lance"` ~70, `"One
  Shot"` ~30+). `ANTI-non‑MONSTER/VEHICLE 2+` (Aeldari's Stone Stave) also uses a U+2011
  non-breaking hyphen instead of ASCII `-`, plus a dual slash-separated exclusion target — a
  different targeting shape from the modeled Anti-X mechanic, not a simple spelling fix. BSData
  keywords are confirmed to be added mid-edition, so this needs periodic re-scanning via
  `WeaponKeywordScanTests`, not a one-time fix.

---

## Spec Debt

Nothing outstanding.

> Keep this section honest. If you change code without updating the
> corresponding spec file, log it here immediately. A growing list
> means the spec is drifting from reality.

---

## Next Session Prompt

**Immediate next step**: `import-army-list-for-live-play` is implemented (27/27 tasks) but not yet
archived — run the archive flow (`openspec-archive-change`/`/opsx:archive`) once the user has
reviewed it, syncing its four delta specs (`army-list-parsing`, `army-roster-enrichment`,
`army-list-import`, the `live-play-view` modification) into `openspec/specs/`.

**After that**, two candidates surfaced by this change itself, neither scoped or agreed yet — raise
with the user before starting either:

- The deferred "ability-text tuning pass" this change's own design.md explicitly left a slot for
  (between `enrich` and `/LivePlay`, operating on the built `ArmyRoster`'s distinct `Datasheet`s) —
  this is the same "Ability-text interpretation pass" idea from `.claude/vnext-ideas.md`/prior
  sessions (resolving a `Caveated = true` invulnerable save's linked `CaveatAbility.Text` against a
  small closed vocabulary of known real phrasings, flipping the result to `Caveated: false` with the
  resolved `MeleeInSv`/`RangedInSv` split), now with a second, independent motivation from the real
  import pipeline rather than only hand-authored fixtures. Not general ability-text-to-behavior
  parsing, which stays permanently out of scope. Start with an `openspec explore`/`propose` pass
  before writing code, per this project's established pattern for InSv work.
- A corpus-wide scan of `ArmyRosterEnricher`'s resolution fallback chains (mirroring
  `bsdata-corpus-scan-tests`'s existing permanent-scan pattern), since the current verification only
  covers the three captured exports (~4 factions) — see the new "Known Issues" entry above.

`redesign-invulnerable-save-display`, `structured-invulnerable-save`, and `bsdata-corpus-scan-tests`
are all implemented, archived, and synced to main specs; both permanent scans pass against the live
clone. Other candidate follow-ups surfaced by prior sessions, none scoped or agreed yet — raise with
the user before starting any of them:

- Triage the "Known Issues" weapon-keyword findings into real `WeaponKeywordParser` fixes: a
  case-insensitivity pass for the existing exact-match vocabulary, widening `RapidFire`/
  `SustainedHits` to `DiceExpression` (same representation question as the parked Ork Strength
  issue), and deciding whether `Extra Attacks`/`Psychic`/`Lance`/`One Shot` are common enough to
  earn dedicated `WeaponProfile` flags.
- The Ork dice-Strength `WeaponProfile.S` representation decision — same class of problem as the
  now-resolved `Statline.InSv` gap, deliberately handled separately.
