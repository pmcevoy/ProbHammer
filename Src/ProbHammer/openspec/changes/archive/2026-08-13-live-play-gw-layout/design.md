## Context

`/LivePlay` (`LivePlay.cshtml` / `LivePlay.cshtml.cs`) currently renders a flat 9-column statline
table and an 8-column weapon table per unit, always fully expanded, headed by an ordinal ("Unit
1", "Unit 2", ...) because `AttachedUnitAggregateView` carries no unit-level name today. The page
is a pure `OnGet` render sourced from `Examples.View.MyArmy()` — stateless, no session, no
persistence (see `.claude/domain-model-11e.md`). The app's shared dark tactical theme is declared
as CSS custom properties on `:root` in `site.css` (`.claude/design-tokens.md`); `.live-play-page`
currently sets no background of its own and inherits `body`'s.

See `proposal.md` for motivation and full scope; this document covers how the layout rewrite is
structured.

## Goals / Non-Goals

**Goals:**
- Replace ordinal unit headers with real names, computed from data already on the Datasheets
  involved (no new input/export data required).
- Reorganize statline and weapon rendering around GW's own in-game reference conventions (name-per-
  block statlines, Ranged/Melee weapon split, inline ability tags, importance-ordered weapons).
- Make disclosure a per-section (not just per-unit) control, collapsed by default, built as the
  reusable primitive a later phase-view feature would toggle in bulk.
- Give `/LivePlay` its own visual theme without touching the app-wide one.

**Non-Goals:**
- No live casualty mutation and no session/persistence layer — the page stays a pure GET render.
- No changes to `Simulation/*`, `ArmyListParser`, or BSData/export wiring.
- No ability-source categorization (Core/Faction/Psychic), no per-`ModelLine` keywords, no multi-
  profile-weapon disclaimer — see proposal.md's Out of Scope list.
- No app-wide theme change — `.claude/design-tokens.md` continues to govern every other page.

## Decisions

### 1. `ICombatUnit.Name` is computed, not stored

`Name` is a get-only computed property, mirroring `KeywordResolution.EffectiveKeywords` /
`ToughnessResolution.ResolveDefendingToughness`'s pure-function shape rather than an `init`-stored
field. Neither BattleScribe/NewRecruit exports nor GW's own app support free-form per-instance unit
naming, so there is no external data source a stored value would ever be populated from — it is
fully derivable from the `Datasheet.Name` of whichever components are present. Computing it also
means it can't drift from those Datasheets the way a cached value could under a `with` expression.

- `Unit.Name => Datasheet.Name`
- `AttachedUnit.Name`:
  - zero attached units → `Bodyguard.Datasheet.Name` alone (no "with" clause)
  - one attached unit → `"{Bodyguard} with {A}"`
  - two attached units → `"{Bodyguard} with {A} and {B}"` (no comma — matches the user's own
    reference example, `"Crusader Squad with Marshal and Lieutenant"`)
  - three or more → Oxford-comma join: `"{Bodyguard} with {A}, {B}, and {C}"` (comma only kicks in
    once there are 3+ names to join)

Alternative considered: an `init`-settable property populated once at construction. Rejected —
Datasheet names never change after construction, so a stored value only adds a second place `Name`
could be set inconsistently, with no benefit over computing it on read.

Known unhandled edge case: two attached leaders sharing the exact same Datasheet name render as two
separate entries (`"...with Marshal, and Marshal"`), not collapsed to `"2× Marshal"`. Confirmed
acceptable — deferred, not designed around.

### 2. Statlines render as one header+tile block per entry, never merged by value

Continues the existing rule that statline entries sharing identical M/T/Sv/W/Ld/Oc values are never
merged (already a locked scenario in the `live-play-view` spec from `render-aggregate-view`), but
changes the rendering shape from a table row to a name header bar + six stat tiles, matching GW's
mobile app rather than its desktop card (which compresses matching-valued entries into one row with
a comma-separated name list). The mobile pattern was chosen specifically because a comma-separated
name list has no way to become a per-model tap target later, while a separate block per entry does.

`Loadouts` sub-rows render only when an entry has more than one (`Loadouts.Count > 1`); a single-
loadout entry — the common case for both a uniform anonymous squad and a named individual character
— shows its remaining/initial count directly in the block header, with no redundant single sub-row.

### 3. Weapons split into Ranged/Melee sections, ordered by descending expected value

