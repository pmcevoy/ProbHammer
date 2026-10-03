## MODIFIED Requirements

### Requirement: Weapon Ability-Contribution Row Rendering
Within a weapon entry's contribution breakdown, a matched, non-caveated Attacks-characteristic
effect (per `attached-unit-tracker`'s "Aggregate Weapon Count View") SHALL render as a separate line
naming its source ability, using the same `(Count×Amount)` notation the breakdown's own base
contributor rows already use for their own Count and per-model Attacks value — additive to the
contributor row(s) it reaches. Every rendered number in the breakdown, base contributor value and
ability-contribution amount alike, SHALL be a genuine addend of the entry's total Attacks value:
never a value that already has another rendered number folded into it. A contributor row's own value
SHALL always be its base (unmutated) per-model Attacks value.

An ability-contribution line's placement depends on how much of the entry's current contribution set
it reaches: an effect whose reach provably equals every current contributor of the entry SHALL render
its line once, after every base contributor row of the breakdown (including merged and
selection-excluded rows), at the same indent as those base contributor rows, rather than repeated
under each contributor row. An effect reaching only some of the entry's current contributors
(including exactly one) SHALL render its line nested directly under each contributor row it reaches,
one indent step deeper than that row, and SHALL NOT render under a row it does not reach.

Each ability-contribution line SHALL surface its source ability as an interactive popover trigger,
using the same popover mechanism as any other ability name in the unit block.

#### Scenario: A row-bound ability contribution nests under its own contributor row
- **WHEN** a weapon entry's contribution breakdown includes one contributor row whose Attacks value
  is affected by a matched, non-caveated ability reaching only that contributor
- **THEN** that ability renders as its own line nested directly under that contributor's row,
  showing the reaching Count and the resolved per-model Amount, and the contributor's own row still
  shows its base (unaffected) value

#### Scenario: A group-wide ability contribution renders once, above the breakdown
- **WHEN** a matched, non-caveated ability's effect reaches every one of a weapon entry's current
  contributors identically
- **THEN** that ability renders as a single line after the last base contributor row of the
  breakdown, at the same indent as the base contributor rows, not repeated under any individual
  contributor row and not above any base contributor row

#### Scenario: A partial-reach ability contribution nests under each contributor row it reaches
- **WHEN** a matched, non-caveated ability's effect reaches some, but not all, of a weapon entry's
  current contributors
- **THEN** that ability renders nested under each of the reached contributor rows, and does not
  render under an unreached row

#### Scenario: Every rendered number is a genuine addend
- **WHEN** a weapon entry's contribution breakdown includes both unaffected contributor rows and one
  or more ability-contribution lines
- **THEN** the entry's own aggregated total Attacks value equals the sum of every rendered
  contributor-row value and every rendered ability-contribution amount, with no rendered number
  itself already including another

#### Scenario: An ability affecting a different characteristic renders via the existing marker convention, not this mechanism
- **WHEN** a matched, non-caveated effect targets Strength, Armour Penetration, or Damage rather than
  Attacks
- **THEN** it renders via the existing "Flagged Weapon Characteristic Rendering" marker/legend
  convention, unaffected by this requirement
