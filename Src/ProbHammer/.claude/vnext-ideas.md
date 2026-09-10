# vNext Ideas — Deferred Features

Ideas that came up during 11e development and were deliberately not built. This is a parking lot
to draw from when picking the next `openspec` change, not a record of what shipped — keep entries
short, and delete one once it's turned into a change (the archived change is the historical
record, not this file).

---

## `/LivePlay` — Attached Unit view

- **Per-ability Model/Unit scope marker.** A prefix/symbol on the ability name (mirroring the
  Enhancement `✦` prefix) to show whether an ability is model- or unit-scoped.
- **Core/Faction/Psychic ability source tagging** — distinguish where an ability comes from, not
  just its name/scope.
- **Per-`ModelLine` keywords.** So a leader's personal keyword leaves the unit's effective keyword
  union when that specific model is removed as a casualty — keywords are currently only tracked at
  the `Datasheet` level.
- **Multi-profile-weapon "select one profile" disclaimer** — some weapons have multiple firing
  profiles the player picks between; not flagged today.
- **Ability-driven attack modifiers** — e.g. a unit-wide "+1 Attack" ability changing a printed
  total. Depends on the ability-text interpretation pass below.
- **Split `Keywords` into unit-wide-union vs. per-component, or add `FactionKeywords`** — currently
  one unioned set.
- **Split `wwwroot/css/site.css` into a `/LivePlay`-only stylesheet** — it still ships dead 10e
  selectors interleaved with the live rules.

## Domain / data pipeline

- **Ability-text interpretation pass.** Classify an ability's Description against a small closed
  vocabulary of known fixed phrasings (never general NLU — that stays permanently out of scope).
  Should also resolve Model-vs-Unit scope for Enhancements, which always render at Unit scope
  today regardless of what the text actually says.
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
  instead of silently extracting nothing. Piloted for InSv; extending to the six Statline scalars
  needs an equivalent detection gate built first.
- **`WeaponProfile`-targeting rule effects** (attack/weapon-stat buffs) and **Multiply/Divide
  verbs** — not yet recognized by the rule-effect classifier.
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
