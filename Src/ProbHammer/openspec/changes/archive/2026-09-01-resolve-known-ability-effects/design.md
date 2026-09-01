## Context

See proposal.md for motivation. Three existing pieces of the codebase anchor this design:

- `BsdataDatasheetMapper.ResolveInvulnerableSave`/`ResolveCaveatAbility` and
  `BattleScribeRosterMapper`'s own near-identical copies each resolve a Statline's raw InSv text
  into an `InvulnerableSave` value, independently, at catalogue/synthesis time — before any specific
  army roster exists. A `Datasheet` produced this way is shared: every unit of that type built from
  the same starting file re-resolves to a fresh `Datasheet` instance, but its `Abilities`/`Statlines`
  describe the unit *type*, not a specific army list's specific model.
- `AttachedUnitAggregator.Build` (and its private `BuildStatlines`/`BuildAbilities`) is the one place
  that already recomputes a fully live, casualty-filtered view of a specific resolved
  `ICombatUnit` — `AggregateStatlineEntry`/`AggregateAbilityEntry` — on every request, with no
  caching, exactly the granularity ("is this component present, is this model-line's remaining
  count above zero") the new rule engine needs.
- `_UnitBlock.cshtml` renders a caveated InSv as a single `stat-tile-flagged` tile plus an always-
  visible `.insv-caveat-text` paragraph underneath, inside that run's own `.statline-cell.col-
  statline` grid cell.

## Goals / Non-Goals

**Goals:**
- Resolve the two InSv footnote shapes wherever the source text exactly matches a known template,
  with zero risk of silently dropping information the ability's own text also carries.
- Add a general mechanism for flagging any Statline characteristic from a matched ability, that
  re-derives correctly as casualties change, without ever hiding the source ability.
- Replace the InSv-only caveat paragraph with one mechanism both InSv and the new rule engine share.
- Drop the three purely-redundant attachment-eligibility abilities from both import pipelines.

**Non-Goals:**
- No general ability-text parsing engine, no NLU — every recognized shape stays a small, closed,
  hand-curated vocabulary (see `datasheet-catalogue`'s existing "Ability Shape" precedent and
  `WeaponKeywordParser`'s existing exact-token convention).
- No user-facing configuration of which rules are active.
- No attempt to generalize beyond the two seed rules' own target characteristics (InSv, OC) ahead of
  a real third example.

## Decisions

### D1 — InSv caveat classifier is a shared pure function, called inline at both mapping sites
`InvulnerableSaveCaveatClassifier.TryResolve(text, footnotedDigit, plainDigit?) -> (Melee, Ranged)?`
(exact shape TBD in implementation) lives in `Domain.Catalogue` (alongside `InvulnerableSave`
itself, not under `Bsdata/`, since `BattleScribeRosterMapper` also needs it and must not depend on
the `Bsdata` namespace). Both `BsdataDatasheetMapper.ResolveInvulnerableSave` and
`BattleScribeRosterMapper.ResolveInvulnerableSave` call it at the exact point they already compute
today's fallback, using its result only when non-null.

**Alternative considered**: widen `InvulnerableSave` to retain both raw candidate digits
unclassified, and classify in a later, decoupled pass over the finished `Datasheet`. Rejected: for
the split-footnote shape, the finished `Datasheet` as it exists today only ever stores one
"best-guess" digit on both sides once caveated — recovering the true footnoted/plain pair would mean
storing a genuinely asymmetric-but-unclassified value on a type `_UnitBlock.cshtml`'s current InSv
rendering assumes is always symmetric while caveated. Classifying inline avoids ever producing that
intermediate state, so the rendering contract stays exactly what it is today for every case this
classifier doesn't resolve. A secondary benefit: this also collapses real, pre-existing duplication
between the two pipelines' own digit-resolution logic (`BattleScribeRosterMapper`'s copy is
currently flagged UNVERIFIED for caveated InSv in its own doc comment).

### D2 — The Leader/Support/Attached-Unit exclusion filters in `Datasheet`'s own constructor
Rather than adding the same exact-name filter separately inside `BsdataDatasheetMapper` and
`BattleScribeRosterMapper` (two call sites that would need to stay in sync), the filter applies once,
centrally, where `Datasheet.Abilities = abilities.ToList()` is set today. Both pipelines already
funnel every Ability they build through this one constructor, so this is a genuine single injection
point, not a per-pipeline duplication — unlike D1's InSv classifier, which structurally cannot be
centralized this way (see D1's rejected alternative).

**Alternative considered**: filter at each mapper's own call site instead. Rejected: no
correctness difference, but two copies of a three-name exact-match list is pure duplication risk
for no benefit, when `Datasheet`'s constructor is already the natural shared choke point.

### D3 — The rule engine runs inside `AttachedUnitAggregator.Build`, not at import time
A resolved `Datasheet` is shared across every unit of that type; a wargear/Enhancement/ability-driven
mutation is true of one specific unit's specific selections, and — per the casualty-liveness
requirement — must stop applying the moment its specific bearer becomes a casualty. `Attached
UnitAggregator.Build` already recomputes `AggregateStatlineEntry`/`AggregateAbilityEntry` fresh on
every request (no caching, per its own existing documented contract), already knows exactly which
components are `IsPresent` and which `ModelLine`s have `RemainingCount > 0`, and already produces the
`AggregateAbilityEntry` list a rule needs to scan for a match. The rule pass runs as an additional
step within `Build`, after `BuildAbilities`/`BuildStatlines` produce their live results, consuming
both and producing a decorated statline/flag view for the caller — never mutating `Datasheet` or
`Unit` themselves, which stay exactly as import produced them.

**Alternative considered**: apply rules once during `ArmyRosterEnricher`/`BattleScribeRosterMapper`
roster assembly, mutating the built `Unit`/`AttachedUnit`. Rejected outright once the casualty-
liveness requirement was raised: a roster-assembly-time mutation has no path to being un-applied
later purely from a casualty-marking POST rebuilding `AttachedUnitAggregator`'s view, short of
re-running the whole rule pass there anyway — so the aggregator boundary is the only place that is
both correct and non-redundant.

### D4 — Rule shape and matching granularity
Each rule matches against one `AggregateAbilityEntry` at a time (exact `Ability.Name` + exact
`Ability.Text`), using that same entry's `ComponentName`/`StatlineName` to know its bearer: a
`StatlineName`-bound entry's bearer is that one model-line (liveness = that line's own
`RemainingCount`); a component-wide entry's bearer is that whole component (liveness = that
component's own `IsPresent`). A rule also declares its own target scope — Shield Dome's mutation
targets only its own bearer's row(s); Vexilla's targets every row of the whole `ICombatUnit`
(matching real 11e attachment rules — "the bearer's unit" spans every component of an attached
formation, not just the bearer's own). This is a property of each rule's own definition, not
something inferred generically from the ability text.

### D5 — Marker assignment and legend placement
A marker registry is built once per unit block (per `AttachedUnitAggregateView`/render pass): the
first distinct flag-producing source (an unresolved InSv `CaveatAbility`, or a matched rule's source
Ability) seen while walking that unit's runs gets `*`, the next distinct source gets `**`, and so on;
every later occurrence of the same source reuses its already-assigned marker. The legend renders
inside each run's own `.statline-cell.col-statline`, immediately after `.statline-tiles`, replacing
today's `.insv-caveat-text` paragraph — listing only the markers whose flagged tile appears in that
specific run, even when the same marker/source also appears in other runs.

## Risks / Trade-offs

- **[Risk]** The digit-consistency check in the split-footnote classifier (D1) could reject a
  genuinely resolvable case if the raw characteristic text and the linked ability text disagree in a
  way that's actually a benign BSData authoring quirk, not a real anomaly.
  **Mitigation**: fails safe — an unmatched/mismatched case falls back to exactly today's existing
  caveated behavior, never a wrong answer, only a missed resolution. A corpus-scan test surfaces any
  real mismatch found in the wild for manual triage, mirroring this project's existing scan pattern.
- **[Risk]** Centralizing the Leader/Support/Attached-Unit filter in `Datasheet`'s constructor (D2)
  touches a type both pipelines and every existing `Datasheet`-level test already depend on.
  **Mitigation**: the change is purely subtractive and name-scoped (three exact names only);
  existing tests that don't construct a Datasheet with those names are unaffected, and a new
  corpus-scan test verifies no other real ability shares these three names across the live BSData
  clone.
- **[Trade-off]** Running the rule engine inside `AttachedUnitAggregator.Build` (D3) means it
  re-scans every present ability on every request rather than once at import time. Given the
  aggregator already does comparable work every request with no reported performance concern, this
  is judged negligible.

## Open Questions

None — the architecture decisions above resolve every question raised during exploration, including
the casualty-liveness requirement.
