## MODIFIED Requirements

### Requirement: Value Provenance Highlight
A Statline tile (M, T, Sv, W, Ld, OC, InSv) or a weapon-table Attacks, BS/WS, Strength, AP or Damage value
SHALL render as an inset tile with a corner tick whenever an ability has something to say
about it: an applied ability change, a caveat left unresolved, or a conditional effect that was not
added. The tile SHALL be amber, except that a value reached only by conditional effects that were
not added SHALL use the conditional colour. The tile SHALL be a popover trigger. A value nothing
touches SHALL render plain.

#### Scenario: An ability-modified weapon value is highlighted
- **WHEN** Helbrecht leads a Crusader Squad and Crusade of Wrath adds 1 to the Strength of the
  squad's melee weapons
- **THEN** each affected melee weapon's Strength value renders as the highlighted tile, and its AP
  value, which no ability touches, renders plain

#### Scenario: An ability-modified Statline value is highlighted
- **WHEN** Faith-Fuelled Resolve adds 1 to a Sword Brethren Squad unit's Objective Control
- **THEN** each affected run's OC tile renders highlighted

#### Scenario: A caveated value is highlighted
- **WHEN** a run's invulnerable save is still caveated because its linked ability could not be
  resolved
- **THEN** that InSv tile renders highlighted, the same as a modified value

#### Scenario: A value with only a not-added conditional effect is highlighted
- **WHEN** a Chaos Lord carries Chance for Glory (once per battle, +1 to the Strength, Attacks, AP
  and Damage of its melee weapons)
- **THEN** the Daemon hammer's Attacks, Strength, AP and Damage values each render highlighted in
  the conditional colour, each still showing its unmodified value

#### Scenario: Tapping a highlighted value opens its provenance popover
- **WHEN** a player taps a highlighted value
- **THEN** that value's provenance popover opens, per "Value Provenance Popover Content"

#### Scenario: A value with an applied change and a not-added effect stays amber
- **WHEN** one value is reached by an applied ability change and by a different ability's conditional
  effect that was not added
- **THEN** that value renders highlighted in amber, not the conditional colour

#### Scenario: A caveated value stays amber
- **WHEN** a run's invulnerable save is still caveated and no other ability reaches it
- **THEN** that InSv tile renders highlighted in amber, not the conditional colour

#### Scenario: An ability-modified weapon skill is highlighted
- **WHEN** Knight Diabolus improves the Weapon Skill of the bearer's melee weapons by 1
- **THEN** each melee weapon's WS value renders as the highlighted tile showing the improved value,
  and tapping it opens a provenance popover listing Knight Diabolus

#### Scenario: An unselected choice-branch skill change is shown as conditional
- **WHEN** a unit has Doctrina Imperatives and no Imperative is selected
- **THEN** its ranged weapons' BS values and its melee weapons' WS values render in the conditional
  colour, each unchanged

#### Scenario: Selecting an Imperative applies only its own skill change
- **WHEN** the player selects the Protector Imperative for a unit with Doctrina Imperatives
- **THEN** its ranged weapons' BS values render highlighted in amber showing the improved value, and
  its melee weapons' WS values render plain
