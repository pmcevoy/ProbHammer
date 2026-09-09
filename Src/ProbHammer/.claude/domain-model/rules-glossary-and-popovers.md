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
RuleDefinition(Name, Aliases, Text, Modifiers)   // Domain/Catalogue/Bsdata/RuleDefinition.cs - see
                                       // record's own doc comment

RuleGlossary                          // Domain/Catalogue/Bsdata/RuleGlossary.cs
  Build(BsdataClosure closure) -> RuleGlossary
                                       // see method's own doc comment for the full resolution-order/
                                       // indexing rationale (real Alias gaps: "Cleave",
                                       // "Close-quarters")
  TryResolve(string nameOrAlias) -> RuleDefinition?   // see method's own doc comment (non-throwing,
                                       // same Normalize pipeline)

  Normalize(string text) -> string    // private; every Name/Alias and query goes through this
                                       // ordered pipeline (lowercase, strip trailing value/dice
                                       // suffix, collapse "Anti-*" to bare "anti", strip non-
                                       // alphanumeric) since real text references a generic
                                       // mechanic's bare name with a value or target category
                                       // appended (e.g. "SUSTAINED HITS 1") - see the method's own
                                       // comment for the exact ordering constraints and real
                                       // examples. Dropped the full-corpus scan's (below) unresolved
                                       // bracket-token count from 1,964 to 2 - see
                                       // rules-glossary-popovers/design.md for the full before/after.

RuleTextTokenizer                     // Domain/Catalogue/Bsdata/RuleTextTokenizer.cs - see the
                                       // class's own doc comment
  ExtractBracketTokens(text) -> IReadOnlyList<string>
                                       // simple non-nested "[...]" regex extraction; normalizes
                                       // each captured token (below)
  Normalize(string token) -> string   // see method's own doc comment; a "light" pass distinct from
                                       // RuleGlossary's own "aggressive" Normalize (a bracket goes
                                       // through this first, then that when resolving)

RuleTextEmphasisRenderer              // Domain/Catalogue/Bsdata/RuleTextEmphasisRenderer.cs - see
                                       // the class's own doc comment
  Render(text, renderBracket: Func<string, (string Inline, string Popovers)>)
    -> (string Inline, string Popovers)
                                       // see class's own doc comment

ResolvedBsdataCatalogue.Glossary: RuleGlossary
                                       // built alongside the rest of ResolvedBsdataCatalogue.Build
                                       // - no new cache, rides BsdataCatalogueCache for free
```

**`/LivePlay` wiring** (`ProbHammer.Web`): `IArmyRosterProvider.Build` returns
`ArmyRosterBuildResult(ArmyRoster Roster, RuleGlossary Glossary)` (was bare `ArmyRoster`) so both
`LivePlayModel.OnGet` and `LivePlayCasualtyService`'s fragment re-render have the glossary;
`UnitBlockRenderModel` carries it as a third field. Since `display-army-header-and-detachment-rules`,
the trigger/panel-building logic previously local to `_UnitBlock.cshtml` is extracted into
`ProbHammer.Web.Rendering.RulePopoverRenderer` — see its own class doc comment for why and how it's
scoped; `_UnitBlock.cshtml`'s own `BuildRulePopover` is now a one-line forwarder. Every
weapon-keyword chip and ability name is looked up via `TryResolve`; a match becomes a
`<button popovertarget="p-{id}">` trigger with a sibling `<div id="p-{id}" popover="auto">` panel
(never nested — a `<button>` can't contain interactive content), positioned via a per-instance
`anchor-name`/`position-anchor` CSS custom-property pair rather than the newer `anchor="..."` HTML
attribute (real device testing found the latter unsupported where the former's properties at least
register — see design.md's "Popover mechanism" Risks section). `RenderNestedReference` recurses
into a resolved reference's own `RuleDefinition.Text` to arbitrary depth (confirmed live to 3
levels) — see `BuildRulePopover`'s own doc comment for the `shownRuleNames` self-reference guard
and the real stack-overflow bug it fixes.

**Army header** (`display-army-header-and-detachment-rules`): `LivePlay.cshtml` renders a new
`_ArmyHeader.cshtml` partial above the unit-block loop, fed
`ArmyHeaderRenderModel(ArmyHeaderViewModel Header, RuleGlossary Glossary)` — mirrors
`UnitBlockRenderModel`'s pairing. See `ArmyHeaderViewModel`'s own doc comment for what
`LivePlayModel.BuildArmyHeader(roster)`/`BuildArmyRules(roster)` compute, why the ArmyRule
aggregation is deliberately separate from `AttachedUnitAggregator.PromoteArmyRuleAbilities`'s
existing per-unit dedup (which is unchanged — the per-unit box keeps rendering alongside the new
header), and why it carries no DP/points cost. The rules section lays out two columns
(Army/Detachment) via the same `<thead><th>` convention the weapon tables use; a Detachment renders
its own name once as plain text with one `RulePopoverRenderer`-built trigger per resolved rule
beneath it.

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
tokens and resolving each against that closure's glossary. `RuleGlossary.Normalize`'s landing
dropped the unresolved-occurrence count from 1,964 to 2, individually allowlisted in
`BracketTokenResolutionAllowlist.cs` as one-off BSData authoring anomalies — see
`rules-glossary-popovers/design.md` for the full before/after and what the two remaining anomalies
are.

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
