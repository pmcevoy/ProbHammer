## Context

See proposal.md - Why. Today `Datasheet.Statlines` is `IReadOnlyDictionary<string, Statline>`,
built in the constructor as `new Dictionary<string, Statline>(statlines, StringComparer
.OrdinalIgnoreCase)`. `Dictionary<TKey,TValue>` enumeration order is an implementation detail, not
a documented .NET contract — it happens to preserve insertion order today only because nothing
ever removes an entry, which is exactly the kind of accidental behavior this change replaces with
an explicit, tested guarantee.

`AttachedUnitAggregator.BuildStatlines` currently builds one flat list of `(Unit, ModelLine)`
pairs across every present component of an `ICombatUnit` (`combatUnit.Components.SelectMany(...)`)
and groups that flat list by `StatlineName` — so today, two different components sharing a
statline name would merge into one row. No existing test exercises that path.

## Goals / Non-Goals

**Goals:**
- Make Datasheet-declared statline order a real, tested property, not an accident of `Dictionary`
  enumeration.
- Render each unit-card's statline rows in that declared order, grouped and ordered by component
  (Attached units first in their attachment order, then Bodyguard; trivially just itself for a
  plain Unit).

**Non-Goals:**
- No BSData/export-parser changes — this only touches the hand-built `Examples/Datasheets.cs` data
  and the domain types it feeds. A real parser will supply statlines in whatever order the source
  export lists them; this change doesn't anticipate that format.
- No validation that an AttachedUnit's components don't share a statline name — per proposal.md,
  the real 40k attachment rules already prevent this, and validating exporter output is out of
  scope for this project (see `.claude/domain-model-11e.md` - Deliberate Omissions).

## Decisions

**`Statlines` becomes `IReadOnlyList<(string Name, Statline Statline)>`, not a new named record
type.** `Statline` itself deliberately carries no `Name` (per `.claude/domain-model-11e.md` — the
name is genuine external metadata). A plain tuple keeps the pairing without introducing a
single-use wrapper type; the project already uses ad hoc tuples for the same kind of pairing
inside `AttachedUnitAggregator` (`(Unit Unit, ModelLine ModelLine)`). `GetStatline(name)` stays as
the primary lookup API for existing callers (`ToughnessResolution`, tests); internally it's backed
by a `Dictionary<string, Statline>` built once in the constructor from the ordered list, so lookup
stays O(1) and the ordered list stays the single source of truth for both.

**`AttachedUnitAggregator.BuildStatlines` iterates components-then-declared-statlines directly,
rather than grouping a flattened list and sorting after the fact.** For each component (in display
order), walk `component.Datasheet.Statlines` in declared order; for each declared name, collect
that component's own `ModelLine`s matching it (case-insensitive), skip if none are present or none
have `RemainingCount > 0`, else emit one `AggregateStatlineEntry` summed from just those lines.
This makes correct output the only thing the loop can produce — no separate sort step, no risk of
a sort key disagreeing with the grouping. It's also what makes the merge-scoping change (per
proposal.md) fall out for free: a name can only be matched against the component currently being
walked, so two different components can never merge.

**Component display order (`Attached` list, then `Bodyguard`) is computed locally inside
`AttachedUnitAggregator`, not added to `ICombatUnit`/`AttachedUnit`'s public shape.** `ICombatUnit
.Components` already has a defined order (`[Bodyguard, ..Attached]`) used by other aggregation
logic (weapons, abilities) that doesn't care about component order at all — those already iterate
`Components`/`presentLines` without regard to sequence. Only statline rendering needs "Attached
first" order, so it's computed with a private local branch (`combatUnit is AttachedUnit au ? [..
au.Attached, au.Bodyguard] : combatUnit.Components`) scoped to `BuildStatlines`, rather than adding
a second, purpose-specific ordering concept to the domain interface for one caller.

**Datasheet statline order is corrected by hand in `Examples/Datasheets.cs`, not inferred.**
Spot-checking during design showed some datasheets (Assault Intercessor Squad, Crusader Squad)
already declare their Sergeant-equivalent entry first; others need verification. This is a plain
data-authoring task (task 2.x), not a heuristic — same principle as everything else in this
change: prefer an explicit, correct source over a guess.

## Risks / Trade-offs

[Two attached components sharing a statline name would now render as two adjacent rows instead of
one summed row, if it ever happened] → Confirmed with the user this can't occur under the real
attachment rules (Leader/Support ability, not name-based), and validating exporter output is
already out of scope for this project. No mitigation needed beyond documenting it in the spec
scenario.

[Migrating ~22 `Datasheet` construction call sites is mechanical but broad — a missed site fails
to compile (constructor signature change), so there's no risk of a silently-wrong migration] →
The build is the safety net here; `dotnet build` after the domain-layer change will list every
remaining call site that needs updating.

## Migration Plan

Breaking change, no compatibility shim — consistent with this project's established convention
(see `promote-dice-expression-to-domain`, `attached-unit-domain-model` for precedent). Order:
domain type change first (`Datasheet`), then call-site migration (fails to compile until done),
then the aggregator rewrite, then tests. No persisted state to migrate — `/LivePlay` rebuilds from
`Examples.View.MyArmy()` on every request.
