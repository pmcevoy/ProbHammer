## Context

See proposal.md - Why. **This change depends on `compress-loadout-labels` being implemented and
archived first.** Its own spec delta in this change (`specs/live-play-view/spec.md`) was written
against the *current* main spec (uncompressed loadout labels) so it validates independently right
now — it does not yet encode compress-loadout-labels' compressed-label wording. Before this change
is applied, its spec/design must be reconciled to build on top of whatever `Statline Section
Rendering` reads after compress-loadout-labels archives (almost certainly via
`openspec-update-change`, a short sync pass, not a rewrite — the behavioral additions in this
change's delta don't depend on the exact loadout-label wording, only on loadouts being individually
addressable, which is already true today).

`AggregateWeaponEntry.Contributions` (`WeaponContribution(ComponentName, StatlineName, Count,
PerModelAttacks)`) is built once per contributing `ModelLine` in `AttachedUnitAggregator
.BuildWeapons`. `LivePlayModel.BuildContributionBreakdown` already groups these by
`(ComponentName, StatlineName)` and renders them into the DOM as `<tr class="weapon-contribution-
row">` elements (see `LivePlay.cshtml`), toggled by the existing `live-play.js` (a single
click-to-expand/collapse script, `/LivePlay`'s only JS today). `AggregateStatlineEntry.Loadouts`
(`ModelLineLoadout(WeaponsLabel, RemainingCount, InitialCount)`) is already an ordered list — one
entry per contributing `ModelLine` under that statline name, in the same order `BuildStatlines`
walks them.

## Goals / Non-Goals

**Goals:**
- Give every `WeaponContribution` a stable identity that a client-side selection state can key
  off, unambiguous even when two loadouts share a `Count`.
- Keep all filtering/recompute arithmetic client-side, reusing data already present in the DOM.
- Keep selection state confined to one unit block — no cross-unit interaction.

**Non-Goals:**
- No server endpoint, no session/persistence for selection state (see proposal.md - ephemeral by
  design, matching the rest of this read-only page).
- No change to `AttachedUnitAggregator.BuildWeapons`'s own totals — it keeps computing full,
  unfiltered `TotalAttacks` server-side exactly as today; filtering is purely a client-side
  rendering concern layered on top.
- No change to casualty tracking, ability rendering, or any other `/LivePlay` section.

## Decisions

**Loadout identity: an ordinal index into `AggregateStatlineEntry.Loadouts`, threaded onto
`WeaponContribution`.**
Add an integer field (e.g. `LoadoutIndex`) to `WeaponContribution`, set in
`AttachedUnitAggregator.BuildWeapons` to the contributing `ModelLine`'s position within its
statline's `Loadouts` list (the same ordering `BuildStatlines` already establishes) — `-1` or
similar sentinel for a statline with only one `ModelLine` (no `Loadouts` rendered, nothing to
index). This makes `(ComponentName, StatlineName, LoadoutIndex)` a reliable identity that a
loadout's DOM element and a weapon contribution's DOM row can both carry as a `data-*` attribute
and match on directly, with no dependency on weapon-list content (which `compress-loadout-labels`
deliberately makes non-unique-per-loadout as a *display* label) or on `Count` (which two loadouts
can coincidentally share).
Alternative considered: derive identity from the loadout's raw weapon list (already exposed once
`compress-loadout-labels` lands). Rejected — two genuinely different loadouts could theoretically
carry an identical raw weapon list (the same "input anomaly" case `compress-loadout-labels`
already declines to defend against for its own purposes), and even where lists differ, string-list
equality is a heavier and less direct identity key than an ordinal the aggregator already
implicitly has.

**Selection state model: per-unit-block, keyed by `(ComponentName, StatlineName, LoadoutIndex |
null)`.**
A `null`/absent `LoadoutIndex` means "the whole statline entry" (used for both the actual toggle
target when a statline has only one loadout, and internally as the tri-state parent's own
derived-not-stored state, computed by comparing all its `LoadoutIndex` children's states rather
than tracked as separate state). JS holds one `Set` (or equivalent) of currently-deselected keys
per unit block, initialized empty (nothing deselected = default = everything selected).

**Recompute happens client-side by reading `data-*` attributes already rendered into contribution-
breakdown rows, not by re-fetching or re-serializing `DiceExpression` arithmetic in JS.**
`PerModelAttacks` and `Count` per contribution are already rendered as text in the existing
breakdown rows (`add-live-play-weapon-provenance`). Attack aggregation for *fixed* values is plain
integer arithmetic; this change relies on the page still rendering a plain integer subtotal per
contribution row (as it already does today, per that shipped feature's `Subtotal` field) rather
than needing JS to understand dice-expression scaling/addition itself — the server has already done
that reduction once per contribution, and the client just re-sums whichever subtotals are currently
selected. This avoids porting any of `DiceExpression`'s `Scale`/`Add` semantics into JavaScript.
Alternative considered: have JS parse and re-roll/re-scale `DiceExpression` strings itself.
Rejected as unnecessary complexity — the per-contribution subtotals the server already renders are
exactly the granular numbers a client-side sum needs; no dice semantics need to cross the
server/client boundary at all.

**Weapon-row visibility and breakdown-group splitting are driven by the same selection Set,
evaluated per weapon row on every toggle.**
On each selection change, `live-play.js` re-evaluates every weapon row in the affected unit block:
sum the subtotals of contributions whose `(ComponentName, StatlineName, LoadoutIndex)` key is not
in the deselected Set; hide the row if that sum is zero contributions (not merely a zero Attacks
value — a weapon could theoretically have a real zero-Attacks contribution, though not seen in
practice); otherwise show the row with the recomputed total. Breakdown-row visibility follows the
same per-contribution check independently.

**Badges are computed, not toggled by hand.**
The Statline section's "N/M selected" count is `total keys - deselected.size` — read directly off
the Set. The Ranged/Melee "filtered" indicator is computed by checking whether *any* row in that
specific section was hidden or had its total changed by the current selection, evaluated after each
recompute pass rather than tracked as separate state — keeps the source of truth singular (the
deselected-keys Set) and avoids the badge logic silently drifting out of sync with what actually
rendered.

## Risks / Trade-offs

- [`live-play.js` grows from a ~13-line single-purpose script into real client-side state
  management (a per-unit-block Set, recompute-on-toggle, badge computation) — the page's first
  non-trivial JS] → Structure it as small, named functions per concern (toggle handling, per-row
  recompute, badge recompute) rather than one large handler, so it stays readable and testable in
  isolation even without a JS test runner in this project today; keep `LivePlayModel.cs` responsible
  for everything that can be precomputed server-side (loadout identity, per-contribution subtotals,
  compressed labels), so JS only ever does selection bookkeeping and summation, never re-derives
  anything the server already computed.
- [A weapon whose contributions are `PerModelAttacks`-uniform but selection-diverges must split its
  breakdown row on the client, using data the server rendered as one merged row (today's shipped
  behavior collapses uniform groups server-side)] → The server must always render the ungrouped,
  per-`LoadoutIndex` contribution rows into the DOM (`hidden` by default when the group would
  collapse, matching today's existing pattern for the outer breakdown-row visibility), so JS only
  ever needs to show/hide/re-merge pre-rendered rows — never synthesize new DOM content. This
  mirrors the existing shipped design (`add-live-play-weapon-provenance` already renders hidden
  breakdown rows for JS to reveal) and keeps this change from needing client-side DOM templating.

## Open Questions

None — remaining unknowns (exact CSS for the tri-state indicator, exact badge copy/placement) are
presentation details deferred to implementation, not decisions that would change the spec or task
breakdown.
