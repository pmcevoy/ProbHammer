# vNext Ideas — Deferred Features

Ideas that came up during 11e development and were deliberately not built. This is a parking lot
to draw from when picking the next `openspec` change, not a record of what shipped — keep entries
short, and delete one once it's turned into a change (the archived change is the historical
record, not this file).

---

## `/LivePlay` — Attached Unit view

- **Per-ability Model/Unit scope marker.** A prefix/symbol on the ability name (mirroring the
  Enhancement `✦` prefix) to show whether an ability is model- or unit-scoped.
- **Psychic ability source tagging** — `AbilityOrigin` already distinguishes Core rules from
  Faction-wide ones (`CoreRule`/`ArmyRule`); a Psychic-specific tag doesn't exist yet.
- **Multi-profile-weapon "select one profile" disclaimer** — some weapons have multiple firing
  profiles the player picks between; not flagged today.
- **Ability-driven attack modifiers** — e.g. a unit-wide "+1 Attack" ability changing a printed
  total. Depends on the `WeaponProfile`-targeting rule effects idea below.
- **Split `Keywords` into unit-wide-union vs. per-component, or add `FactionKeywords`** — currently
  one unioned set.
- **Split `wwwroot/css/site.css` into a `/LivePlay`-only stylesheet** — it still ships dead 10e
  selectors interleaved with the live rules.

## `WeaponProfile`-targeting rule effects — phased plan

Explored in depth 2026-09-10 (sibling to the already-shipped Statline characteristic-effect
resolution work — `RuleEffectClassifier`/`CharacteristicEffect`/`CharacteristicModificationResolver`/
`RuleClassificationBaseline`). Six phases, in dependency order — only the first is ready to scope as
a real OpenSpec change; the rest stay here until their turn. Don't re-litigate the decisions already
made below without new evidence.

- **Phase 0 (ready to propose now)**: give `WeaponContribution` its own weapon Name (currently
  absent — the rendered group name today is "whichever contributor was inserted first," not
  reliable). Unlocks two things at once: named-weapon effect targeting later, and immediate
  composite group naming today — `AggregateWeaponEntry` groups by `WeaponProfileEqualityKey`,
  which deliberately excludes Name, so two differently-named weapons sharing an identical profile
  already silently merge under one arbitrary name; once Name is tracked per contribution, a merged
  group should render every distinct name it merged, joined ("Bolt rifle and Combat Rifle";
  Oxford-comma for 3+, mirroring `AttachedUnit.Name`'s existing joining convention — NOT
  `DetachmentNameResolver`, which was floated during exploration but turned out to run the
  opposite direction: it parses one blob of text into separate names, with no join/format logic
  of its own). **Now proposed** as `openspec/changes/name-weapon-group-contributions/` — read that
  change's own design.md rather than re-deriving this from scratch.
- **Phase 1 (do the corpus spike first)**: pull real weapon-effect ability text from the live
  BSData clone before deciding anything further — same discipline as the original four-ground-
  truth-example start of `classify-rule-effects-from-text`. Settles: whether/how to handle one
  sentence stating two characteristics at once ("Attacks and Damage") — a live version of the
  already-parked "and"-joined coordinate-effect gap; whether `RuleClassification.Target` ever
  legitimately needs to vary across one ability's own Effects (no real corpus evidence for this
  found yet as of the exploration session — Dark Pact, initially cited as a candidate, turned out
  to be a Phase/Condition-gating case instead, already a separately-tracked Non-Goal, not a
  Target-cardinality problem; revisit only if the spike surfaces a genuine case).
- **Phase 2**: new `WeaponCharacteristicEffect(WeaponSelector, Characteristic, Verb, Amount)` +
  `WeaponSelector` (`NamedWeapon`/`WeaponClass`/`AllWeapons`) as a sibling `CharacteristicEffect`
  subtype — one shape covers both Attacks and Damage at classification time (they diverge only at
  resolution time, see Phase 3/4). Widen `RuleEffectClassifier` for weapon phrasing — budget for a
  real live-review pass, the same as the original Statline classifier work found 6 real bugs
  beyond its own passing tests. Extend `CharacteristicModificationKind`/`Resolver` to cover
  Damage (excluded today) — likely `Plain` kind, dice-aware arithmetic via `DiceExpression.Add`
  (already exists) instead of the resolver's current plain-int math; `WS`/`BS`/`AP`/`S` are
  already in `CharacteristicModificationKinds`' lookup table, unconsumed, just needing real weapon
  data to prove them against for the first time. Retype `WeaponProfile.D` to
  `ScalarCharacteristicView`, mirroring `S`/`Ap`/`Bs`/`Ws` (all `EqualityKey` members already).
