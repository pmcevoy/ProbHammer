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
- **A Feel No Pain stat box, mirroring InSv's.** Given how common Feel No Pain grants are in the
  corpus (153+ hits), a dedicated box beneath the M/T/Sv/W/Ld/Oc row — same treatment as the
  invulnerable-save box — could surface it the way InSv already is. Purely an idea, no display
  field exists today.
- **Suppress a bare glossary-tag ability entry whose Name carries no concrete value.** Confirmed via
  real BSData (Custodian Wardens): a datasheet can link the shared, valueless `"Feel No Pain"` rule
  directly as its own ability-list entry (a keyword tag, no append-name modifier) — unconditionally
  useless to display ("this unit has some Feel No Pain-ish thing, value withheld"), regardless of
  whether another ability on the same unit happens to state the value elsewhere in its own prose
  (e.g. Custodian Wardens' "Living Fortress": "...have the Feel No Pain 4+ ability"). No cross-
  ability reconciliation needed — this is a plain per-record check against the closed template-
  family list (same "This ability always takes the form..." regex already used to detect the
  family) plus "this occurrence's own Name has no value suffix," fully determinable from the corpus
  alone, independent of whether `classify-abilities-via-llm-batch` classification succeeds. A
  companion idea considered and dropped: a classifier-emitted "hide this ability" effect kind —
  rejected as conflating a mechanical effect (what the rules text does) with a display/consume-time
  decision (whether to render an entry); if a classifier-side signal ever turns out to be needed
  here after all, it belongs on the existing `CoverageStatus`/`UnclassifiedResidue` open question in
  `classify-abilities-via-llm-batch/design.md`, not a new effect kind.
- **Battle-round awareness in the phase/turn tracker.** The tracker knows My Turn/Their Turn but
  not who goes first in a round, so "at the start/end of the battle round" abilities (~28/~11
  corpus texts) light up in both players' Command/Fight phases. Tracking the first player (or a
  round boundary) would let them light up only in the right turn.

## `WeaponProfile`-targeting rule effects — deferred coverage

Real corpus shapes found while classifying weapon-characteristic effects but not yet classified or
resolved — candidates for a future phase, not scoped anywhere yet:
- WS/BS weapon-characteristic mutations (`CharacteristicModificationKinds` already has entries for
  both, unconsumed by any real weapon data).
- An ability-flag-qualified weapon selector ("models from your army with this ability").
- A whole-unit-scoped selector phrased without "equipped by" ("this unit's melee weapons").
- A coordinate clause with a different amount per characteristic ("add 1 to Attacks... and add 2 to
  Strength...").
- A two-branch conditional ("add 1..., if Battle-shocked, add 2... instead").
- A `Set`-verb-shaped effect, and a dice-valued amount (`WeaponCharacteristicEffect.Amount` is `int`
  today).
- The "...and those weapons have the [KEYWORD] ability" anaphora continuation (weapon keyword
  grant) — see `openspec/changes/classify-abilities-via-llm-batch/` for the current exploration
  covering this, plus Feel No Pain and closed-vocabulary named-ability grants.

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
- **Replacing `RuleEffectClassifier`'s regex-pattern classification with an LLM-based (Haiku,
  Batch API) offline pipeline** — supersedes both the `KeywordEffect`/`AbilityEffect` idea and the
  older "LLM-assisted prose classification" idea above with one converged direction, following a
  real corpus review that found Feel No Pain grants (153 hits, zero existing representation) as
  the largest real gap, plus closed-vocabulary named-ability grants and the weapon-keyword-grant
  anaphora case above. Built as two standalone tools
  (`openspec/changes/classify-abilities-via-llm-batch/`,
  `tools/AbilityPipeline/{Extractor,Classifier}/`): the extractor's corpus walk, schema (generated
  from the classifier's own POCOs), prompt v1, and a real-corpus-verified few-shot set are all in
  place and wired end-to-end through the Batch API request boundary. Still open: actually submitting
  the real ~3,823-hash corpus run (needs an Anthropic API key + the ~$4-5 one-time cost authorized),
  and the review-at-scale workflow once real output exists to review.
- **Unit keyword grants/removals as a classified effect kind.** e.g. "Friendly BULLGRYN SQUAD/OGRYN
  SQUAD/RATLINGS units have ABHUMAN", "the bearer has the Grenades keyword", "loses the Smoke
  keyword" — a few dozen corpus texts (Grenades, Smoke, PENITENT, Officer, Soul Forge, a Faction
  keyword replacement, ~4 losses), classified as residue today. Matters because keywords feed
  `KeywordTarget` and Stratagem eligibility. Would need a `UnitKeywordGrant { keyword,
  replacesKeyword? }` (plus removal), an any-of `KeywordTarget` (slash = OR), and roster-build
  ordering that applies keyword grants before resolving keyword-targeted effects.
- **Detachment-rule structural-modifier detection.** A real minority of Detachments carry a
  structured per-unit stat modifier gated by the same selection condition as the Detachment
  itself — could render as an orphaned per-unit ability instead of only the army-wide header
  block.

## Bigger picture

- **A full "Gameplay" bounded context** for live/mutable game state beyond casualty counts —
  positional/objective conditions, a simulated Battle-shock test, automatic phase advancement.
  Today's phase/turn tracker and Battle-shock flag are purely player-asserted, never simulated.
