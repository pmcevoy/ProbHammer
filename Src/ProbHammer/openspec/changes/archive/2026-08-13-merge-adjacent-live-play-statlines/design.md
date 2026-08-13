## Context

See proposal.md - Why. This change sits directly on top of `order-statlines-by-declaration`: it
assumes `AttachedUnitAggregateView.Statlines` is already ordered component-by-component,
declared-order-within-component (that change's `BuildStatlines` rewrite iterates components then
each component's Datasheet-declared statlines). This change adds one field to the entries that
rewrite already produces, and does the rest entirely at the page layer.

This project has already tried, and explicitly reversed, a *by-value* statline collapse once
before (`render-aggregate-view` → reverted in `rework-attached-unit-aggregate-view`, documented in
`openspec/specs/live-play-view/spec.md`'s prior "Squad with a distinct sergeant statline sharing
the same values" scenario) — that collapse lost the ability to track remaining/initial counts
per name. This change is not a re-run of that: the underlying `AggregateStatlineEntry` list stays
exactly as many distinct, independently-countable entries as before; only the *rendering* groups
adjacent ones under a shared visual tile. Each entry keeps its own header line, its own count, its
own loadout breakdown — the thing the previous revert was protecting stays intact.

## Goals / Non-Goals

**Goals:**
- Remove duplicate stat-tile rendering for adjacent, same-component, value-identical statline
  entries, without touching how those entries are counted, tracked, or ordered.

**Non-Goals:**
- No global by-value regrouping across the whole card — only a contiguous run in the
  already-established render order. Reordering to force more merges is out of scope; if a future
  change wants that, it's a separate proposal weighing it against the ordering rules just adopted.
- No casualty-tracking UI itself — this change only ensures the markup shape (one distinct header
  element per entry) that future casualty controls will need to hook into individually.

## Decisions

**`ComponentName` is added to `AggregateStatlineEntry` rather than deriving component boundaries
at the page layer some other way.** The flat `Statlines` list has no existing signal for "which
component did this come from" — `WeaponContribution` already solved the identical problem for
weapons with its own `ComponentName` field (`unit.Datasheet.Name`), so this reuses that exact
convention rather than inventing a second one. Populated inside `order-statlines-by-declaration`'s
already-planned `BuildStatlines` rewrite (component-by-component iteration) as a same-method
addition, not a second independent traversal.

**Grouping happens in `LivePlayModel`, not in the domain.** The domain's job is to report every
distinct, individually-trackable statline entry — that doesn't change. Deciding which adjacent
ones happen to look the same on screen is a rendering concern specific to this one page, matching
the precedent already set for weapon ordering and unit-card ordering in this same page's other
recent changes. A new `StatlineBlockViewModel` (or similarly named page-local type) wraps a
non-empty, ordered list of `AggregateStatlineEntry` that share one Statline value and one
`ComponentName`; `UnitBlockViewModel.Statlines` (currently `IReadOnlyList<AggregateStatlineEntry>`)
becomes `IReadOnlyList<StatlineBlockViewModel>`, built by a single forward scan that starts a new
group whenever `ComponentName` or `Statline` differs from the running group's.

**Grouping key is `Statline`'s built-in structural equality**, not a hand-rolled comparison of
individual fields. `Statline` is already documented as a value object; reimplementing field-by-
field comparison would just be a slower, more error-prone version of `Equals`/`==` that risks
drifting if a field is ever added to `Statline`.

## Risks / Trade-offs

[This change can't be implemented or reviewed in isolation — it needs `order-statlines-by-
declaration`'s `BuildStatlines` rewrite as its starting point] → Sequencing note, not a design
risk: implement `order-statlines-by-declaration` first, this change second. Call out that
dependency clearly in tasks.md so apply-time doesn't attempt this out of order.

[A future casualty-tracking change will need to attach a click/toggle affordance to each header
line individually, inside a shared-tile block] → Addressed by keeping each entry's header line as
its own distinct rendered element (per the live-play-view delta's explicit scenario) rather than
concatenating names into one string — the DOM shape a future click-handler needs already exists
after this change.

## Migration Plan

Additive on the domain side (`ComponentName` is a new field, not a breaking signature change to
anything other than `AggregateStatlineEntry`'s record shape, which only this page's view-model
consumes). Page-layer rewrite is self-contained to `LivePlay.cshtml.cs` / `LivePlay.cshtml`. No
persisted state to migrate. Implement after `order-statlines-by-declaration` is merged.
