# condition-activation Specification

## Purpose

Lets the player assert that a conditional ability effect's condition holds for a unit, so the effect
is applied like an unconditional one until the player turns it off.

## Requirements

### Requirement: Activatable Conditions Of An Ability On A Unit
For each present ability on a combat unit whose catalogue record applies to it, the system SHALL
report the ability's activatable conditions: one per distinct condition text among its conditional
effects that belong to no choice branch (one with no text when only a usage limit or turn
restriction makes them conditional), and one per choice group. A condition SHALL be reported only
when at least one of its effects reaches a Statline entry or weapon contribution of that unit.

#### Scenario: A usage-limited ability has one condition
- **WHEN** a Chaos Lord carries Chance for Glory (once per battle, +1 Strength, Attacks, Armour
  Penetration and Damage to melee weapons)
- **THEN** the Chaos Lord's unit reports one activatable condition for Chance for Glory, with no
  condition text and the usage limit "Once per battle"

#### Scenario: Distinct condition texts are separate conditions
- **WHEN** an ability carries one conditional effect with condition text "Only when targeting an
  enemy Monster or Vehicle unit" and another with "Only when targeting an enemy unit that is not a
  Monster or Vehicle", both reaching the unit's weapons
- **THEN** the unit reports two activatable conditions for that ability, one per text

#### Scenario: A choice group is one condition
- **WHEN** a Heretic Astartes unit carries Dark Pacts (choose Lethal Hits or Sustained Hits 1 for
  its weapons)
- **THEN** the unit reports one activatable choice condition for Dark Pacts listing both options and
  allowing at most one selection

#### Scenario: A condition that reaches nothing is not offered
- **WHEN** an ability's only conditional effect is a melee weapon effect and the unit has no melee
  weapons
- **THEN** the unit reports no activatable condition for that ability

### Requirement: Activation State Is Player-Set Per Unit And Ability
Every combat unit SHALL carry player-set activation state, empty by default: for each ability name,
the set of activated condition texts and, per choice group, the selected options. State naming an
ability, condition or option the unit does not have SHALL be ignored. The system SHALL NOT change
activation state on its own, whatever the phase, turn or usage limit.

#### Scenario: The same ability on two units is activated independently
- **WHEN** two units each carry the same conditional ability and the player activates it on one
- **THEN** only that unit's effect is applied

#### Scenario: Stale state is ignored
- **WHEN** the activation state names an ability the unit no longer carries
- **THEN** the unit's view is built as if that entry were absent

#### Scenario: A phase change keeps an activation
- **WHEN** the player activates Chance for Glory and then selects a different phase
- **THEN** Chance for Glory stays activated

### Requirement: An Activated Effect Is Applied Like An Unconditional One
A conditional effect SHALL be activated when it belongs to no choice branch and its condition text
(or no text) is activated for its ability on that unit, or when its choice option is selected. An
activated effect SHALL be applied by every consumer exactly as an unconditional effect, under the
same target, bearer, selector and liveness rules, and SHALL NOT be recorded as not applied.

#### Scenario: Chance for Glory activated changes the melee weapons
- **WHEN** Chance for Glory is activated on a Chaos Lord's unit
- **THEN** the Chaos Lord's melee weapons show Strength, Attacks, Armour Penetration and Damage each
  improved by 1, and no not-applied effect for Chance for Glory remains

#### Scenario: An activated grant reaching some carriers splits the row
- **WHEN** an activated keyword grant reaches only some contributors of a merged weapon entry
- **THEN** the reached contributors form a separate weapon entry, as for an unconditional grant

#### Scenario: An activated effect follows bearer liveness
- **WHEN** the bearer of an activated ability is destroyed
- **THEN** its effect is no longer applied, and the activation state itself is unchanged

### Requirement: A Choice Selection Suppresses The Other Options
When a choice group has at least one selected option, the effects of its unselected options SHALL be
neither applied nor recorded as not applied. When it has no selection, every option's effects SHALL
be recorded as not applied. An effect in a selected option that also has its own condition text
SHALL be activated by the selection alone.

#### Scenario: Picking a pact drops the other
- **WHEN** the player selects Lethal Hits for a unit's Dark Pacts
- **THEN** the unit's weapons gain Lethal Hits as an applied grant, and no not-applied Sustained
  Hits 1 grant from Dark Pacts is recorded

#### Scenario: No selection shows every option as not added
- **WHEN** a unit's Dark Pacts has no selection
- **THEN** its weapons record not-applied grants of both Lethal Hits and Sustained Hits 1
