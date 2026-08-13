## Context

See proposal.md - Why. Today `AttachedUnitAggregator` builds two ability-related fields with very
different shapes: `UnitScopedAbilities` (`IReadOnlyList<Ability>`, deduplicated and combined across
*every* present component of an `AttachedUnit`) and `ModelScopedAbilities`
(`IReadOnlyList<ModelScopedAbilityEntry>`, tied to a `ModelLine` but only ever populated from that
`ModelLine`'s own `Abilities` — never from `Unit.Datasheet.Abilities`, so Datasheet-level
Model-scoped abilities like `LEADER`/`SUPPORT` are silently dropped today). `BuildStatlines`
(from `order-statlines-by-declaration`) already established the pattern this change extends: walk
components in display order, per-component, no cross-component combination.

The page renders `<div>`-based blocks, not an HTML `<table>` — there is no native `rowspan`
attribute available for the "spans every row of its component" placement rule.

## Goals / Non-Goals

**Goals:**
- One per-component, optionally row-bound ability aggregation covering both Scopes and both
  sources (Datasheet and ModelLine), replacing both existing ability fields.
- Render that aggregation as two columns beside the existing Statline column, with a CSS-based
  spanning mechanism for component-wide (non-row-bound) abilities.

**Non-Goals:**
- No ability-text rendering or popup/expand affordance — name only, per proposal.md. A future
  change can add that without touching this one's domain shape (the full `Ability.Text` is already
  present on the type, just unused by the renderer).
- No change to Weapon or Keyword section rendering, or to casualty tracking mechanics.
- No validation that a component's own ability set is free of duplicates — mirrors this project's
  existing "no wargear constraint validation" stance (`.claude/domain-model-11e.md` - Deliberate
  Omissions).

## Decisions

**One new list type replaces both `UnitScopedAbilities` and `ModelScopedAbilities`, rather than
keeping two parallel lists.** `Ability.Scope` already distinguishes Model from Unit; splitting into
two domain-level lists would just mean filtering the same underlying data twice for no benefit,
since the builder's per-component/per-source traversal is identical either way — only the *column*
a page-layer consumer routes an entry into depends on Scope, which is a rendering decision, not a
domain one. Shape:

```csharp
public sealed record AggregateAbilityEntry(
    string ComponentName,
    string? StatlineName,   // null = Datasheet-sourced, applies to the whole component
    Ability Ability);
```

`AttachedUnitAggregateView.Abilities: IReadOnlyList<AggregateAbilityEntry>` replaces both old
fields.

**`AttachedUnitAggregator` gains a `BuildAbilities` walking components in `BuildStatlines`'s
established display order.** For each component, only if `component.IsPresent`: emit one entry per
`Datasheet.Ability` with `StatlineName: null`; then for each of that component's own `ModelLine`s
with `RemainingCount > 0`, emit one entry per that line's own `Abilities` with
`StatlineName: modelLine.StatlineName`. No cross-component combination, no deduplication — mirrors
`BuildStatlines`'s per-component scoping exactly. The explicit `IsPresent` guard is needed for
Datasheet-sourced entries specifically, since (unlike `BuildStatlines`, where a fully-dead
component naturally produces zero entries through its per-statline `RemainingCount` check)
Datasheet abilities have no statline-level gate of their own to fall through.

**Row-binding granularity is the rendered run (`StatlineBlockViewModel`), not the individual
`AggregateStatlineEntry`.** When two entries merge into one shared-tile run (per
`merge-adjacent-live-play-statlines`), a `StatlineName`-bound ability renders beside that whole run
— it does not attempt to distinguish which of the run's several header lines it "really" belongs
to. This matches the agreed mockup (a hypothetical model ability drawn beside a merged
"Sword Brother / Initiate" run as a whole) and keeps matching simple: an ability entry binds to
whichever run contains any `AggregateStatlineEntry` with a matching `ComponentName` +
`StatlineName`.

**Page layer computes two view-model shapes from `Abilities`, mirroring the row-bound vs.
component-wide split:**
- `StatlineBlockViewModel` gains `ModelAbilities`/`UnitAbilities: IReadOnlyList<Ability>` — the
  row-bound entries whose `StatlineName` matches any entry already in that run.
- A new `ComponentAbilitySpanViewModel(int FirstRunIndex, int LastRunIndex, IReadOnlyList<Ability>
  ModelAbilities, IReadOnlyList<Ability> UnitAbilities)`, one per component that has at least one
  Datasheet-sourced ability, computed by finding the index range (into the already-built
  `Statlines: IReadOnlyList<StatlineBlockViewModel>`) of runs belonging to that `ComponentName`.
  `UnitBlockViewModel` gains `ComponentAbilitySpans: IReadOnlyList<ComponentAbilitySpanViewModel>`.

Both are built once in `BuildUnitBlock`, alongside the existing `GroupStatlines` call — no new
traversal of the domain data beyond the single `view.Abilities` list.

**Rendering uses CSS Grid, not flexbox-per-column, because only Grid lets one cell span multiple
row-tracks shared with independent sibling columns.** Flexbox columns flow independently, so a
column-2 cell can't be told "occupy the same vertical space as column-1 rows 3 through 5" without
knowing column 1's actual rendered heights — fragile and layout-thrashy. CSS Grid defines the row
tracks once, shared by all three columns; a spanning cell uses `grid-row: <first> / <last+1>`
(1-indexed, exclusive end) computed server-side from `FirstRunIndex`/`LastRunIndex`, and every
column-1/2/3 cell is placed with explicit `grid-row`/`grid-column` (not auto-flow, which doesn't
mix cleanly with explicit spans in a shared track set). Row-track height is `auto` by default, so a
spanned ability cell taller than its column-1 counterpart simply grows the shared row-track(s) —
no special-case sizing logic needed.

**Component-wide (spanning) ability cells align to the top of their span.** Matches how the
confirmed mockup drew it; avoids needing measured/JS vertical centering across a Grid span.

## Risks / Trade-offs

[Explicit `grid-row`/`grid-column` placement on every cell is more verbose Razor markup than
relying on source-order auto-flow] → Accepted: auto-flow and explicit spans don't combine reliably
in CSS Grid, so explicit placement on every cell is the only fully robust option; the row-index
computation itself is cheap (a `Select` with running index over an already-built list).

[A component contributing several Datasheet-sourced abilities that stack taller than its spanned
statline runs will visually widen that block] → Not mitigated, treated as correct behavior per
Grid's `auto` row sizing — the alternative (truncating or clipping abilities) would hide real game
information, which this project avoids elsewhere (e.g. `.claude/domain-model-11e.md`'s "never show
full options menu" principle cuts the other way: don't hide what's actually selected/in-play).

## Migration Plan

Breaking change to `AttachedUnitAggregateView`/`AttachedUnitAggregator`, no compatibility shim —
consistent with this project's established convention (`order-statlines-by-declaration`,
`promote-dice-expression-to-domain`, `attached-unit-domain-model`). Order: domain type change
first (`AttachedUnitAggregateView.Abilities` + `AttachedUnitAggregator.BuildAbilities`, replacing
`BuildUnitScopedAbilities`/`BuildModelScopedAbilities`), which fails every other call site to
compile until updated; then the page-layer view-model and Razor/CSS rework together (cohesive as
one unit — the markup and its grid CSS aren't meaningfully separable); then remove the old
"Abilities" `<details>` section; then tests. No persisted state to migrate — `/LivePlay` rebuilds
from `Examples.View.MyArmy()` on every request.
