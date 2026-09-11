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
- **Split `Keywords` into unit-wide-union vs. per-component, or add `FactionKeywords`** — currently
  one unioned set.
- **Split `wwwroot/css/site.css` into a `/LivePlay`-only stylesheet** — it still ships dead 10e
  selectors interleaved with the live rules.

## `WeaponProfile`-targeting rule effects — deferred coverage

The plan explored 2026-09-10/11 (sibling to the already-shipped Statline characteristic-effect
resolution work — `RuleEffectClassifier`/`CharacteristicEffect`/`CharacteristicModificationResolver`/
`RuleClassificationBaseline`) shipped in full, including the Attacks-characteristic follow-up picked
up after the original plan closed: `name-weapon-group-contributions`, `classify-weapon-characteristic-
effects`, `resolve-weapon-characteristic-effects`, `render-weapon-characteristic-effects`, and
`resolve-weapon-attacks-effects` (all archived, last one 2026-09-11). Attached Unit weapon entries on
`/LivePlay` now resolve real Strength/AP/Damage/Attacks effects from a checked-in, human-verified
baseline against the live BSData corpus — S/AP/D mutating the profile and splitting/merging
aggregated weapon groups as needed, Attacks instead rendering as a separate, additive ability-
contribution line since it's excluded from a weapon's grouping identity; a caveated match instead
surfaces an unresolved-ability-reference marker + shared legend, reusing the Statline family's own
marker registry. (The one real corpus Attacks example, Scorpion Tail / Writhing Tentacles, turned
out to be Crusade Boons content excluded from a datasheet's always-present `Abilities` — but still
reachable through the same on-demand `Datasheet.TryResolveAbility` path a wargear-granted ability
like Vexilla already uses, proven end-to-end against real bundled BSData in
`WeaponCharacteristicEffectRealCorpusTests`.) See the archived changes for full phase-by-phase
history — this file no longer tracks it.

**Real corpus shapes found during Phase 2's classifier work but not yet classified/resolved** —
candidates for a future phase, not scoped anywhere yet:
- WS/BS weapon-characteristic mutations (`CharacteristicModificationKinds` already has entries for
  both, unconsumed by any real weapon data).
- An ability-flag-qualified weapon selector ("models from your army with this ability").
- A whole-unit-scoped selector phrased without "equipped by" ("this unit's melee weapons").
- A coordinate clause with a different amount per characteristic ("add 1 to Attacks... and add 2 to
  Strength...").
- A two-branch conditional ("add 1..., if Battle-shocked, add 2... instead").
- A `Set`-verb-shaped effect, and a dice-valued amount (`WeaponCharacteristicEffect.Amount` is `int`
  today).
- Two "...and those weapons have the [KEYWORD] ability" anaphora continuations — correctly extract
  the characteristic Effect and flag `IsCaveated`, but leave the keyword grant itself unextracted
  (feeds the `KeywordEffect`/`AbilityEffect` idea below).

Resolving an Attacks (`"A"`) effect — the last item that was on this list — is done too (see above).

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
