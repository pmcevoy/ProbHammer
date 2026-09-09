# Rules Glossary & Popovers

Full requirements: `openspec/changes/rules-glossary-popovers/`. Resolves BSData's `sharedRules`
(game-system level) and `rules` (faction/library level) arrays — previously read by no code path —
into a name/alias-queryable glossary, and mechanically extracts `[BRACKET]`-style cross-reference
tokens from ability/rule text for lookup against it. Text lookup and display only, never
behavioral interpretation, consistent with "ability text is never auto-parsed into behavior"
(Deliberate Omissions, deliberate-omissions.md). Wired into `/LivePlay`: every weapon-keyword chip and ability name
is a tap trigger opening a native Popover-API panel with that keyword's/ability's full rule text;
a resolved `[BRACKET]` reference inside becomes a further nested trigger.

```
RuleDefinition(Name, Aliases, Text)   // Domain/Catalogue/Bsdata/RuleDefinition.cs - one BSData
                                       // sharedRules/rules entry, carried through as opaque text

RuleGlossary                          // Domain/Catalogue/Bsdata/RuleGlossary.cs
  Build(BsdataClosure closure) -> RuleGlossary
                                       // iterates closure.Files in resolution order (local beats
                                       // imported) collecting Rules, then GameSystem?.SharedRules,
                                       // indexed by a Normalize()d key built from both Name and
                                       // every Alias - first occurrence wins. Indexing by Name too
                                       // is what makes a rule with no declared Alias at all (real
                                       // gaps: "Cleave", "Close-quarters") still resolve. See the
                                       // method's own doc comment for the full rationale.
  TryResolve(string nameOrAlias) -> RuleDefinition?   // non-throwing, same Normalize pipeline

  Normalize(string text) -> string    // private; every Name/Alias and query goes through this
                                       // 4-step pipeline (lowercase, strip trailing value/dice
                                       // suffix, collapse "Anti-*" to bare "anti", strip non-
                                       // alphanumeric) since real text references a generic
                                       // mechanic's bare name with a value or target category
                                       // appended (e.g. "SUSTAINED HITS 1"). See the method's own
                                       // doc comment for the exact ordering constraints (why each
                                       // step must run before the next) and real examples. Landing
                                       // this dropped the full-corpus scan's (below) unresolved
                                       // bracket-token count from 1,964 to exactly 2.

RuleTextTokenizer                     // Domain/Catalogue/Bsdata/RuleTextTokenizer.cs
  ExtractBracketTokens(text) -> IReadOnlyList<string>
                                       // simple non-nested "[...]" regex extraction - markup
                                       // characters are never a reference signal by themselves.
                                       // Normalizes each captured token (below).
  Normalize(string token) -> string   // public, reused by RuleTextEmphasisRenderer - strips
                                       // markup/Unicode noise from a bracket's raw inner text; a
                                       // "light" pass distinct from RuleGlossary's own "aggressive"
                                       // Normalize (a bracket goes through this first, then that
                                       // when resolving). See the method's own doc comment for the
                                       // exact characters handled.

RuleTextEmphasisRenderer              // Domain/Catalogue/Bsdata/RuleTextEmphasisRenderer.cs
  Render(text, renderBracket: Func<string, (string Inline, string Popovers)>)
    -> (string Inline, string Popovers)
                                       // Renders "*italic*"/"**bold**"/"^^small-caps^^" markup
                                       // (including a nested "***bold+italic***" split-close case)
                                       // as HTML styling only - never an interactive trigger by
                                       // itself. Every [BRACKET] token is handed to the caller's
                                       // renderBracket delegate, never resolved here, keeping this
                                       // class free of RuleGlossary/popover-id concerns. Returns
                                       // inline HTML separately from popover markup so a caller can
                                       // emit panels as flow-content siblings rather than nesting
                                       // them inside this text's own emphasis wrapping (which would
                                       // leak styling into a nested popover's inherited CSS).

ResolvedBsdataCatalogue.Glossary: RuleGlossary
                                       // built alongside the rest of ResolvedBsdataCatalogue.Build
                                       // - no new cache, rides BsdataCatalogueCache for free
```

