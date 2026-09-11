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
`RuleClassificationBaseline`). Six phases, in dependency order. Phase 0 shipped as
`name-weapon-group-contributions` (archived) — `WeaponContribution`/`AggregateWeaponEntry` now carry
a real weapon Name and a computed composite display Name. Phase 1 is done (informal spike, no
OpenSpec change — see below for why). **Phase 2 is done** (`classify-weapon-characteristic-effects`,
2026-09-10, `IsCaveated` correction 2026-09-11 — see below). Phase 3 is next ready to scope as a real
OpenSpec change, now that Phase 2's own `WeaponCharacteristicEffect`/`WeaponSelector` shape is real
rather than speculative; the rest stay here until their turn. Don't re-litigate the decisions already
made below without new evidence.

- **Phase 1 (done 2026-09-10, informal spike, no OpenSpec change)**: pulled real weapon-effect
  ability text from the live BSData clone via an ad hoc `jq` dump (every `Abilities`/`sharedRules`
  Name+Text pair across all 46 real catalogue files, grepped for weapon-characteristic phrasing) —
  a pure research question with no shippable behavior of its own, so it stayed outside OpenSpec
  entirely (an earlier attempt to scope it as a full `openspec` change with its own spec/design/
  tasks was corrected as overkill mid-session; the actual corpus query was thrown away after use,
  not checked in). Settled both open questions:
  - **Coordinate "and"-joined effects are real and common**, in two shapes, always sharing exactly
    one weapon selector across every characteristic touched (never a different selector or a
    different amount per characteristic in any real example found): the dominant shape (~30 real
    examples across Aeldari/Chaos/Imperium/Necrons/Orks) is one verb + one shared amount + a
    comma/and-joined list of 2-4 characteristics — e.g. Zealot (Adepta Sororitas): "improve the
    Strength and Attacks characteristics of melee weapons equipped by this model by 3"; Chance for
    Glory (Chaos Space Marines), a real 4-way list: "improve the Strength, Attacks, Armour
    Penetration and Damage characteristics of melee weapons equipped by this model by 1". A rarer
    shape (3 examples — Brutal Raider/Chaos Space Marines, Euphoric Strikes/Emperor's Children x2)
    is two full clauses each with their own verb, joined by "and", referring back to the same
    weapons via anaphora: "...add 1 to the Strength characteristic of melee weapons equipped by
    this model and improve the Armour Penetration characteristic of those weapons by 1."
  - **`RuleClassification.Target` does not need to vary across one ability's own Effects** — no
    real corpus evidence found (confirming the Dark Pact retraction above). The only texts that
    *look* like varying targets are multi-choice menus bundled into one Ability's Text field
    (Adeptus Mechanicus Doctrina Imperatives, Astra Militarum Orders, Adepta Sororitas Vows of
    Atonement) where several mutually-exclusive, independently-named sub-abilities happen to share
    one Name+Text pair — an import-chunking question (should each named bullet become its own
    Name+Text pair before classification?) for whoever eventually handles those specific abilities,
    not evidence `RuleClassification`/`WeaponCharacteristicEffect` need a per-effect selector.
- **Phase 2 (done 2026-09-10, `classify-weapon-characteristic-effects`)**: shipped exactly as
  scoped below, plus one real, evidence-driven deviation and several new corpus findings deferred
  rather than folded in — see the "Phase 2 findings" bullet below for both. `WeaponCharacteristicEffect(WeaponSelector,
  Characteristic, Verb, Amount)` + `WeaponSelector` (`NamedWeapon`/`WeaponClass`/`AllWeapons`) landed
  as a sibling `CharacteristicEffect` subtype, one shape covering both Attacks and Damage at
  classification time. `RuleEffectClassifier` widened for both real shapes (dominant coordinate-list,
  two-verb anaphora) via two new patterns plus a new `WeaponEffectStart` anchor (see below). A live
  corpus review found 19 real weapon-characteristic Effect results, initially reported as all
  confirmed correct (zero bugs, unlike the original Statline work's 6) — all 19 baselined.
  **Correction (2026-09-11, post-ship user review)**: that "zero bugs" claim was wrong.
  `IsCaveated` only ever checked text *after* the last contributing match, never text *before* the
  first one — safe under the plain `SentenceStart` anchor (nothing conditional can precede a true
  sentence start within the same sentence), but `WeaponEffectStart`'s whole reason for existing is
  to match *after* a mid-sentence temporal-scope clause, and every real example of that shape has a
  genuine, unmodeled activation condition ("Once per battle... If it does," / "Each time...") sitting
  immediately in front of it that `IsCaveated` never inspected. Result: 7 of the 19 baselined
  abilities (Brutal Raider, Might of Titan ×2, Euphoric Strikes, Mantra of Strength, Chance for
  Glory, Moment of Glory/Zealot, Master of Combat — 9 baseline rows counting byte-variant
  duplicates) were wrongly `IsCaveated: false`. Fixed by widening `IsCaveated` to also check the
  span between the true sentence-start enclosing the earliest contributing match and that match's
  own start position — a structural check (finds the real sentence boundary, not a phrase list):
  Brutal Raider's own trigger, "Each time this model's unit ends a Charge move,", doesn't start with
  "Once per battle" and would have slipped past a hand-maintained denylist. All 9 rows now correctly
  `IsCaveated: true` and sit in the baseline's "Caveated entries needing review" queue awaiting a
  human `Note`; Effects stay populated rather than nulled out, matching the same "extract now, never
  auto-apply while caveated" convention the Statline/InSv work already established. `WeaponType` also
  picked up `[JsonConverter(typeof(JsonStringEnumConverter))]` in the same pass, mirroring
  `EffectVerb`'s own convention (the baseline JSON was serializing it as a bare `0`/`1`).
  `CharacteristicModificationKind`/
  `Resolver`/`Clamp` extended to cover Damage (`Plain` kind, dice-aware arithmetic via
  `DiceExpression`'s existing `+` operator plus a new `ApplyToDice` guaranteed-minimum clamp path);
  `S`/`AP` got their first real proving example against genuine weapon data, `WS`/`BS` did not (see
  below - out of this change's own weapon-characteristic vocabulary). `WeaponProfile.D` retyped to
  `ScalarCharacteristicView`, mirroring `S`/`Ap`/`Bs`/`Ws`.
  - **Real deviation found during implementation**: the plain `SentenceStart` anchor does not work
    for this family at all — every real ground-truth text (Zealot, Chance for Glory, Brutal Raider,
    Euphoric Strikes ×2) states its mutation clause immediately after a ", until the end of the
    phase/turn," temporal-scope clause that is itself not at a true sentence start, so
    `SentenceStart` applied unmodified would reject all five. Added `WeaponEffectStart`, a second,
    narrower anchor scoped only to the two dominant-shape weapon patterns (see
    `.claude/domain-model/rule-effect-classification.md`'s own "Weapon-characteristic Effects"
    section for the structural reasoning) — `SentenceStart` itself and every Statline/InSv pattern
    are untouched.
  - **New corpus findings deferred to a future phase, not folded into Phase 2's own scope**
    (`classify-weapon-characteristic-effects` tasks.md task 1.2): a real `Set`-verb-shaped weapon
    effect ("...change the Attacks characteristic of melee weapons equipped by this model to 12.");
    a real dice-valued amount ("add D3 to the Strength characteristic..." — `WeaponCharacteristicEffect.Amount`
    is `int`); two further real weapon-selector shapes beyond `WeaponClass`/`AllWeapons` (an
    ability-flag-qualified selector - "Psychic weapons", "Lethal Hits weapons" - and a
    whole-unit-scoped variant - "weapons equipped by models in this unit" / "the bearer's melee
    weapons" / "this model's {named weapon}", no "equipped by" at all); two further real weapon
    characteristics beyond the four this change's own vocabulary covers (Weapon Skill/Ballistic
    Skill - `CharacteristicModificationKinds` already has `WS`/`BS` entries from an earlier change,
    unconsumed by any real weapon data still); and two real "...and those weapons have the [KEYWORD]
    ability" anaphora continuations (Finest Hour/Instrument of the Emperor's Wrath, Possessed Lord) -
    correctly extract only the real characteristic Effect and correctly flag `IsCaveated`, but the
    keyword grant itself is unextracted (feeds the existing `KeywordEffect`/`AbilityEffect` idea
    below).
  - **Broader corpus grep, 2026-09-11 (ad hoc, no OpenSpec change)**: a raw text search across the
    whole live clone for any ability description containing both "characteristic" and "weapon" found
    29 distinct hits total, against the 19 this change actually baselined - confirming the 19 is a
    narrow slice (bearer-scoped only, two phrasings, four characteristics), not the full corpus
    picture. Roughly 15 of the 29 are real additional weapon-characteristic mutations the classifier
    still doesn't recognize at all (not caveated, not baselined - just silently unclassified),
    confirming several of the shapes named above with real corpus evidence: **WS/BS** (Doctrina
    Imperatives' "Improve the Ballistic Skill characteristic..."; a T'au Sept ranged-attack rule; a
    Sniper-type ability that worsens WS on an *enemy* unit - permanently out of scope per this file's
    own "debuffing an enemy's weapon" boundary below, not a near-term miss); **ability-flag-qualified
    selector** (Waaagh!/Void Waaagh!/Martial Ka'tah, "models from your army with this ability");
    **whole-unit-scoped selector** (World Eaters' and Adeptus Astartes' own Charge abilities,
    Emperor's Children's Sensational Performance using "this unit's melee weapons" phrasing instead
    of "weapons equipped by"). Two further real shapes not previously named: a **different amount per
    characteristic within one coordinate clause** ("add 1 to Attacks... and add 2 to Strength...",
    distinct from both the shared-amount coordinate list and the two-verb-anaphora pattern - neither
    existing pattern covers per-characteristic amounts that differ), and a **two-branch conditional**
    ("add 1..., if Battle-shocked, add 2... instead"). The remaining ~14 of the 29 are not misses:
    `[MELTA]`/`[CLEAVE]`/`[RAPID FIRE]`/`[BLAST]`/`[DEVASTATING WOUNDS]` core-rule text describing a
    weapon's own printed characteristic generically (not a bearer mutation), a couple of per-*attack*
    roll modifiers (a transient concept, distinct from a persistent `WeaponProfile` mutation), and
    2-3 multi-choice-menu-bundled abilities (the same Doctrina-Imperatives-shaped import-chunking
    question already noted above, not a new finding).
- **Phase 3**: the aggregation mechanism — mutate the relevant `WeaponProfile`(s) with a resolved
  `WeaponCharacteristicEffect` before/at `BuildWeapons`' own grouping, and let
  `WeaponProfileEqualityKey` do group split/merge for free (an ability reaching every current
  contributor identically re-merges into one group; one reaching a single contributor forces it
  into its own singleton group — both fall out of structural-equality grouping the domain model
  already has, no new grouping logic needed). Provable in isolation against hand-built fixtures
  first, same sequencing `InvulnerableSaveEffectResolver` used. Weapon-selector-to-contribution
  resolution (named-weapon matching needs Phase 0's Name field; class matching already trivial via
  `WeaponProfile.Type` — confirmed real corpus data only ever produces `WeaponClass`/`AllWeapons`
  today, never `NamedWeapon`, per Phase 2's own 19-result corpus review). The "does this ability
  reach every current contributor" evaluation that Phase 4's row-placement tiers depend on.
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
