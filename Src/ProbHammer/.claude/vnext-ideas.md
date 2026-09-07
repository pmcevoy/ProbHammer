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

- **Characteristic-modification domain hardening: the general engine (still not built).**
  `introduce-characteristic-domain-model`, `unify-invulnerable-save-characteristic-view`, and
  `unify-objective-control-characteristic-view` (all archived) wired `CharacteristicValue`/
  `CharacteristicView` into every real consumer — `Statline.M`/`T`/`Sv`/`W`/`Ld`/`Oc`/`InSv` and
  `WeaponProfile.S`/`Ap`/`Bs`/`Ws` all now carry `OriginalValue`/`DerivedValue`/
  `ContributingAbilities` uniformly, and the old `StatlineFlag`/`StatlineFlagCharacteristic`
  side-channel is gone. What's described below is what's still missing: a real modification/
  legality *engine* handling multi-rule stacking order and a characteristic-type-wide clamp table —
  `StatlineFlagRule`/`AttachedUnitAggregator.ApplyStatlineFlagRules`'s own same-field-stacking order
  (when two *different* sources both touch one characteristic on the same unit) is still whatever
  order matches happen to be discovered in, not a rulebook-derived one.

  **A first attempt at the build-time half was built, then reverted (2026-09-07)** —
  `resolve-structured-characteristic-modifiers`, un-archived and removed from the tree; see
  `project_resolve_structured_characteristic_modifiers_change` memory for the full history. It
  resolved BSData's structured `BsModifier` data (1,169 real instances, corrected from an original
  717 estimate) into `Datasheet.StructuralModifierGrants`, applied by `AttachedUnitAggregator` only
  once the granting ability was confirmed present on a live unit — deliberately never baking a
  value into the shared, selection-blind `Datasheet` itself (see
  [[project_datasheet_selection_blind_catalog_boundary]]), *except* for a modifier judged
  "unconditional" by two signals: its own condition tree provably always true, or its granting
  entry not being a player-selectable `"type": "upgrade"` choice. Both signals turned out unreliable
  against real data, independently: the condition check recognized only Force-type gates, silently
  treating every other real condition shape (a specific other character present elsewhere in the
  army, a sibling relic selection, a live attachment state) as unconditional; and a real, capped,
  player-chosen squad slot (Adeptus Custodes' "Allarus Custodian (Vexilla & Misericordia)") is typed
  `"model"`, not `"upgrade"`, so its OC+1 baked into every model in the squad regardless of whether
  that model was actually taken. 42 of the 45 cases the "unconditional" path judged safe were
  confirmed wrong by cross-checking real BSData source text and NewRecruit's own rendering — not a
  scan/test gap (515+ automated tests and full-corpus scans all passed throughout), a genuine blind
  spot in what a scan can check versus a domain-informed manual read. The next attempt (not yet
  started) reframes the goal in two explicit phases instead of one bake/defer decision: first
  identify that an ability/rule/enhancement/wargear targets a specific characteristic ("caveated" —
  populating `ContributingAbilities` with no `DerivedValue`, using the existing `IsCaveated`
  vocabulary), strictly presence-gated at the `AttachedUnitAggregator` roster layer with no
  exception for "provably unconditional"; only later, as an explicit separate step, compute an
  actual derived value ("resolved"). Classification itself splits into structured-BSData-modifier
  detection (mechanical, most of what the reverted attempt built) and prose classification for
  abilities with no structured data behind them at all (confirmed real: Darnath Lysander's
  "Inspiring Commander," an army-wide OC-affecting ability with zero `BsModifier` data) — ties into
  the paused prose-classification schema described below. Same-unit application ships first;
  cross-unit application (Lysander boosting a different unit elsewhere in the army) is an explicit,
  deferred follow-on, since `AttachedUnitAggregator.Build` takes one `ICombatUnit` at a time with no
  roster-wide visibility today.

  **Remaining gap on the retyped fields**: `Statline`/`WeaponProfile.S`/`Ap`/`Bs`/`Ws` now use
  `ScalarCharacteristicView` (a `CharacteristicValue`, which does support `DiceCharacteristicValue`/
  `SymbolicCharacteristicValue`) but every real construction call site still only ever constructs a
  plain `NumericCharacteristicValue` via the implicit `int` conversion — `CharacteristicResolutionAllowlist.cs`
  still documents the same real, unfixed instance (Ork Battlewagon "D6+6" `S` text skipped rather
  than parsed as `DiceCharacteristicValue`) as before; the domain shape to fix it now exists, the
  parsing to populate it doesn't yet.

  **The prose half has a drafted, paused classification approach**: a confidence-tiered schema
  (Tier 0 no signal → Tier 1 "something's modified" → Tier 2 "this specific characteristic" → Tier 3
  a real hand-written `StatlineFlagRuleCatalogue` entry, promoted only by a person), an LLM pass
  proposed only as a Tier 1/2 discovery accelerant feeding a human-reviewed table — deliberately
  paused before writing any prompt. Ties into `live-play-phase-turn-tracker`: an ability's extracted
  Phase/TurnOwnership becomes automatically evaluable against real page state once classification
  can extract it.

  **One unresolved complication, no answer yet**: the rulebook's "Ignore Modifiers" rule lets a
  player selectively discard some applied modifiers while keeping others — correct resolution isn't
  a pure function of a final delta, it needs each applied modifier tracked as a discrete, toggleable
  item, materially bigger than a clamp layer and not scoped anywhere.

  **The rulebook's own Improve/Worsen text** (verbatim):

  > Improving WS, BS, Sv and Ld: subtract the amount from the number before the `+` (WS 3+ improved
  > by 1 → 2+). Worsening WS, BS, Sv and Ld: add to it (WS 3+ worsened by 1 → 4+). Improving AP:
  > subtract (AP -1 improved by 1 → -2). Reducing/worsening AP: add, **to a maximum of 0** (AP -1
  > worsened by 1 → 0; AP 0 worsened by 1 → 0, stays 0). Improving/worsening any other characteristic
  > (no `+`/`-` symbol): plain add/subtract (S improved by 1 → +1).

  That's 3 distinct arithmetic behaviors — `RollThreshold` (WS/BS/Sv/Ld: inverted, since a lower
  number is an easier die-roll bar), `ArmourPenetration` (AP: inverted, worsen capped at 0), `Plain`
  (everything else) — not one behavior per named characteristic. Clamp bounds are an *orthogonal*
  axis (WS/BS share a bound while sharing `RollThreshold`'s arithmetic with Ld, which has its own
  bound), so they're per-characteristic lookup data, not part of the arithmetic type.

  **Authoritative clamp table** (user-supplied verbatim — "or better"/"or worse" are exclusive of
  the named threshold):

  > Characteristics of '-', '\*' and 'N/A' can never be modified. Rules that modify a model's WS
  > and/or BS characteristic modify the WS and/or BS characteristic of every weapon equipped by that
  > model. After all modifiers have been applied: M cannot be less than 1". T cannot be less than 1.
  > Sv cannot be 1+ or better. InSv cannot be 1+ or better. Ld cannot be 4+ (or better) or 9+ (or
  > worse). OC cannot be less than 0 or '-'. Range characteristics cannot be less than 1". A cannot
  > be less than 1. WS cannot be 1+ (or better) or 7+ (or worse). BS cannot be 1+ (or better) or 7+
  > (or worse). S cannot be less than 1. AP cannot be worse than 0. D cannot be less than 1.

  Worked out: `Sv`/`InSv` floor at 2, no ceiling; `Ld` ∈ `[5,8]`; `WS`/`BS` ∈ `[2,6]`; `Oc` floors at
  0, no ceiling (can never produce `-` — moot, since a symbolic value can never be modified at all);
  `AP` ceilings at 0 (worsening stops there), no floor; `M`/`T`/`A`/`S`/`D`/Range all floor at 1 (1"
  for the length-valued ones), no ceiling.

  **Where `Improve`/`Worsen` belongs**: NOT on `CharacteristicValue` itself (would make the value
  type responsible for computing its own mutation — the same mistake `ComputeDerivedValue` was
  reverted for on `CharacteristicView`, see
  [[feedback_avoid_premature_behavior_on_unconsumed_domain_types]]) and not on a mutator rule
  directly, but on the `RollThreshold`/`ArmourPenetration`/`Plain` "Kind" concept as pure
  sign-resolution: `Kind.ResolveDelta(Improve, amount) -> signed int`. A mutator rule states intent
  in ability-vocabulary terms ("Vexilla improves Oc by 1"); that resolves to a signed `Add` step
  before reaching the engine, which never sees "Improve"/"Worsen" at all. Proposed layering, not
  built:

  ```
  CharacteristicView          pure data - Value/IsCaveated/ContributingAbilities/OriginalValue,
                               computes nothing (unchanged from today)
        ▲ built by
  Modification Engine         groups modifiers by step-type, applies Set→Multiply→Add→Divide→
                               Subtract→round-up (the 6-step algorithm), clamps to the
                               characteristic's bound (the table above)
        ▲ consumes
  CharacteristicModifier      one per (ability, target characteristic) pair - an ability
                               touching 2 stats emits 2 of these; StepType + signed Amount +
                               SourceAbility
        ▲ produced by matching
  Mutator rule                StatlineFlagRule's successor - recognizes an ability by
                               name+text, emits its Modifier(s)
  ```

  **Ordering is simpler than feared**: the rulebook's algorithm only requires *grouping by
  step-type* and applying step-types in the fixed sequence — same-step-type contributors just
  combine (sum for Add/Subtract, product for Multiply/Divide; both commutative, no rule-vs-rule
  priority needed within one step-type). `Set` is the exception — a replace, not a combine, that
  short-circuits every later step for that characteristic.

  **Two design questions have real, worked answers, ready whenever a real caller needs them**:
  - *Set-vs-Set conflicts*: given two abilities both `Set`-type on the same characteristic (e.g.
    "Set InSv 4+" and "Set InSv 5+"), keep the *better* value and discard the other, then apply any
    remaining `Improve`/`Worsen` on top — this is the real invulnerable-save FAQ rule ("only the
    best applies") generalized to any characteristic. Needs a `Kind.Better(a, b)` comparison
    alongside `ResolveDelta`.
  - *Multi-characteristic abilities* (e.g. "Add 1 to Attacks of all melee weapons and 1 to
    Strength"): don't invent a generic `CharacteristicModifier.Target` spanning
    Statline/WeaponProfile — feed both `Statline` and the relevant `WeaponProfile`s in as *context*
    to whichever hand-written rule recognizes the ability, and let that rule mutate directly, the
    same self-contained way `StatlineFlagRule.Apply` already works. Keeps
    `StatlineFlagRuleCatalogue`'s "no general ability-text parsing engine" non-goal intact.
    `Apply`'s signature eventually widens to also carry `WeaponProfile`s when a real rule needs it.

  **Also still open**: a ranged-aura ability ("Improve OC for all units within 6\" of this model")
  can't be safely auto-resolved — no positional/board-state data exists in this domain (root
  `CLAUDE.md`'s Deliberate Omissions) — so a matching rule should emit a `Caveated` view, not
  attempt a `Resolved` computation. No concrete real BSData instance has been pinned down yet; find
  one before scoping this into a change, per
  [[feedback_verify_bsdata_mapper_changes_against_real_export]].

  **Next real step**: wire `CharacteristicValue` into `WeaponProfile.S` (smallest bounded starting
  point — already has a confirmed real bug driving it, the Ork Battlewagon dice-notation skip
  above) as a stress test before designing the engine further in the abstract. Expect friction:
  `CharacteristicValue` has no comparison/ordering operator yet, and every site doing raw int
  arithmetic against these fields needs to unwrap/repack rather than operate directly.

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
