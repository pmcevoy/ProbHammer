## ADDED Requirements

### Requirement: Army Header Rendering
The `/LivePlay` page SHALL render a header, above every unit block, showing the ArmyRoster's Name,
Faction (in declared order), BattleSize together with PointsSpent and PointsLimit, and
ForceDisposition. Below this, the page SHALL render every distinctly-named `ArmyRule`-origin
ability present on any unit in the roster together with every selected Detachment's own resolved
rule(s), grouped into two columns — one for army-wide rules, one for Detachment rules — under a
section heading of its own. Each individual rule SHALL render as its own named, popover-capable
trigger (per the existing Ability And Rule Text Popover mechanism), showing that rule's full text
when tapped, rather than rendering its text inline by default. A Detachment SHALL render its own
name once, as a plain (non-interactive) label, with one popover trigger beneath it per rule it
resolved — including zero triggers when it resolved no rule at all. Neither a Detachment's own DP
cost nor any unit's own points cost SHALL be shown anywhere on the page, even though the roster's
own total PointsSpent/PointsLimit renders in the header.

#### Scenario: Army metadata renders above the unit blocks
- **WHEN** `/LivePlay` renders a roster
- **THEN** the page shows the roster's Name, Faction, BattleSize, PointsSpent/PointsLimit, and
  ForceDisposition above every unit block

#### Scenario: A Detachment with one rule shows one popover trigger under its name
- **WHEN** a selected Detachment resolved exactly one rule
- **THEN** the header shows that Detachment's own name as a plain label, with one popover trigger
  beneath it for its rule

#### Scenario: A Detachment with more than one rule shows one trigger per rule
- **WHEN** a selected Detachment resolved more than one rule (e.g. "Black Spear Task Force",
  resolving both "Kill Teams" and "Mission Tactics")
- **THEN** the header shows that Detachment's own name once, as a plain label, with one popover
  trigger per resolved rule listed beneath it

#### Scenario: A Detachment with no resolved rule shows its name with no popover trigger
- **WHEN** a selected Detachment's resolved rule list is empty
- **THEN** the header shows that Detachment's own name as a plain label with no popover trigger
  beneath it

#### Scenario: More than one ArmyRule-origin ability renders in the army-wide column
- **WHEN** the roster's units carry more than one distinctly-named `ArmyRule`-origin ability (e.g.
  a Drukhari army's "Power from Pain" and "Corsairs and Travelling Players")
- **THEN** the header's army-wide column shows one popover trigger per distinctly-named ability

#### Scenario: No ArmyRule ability omits the army-wide column
- **WHEN** no unit in the roster carries an `ArmyRule`-origin ability
- **THEN** the header's rules section omits the army-wide column entirely, rather than rendering it
  empty

#### Scenario: An ArmyRule ability continues to render on its own unit too
- **WHEN** a unit carries an `ArmyRule`-origin ability that is also shown in the header's army-wide
  column
- **THEN** that unit's own existing per-unit rendering of the ability (per the existing
  dedupe-and-span behavior) is unchanged by the header also showing it

#### Scenario: No per-Detachment or per-unit points cost is shown
- **WHEN** the header renders its Detachment rules, or any unit block renders
- **THEN** no individual Detachment's own DP cost and no individual unit's own points cost is
  rendered anywhere on the page
