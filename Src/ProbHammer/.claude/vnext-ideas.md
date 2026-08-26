# vNext Ideas — Deferred Features

Ideas and follow-on work that came up during 11e development and were deliberately deferred
rather than built. Nothing here is scheduled — this is a parking lot to draw from when picking
the next `openspec` change. Update this file whenever a new idea gets deferred, and remove an
entry once it's been turned into a change (archived changes remain the historical record).

---

## `/LivePlay` — Attached Unit view

- **Phase-level section toggling across all units at once** — collapse/expand every unit's
  statline/ranged/melee/abilities section together, reusing the same disclosure unit each
  section already uses individually. Raised again during `half-strength-and-battleshock-
  indicators`' exploration, as the motivation for putting that change's half-strength/Battle-
  shock status glyphs directly on the unit-name header (the one thing that stays visible
  regardless of section collapse state) rather than in a separate row — deliberately kept out of
  that change's own scope, still a candidate for its own change.
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
  domain model as a new, immutable `Datasheet`. Explicitly **not** general ability-text-to-
  behavior parsing (that stays permanently out of scope per `domain-model-11e.md`'s Deliberate
  Omissions) — this is pattern-matching known, previously-catalogued sentences, not NLU. First
  concrete driver: the InSv-caveat representation change (mapper always marks a footnoted/
  ability-linked invulnerable save `Caveated = true` and captures the resolved ability rather
  than interpreting it inline; this pass is what would later resolve the known cases and flip
  `Caveated` back to `false`) — deliberately deferred out of that change's scope, to be designed
  as its own change once the shape needed here is clearer. Longer-term motivation is the
  "Ability-driven attack modifiers" idea above (e.g. folding a "+1 Attack to melee weapons"
  ability into weapon-aggregation totals) — same class of problem, not yet designed together.
  **Revisit once the InSv-caveat change is implemented/archived — explicitly asked to be
  reminded.** Should also resolve Model-vs-Unit scope for Enhancements specifically: today every
  Enhancement is displayed at Unit scope unconditionally (`resolve-enhancement-abilities` shipped
  Enhancement *resolution*, not scope classification), even though a real Enhancement's actual
  scope depends on its rules text and can genuinely be Model — this pass is where that
  distinction should get made instead of guessed at, per the same "no scope on
  Enhancement" acknowledgment.

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
  - **`Leader`/`Support`/`Attached Unit` should be dropped entirely, not scope-classified.** These
    three are redundant with the export's own "Attached as: Role" line (already parsed and
    rendered) — the app trusts the export's already-validated attachment, same reasoning as the
    existing "no wargear constraint modeling" stance, so there's no live-play value in also
    showing the attachment-eligibility rules text. Note `Attached Unit`'s own text is Unit-shaped
    by the Model/Unit heuristic above (*"...attached to **this unit** instead"*) — this is a
    deliberate override of that heuristic, not something the heuristic itself would derive, so
    keep it a small exact-name drop-list (same pattern `WeaponKeywordParser`/`RuleGlossary` already
    use for exact-name matching) rather than folding it into general scope classification.
  - **Wargear-granted invulnerable saves should convert to a populated InSv box, not render as a
    plain ability chip.** Concrete driver: Impulsor's `Shield Dome` (*"The bearer has a 5+
    invulnerable save"*). Distinct from the InSv-caveat case above, though it renders the same
    way in the end (a value in the InSv box, linked text beneath it) — the caveat case *caveats an
    existing intrinsic value* (footnoted `"5+*"` on the Unit profile itself); Shield Dome *grants*
    one where the Unit profile's own `InSv` is empty (Impulsor has no baseline invulnerable save
    at all). Keep these two as distinct recognized shapes rather than forcing the wargear-grant
    case through the existing `Caveated`/`CaveatAbility` pair unchanged.
  - **Architecture: a composable, user-toggleable rule pipeline, not a single fixed pass.** Rather
    than one hardcoded transformation, envisioned as a pipeline of named rules ("drop
    Leader/Support/Attached Unit", "convert Shield Dome to a granted InSv", "resolve a known
    InSv-caveat to non-caveated", ...) that mutate the resolved domain model after
    `BuildDatasheet`, each independently enable/disable-able by the player, and potentially
    re-evaluated as live state changes (e.g. a rule that only applies while its granting model is
    not a casualty) — a materially bigger scope than "classify once at import time," worth
    designing as its own change once picked up, not assumed.

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

- **A full "Gameplay" bounded context** for live/mutable game state beyond casualty counts — turn
  tracking, and other conditions that rules text sometimes references ("while this model is on
  the battlefield...", "gets Lethal Hits while within range of an objective marker"). Some ability
  text is permanently out of scope for auto-parsing because it needs state (positioning) the
  domain has no way to represent even with this context built — see `.claude/domain-model-11e.md`'s
  Deliberate Omissions. Battle-shock and per-unit Objective-control-goes-nothing display are no
  longer part of this deferred item — `half-strength-and-battleshock-indicators` implemented a
  narrow, player-reported (never simulated) slice of both. What's still missing here is the
  broader picture this bullet originally meant: turn/Command-phase tracking (so Battle-shock could
  in principle even be prompted rather than purely player-toggled) and any other condition-tracking
  beyond that one narrow slice.