**`/LivePlay` wiring** (`ProbHammer.Web`): `IArmyRosterProvider.Build` returns
`ArmyRosterBuildResult(ArmyRoster Roster, RuleGlossary Glossary)` (was bare `ArmyRoster`) so both
`LivePlayModel.OnGet` and `LivePlayCasualtyService`'s fragment re-render have the glossary;
`UnitBlockRenderModel` carries it as a third field. Since `display-army-header-and-detachment-rules`,
the trigger/panel-building logic previously local to `_UnitBlock.cshtml`
(`BuildRulePopover`/`NextPopoverId`/`RenderNestedReference`) is extracted into
`ProbHammer.Web.Rendering.RulePopoverRenderer` — one instance per render pass, constructed with the
`RuleGlossary` plus an explicit id-scope prefix (`"u{i}"` per unit block, `"hdr"` for the header)
instead of relying implicitly on the unit's page index, so `_ArmyHeader.cshtml` (below) can produce
identical markup with no unit index to scope against. `_UnitBlock.cshtml`'s own
`BuildRulePopover` is now a one-line forwarder. Every weapon-keyword chip and ability name is
looked up via `TryResolve`; a match becomes a `<button popovertarget="p-{id}">` trigger with a
sibling `<div id="p-{id}" popover="auto">` panel (never nested — a `<button>` can't contain
interactive content), positioned via a per-instance `anchor-name`/`position-anchor` CSS
custom-property pair rather than the newer `anchor="..."` HTML attribute (real device testing
found the latter unsupported where the former's properties at least register — see design.md's
"Popover mechanism" Risks section). `RenderNestedReference` recurses into a resolved reference's
own `RuleDefinition.Text` to arbitrary depth (confirmed live to 3 levels), threading a
`shownRuleNames` ancestor-chain set so a bracket resolving back to an already-open rule in its own
chain renders as plain text — several generic mechanics self-reference their own name
(`Sustained Hits`, `Anti`, `Cleave`), and an unguarded first version stack-overflowed rendering a
real weapon's chip.

**Army header** (`display-army-header-and-detachment-rules`): `LivePlay.cshtml` renders a new
`_ArmyHeader.cshtml` partial above the unit-block loop, fed
`ArmyHeaderRenderModel(ArmyHeaderViewModel Header, RuleGlossary Glossary)` — mirrors
`UnitBlockRenderModel`'s pairing. `LivePlayModel.BuildArmyHeader(roster)` builds the view model from
the roster's own metadata (Name/Faction/BattleSize/PointsSpent/PointsLimit/ForceDisposition) plus
two aggregations: the roster's resolved `Detachments` (carried through unchanged), and
`BuildArmyRules(roster)` — every distinctly-named `ArmyRule`-origin ability present on ANY unit,
deduped by Name, computed by walking every component's own `Datasheet.Abilities` directly (NOT
through `AttachedUnitAggregator`'s present-only view) — deliberately independent of casualty state,
since an ArmyRule is a fact about army identity, not which models are alive, so this header entry
never disappears mid-game. A new aggregation, separate from
`AttachedUnitAggregator.PromoteArmyRuleAbilities`'s existing per-unit dedup, which is unchanged —
the per-unit box keeps rendering alongside the new header. The rules section lays out two columns
(Army/Detachment) via the same `<thead><th>` convention the weapon tables use; a Detachment renders
its own name once as plain text with one `RulePopoverRenderer`-built trigger per resolved rule
beneath it. No DP/points cost rendered anywhere on the page.

Since `live-play-touch-target-improvements`, `_ArmyHeader.cshtml`'s own outer element is itself a
`<details class="army-header" open>` whose `<summary>` wraps `<h1 class="army-header-name">` —
the same whole-block-collapse pattern `_UnitBlock.cshtml`'s own outer `<details>` already uses
(collapsing hides the metadata line and every nested section together, in one action; each nested
`<details>` — Rules, All Keywords — keeps its own independent open/closed state across the parent
collapsing and re-expanding). The phase/turn tracker moved out of this partial entirely (see
"Phase/Turn Tracker" in phase-turn-tracker.md) specifically so it's structurally unaffected by this collapse. An
earlier draft nested a second `<details>` inside the header wrapping only the meta line and Rules,
keeping the tracker inside the header but outside that inner wrapper — reverted after direct user
review found the two stacked, near-identical disclosure bars ("Details" over "Rules") read as
redundant chrome rather than solving the actual want (collapsing the whole thing to see the unit
list).

