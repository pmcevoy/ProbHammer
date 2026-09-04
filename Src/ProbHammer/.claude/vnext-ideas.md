# vNext Ideas — Deferred Features

Ideas and follow-on work that came up during 11e development and were deliberately deferred
rather than built. Nothing here is scheduled — this is a parking lot to draw from when picking
the next `openspec` change. Update this file whenever a new idea gets deferred, and remove an
entry once it's been turned into a change (archived changes remain the historical record).

---

## `/LivePlay` — Attached Unit view

- **Per-ability Model/Unit scope marker.** `live-play-landscape-only` merged the Statline grid's
  separate Model Abilities and Unit Abilities columns into one stacked-list column, at the user's
  own explicit request — recalling that the two-column split existed only to distinguish
  model-scoped from unit-scoped abilities, and deciding that distinction doesn't need its own
  column. The user floated a future prefix/symbol on the ability name itself (mirroring the
  Enhancement `✦` prefix, `AbilityDisplayName` in `_UnitBlock.cshtml`) as the way to surface the
  distinction again if wanted. `Ability.Scope` and the view models' `ModelAbilities`/
  `UnitAbilities` split were deliberately left untouched by that change specifically so this stays
  easy to build later with no domain rework.
- **Core/Faction/Psychic ability source tagging** — distinguish where an ability comes from,
  not just its name/scope.
- **Per-`ModelLine` keywords.** Needed so a leader's personal keyword leaves the unit's effective
  keyword union when that specific model is removed as a casualty — currently keywords are only
  tracked at the component (`Unit.Datasheet.Keywords`) level.
- **Multi-profile-weapon "select one profile" disclaimer** — some weapons have multiple firing
  profiles the player picks between; the view doesn't currently flag this.
- **Ability-driven attack modifiers** — e.g. a unit-wide "+1 Attack to melee weapons" ability
  changing a total from A28 to A35. Not modeled at all yet. Expected to be a consumer of the
  "Ability-text interpretation pass" idea below, once that exists, rather than its own bespoke
  parsing.
- **Splitting `Keywords` into unit-wide-union vs. per-component, or adding `FactionKeywords`** —
  currently left as one unioned `IReadOnlySet<string>`.
- **Split `wwwroot/css/site.css` into a `/LivePlay`-only stylesheet.** Deferred by
  `archive-10e-pipeline`: the file's 10e-only, `/LivePlay`-only (`.live-play-page`-scoped),
  and shared base rules are interleaved with no clean physical boundary, and the archive
  change's own goal was safety (no risk to the one live page), not a redesign. `site.css`
  currently still ships several hundred lines of now-dead 10e selectors (harmless — they
  never match anything once `Index`/`ArmyView` markup is gone — but worth tidying up
  eventually). Extracting `/LivePlay`'s rules into their own file would let `_Layout.cshtml`
  stop loading dead CSS and make the stylesheet's scope match the app's actual scope
  (`/LivePlay`-only).

## Domain / data pipeline

