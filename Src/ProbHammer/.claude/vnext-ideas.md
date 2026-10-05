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
- **Style cleanup and alternate colour themes.** Rename colour-named tokens to the state they mean:
  `--amber` today carries two states (an active control, and a value an ability has something to
  say about), so it splits into something like `--active` and `--noted`, alongside the semantic
  `--cond` added by `granted-weapon-keyword-chips`. With semantic names in place, a theme is a set
  of redefinitions on one selector. Pairs naturally with the stylesheet split above.
- **Provenance popover polish.** The popover table draws a rule under every row, so the Total row
  ends with a double line (its own top border plus the row rule above). Five minutes in `site.css`.
- **Fill in `live-play-view`'s `## Purpose`.** Still the placeholder `openspec archive` wrote, so
  `openspec validate --specs --strict` fails on it. Edit the main spec directly.
- **A Feel No Pain stat box, mirroring InSv's.** Feel No Pain grants are common (153+ corpus hits)
  and the catalogue already loads them as `FeelNoPainEffect` (data only). A dedicated box beneath
  the M/T/Sv/W/Ld/Oc row - same treatment as the invulnerable-save box - would be its first
  consumer.
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
  here after all, it belongs with prompt v3's residue scope rule (below), not a new effect kind.
- **Battle-round awareness in the phase/turn tracker.** The tracker knows My Turn/Their Turn but
  not who goes first in a round, so "at the start/end of the battle round" abilities (~28/~11
  corpus texts) light up in both players' Command/Fight phases. Tracking the first player (or a
  round boundary) would let them light up only in the right turn.
- **Phase/turn ability highlighting.** Every catalogue record carries `Phases`/`TurnOwnership`
  (even unclassifiable ones), so an ability button could change colour in the phases it matters in,
  the tracker's original purpose for those fields.
- **Unit-fact conditions (riled up first).** A classified effect gains a structured `requires`
  condition (e.g. `["RILED UP"]`), applied when every required fact holds for the unit. Facts are
  the unit's `EffectiveKeywords` plus player-set states (Riled Up, Battle-shocked, Below
  Half-strength), so one condition kind covers both "while this unit has keyword X" and "while
  riled up", for any army. States are stored like Battle-shock and set from the unit toolbar,
  never shown as keywords. Riled up is the motivating case: 22 Ork records mention it; Waaagh!,
  More Dakka, Even More Dakka and Targetin' Gizmos carry its value/keyword effects, and others
  (Super Runts, War Cry, Intimidating Motivation) make a unit riled up. The Riled Up control shows
  on units with Waaagh! ("Friendly ORKS with this ability can... become riled up"). Hand-correct
  the few consuming records first; prompt v3 extracts the field later. Also needs the Army-Rule
  applicability fix: Waaagh!'s real classification targets `Keyword: ["Orks"]` with ArmyRule
  origin, so `TryGetApplicableClassification` drops its effects even though New Recruit attaches
  it to the right units (confirmed live 2026-10-04: no blue InSv on Boyz). Admit ArmyRule origin
  like the DetachmentRule exception. Until then, condition toggles are the stopgap (user-agreed
  2026-10-04).
- **"Used" marker for usage-limited abilities.** Mark a once-per-battle ability as spent so it
  renders less prominently; separate from its condition toggle.
- **Link a condition to another ability's activation.** Empyric Wellspring's +1 S applies "if that
  unit makes a Dark Pact"; a structured "requires ability X active" field would derive it from the
  Dark Pacts toggle instead of its own switch.
- **Consume the remaining data-only effect kinds.** `NamedAbilityGrant` (an inbound ability; needs
  `RuleGlossary.Normalize` to strip a trailing `9"`-style value so `Scouts 9"` resolves),
  `NamedAbilityRemoval` (hide or strike the removed ability).

## `WeaponProfile`-targeting rule effects — deferred coverage

Real corpus shapes found while classifying weapon-characteristic effects but not yet classified or
resolved — candidates for a future phase, not scoped anywhere yet:
- A model-keyword-subset selector ("weapons equipped by SERVITOR models in this unit") - Mindlock's
  five variants (BS/WS +1 while led by a Tech-Priest) need it.
