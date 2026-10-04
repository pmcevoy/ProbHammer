## ADDED Requirements

### Requirement: Value Provenance Highlight
A Statline tile (M, T, Sv, W, Ld, OC, InSv) or a weapon-table Attacks, Strength, AP or Damage value
SHALL render as an inset amber tile with a corner tick whenever an ability has something to say
about it: an applied ability change, a caveat left unresolved, or a conditional effect that was not
added. The tile SHALL be a popover trigger. A value nothing touches SHALL render plain.

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
- **THEN** the Daemon hammer's Attacks, Strength, AP and Damage values each render highlighted, each
  still showing its unmodified value

#### Scenario: Tapping a highlighted value opens its provenance popover
- **WHEN** a player taps a highlighted value
- **THEN** that value's provenance popover opens, per "Value Provenance Popover Content"

### Requirement: Value Provenance Popover Content
A highlighted value's popover SHALL show a title naming the characteristic and the statline or
weapon, the original value, one line per ability that has something to say about it, and the
resulting value. Each ability line SHALL be an ability-name popover trigger (opening that ability's
own text, per "Nested Reference Popover") with the change it makes, and SHALL note the ability's
unmodelled residue when its classification records one.

#### Scenario: A modified value's popover shows original, ability and total
- **WHEN** a player taps the highlighted Strength 9 of a Power fist in Helbrecht's Crusader Squad
- **THEN** the popover shows original 8, a Crusade of Wrath line with +1, and total 9

#### Scenario: An ability line opens the ability's text
- **WHEN** a player taps the Crusade of Wrath line inside a provenance popover
- **THEN** a nested popover opens showing Crusade of Wrath's rules text, and the provenance popover
  stays open beneath it

#### Scenario: Residue is noted under its ability
- **WHEN** a provenance popover lists Faith-Fuelled Resolve, whose classification records an
  unmodelled army-construction restriction
- **THEN** that restriction renders as a note beneath the Faith-Fuelled Resolve line

#### Scenario: A resolved datasheet footnote shows the printed value as the original
- **WHEN** a player taps an Impulsor's InSv tile whose footnoted datasheet save was resolved by
  Refractor Field
- **THEN** the popover shows the printed save as the original, a Refractor Field line, and the
  resolved save as the total

### Requirement: Conditional Effects Are Shown But Not Added
A conditional effect (usage limit, turn restriction, residual condition, or choice branch) reaching
a value SHALL appear in its popover as an ability line with its change, its condition text, and a
note that it was not added. The value shown SHALL NOT include it. A popover holding only such lines
SHALL end with the shown value rather than a total.

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

### Requirement: Caveated Value Popover Content
A caveated value's popover SHALL show the datasheet value and state that it may be modified by the
caveat's linked ability, as an ability-name popover trigger. It SHALL NOT show a total, since no
resolved value exists.

#### Scenario: An unresolved caveat names its ability without a total
- **WHEN** a player taps a still-caveated InSv tile whose linked ability could not be resolved
- **THEN** the popover shows the datasheet save and "may be modified by" that ability, with no total

### Requirement: Weapon Attacks Total Provenance
A weapon row's total Attacks SHALL be highlighted when any contribution carries an applied or
not-added Attacks effect. Its popover's original SHALL be the total without ability contributions,
each ability line SHALL show its total amount with the per-model amount and model count, and the
total SHALL equal the row's displayed A. The expanded breakdown SHALL keep its ability lines.

#### Scenario: A Power fist's total Attacks popover
- **WHEN** a player taps the highlighted A 8 of a Power fist carried by two Initiates with Crusade of
  Wrath applied
- **THEN** the popover shows original 6, a Crusade of Wrath line with +2 (+1 per model, 2 models),
  and total 8

#### Scenario: The breakdown still lists the ability line
- **WHEN** a player expands that Power fist's contribution breakdown
- **THEN** the breakdown shows the Initiate row (2×3) 6 followed by the Crusade of Wrath (2×1) 2
  line, unchanged

#### Scenario: Selection filtering keeps the popover in step
- **WHEN** a player deselects some of a weapon's contributors and the row's displayed A is
  recomputed
- **THEN** the A popover's original and total match the recomputed value

### Requirement: No Footnote Markers Or Flag Legends
The page SHALL NOT render footnote markers on Statline labels, weapon values or weapon names, nor any
flag-legend line or row. The value provenance popover is the only place a value's source abilities
are listed.

#### Scenario: A modified tile's label is unmarked
- **WHEN** a run's OC is modified by an ability
- **THEN** the tile's label reads "OC" with no marker, and no legend renders beneath the run

#### Scenario: A weapon with a conditional effect has no name marker
- **WHEN** a weapon is reached only by a conditional effect
- **THEN** its name renders unmarked, no legend row renders for it, and its reached values are
  highlighted instead