- **Phase 3**: the aggregation mechanism — mutate the relevant `WeaponProfile`(s) with a resolved
  `WeaponCharacteristicEffect` before/at `BuildWeapons`' own grouping, and let
  `WeaponProfileEqualityKey` do group split/merge for free (an ability reaching every current
  contributor identically re-merges into one group; one reaching a single contributor forces it
  into its own singleton group — both fall out of structural-equality grouping the domain model
  already has, no new grouping logic needed). Provable in isolation against hand-built fixtures
  first, same sequencing `InvulnerableSaveEffectResolver` used. Weapon-selector-to-contribution
  resolution (named-weapon matching needs Phase 0's Name field; class matching already trivial via
  `WeaponProfile.Type`). The "does this ability reach every current contributor" evaluation that
  Phase 4's row-placement tiers depend on.
- **Phase 4 (rendering)**: widen `ShowsBreakdownTrigger` to fire on any weapon-effect ability
  contribution, not just >1 model line. New ability-contribution row (ability-name popover trigger
  + optional resolved delta, e.g. "+1") placed via three tiers mirroring `AggregateAbilityEntry`'s
  existing `ComponentName`-null convention: row-bound (targets one modelline's contribution),
  group-wide (reaches every current contributor — rendered once, not repeated), partial-subset
  (reaches some but not all — nested under just the affected modellines). Group-name caveat
  marker: an asterisk appended to `weapon.Name` (mirrors the Statline `.stat-label` marker
  convention) when an unresolved ability contribution exists anywhere in the group — no separate
  legend block needed, unlike Statline, since the table's own existing expand-to-see-breakdown
  affordance already is the explanation. Resolved-value marker: `--amber-tint` + an inline marker
  on the specific S/AP/D VALUE cell itself ("5*"), not a label — the weapon table has no per-row
  label the way a Statline tile does. Open: does a weapon-table marker share Statline's
  per-unit-block `AssignFlagMarkers` registry, or get its own?
- **Phase 5**: wire resolved weapon effects into `AttachedUnitAggregator` via the baseline lookup
  — mirrors `apply-rule-effect-baseline`'s classify-offline/human-verify-into-baseline/
  runtime-reads-baseline-only convention. Real-captured-export verification pass before calling
  this done, per this project's standing practice for this class of change.

**Permanent boundaries, not tasks**: an effect debuffing an *enemy's* weapon is unresolvable until
the attacker/defender two-roster half of the app exists (no opposing-roster concept today).
`Multiply`/`Divide` verbs stay unsupported — not needed for the `+1`-shaped examples driving this.
Some real abilities (e.g. a Marshal's "+1 Attacks per enemy unit within 6\", to a maximum of +3")
are permanently uncomputable (no positional data modeled) and will render caveated forever, by
design, not as a gap to eventually close.

## Domain / data pipeline

- **Enhancement Model/Unit scope classification.** Every Enhancement renders at Unit scope
  unconditionally today, even though real rules text sometimes signals Model scope instead (e.g.
  "this model's Objective Control"). The closed-vocabulary text-classification approach this would
  build on already exists (`RuleEffectClassifier`, `InvulnerableSaveCaveatClassifier`) — this
  specific classification hasn't been.
- **Characteristic modification engine.** A real engine for stacking multiple rules on one
  characteristic (Set→Multiply→Add→Divide→Subtract order, per-characteristic clamp bounds) and
  applying Improve/Worsen verbs. The sign/clamp resolver exists; the modifier/engine/mutator-rule
  layers above it don't. Open sub-problems: Set-vs-Set conflicts (keep the better value and drop
  the other), multi-characteristic abilities, a caveated characteristic naming more than one
  contributing ability, tier-3+ conditions (cross-unit or sibling-selection gating), a
  ranged-aura ability with no positional data to resolve against, and the "Ignore Modifiers" rule
  (needs each applied modifier tracked as a discrete, toggleable item).
- **Fallback "known-affected, unresolved" effect marker.** When text clearly touches a
  characteristic but matches no specific extraction pattern, emit an unresolved/caveated marker
  instead of silently extracting nothing. A detection gate for "text mentions invulnerable save"
  exists (`RuleEffectClassifier.MayStateInvulnerableSave`) but the marker type itself doesn't;
  extending this to the six Statline scalars needs an equivalent gate for each first.
- **`KeywordEffect`/`AbilityEffect`** — sibling types to `CharacteristicEffect` for rules that
  grant a keyword or a separate ability rather than mutate a stat.
- **LLM-assisted prose classification.** An offline, batch pass to help discover known-phrasing
  candidates for the text classifier, feeding a human-reviewed table. Not started.
- **Detachment-rule structural-modifier detection.** A real minority of Detachments carry a
  structured per-unit stat modifier gated by the same selection condition as the Detachment
  itself — could render as an orphaned per-unit ability instead of only the army-wide header
  block.

## Bigger picture

- **A full "Gameplay" bounded context** for live/mutable game state beyond casualty counts —
  positional/objective conditions, a simulated Battle-shock test, automatic phase advancement.
  Today's phase/turn tracker and Battle-shock flag are purely player-asserted, never simulated.