- An ability-flag-qualified weapon selector ("models from your army with this ability").
- A whole-unit-scoped selector phrased without "equipped by" ("this unit's melee weapons").
- A coordinate clause with a different amount per characteristic ("add 1 to Attacks... and add 2 to
  Strength...").
- A two-branch conditional ("add 1..., if Battle-shocked, add 2... instead").
- A `Set`-verb-shaped effect, and a dice-valued amount (`WeaponCharacteristicEffect.Amount` is `int`
  today).

**Permanent boundaries, not tasks**: an effect debuffing an *enemy's* weapon is unresolvable until
the attacker/defender two-roster half of the app exists (no opposing-roster concept today).
`Multiply`/`Divide` verbs stay unsupported — not needed for the `+1`-shaped examples driving this.
Some real abilities (e.g. a Marshal's "+1 Attacks per enemy unit within 6\", to a maximum of +3")
are permanently uncomputable (no positional data modeled) and will render caveated forever, by
design, not as a gap to eventually close.

## Domain / data pipeline

- **Ability classification prompt v3 + re-run**, timed with the next Space Marine codex once BSData
  has it (the rewording stales today's classifications anyway; a run costs ~$16.50). Start by
  working through the `partial` list for schema tweaks that push records to `complete`. Evidence and
  examples: the archived `classify-abilities-via-llm-batch` design.md, "Full-run review, 2026-10-03".
  - **Residue scope rule.** Ask the model to sort its residue into *in scope but not expressible*
    (LivePlay or the simulation would use it) vs. *outside LivePlay's scope* (tactical actions,
    resource/token economies, CP, movement/reserves/deployment, enemy-only effects, objective
    control, list legality). Only the former blocks `complete`; for the latter, phase/target/timing
    is the whole extraction.
  - **Roll-modifier effect kind** - the largest in-scope gap (Army/Detachment rules are 1/25 and
    24/332 complete; Oath of Moment fails on this alone) and a core simulation input. Draft fields:
    roll (Hit/Wound/Damage/Save/Advance/Charge/Battle-shock), modifier (re-roll / re-roll 1s / +N /
    -N / critical on X+), direction (made by vs. targeting this unit), existing weapon selector.
    LivePlay: parked - a roll modifier changes no printed value, so value-provenance amber can't
    surface it; maybe an "info" area in the statline cell, or a section-level effect strip in the
    weapon panel (rejected for value changes, could return for this). Design once rolls are
    actually extracted. Also covers the ~12 "ignore any or all modifiers to BS/WS and/or the Hit
    roll" texts (Deathwing, Fury of the First, Inescapable Accuracy).
  - **Per-effect timing** - `phases`/`turnOwnership`/`usageLimit` are record-level, so "has Deep
    Strike; once per battle, redeploy" gates the permanent grant too.
  - **Structured `evaluable-now` conditions** beside `conditionText`: led by keyword (Ardent
    Protectors, Bound Daemon), has/lacks keyword (Flesh Sigils), contains model (Ministorum Sermon,
    Medicae Medi-packs). Not a composite target - `Target` is who benefits.
    `KeywordResolution.EffectiveKeywords` is already casualty-aware. Check first that no bodyguard
    datasheet carries CHARACTER itself.
  - **FNP qualifier vocabulary** (mortal wounds / Psychic Attacks / both) for the FNP-like-InSv
    display; `export` already maps "unqualified" sentinels to `null`.
  - **Prompt fixes:** event triggers ("each time this unit Advances/makes a Charge move") are
    `never`, not `none` - v2 gets this right only when the text also has an always-on part (Feral
    Rage, Spearpoint Paragon); a trigger-only text is folded into `phases` and applied permanently.
    Hand-corrected to `never` on 2026-10-04 (each record's `ReviewerNote` says so); v3 must reproduce
    the fix unaided, or the re-run reverts it: Brutal Raider, Attuned Onslaught, Battle-lust, Impetuous Glory, Whirling
    Death, Speed of Vaul, Swift as the wind, Extremis Trigger Word, Talonstrike Doctrines, Frantic
    Focus, Debt to the Soulforge; "models with the bearer's own keyword in that unit" is `Self`; an FNP
    qualifier is only a damage source; `turnOwnership` only when the text says whose turn; don't
    split a multi-word weapon name on "and" ("Tyrnak and Fenrir"); split faction + type keywords
    ("LEGIONES DAEMONICA TZEENTCH"); emit JSON `null`, not the string `"null"`.
  - **"Select N units"** selections (Wolf Master, Obscuroptikon) - covered by the planned
    per-condition player toggle, not the schema.
  - **Incremental runs** (`submit` picks only unclassified hashes - proven 2026-10-03 with 5 InSv
    footnote texts): a small batch's requests run in parallel, so none reads the prompt cache and
    each pays the write premium (5 requests: 0 reads, 84k written). Consider skipping the cache
    breakpoint below some batch size.
- **Enhancement Model/Unit scope classification.** Every Enhancement renders at Unit scope
  unconditionally today, even though real rules text sometimes signals Model scope instead (e.g.
  "this model's Objective Control"). Could be a new field on the LLM classification schema.
- **Characteristic modification engine.** A real engine for stacking multiple rules on one
  characteristic (Set→Multiply→Add→Divide→Subtract order, per-characteristic clamp bounds) and
  applying Improve/Worsen verbs. The sign/clamp resolver exists; the modifier/engine/mutator-rule
  layers above it don't. Open sub-problems: Set-vs-Set conflicts (keep the better value and drop
  the other), multi-characteristic abilities, a caveated characteristic naming more than one
  contributing ability, tier-3+ conditions (cross-unit or sibling-selection gating), a
  ranged-aura ability with no positional data to resolve against, and the "Ignore Modifiers" rule
  (needs each applied modifier tracked as a discrete, toggleable item).
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