### Full-Corpus Bracket-Token-Resolution Scan

`tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/BracketTokenResolutionScanTests.cs` —
same permanent, manually-triggered `[Fact(Explicit = true)]` pattern as the other CorpusScan tests.
Walks every closure's local `rules` text, the game system's `sharedRules` text (once, not per
starting file), and every locally-defined entry's resolved `Ability.Text`, extracting bracket
tokens and resolving each against that closure's glossary. The first run (before `RuleGlossary`'s
normalized-key resolution existed) found 1,964 unresolved occurrences across 71 tokens; after that
landed, exactly 2 remain, individually allowlisted in `BracketTokenResolutionAllowlist.cs` as
one-off BSData authoring anomalies (a bracket-placement typo in one Custodes ability; a stray
non-reference bracket in one Blood Angels ability) — neither fixed, since no general rule could
resolve either without risking a wrong fix elsewhere.

### Remaining Scalar Characteristics Retyped

`Statline.M`/`T`/`Sv`/`W`/`Ld` and `WeaponProfile.S`/`Ap`/`Bs`/`Ws` (`Bs` on `RangedWeapon`, `Ws` on
`MeleeWeapon`) are `ScalarCharacteristicView`, not plain `int` — the same shape `Oc`/`InSv` already
had (`unify-objective-control-characteristic-view`/`unify-invulnerable-save-characteristic-view`).
`InSv` itself is untouched (still `ResolveInvulnerableSave`'s own dedicated path). Every real BSData
mapping call site (`MapStatline`/weapon profile parsing) wraps its parsed base value as a plain,
non-caveated view (`ContributingAbilities: []`) — nothing populates a real caveat on any of these
five characteristics yet. `/LivePlay`'s rendering (`LivePlayModel.GetScalarField`/`GroupStatlines`,
`_UnitBlock.cshtml`'s `RenderScalarTile`) already reads and displays all six scalar characteristics
uniformly through the same flagged-tile-and-legend mechanism `Oc`/`InSv` established, so a real
caveat source landing on any of `M`/`T`/`Sv`/`W`/`Ld` in the future needs no rendering change — only
something that actually populates `ContributingAbilities`.

A structured-BSData-modifier resolver (baking/deferring `BsModifier` data into these fields) was
built and then reverted (2026-09-07) — see `project_resolve_structured_characteristic_modifiers_change`
memory for the full history. Two independent, real-data-confirmed defects killed it: (1) its
condition-tree check recognized only Force-type gates, silently treating BSData's other condition
shapes (a specific other unit/character present, a sibling relic selection, a live attachment
state) as unconditional; (2) its "is this optional" signal (`entry.Type == "upgrade"`) is
unreliable — a real, capped, player-chosen squad slot (Adeptus Custodes' "Allarus Custodian
(Vexilla & Misericordia)") is typed `"model"`, not `"upgrade"`, causing its OC+1 to bake into every
model in the squad unconditionally. Both defects trace to the same root cause: `Datasheet` is a
selection-blind catalog (see `project_datasheet_selection_blind_catalog_boundary`), and this
resolver existed specifically to bypass that boundary for cases it judged "safe" — no BSData signal
proved reliable enough to make that judgment. The planned replacement (not yet built) instead
extends the existing caveat/resolved vocabulary (`CharacteristicView.IsCaveated`) uniformly: first
identify that an ability/rule/enhancement/wargear targets a specific characteristic ("caveated"),
strictly presence-gated at the `AttachedUnitAggregator` roster layer like `StatlineFlagRule` already
is — never baked into the shared `Datasheet` — with computing an actual derived value ("resolved")
as an explicit later step.
