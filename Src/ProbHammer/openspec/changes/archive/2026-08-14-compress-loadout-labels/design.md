## Context

See proposal.md - Why. `ModelLineLoadout(WeaponsLabel, RemainingCount, InitialCount)` is built
inside `AttachedUnitAggregator.BuildStatlines` (`src/ProbHammer.Core/Domain/Roster/`), one per
contributing `ModelLine`, with `WeaponsLabel` already a comma-joined string of that `ModelLine`'s
`Weapons`. `LivePlay.cshtml` renders it directly today (`@loadout.WeaponsLabel`). Compression needs
to compare a loadout's weapon list against its siblings' — the aggregator has no cross-loadout
comparison anywhere; `LivePlayModel` already does this shape of grouping/formatting work for other
page-only concerns (`GroupStatlines`, `BuildContributionBreakdown`).

## Goals / Non-Goals

**Goals:**
- Compute each loadout's compressed label as its own weapon list minus the multiset intersection
  shared by every loadout under the same statline entry.
- Keep the compression entirely at the page-view-model layer; `AttachedUnitAggregator` continues to
  expose only raw per-`ModelLine` data, no compression logic.

**Non-Goals:**
- No change to `AggregateStatlineEntry`'s counts, grouping, or the statline-run merge logic in
  `GroupStatlines`.
- No change to weapon-contribution breakdown rows (`BuildContributionBreakdown`) — this change only
  touches the Statline section's loadout `<li>` labels. Reusing compression there is explicitly
  deferred to the next, separately-proposed change (selection-scoped weapon filtering).

## Decisions

**Expose the raw weapon list on `ModelLineLoadout`, keep `WeaponsLabel` as-is.**
Add a `Weapons: IReadOnlyList<string>` field to `ModelLineLoadout` alongside the existing
`WeaponsLabel`, populated from the same `ModelLine.Weapons` already used to build `WeaponsLabel`.
`WeaponsLabel` stays untouched (still the full, uncompressed, comma-joined string) so it remains
available as-is for any other current or future consumer; the page layer computes the compressed
label from `Weapons` rather than re-parsing `WeaponsLabel`'s comma-joined string (fragile if a
weapon name itself ever contained a comma, and duplicative of work `AttachedUnitAggregator` has
already done once).

**Compression algorithm: multiset intersection across all sibling loadouts, then per-loadout
multiset subtraction.**
For a statline entry with N `Loadouts`, build a `Dictionary<string, int>` counting occurrences of
each weapon name in the first loadout's `Weapons`, then for each subsequent loadout intersect
(per-key `Math.Min`) against that running count. The result is the shared multiset. For each
loadout, subtract the shared multiset's per-weapon counts from its own weapon-name counts (floored
at zero) and join what remains, in the loadout's original weapon order, with `", "`. This is a true
multiset intersection/difference — not a longest-common-prefix strip — so shared weapons that
appear at different list positions in each loadout (see the non-contiguous scenario in specs) are
still correctly excluded, and a loadout carrying an extra copy of an otherwise-shared weapon keeps
that one extra copy visible.
Alternative considered: strip only a common leading prefix of each loadout's weapon list. Rejected
— cheaper, but wrong whenever shared weapons aren't declared in the same relative order across
loadouts (plausible in real BSData-derived fixtures, where weapon order follows entry declaration
order, not usage), and the proposal explicitly calls this out as a must-not-repeat mistake.

**Compression scope: per statline entry, never across entries or components.**
The shared multiset is computed only from the `Loadouts` belonging to one `AggregateStatlineEntry`
(one statline name within one component) — matching the existing rule that `Loadouts` is already
scoped that way. No new cross-entry comparison is introduced.

**No fallback for an all-shared (empty-result) loadout.**
Per proposal.md, two loadouts under one statline entry with identical weapon lists is treated as an
input-data anomaly. The compression function is not required to special-case an empty result; if it
occurs, the loadout simply renders with an empty weapon label rather than the code attempting to
recover a "sensible" fallback. This keeps the function's contract simple: it computes what it
computes.

## Risks / Trade-offs

- [A loadout's weapon list containing near-duplicate names that differ only by BSData's internal
  formatting quirks (e.g. trailing whitespace) could be treated as different weapons and fail to
  intersect as expected] → Compare using the same string equality already used elsewhere for weapon
  identity in this codebase (ordinal, exact match on `ModelLine.Weapons` entries, which come from
  already-normalized `Examples`/parsed data) — not a new concern introduced by this change, since
  today's uncompressed rendering already assumes these strings are clean.
- [Computing the shared multiset from `Loadouts[0]` as the seed makes the result technically
  order-dependent on which loadout happens to be first, even though multiset intersection is
  mathematically commutative] → No behavioral risk: intersection order does not change the final
  intersected multiset, only which loadout's list order is used to seed the iteration, which is
  discarded before the pairwise `Math.Min` step.

## Open Questions

None — the algorithm, scope, and layering are all settled above.
