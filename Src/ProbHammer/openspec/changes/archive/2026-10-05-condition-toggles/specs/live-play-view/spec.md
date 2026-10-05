## MODIFIED Requirements

### Requirement: Conditional Effects Are Shown But Not Added
A conditional effect (usage limit, turn restriction, residual condition, or choice branch) reaching
a value SHALL appear in its popover as an ability line with its change, its condition text, and a
note that it was not added. The value shown SHALL NOT include it. A popover holding only such lines
SHALL end with the shown value rather than a total. A conditional effect the player has activated is
not shown this way (see "Activated Effects Are Marked In Provenance").

#### Scenario: A once-per-battle effect is listed but not added
- **WHEN** a player taps the highlighted Strength 8 of a Chaos Lord's Daemon hammer
- **THEN** the popover shows original 8, a Chance for Glory line with +1 noting once per battle and
  not added, and shown value 8

#### Scenario: A conditional effect beside an applied one
- **WHEN** one value is reached by an applied ability change and by a different ability's
  conditional effect
- **THEN** the popover lists both lines, and its total includes only the applied change

#### Scenario: A conditional invulnerable save gives an otherwise absent save a tile
- **WHEN** a unit with no invulnerable save carries Waaagh! (5+ invulnerable save while riled up)
- **THEN** the run renders a highlighted InSv tile showing no save, and its popover lists Waaagh!
  with 5+ and the condition "while the unit is riled up", not added

## ADDED Requirements

### Requirement: Condition Controls In The Ability Popover
An ability's popover within a unit block SHALL show, below its unchanged rule text, an Apply section
with one control per activatable condition of that ability on that unit (per
`condition-activation`), reflecting the unit's current activation state. An ability with no
activatable condition on that unit, and every popover outside a unit block, SHALL show no Apply
section.

#### Scenario: Chance for Glory shows a switch
- **WHEN** a player opens Chance for Glory's popover in a Chaos Lord's unit block
- **THEN** below its rule text the popover shows an Apply section with one switch labelled with the
  usage limit "Once per battle", off by default

#### Scenario: A condition with text is labelled with that text
- **WHEN** an ability's activatable condition has condition text
- **THEN** its switch is labelled with that condition text

#### Scenario: The Army Header's rule popover has no controls
- **WHEN** a player opens Dark Pacts' popover from the Army Header's Rules section
- **THEN** the popover shows the rule text with no Apply section

### Requirement: Choice Conditions Render As An Option List
A choice condition allowing at most one selection SHALL render as a radio list whose first option is
"None", followed by each option's text as classified. A choice condition allowing more SHALL render
one checkbox per option, preventing more selections than its maximum.

#### Scenario: Dark Pacts renders three radio options
- **WHEN** a player opens Dark Pacts' popover in a Heretic Astartes unit block with no selection
- **THEN** the Apply section shows radio options None, [LETHAL HITS] and [SUSTAINED HITS 1], with
  None selected

#### Scenario: A multi-select group caps its selections
- **WHEN** a choice condition allows at most two selections and two are selected
- **THEN** the remaining options cannot be selected until one is cleared

### Requirement: Condition Changes Apply When The Popover Closes
A change to an Apply control SHALL be recorded immediately, and SHALL take visible effect on the unit
block when the popover holding it closes, without the change itself closing the popover.

#### Scenario: Picking a pact turns its chips amber
- **WHEN** a player selects [LETHAL HITS] in Dark Pacts' popover and then closes the popover
- **THEN** the unit's weapons show an amber Lethal Hits chip and no Sustained Hits 1 chip from Dark
  Pacts

#### Scenario: The popover stays open while changing a control
- **WHEN** a player switches Chance for Glory on
- **THEN** the popover stays open showing the switch on until the player closes it

### Requirement: Activated Effects Are Marked In Provenance
An activated effect reaching a value or keyword chip SHALL appear in its popover as an applied
ability line noting that the player activated it, and the value or chip SHALL render with the
applied (amber) highlight rather than the conditional colour.

#### Scenario: An activated Strength change is noted
- **WHEN** Chance for Glory is activated and a player taps the Daemon hammer's Strength 9
- **THEN** the popover shows original 8, a Chance for Glory line with +1 noting it was activated,
  and total 9

### Requirement: Condition Activation State Persists Across Reloads and Restarts
The page SHALL record every condition activation so that reloading the page, restarting the
browser, or the server restarting reproduces the same activations on the same browser/device, the
same guarantee made for casualty and Battle-shocked state.

#### Scenario: Reloading keeps a selected pact
- **WHEN** a player selects Lethal Hits for a unit's Dark Pacts and reloads the page
- **THEN** the unit's weapons still show the amber Lethal Hits chip and the popover shows Lethal
  Hits selected
