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
- **Alternate colour themes.** Rename colour-named tokens to the state they mean: `--amber` carries
  two states (an active control, and a value an ability has something to say about), so it splits
  into something like `--active` and `--noted`, beside the already-semantic `--cond`. With semantic
  names in place, a theme is a set of redefinitions on one selector.
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
  the few consuming records first; prompt v3 extracts the field later. Condition toggles (Waaagh!'s
  own riled-up switch) cover it meanwhile.
- **Army Rule profiles.** Grow `ArmyRuleNameLookup` (~30 hand-verified rows) from a name list into
  a small hand-maintained profile per rule, since the classifier handles Army Rules worst (of 25:
  1 complete, 7 partial, 17 unclassifiable). Fields only as consumers appear: selection scope (once
  for the army vs. per unit vs. none), when the choice is made (once per battle vs. each Command
  phase), a unit state it introduces (Riled Up), and an army resource note (tokens/points/dice).
  First consumer: Space Marines' Combat Doctrines (2026 codex) is one army-wide choice remade each
  Command phase, but activations are keyed per unit (`{unitIndex}::{abilityName}`), so today each
  unit picks its own. Choose it in the Army Header's popover, store it once by ability name, apply it
  to every unit carrying it, and show it read-only in unit-block popovers. Templar Vows, now the
  Vow-Sworn Crusaders Detachment rule, is the same shape chosen once per battle. Origin and
  choice-vs-condition can't tell the scopes apart (Dark Pacts is a per-unit Army Rule choice).
- **Army resource counter.** A named number with −/+ in the Army Header, stored like casualties,
  covering "gain X, spend X" economies without modelling their rules. Text mentions tokens, points,
  dice or a pool in Acts of Faith, Power from Pain, Battle Focus, Blessings of Khorne, Cabal of
  Sorcerers, Prioritised Efficiency and Cult Ambush (keyword scan only, rules not read). Parked
  until someone plays one of those armies.
- **"Used" marker for usage-limited abilities.** Mark a once-per-battle ability as spent so it
  renders less prominently; separate from its condition toggle.
- **Link a condition to another ability's activation.** Empyric Wellspring's +1 S applies "if that
  unit makes a Dark Pact"; a structured "requires ability X active" field would derive it from the
  Dark Pacts toggle instead of its own switch.
- **A marker for a limited-only FNP tile.** A tile whose only FNP is limited (e.g. against Psychic
  Attacks and mortal wounds) shows an amber "–", with the limit in its popover. Something like
  "FNP: *" might read better; waiting on table feedback before changing it.
- **Keyword-targeted aura grants that reach their own bearer.** Nekrosor Ammentar's "Feel No Pain
  5+*" stays caveated because Nullstone Field Generator is a Necrons-keyword aura, and keyword
  targets aren't evaluated on a unit, so its qualified 5+ never reaches the bearer to explain the
  footnote.
- **Consume the remaining data-only effect kinds.** `NamedAbilityGrant` (an inbound ability; needs
  `RuleGlossary.Normalize` to strip a trailing `9"`-style value so `Scouts 9"` resolves),
  `NamedAbilityRemoval` (hide or strike the removed ability).

## `WeaponProfile`-targeting rule effects — deferred coverage

Real corpus shapes the weapon-effect vocabulary can't express yet (a schema change plus prompt v3
support each):
- A model-keyword-subset selector ("weapons equipped by SERVITOR models in this unit") - Mindlock's
  five variants (BS/WS +1 while led by a Tech-Priest) need it.
