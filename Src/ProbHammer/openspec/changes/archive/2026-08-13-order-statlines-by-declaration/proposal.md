## Why

Unit-card statline rows on `/LivePlay` currently render in whatever order `Dictionary<string,
Statline>` enumeration happens to produce — an implementation detail, not a documented contract.
Real army-list exports (NewRecruit, the GW app) already list a squad's Sergeant/leader-equivalent
model first, followed by the rest — that listing order is exactly the order players expect at the
table, and it's already present in the source data. Making the Datasheet's own declared statline
order the authoritative render order (instead of guessing "who's the sergeant" from name or model
count, both of which are unreliable — the fixtures already contain a name, "Sword Brother", that
means "the squad leader" on one Datasheet and "rank and file" on another) gets the desired order
for free and removes a class of heuristic that would otherwise need to live somewhere.

## What Changes

- **BREAKING**: `Datasheet.Statlines` changes from `IReadOnlyDictionary<string, Statline>` to an
  ordered `IReadOnlyList<(string Name, Statline Statline)>`, and the constructor's `statlines`
  parameter changes to match. Declaration order is now a load-bearing, documented property of a
  Datasheet, not an incidental `Dictionary` enumeration detail. `GetStatline(name)` is unaffected.
- Every existing `Datasheet` construction site (`Examples/Datasheets.cs` and four test fixture
  files) is migrated to the ordered form, and each Datasheet's statline list is verified/reordered
  so its Sergeant/leader-equivalent entry is declared first — matching the real export convention
  this change is designed to surface.
- The Attached Unit aggregate statline view (`AttachedUnitAggregator.BuildStatlines`) is rewritten
  to walk components in a fixed display order — for an `AttachedUnit`: each Attached unit in its
  attachment-list order, then the Bodyguard; for a plain `Unit`: itself — and within each
  component, in that component's own Datasheet's declared statline order.
- **Behavior change**: per-name statline merging is now scoped to a single component's own
  model-lines, not merged globally across every component of an `AttachedUnit` as it is today.
  Confirmed acceptable: the actual 40k rules attach units via a Leader/Support ability, not by
  name, and don't allow two identically-named Leader/Support units to be attached to the same
  Bodyguard — so two different components sharing a statline name can't occur in valid data.
  Validating that is the exporting app's job, not ours (consistent with this domain model's
  existing "no wargear constraint validation" stance).

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `datasheet-catalogue`: the "Multiple Named Statlines" requirement gains a declared-order
  guarantee — a Datasheet's statlines are exposed in the order supplied at construction, not as an
  unordered named set.
- `attached-unit-tracker`: the "Aggregate Statline View" requirement's grouping and ordering
  changes — entries are grouped per-component (not merged across an AttachedUnit's components) and
  ordered by component display order, then by each component's Datasheet-declared statline order.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/Datasheet.cs` — constructor signature and `Statlines`
  property type change.
- `src/ProbHammer.Core/Domain/Examples/Datasheets.cs` — 9 `Datasheet` construction sites migrated
  to ordered form; statline order verified/corrected per-datasheet.
- `tests/ProbHammer.Tests/Domain/Fixtures/DatasheetFixtures.cs` (4 sites),
  `AttachedUnitFixtures.cs` (2 sites), `AggregateViewFixtures.cs` (6 sites), and one inline
  `Datasheet` construction in `AttachedUnitAggregatorTests.cs` — same migration.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — `BuildStatlines` rewritten.
- No change to `/LivePlay`'s Razor markup or `LivePlayModel` — the page already renders
  `AttachedUnitAggregateView.Statlines` "in the order returned"; only what order the domain layer
  returns them in changes.
- Test coverage: `DatasheetTests.cs` (declared-order preserved), `AttachedUnitAggregatorTests.cs`
  (component display order, per-component declared order, per-component merge scoping).