Group `unit.Weapons` by `Profile.Type`: all `WeaponType.Ranged` entries first, then all
`WeaponType.Melee` entries, each under its own section header — mirroring GW's two-section
datasheet layout. `WeaponProfile.Skill` is already a unified computed property across
`RangedWeapon`/`MeleeWeapon`, so both sections share one column layout with no new domain shape.

Within each section, order by descending expected value of `TotalAttacks`. `DiceExpression` has no
natural total order (`3D6` vs. `12`?), so this adds `DiceExpression.ExpectedValue()`:
`Count == 0 ? Modifier : Count * (Sides + 1) / 2.0 + Modifier`. It lives on `DiceExpression` itself
(`Domain.Catalogue`), consistent with `promote-dice-expression-to-domain`'s rationale that dice-
related computation belongs on the value type, not scattered into callers.

Alternative considered: sort only fixed-attack rows numerically, leave dice-attack rows in
encounter order. Rejected — expected value matches how a player already mentally compares "3
attacks" against "2D6 attacks" at the table.

### 4. Weapon ability flags render as inline bracketed tags, not a column

Append a `[TAG, TAG]` suffix to the weapon name cell (e.g. `Storm bolter [RAPID FIRE 2]`), sourced
from `WeaponProfile`'s flattened ability properties. This needs a new formatting helper — distinct
from `_UnitCard.cshtml`'s existing `WeaponAbilityText(WeaponAbilities a)`, which operates on the
10e/paused `Contracts.WeaponAbilities` type, not `Domain.Catalogue.WeaponProfile`'s flattened
properties. Not shared between the two Razor pages.

### 5. Per-section disclosure via native `<details>`/`<summary>`, default collapsed

Each unit block's four sections (statline / ranged weapons / melee weapons / abilities) are
independent `<details>` elements, closed by default. `ArmyView`'s card header uses custom JS
(`toggleCard()`) because its open/close state coordinates with weapon-selection logic elsewhere on
the page (phase locking, highlight state); `/LivePlay` has no such cross-component state, so plain
`<details>` gets open/close and keyboard access for free, with no JS — consistent with
`_UnitCard.cshtml`'s existing ability sections, which already use this same native pattern.
Keywords keep rendering as a plain non-collapsible footer line (matching both GW references) —
nothing to disclose, it's already one short list.

### 6. Theme override via local CSS custom-property re-declaration

`site.css` declares all seven design tokens on `:root`. Rather than hardcoding a parallel light
palette or introducing new token names, redeclare the same custom property names locally on the
`.live-play-page` container — every rule that already reads `var(--bg)` etc. picks up the local
values automatically, and reverting the page to the app-wide theme later is deleting one block.
`.live-play-page` must set its own `background: var(--bg)` explicitly (it doesn't today, so it
currently shows through to `body`'s background, which resolves against the *global* `:root` value,
not a page-local override).

Typography: `.claude/design-tokens.md`'s "monospace for all game stats" rule is intentionally not
followed within `.live-play-page` — stat tiles and weapon numbers use a bold sans-serif treatment
instead, matching the GW references. This is a documented, scoped exception: per this project's
CLAUDE.md doc-maintenance rule, `.claude/design-tokens.md` and/or `.claude/web-app.md` gets a short
note added calling out that `/LivePlay` is the one page that doesn't follow the shared token/
typography rules, and why.

## Risks / Trade-offs

- [Risk] A second, page-scoped visual language increases long-term maintenance surface. →
  Mitigation: fully contained by the CSS custom-property override (Decision 6); no shared
  component/partial is forked, so exactly one stylesheet block diverges.
- [Risk] `ExpectedValue()` sorting can rank a swingy weapon (e.g. `D6`, expected 3.5) above a
  steadier one with a similar-looking total, which may not always match a player's felt sense of
  "which weapon matters most." → Mitigation: acceptable for v1 — a small number of rows per
  section, easily scanned/overridden visually; no secondary tie-break heuristic needed yet.
- [Risk] Building section-level `<details>` disclosure now, ahead of the deferred phase-view
  feature, risks over-designing for a feature that may end up wanting a different shape. →
  Mitigation: `<details>` is the smallest possible primitive (native HTML, no bespoke state) — very
  little to unwind if the phase-view design later wants something else.
- [Risk] Duplicate attached-leader names (Decision 1's known edge case) could render confusingly if
  more common in practice than assumed. → Mitigation: explicitly deferred; revisit if it surfaces.

## Migration Plan

Not applicable in the usual sense: `/LivePlay` is a stateless, read-only page rebuilt from
`Examples.View.MyArmy()` on every request. There is no data to migrate and no rollback beyond
reverting the commit.