- An ability-flag-qualified weapon selector ("models from your army with this ability").
- A weapon-keyword-filtered selector ("melee weapons ... with the [Psychic] ability also have
  [Lethal Hits]"). Without it the grant lands on every melee weapon; the Grey Knights fight-phase
  rule (`cc747e07…`) was hand-corrected to `partial` for this on 2026-10-27.
- A whole-unit-scoped selector phrased without "equipped by" ("this unit's melee weapons").
- A coordinate clause with a different amount per characteristic ("add 1 to Attacks... and add 2 to
  Strength...").
- A two-branch conditional ("add 1..., if Battle-shocked, add 2... instead").
- A dice-valued amount (`WeaponCharacteristicEffect.Amount` is `int`).

**Permanent boundaries, not tasks**: an effect debuffing an *enemy's* weapon is unresolvable until
the attacker/defender two-roster half of the app exists (no opposing-roster concept today).
`Multiply`/`Divide` verbs stay unsupported — not needed for the `+1`-shaped examples driving this.
Some real abilities (e.g. a Marshal's "+1 Attacks per enemy unit within 6\", to a maximum of +3")
are permanently uncomputable (no positional data modeled) and will render caveated forever, by
design, not as a gap to eventually close.

## Domain / data pipeline

- **Ability classification schema and prompt gaps:**
  - **Per-effect timing** - `phases`/`turnOwnership`/`usageLimit` are record-level, so "has Deep
    Strike; once per battle, redeploy" gates the permanent grant too.
  - **Structured `evaluable-now` conditions** beside `conditionText`: led by keyword (Ardent
    Protectors, Bound Daemon), has/lacks keyword (Flesh Sigils), contains model (Ministorum Sermon,
    Medicae Medi-packs). Not a composite target - `Target` is who benefits.
    `KeywordResolution.EffectiveKeywords` is already casualty-aware. Check first that no bodyguard
    datasheet carries CHARACTER itself.
  - **FNP qualifier vocabulary** (mortal wounds / Psychic Attacks / both). Some records put a model
    subset or situation in the qualifier (CHARACTER models only, Vehicle models, within an
    objective), which the FNP tile shows as a damage-source caveat; `export` already maps
    "unqualified" sentinels to `null`.
  - **Incremental runs** (`submit` picks only unclassified hashes): a small batch's requests run in
    parallel, so none reads the prompt cache and each pays the write premium (5 requests: 0 reads,
    84k written). Consider skipping the cache breakpoint below some batch size.
- **Roll-modifier reminder strip in LivePlay.** Roll modifiers are classified (765 records), but a roll
  modifier changes no printed value, so value-provenance amber can't surface it. Show the ones in
  force as a phase-aware reminder, e.g. an "info" area in the statline cell or a section-level strip
  in the weapon panel. The simulation is the other consumer; netting and capping per the 11e stacking
  rule belong there.
- **BattleScribe import renders a unit's duplicated rule twice.** NewRecruit's export of the Judiciar
  lists Fights First twice in its own `rules` (same id `24-c886-e8ba-5a89`; BSData links it once),
  and `BattleScribeRosterMapper.BuildUnit` maps every `rules[]` entry, so the ability cell shows it
  twice. De-duplicate the rules by id before mapping (seen 2026-10-27 in
  `nr-templars-for-unit-tests.json`).
- **Enhancement Model/Unit scope classification.** Every Enhancement renders at Unit scope
  unconditionally today, even though real rules text sometimes signals Model scope instead (e.g.
  "this model's Objective Control"). Could be a new field on the LLM classification schema.
- **Characteristic modification engine.** A real engine for stacking multiple rules on one
  characteristic (Set→Multiply→Add→Divide→Subtract order, per-characteristic clamp bounds). The
  sign/clamp resolver applies each Improve/Worsen/Set effect today; an ordered stacking engine
  above it doesn't exist. Open sub-problems: Set-vs-Set conflicts (keep the better value and drop
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
- **Next prompt: state where eligibility rules sit.** v3 sorts "eligible to shoot/charge after
  Advancing or Falling Back" inconsistently: Combat Doctrines' Tactical Doctrine (`02666a37…`) went to
  out-of-scope residue (`complete`), while "re-roll Advance rolls…, and the bearer's unit is
  eligible to shoot in a turn in which it Fell Back" (`f3bdecfd…`) went to in-scope residue
  (`partial`, hand-corrected 2026-10-27). Name eligibility explicitly in the out-of-scope list.
  Also decide whether an enemy unit that "made a Charge move this turn" implies the opponent's turn
  (`4db8e26e…` is unset; v3's rule says a Charge move implies no ownership).
  And map "X units with this ability" to the bearer's unit, not a Keyword target: v3 keyword-targeted
  seven such datasheet texts (Aegis of the Emperor, Daughters of the Abyss, ...), which then never
  apply; hand-corrected to `AttachedUnit` on 2026-10-27.
- **Structurally resolved InSv footnotes still show their classification note.** Judiciar's "Against
  melee attacks only" resolves to melee 4+ without a classification, but the full v3 run classified
  it `unclassifiable` anyway (`b00ea448…`), so its provenance line carries "nothing can be
  classified… needs human review" beside a resolved value. Either suppress the note when the
  structural rule resolves, or keep such texts out of `submit`.
- **Plural keyword resolution in `NameResolver`.** Rules text can pluralize a keyword BSData spells
  singular ("Aspect Warriors" vs. "Aspect Warrior", `47c806a8…`), which leaves the target keyword
  unresolved and matching no unit. Try a singular fallback when the exact name misses.
- **Detachment-rule structural-modifier detection.** A real minority of Detachments carry a
  structured per-unit stat modifier gated by the same selection condition as the Detachment
  itself — could render as an orphaned per-unit ability instead of only the army-wide header
  block.

## Bigger picture

- **A full "Gameplay" bounded context** for live/mutable game state beyond casualty counts —
  positional/objective conditions, a simulated Battle-shock test, automatic phase advancement.
  Today's phase/turn tracker and Battle-shock flag are purely player-asserted, never simulated.
