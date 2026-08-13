## Context

`/LivePlay` renders each unit's `AggregateWeaponEntry` rows from `AttachedUnitAggregateView`.
`Contributions` (`WeaponContribution(ComponentName, StatlineName, Count, PerModelAttacks)`) is
already computed correctly by `AttachedUnitAggregator.BuildWeapons` but is currently unused by the
view — `LivePlay.cshtml` reads only `entry.Profile` and `entry.TotalAttacks`. The page has zero
JavaScript today; every existing disclosure (Statline/Ranged/Melee/Abilities sections) uses native
`<details>/<summary>`. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**
- Render a per-`(ComponentName, StatlineName)` contribution breakdown beneath any weapon row on a
  unit that has more than one `ModelLine` in total, collapsed by default - including a row whose
  own `Contributions` has only one item, since naming that single source is still useful when the
  unit has others to distinguish it from.
- Collapse a group to one row when its contributions agree on `PerModelAttacks`; fall back to
  one row per contributing `ModelLine` when they don't, so a shown subtotal is always verifiable
  from the numbers beside it.

**Non-Goals:**
- The ability/weapon-keyword definitions popup (a different, floating-overlay mechanism) —
  tracked separately in `.claude/vnext-ideas.md`.
- Selection-scoped weapon aggregation (checkbox-driven filtering of the Statline section) — a
  deliberate follow-on to this change, tracked separately in `.claude/vnext-ideas.md`; this change
  only needs to leave the per-contribution data cleanly groupable, not build any selection state.
- Any domain-model change. `WeaponContribution` already carries every field this needs.
- Live casualty mutation, session persistence, or any `Simulation/*` integration — unrelated to
  this change and already tracked separately.

## Decisions

**1. Breakdown rows are pre-rendered sibling `<tr>`s, toggled by a small script — not a native
`<details>`-only interaction.** A `<details>` element cannot span additional `<tr>`s beneath the
row that triggers it; `<tr>` is the only valid direct child of `<tbody>`. Considered a CSS-only
checkbox-hack (hidden `<input type="checkbox">` + sibling combinators) instead of JS, but a bare
`<input>` isn't a valid `<tbody>` child either, and working around that would mean restructuring
the table in a way that costs more than the alternative. A few lines of JS is the smaller cost,
and it isn't really a new cost in kind — the ability/keyword popup (tracked separately) needs real
JS regardless, so `/LivePlay` was always going to get its first script; this just makes that
explicit rather than incidental.

**2. Grouping/formatting logic lives in `LivePlay.cshtml.cs`'s view-model construction, not in
`AttachedUnitAggregator`.** Matches the page's own precedent: `StatlineBlockViewModel`'s
by-value statline merge is page-layer too, because it's a rendering-only grouping over data the
domain already computed correctly — the domain's job (retaining ungrouped, per-`ModelLine`
provenance) is already done; only the display grouping is new.

**3. Grouping algorithm.** For each `AggregateWeaponEntry`, when the unit qualifies (Decision 4):
group `Contributions` by `(ComponentName, StatlineName)`, preserving first-seen order (declaration
order, not dictionary enumeration order — matching the convention already established for
`Datasheet.Statlines`). Within a group, compare `PerModelAttacks` by structural equality
(`DiceExpression` is a record, so `==` already does this — no custom comparer needed):
- All equal (a group of exactly one contribution trivially qualifies) → one row: label = the group
  key, `Count` = sum of the group's `Count` values, subtotal = `PerModelAttacks.Scale(summedCount)`.
- Any differ → one row per `WeaponContribution` in the group, each showing its own `Count`,
  `PerModelAttacks`, and `PerModelAttacks.Scale(Count)`.

A single-contribution `AggregateWeaponEntry` therefore still produces exactly one breakdown row
when the unit qualifies - the grouping loop needs no special case for it, since a group of one is
just the smallest possible group.

**4. Trigger condition: unit-level, not per-weapon.** Originally scoped as `Contributions.Count >
1` (only render a trigger when this specific weapon has multiple contributors). Revised during
implementation: a single contributor is still worth naming - e.g. a Pyre pistol carried only by a
unit's Sword Brother - as long as the *unit* has other `ModelLine`s it could be confused with. The
actual condition is `totalModelLinesInUnit > 1`, computed once per unit as
`view.Statlines.Sum(s => Math.Max(s.Loadouts.Count, 1))` - `Loadouts` is empty when exactly one
`ModelLine` shares a statline name (the "no redundant breakdown row" rule on
`AggregateStatlineEntry`), so `Max(.,1)` recovers the true per-name count in that case, and summing
across every statline entry gives the unit's true total even though `BuildStatlines` has already
collapsed same-named lines into one entry each. This can only ever exclude a single-`ModelLine`
plain `Unit` (e.g. Impulsor) - an `AttachedUnit` always totals two or more by construction
(Bodyguard + at least one Attached component, each contributing at least one `ModelLine`).

**5. Trigger element.** The weapon-name cell becomes a `<button type="button"
class="weapon-name-toggle">` only when the unit qualifies (Decision 4) and the entry's computed
breakdown is non-empty; otherwise it renders exactly as today (plain text, no affordance).
`type="button"` avoids any implicit form-submission semantics, even though the page has no forms
today.

**6. `live-play.js` (new file).** Selects `.weapon-name-toggle` buttons; each toggles a `hidden`
attribute (or class) on its associated breakdown `<tr>`s. Association is via a shared
`data-weapon-id` attribute on the trigger and its breakdown rows, not DOM adjacency — so a future
markup change can't silently desync the toggle from its target. No framework, no build step,
consistent with `army-view.js`'s existing plain-JS approach. Progressive-enhancement-friendly: if
the script fails to load, breakdown rows simply stay in their default hidden state — no other page
functionality depends on it.

**7. Styling.** New `.weapon-contribution-row` class — dimmer, smaller, indented text relative to
primary weapon rows — scoped under `.live-play-page` per this page's existing local-token-override
convention (see `.claude/design-tokens.md`).

**8. Zebra striping is driven by list index, not `:nth-child`.** Discovered mid-implementation:
breakdown rows are always present in the DOM (hidden, not removed), so `tr:nth-child(even)`
counts them and shifts the parity of every primary row that follows an expandable weapon above it
— confirmed live, e.g. Ferocity (the 4th weapon, should be even/grey) rendered white because three
hidden breakdown rows ahead of it in the DOM pushed it to an odd `:nth-child` position. Fix:
compute `weapon-row-alt` server-side from the weapon's own position in `RangedWeapons`/
`MeleeWeapons` (`w % 2 == 1`) and apply it as an explicit class, immune to how many hidden siblings
surround it. A breakdown row gets the same class as its parent (rather than always rendering
white), so an expanded row reads as one continuous block instead of a white gap inside a grey one.

## Risks / Trade-offs

- **[Risk]** The "contributions disagree on `PerModelAttacks`" fallback path has no real-catalogue
  fixture exercising it (only hand-built `Examples/*` data exists, and no current fixture produces
  disagreement within a group). → **Mitigation**: cover it with a synthetic unit test regardless;
  the fallback only ever produces *more* detail, never an incorrect number, so an untested-in-the-
  wild path degrades safely if it ever fires.
- **[Risk]** This is `/LivePlay`'s first JavaScript, a page that has otherwise stayed
  deliberately dependency-free. → **Mitigation**: single small file, no dependencies, and the
  interaction is additive — nothing else on the page becomes JS-dependent to remain usable.