### Requirement: Weapon Attacks Breakdown Ability Lines
Within a weapon entry's contribution breakdown, a matched, non-caveated Attacks-characteristic
effect SHALL render as a separate line naming its source ability (a popover trigger), in the
`(Count×Amount)` notation the base rows use, additive to the row(s) it reaches. Every rendered
number SHALL be a genuine addend of the total, and a base row SHALL show its unmutated value.

#### Scenario: A row-bound ability contribution nests under its own contributor row
- **WHEN** a weapon entry's contribution breakdown includes one contributor row whose Attacks value
  is affected by a matched, non-caveated ability reaching only that contributor
- **THEN** that ability renders as its own line nested directly under that contributor's row, one
  indent step deeper, showing the reaching Count and the resolved per-model Amount, and the
  contributor's own row still shows its base (unaffected) value

#### Scenario: A group-wide ability contribution renders once, after the breakdown's base rows
- **WHEN** a matched, non-caveated ability's effect reaches every one of a weapon entry's current
  contributors identically
- **THEN** that ability renders as a single line after the last base contributor row of the
  breakdown (including merged and selection-excluded rows), at the same indent as the base
  contributor rows, not repeated under any individual contributor row

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

#### Scenario: An ability affecting Strength, AP or Damage does not add a breakdown line
- **WHEN** a matched, non-caveated effect targets Strength, Armour Penetration, or Damage rather than
  Attacks
- **THEN** no breakdown line renders for it; it surfaces through that value's provenance highlight

## MODIFIED Requirements

### Requirement: Battle-Shocked Objective Control Rendering
Every Objective Control stat-tile belonging to any component of a Battle-shocked combat unit SHALL
render as a value provenance highlight (per "Value Provenance Highlight"), with its displayed value
replaced by the same glyph used for the toolbar's Battle-shock control, rendered in the ordinary
stat-value text color — never the tile's numeric Objective Control value, and never in an amber or
otherwise distinct color. The tile's label SHALL remain "OC", unchanged, and no explanatory text
SHALL render beneath it. Tapping the tile SHALL open its provenance popover, showing the original
value, any ability lines, a "Battle-shocked" line setting the value to 0, and a total of 0. This
rendering applies to every component's Objective Control tile within a Battle-shocked combat unit,
not only whichever statline run happens to sit nearest the unit block's own toolbar.

#### Scenario: An ordinary unit's Objective Control tile shows its numeric value
- **WHEN** a unit block's combat unit is not Battle-shocked
- **THEN** every Objective Control tile in that block renders its numeric value, with no Battle-shock
  treatment

#### Scenario: A Battle-shocked unit's Objective Control tile shows the glyph instead of a number
- **WHEN** a unit block's combat unit is Battle-shocked
- **THEN** every Objective Control tile in that block renders as a value provenance highlight and
  shows the Battle-shock glyph, in the ordinary stat-value text color, in place of the numeric value

#### Scenario: Every component's Objective Control tile is affected, not just one
- **WHEN** an AttachedUnit is Battle-shocked and has more than one statline run across its
  Bodyguard and Attached components
- **THEN** every one of those runs' Objective Control tiles renders the Battle-shock treatment, not
  only the run nearest the unit block's own toolbar

#### Scenario: Clearing Battle-shocked status reverts the tile
- **WHEN** a player clears a unit's Battle-shocked status
- **THEN** every Objective Control tile in that unit's block reverts to its numeric rendering, still
  highlighted if an ability has something to say about it

#### Scenario: Tapping a Battle-shocked tile shows how the value was reached
- **WHEN** a player taps the Battle-shocked OC tile of a Marshal whose OC 1 is raised to 2 by
  Faith-Fuelled Resolve
- **THEN** the popover shows original 1, a Faith-Fuelled Resolve line with +1, a Battle-shocked line
  setting it to 0, and total 0

## REMOVED Requirements

### Requirement: Flagged Statline Characteristic Rendering
**Reason**: Footnote markers and per-run legends are replaced by highlighted, tappable values.
**Migration**: See "Value Provenance Highlight", "Value Provenance Popover Content", "Caveated Value
Popover Content" and "No Footnote Markers Or Flag Legends".

### Requirement: Flagged Weapon Characteristic Rendering
**Reason**: Value markers, the weapon-name "unresolved ability" marker and the weapon legend row are
replaced by highlighted, tappable values.
**Migration**: See "Value Provenance Highlight", "Conditional Effects Are Shown But Not Added" and
"No Footnote Markers Or Flag Legends".

### Requirement: Weapon Ability-Contribution Row Rendering
**Reason**: Its last scenario pointed S/AP/D effects at the retired marker convention; the
requirement is restated without it as "Weapon Attacks Breakdown Ability Lines".
**Migration**: See "Weapon Attacks Breakdown Ability Lines"; behavior is otherwise unchanged.