- **Ability-text interpretation pass.** A distinct pass, run *after* `BsdataDatasheetMapper
  .BuildDatasheet` (not inside it), that classifies a specific ability's `Description` text
  against a small closed vocabulary of known fixed phrasings and folds the result back into the
  domain model. Explicitly **not** general ability-text-to-behavior parsing (that stays
  permanently out of scope per `domain-model-11e.md`'s Deliberate Omissions) — this is
  pattern-matching known, previously-catalogued sentences, not NLU. `resolve-known-ability-effects`
  shipped two narrow slices of this idea directly: `InvulnerableSaveCaveatClassifier` (resolves a
  footnoted InSv's linked ability text against four known templates) and `statline-flag-rules`
  (Shield Dome/Vexilla, matching one specific ability by exact Name+Text to derive a flagged
  Statline value for a resolved unit) — see `domain-model-11e.md`'s own sections for both. Should
  still resolve Model-vs-Unit scope for Enhancements specifically: today every Enhancement is
  displayed at Unit scope unconditionally (`resolve-enhancement-abilities` shipped Enhancement
  *resolution*, not scope classification), even though a real Enhancement's actual scope depends
  on its rules text and can genuinely be Model — this pass is where that distinction should get
  made instead of guessed at, per the same "no scope on Enhancement" acknowledgment.

  Discussion during `resolve-core-rule-abilities`' follow-up (2026-08-24) sharpened the shape of
  this considerably — noting it here so it isn't relitigated from scratch when this gets picked
  up:
  - **Not just Enhancements — every ability source is Model/Unit-blind today.**
    `BsdataDatasheetMapper.MapAbility` hardcodes `Scope = AbilityScope.Unit` unconditionally for
    every origin (Intrinsic, Enhancement, OptionalGrant, CoreRule); `AbilityScope.Model` is set
    nowhere in real-data code, only in the unused `Examples/Datasheets.cs` fixtures. Real text
    reliably signals the split (confirmed against real Black Templars rule text: `Martial Honour`
    — *"add 5 to **this model's** Objective Control"* — is Model; `Crusade of Wrath` — *"...models
    in **that unit**"* — is Unit) but nothing reads it yet.
  - **Architecture: a composable, user-toggleable rule pipeline, not a single fixed pass.**
    `statline-flag-rules` implements a small, fixed, closed-vocabulary version of this (a rule
    matches one exact ability, mutates a flagged Statline value, re-evaluated live as casualty
    state changes — see `domain-model-11e.md`). Still open beyond that seed: a genuinely
    open-ended, **player-toggleable** rule set (so a player could disable a specific rule's
    effect) covering a much wider vocabulary than InSv/OC — the "Ability-driven attack modifiers"
    idea above (e.g. folding a "+1 Attack to melee weapons" ability into weapon-aggregation
    totals) is the next natural driver for growing this vocabulary, not yet designed.

- **Characteristic-modification domain hardening, and `statline-flag-rules` generalization.**
  Supersedes/absorbs an earlier, narrower note here on `statline-flag-rules` modifier-stacking
  found during a `resolve-known-ability-effects` code-review walkthrough (2026-09-01) — a same-day
  explore-mode session broadened it into the fuller picture below. Not yet proposed.

  **The stacking/ordering/cap bug that started this**: real 40k rules let multiple abilities
  cumulatively modify one characteristic, with a defined application order and, for some
  characteristics, a hard cap — none of which the current mechanism models.
  `AttachedUnitAggregator.ApplyStatlineFlagRules` applies every matching rule for a row in whatever
  order `abilities` happens to enumerate them (no absolute-vs-relative ordering), and two
  absolute-set rules on the same row/characteristic don't compose — the later one just overwrites
  the earlier one's result outright. Separately, `LivePlay.cshtml.cs`'s `GroupStatlines` only ever
  credits the *first* matching `StatlineFlag` per characteristic (`.FirstOrDefault(f =>
  f.Characteristic == ...)`), so even where mutations correctly do stack, only one contributing
  ability ever appears in the footnote/legend. Today's catalogue (Shield Dome → InSv, Vexilla → OC)
  can't exercise either gap — exactly one rule per characteristic today — but a third rule targeting
  an already-covered characteristic would hit both immediately.

  **Two confirmed domain gaps block any of this growing past 2 hand-written rules**:
  1. `Statline` (M/T/Sv/W/Ld/Oc) and `WeaponProfile.S`/`Ap` are plain `int` throughout — no
     `DiceExpression`, no representation for a non-numeric (`-`/`*`/`N/A`) characteristic. Already a
     confirmed, real, allowlisted-not-fixed gap: `CharacteristicResolutionAllowlist.cs` skips
     dice-notation `S` text (Ork Battlewagon "D6+6", Big Gunz) rather than parsing it, because
     nothing needed it fixed yet.
  2. No characteristic-modification **legality/clamping layer** exists at all —
     `StatlineFlagRule.Apply` does raw arithmetic with zero bounds-checking, but real 11e Core Rules
     impose caps this app encodes nowhere (M can't go below 1", Ld can't be better than 4+, a
     `-`/`*`/`N/A` characteristic can never be modified, a WS/BS-modifying rule targets a model's
     *weapons* not its own stat) plus a defined 6-step application order: **(1)** set/replace to an
     exact value first — anything set to `0`/`-`/`*` here is untouchable by steps 2–5 — **(2)**
     multiply, **(3)** add, **(4)** divide, **(5)** subtract, **(6)** round fractions up.

  **A structural discovery de-risks half of this.** BSData already ships fully-deserialized,
  structured `BsModifier` (`Type`/`Field`/`Value`/`Conditions`) data for *build-time* characteristic
  changes — a corpus scan found 717 real instances (list-construction-option-driven, e.g. "select
  this wargear and Sv improves to 2+"), parsed today but read only for hidden-gating
  (`IsGameModeGated`), never for literal effect. A purely structural resolver over `Field`/`Value`
  against the known characteristic-id table could deterministically resolve most of these
  (~83% are plain `set`/`increment`/`decrement`; `floor`/`ceil` pairs need sibling-entry correlation
  for the real WS/BS-capped-at-2+ core rule; `replace` looks like safely-ignorable BattleScribe
  display bookkeeping, not a 6th real operation) with zero AI involvement. But this only ever covers
  build-time facts — it has no concept of phase/turn/positional triggers, so a genuine live-trigger
  ability (Vexilla; a Marshal's per-combat weapon buff) has **zero** structured signal and stays a
  prose problem regardless.

  **The prose half has a drafted, paused classification approach**: a confidence-tiered schema
  (Tier 0 no signal → Tier 1 "something's modified" → Tier 2 "this specific characteristic" → Tier 3
  a real hand-written `StatlineFlagRuleCatalogue` entry, promoted only by a person) plus a 15-ability
  stress-test sample set covering weapon-scoped/unnamed targets, multi-modification abilities,
  enemy-debuffs, and several confirmed `never`-bucket flavors (positional range, an incoming attack's
  transient Damage, cross-referencing another rule's default behavior). An LLM pass (Haiku or
  smaller, batch job on BSData updates, never a live runtime call) was proposed as a Tier 1/2
  *discovery accelerant* feeding a human-reviewed checked-in table — deliberately paused before
  writing any prompt or making any call, per the explicit "hold enthusiasm until we know what
  questions we're asking." **Ties into phase tracking**: `live-play-phase-turn-tracker` shipping
  means an ability's extracted `Phase`+`TurnOwnership` is now automatically "evaluable" against real
  page state the moment classification can extract it, rather than landing in an unclassifiable
  bucket for want of anywhere to check it against.

  **One unresolved complication, no answer yet**: the rulebook's own "Ignore Modifiers" rule lets a
  player selectively discard some applied modifiers while keeping others (e.g. keep a beneficial
  one, ignore a detrimental one) — correct resolution isn't a pure function of a final delta, it
  needs each applied modifier tracked as a discrete, toggleable item, a materially bigger feature
  than a clamp layer and not scoped anywhere yet.

  **Sequencing**: land the legality/clamp layer plus `DiceExpression` support for `WeaponProfile.S`
  before a 3rd `StatlineFlagRuleCatalogue` entry lands, hand-written or corpus-discovered — the
  structural-modifier and prose-classification tracks above are independent discovery work and
  don't block this, since neither computes a final mutated value on its own.

  **Update 2026-09-04**: `introduce-characteristic-domain-model` landed the shape half of this —
  `CharacteristicValue` (numeric/dice/symbolic, generalizing the `Statline`/`WeaponProfile` int/
  `DiceExpression` inconsistency this note's gap #1 describes) and `CharacteristicView`
  (`OriginalValue`/`DerivedValue`/`ContributingAbilities`/`IsCaveated`, generalizing
  `InvulnerableSave.Caveated`/`CaveatAbility` to N abilities). Both are unconsumed - new types only,
  not wired into `Statline`/`WeaponProfile` yet.

  A same-day follow-on discussion chased the *other* half - the official Improve/Worsen rule text
  itself:

  > Improving WS, BS, Sv and Ld: subtract the amount from the number before the `+` (WS 3+ improved
  > by 1 → 2+). Worsening WS, BS, Sv and Ld: add to it (WS 3+ worsened by 1 → 4+). Improving AP:
  > subtract (AP -1 improved by 1 → -2). Reducing/worsening AP: add, **to a maximum of 0** (AP -1
  > worsened by 1 → 0; AP 0 worsened by 1 → 0, stays 0). Improving/worsening any other characteristic
  > (no `+`/`-` symbol): plain add/subtract (S improved by 1 → +1).

  That's exactly 3 distinct arithmetic behaviors (Threshold: WS/BS/Sv/Ld: Worsen; ArmourPenetration:
  AP, worsen capped at 0; Plain: everything else) - not one behavior per named characteristic
  (WS/BS/Sv/Ld would share byte-identical logic if split that way). Clamp bounds (gap #2's `M ≥ 1"`,
  `Ld ∈ [4,9]`, `WS/BS ∈ [2,7]`) turned out to be an *orthogonal* axis to this - WS and BS share a
  clamp bound while sharing Threshold's arithmetic with Ld, which has its own different bound - so
  clamps are per-named-characteristic data (a lookup), not per-arithmetic-behavior code, and don't
  belong on the same 3-case type as the arithmetic itself.

  A concrete sketch was drafted (`CharacteristicKind` - 3 sealed private subtypes behind static
  `Threshold`/`ArmourPenetration`/`Plain` instances, mirroring `DiceExpression.D3`/`D6`'s
  static-preset convention, each with `Improve(int, int) -> int`/`Worsen(int, int) -> int`) but
  **deliberately not committed** - caught by direct user review on two grounds: (1) it never touches
  `CharacteristicValue` at all, taking and returning a bare `int` - exactly the "ad hoc int"
  representation `introduce-characteristic-domain-model`'s own `proposal.md` was written to get away
  from, reintroduced as a second, competing currency right next to `NumericCharacteristicValue`; and
  (2) more generally, the whole engine/kind/clamp thread had sprawled well past this project's
  established rhythm of building only against a real, present consumer - nothing in `/LivePlay`
  needs any of it today. If revived, `Improve`/`Worsen` should take and return `CharacteristicValue`
  (mirroring `DiceExpression.Add`/`.Scale`'s own take-and-return-self shape), never drop to a bare
  `int` mid-pipeline, and should stay unattempted until wiring `CharacteristicValue` into a real
  consumer (see next paragraph) surfaces an actual need for it.

  **The suggested next real step, once someone picks this back up**: wire `CharacteristicValue`
  into an actual consumer as a stress test, rather than continuing to design further in the
  abstract. `WeaponProfile.S` is the smallest bounded starting point - unlike `Statline`'s six
  fields, it has one already-confirmed, already-documented real bug driving it
  (`CharacteristicResolutionAllowlist.cs` skips dice-notation `S` text - Ork Battlewagon "D6+6" -
  rather than parsing it), so wiring it in has a concrete payoff, not just validation-for-its-own-
  sake. Expect real friction, not a trivial swap: `CharacteristicValue` has no comparison/ordering
  operator yet (needed if any consumer ever compares `S` values, though none does today), and every
  site currently doing raw int arithmetic against these fields would need to unwrap/repack rather
  than operate directly - `Statline`'s six fields are a bigger version of the same problem
  (`ToughnessResolution`'s "highest T among present models" comparison has no home on
  `CharacteristicValue` as it stands, and `StatlineFlagRule.Apply`'s raw arithmetic would need to
  unwrap/repack too) and would surface it worse. The int→`CharacteristicValue` implicit conversion
  already added means most *construction* call sites (tests included) shouldn't need to change;
  it's *reading* sites doing arithmetic/comparison that carry the real cost.

- **Detachment rule structural-modifier detection ("phase 2" of `display-army-header-and-
  detachment-rules`).** That change captures every Detachment's rule text verbatim and renders it
  army-wide in the `/LivePlay` header - deliberately not attempting to tie any rule to a specific
  unit. A live-clone corpus scan (2026-08-25, during that change's exploration) found this is
  possible for a real minority of the corpus: 22 of 280 Detachments (~8%) carry a BSData `modifiers`
  entry - a characteristic increment/decrement/multiply, or a Keywords `append` - directly on a
  specific existing unit's own profile, gated by the identical Detachment-selection condition
  (`selections`/`force`-scope/`childId`) as the Detachment's own visibility gate. Confirmed
  examples: Black Templars' `Marshal's Household` → Sword Brethren Squad's OC +1 (byte-for-byte
  matches a `Statline` characteristic); Adepta Sororitas' `Sanctified Orators` → 14 different units'
  Leadership improved; Chaos Space Marines' `Devotees of Destruction` / Agents of the Imperium's
  `Ordo Hereticus, Purgation Force` → a literal `Keywords` append on several units each. For this
  subset, the Detachment rule could render as an orphaned per-unit ability - Templar-Vows-style,
  same `AbilityOrigin`/attachment shape `resolve-core-rule-abilities` already built for chapter-wide
  Core rules - rather than only the army-wide header block. The detection signal (a Detachment's own
  force-selection-gate id also appearing in some datasheet's own `modifiers`, on a field other than
  `hidden`/`category`) is a natural fit for a permanent, explicit-only corpus-scan test, mirroring
  `InfoLinkTypeScanTests`/`WeaponKeywordScanTests`, so a newly-shipped Detachment with this shape
  gets caught rather than silently missed.

  Deliberately **not** assumed to generalize to "detachment rules are statline modifiers" - the
  other 258 Detachments (92%) either only gate visibility/availability (which Enhancements exist,
  which units the army can include) or are genuinely free-form, phase-conditional, army-state-
  tracking mechanics (Command-phase Doctrine switches, a mutable "Favoured Champions" unit, etc.)
  with no structural per-unit binding at all - much closer to the still-deferred "Gameplay bounded
  context" idea below than to a stat-mutation problem. A real example surfaced during discussion,
  worth keeping as illustrative of the partial-mechanization risk: Black Templars' `Close-Range
  Eradication` ("Ranged weapons equipped by Adeptus Astartes models from your army have the
  [ASSAULT] ability, and each time an attack made [with] such a weapon targets a unit within 12",
  add 1 to the Strength characteristic of that attack") is a plausible classifier target for the
  `[ASSAULT]` keyword grant half, but the "+1 Strength within 12"" half is a range-conditional
  combat-modifier with no structural BSData signal backing it at all - a rule can be *partially*
  mechanizable, and a future classifier needs to handle that split cleanly rather than assume
  all-or-nothing. Same permanently-deferred "no general ability-text-to-behavior parsing" boundary
  as the "Ability-text interpretation pass" idea above applies to the un-mechanizable half.

## Bigger picture

- **A full "Gameplay" bounded context** for live/mutable game state beyond casualty counts — other
  conditions that rules text sometimes references ("while this model is on the battlefield...",
  "gets Lethal Hits while within range of an objective marker"). Some ability text is permanently
  out of scope for auto-parsing because it needs state (positioning) the domain has no way to
  represent even with this context built — see `.claude/domain-model-11e.md`'s Deliberate
  Omissions. Battle-shock, per-unit Objective-control-goes-nothing display, and player-asserted
  turn/Command-phase tracking are no longer part of this deferred item —
  `half-strength-and-battleshock-indicators` and `live-play-phase-turn-tracker` implemented narrow,
  player-reported (never simulated) slices of all three, the latter persisted via `IPhaseTurnStore`
  in session. What's still missing here is the broader picture this bullet originally meant: the
  tracker only ever reflects what the player asserts (no prompting/simulation of the 2D6-vs-
  Leadership Battle-shock test or automatic phase advancement), and any positional/objective-state
  condition-tracking remains entirely absent.
