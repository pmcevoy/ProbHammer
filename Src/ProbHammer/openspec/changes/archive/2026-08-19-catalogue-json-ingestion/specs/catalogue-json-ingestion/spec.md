## Purpose

Resolves BSData 11th Edition JSON catalogue files into the existing `Domain.Catalogue` shape
(`Datasheet`, `Statline`, `WeaponProfile`, `Ability`), queryable by name, across a faction's full
transitive set of imported catalogue files.

## ADDED Requirements

### Requirement: Faction Catalogue Closure Resolution
Given a starting catalogue file, the system SHALL resolve the full transitive set of catalogue
files reachable by following import links that import all of a linked catalogue's root entries,
recursively — an imported catalogue that itself imports further catalogues SHALL have those
further catalogues included in the closure.

#### Scenario: Single-level import
- **WHEN** resolving the closure starting from a sub-faction file that imports one other catalogue
- **THEN** the closure includes the starting file and that one imported catalogue

#### Scenario: Multi-level transitive import
- **WHEN** resolving the closure starting from `Imperium - Black Templars`, which imports
  `Imperium - Space Marines`, which itself imports `Imperium - Agents of the Imperium`,
  `Library - Titans`, and `Imperium - Imperial Knights - Library`
- **THEN** the closure includes all five files, not just the directly-imported one

#### Scenario: A faction with no sub-faction split still has its own import chain
- **WHEN** resolving the closure starting from `Chaos - Chaos Space Marines` (a faction with a
  single, unsplit faction declaration)
- **THEN** the closure still includes that file's own imports (`Chaos - Chaos Knights Library`,
  `Chaos - Daemons Library`, `Library - Astartes Heresy Legends`), not just the starting file

### Requirement: Local-Over-Imported Name Precedence
When resolving a name against a closure, an entry defined in the file nearer the closure's
starting point SHALL take precedence over a same-named entry only reachable through an import
further out.

#### Scenario: Locally-defined entry is used even though the closure also carries an import
- **WHEN** resolving `"Crusader Squad"` against the Black Templars closure, where `Crusader Squad`
  is fully defined within the starting file itself
- **THEN** the starting file's own definition is returned, not a lookup into any imported file

#### Scenario: Name absent locally falls through to an imported catalogue
- **WHEN** resolving `"Assault Intercessor Squad"` or `"Scout Squad"` against the Black Templars
  closure, where neither name is defined anywhere in the starting file
- **THEN** the definition is resolved from the imported `Imperium - Space Marines` catalogue

### Requirement: Statline Extraction
The system SHALL extract one or more named `Statline`s per resolved unit entry: a single-model
unit's statline SHALL be read from characteristics on the entry itself; a squad unit's statlines
SHALL be read from characteristics on each of its distinct child model entries, preserving their
declared order, so the result satisfies `datasheet-catalogue`'s existing "Multiple Named
Statlines" requirement.

#### Scenario: Single-model unit
- **WHEN** extracting a statline for a single-model entry (e.g. an Epic Hero) whose `Unit`-typeName
  profile carries `M`, `T`, `Sv`, `W`, `LD`, `OC`, and `InSv` characteristics directly
- **THEN** one named `Statline` is produced, including `InSv` read directly from that
  characteristic rather than extracted from free text

#### Scenario: Squad unit with multiple distinct model types
- **WHEN** extracting statlines for a squad entry whose distinct model types (e.g. Sword Brother,
  Initiate, Neophyte) each carry their own `Unit`-typeName profile nested within the squad entry
- **THEN** one named `Statline` per distinct model type is produced, in the order those model
  types are declared in the source file

### Requirement: Weapon Profile Extraction
The system SHALL extract a `RangedWeapon` or `MeleeWeapon` from each `Ranged Weapons`- or `Melee
Weapons`-typeName profile encountered while resolving a unit, reading Range, Attacks, Skill,
Strength, AP, and Damage from that profile's characteristics.

#### Scenario: Ranged weapon profile
- **WHEN** a resolved unit carries a `Ranged Weapons`-typeName profile with `Range`, `A`, `BS`,
  `S`, `AP`, and `D` characteristics
- **THEN** a `RangedWeapon` is produced with those values, with AP stored as the negative integer
  the source data already uses

#### Scenario: Melee weapon profile
- **WHEN** a resolved unit carries a `Melee Weapons`-typeName profile with `A`, `WS`, `S`, `AP`,
  and `D` characteristics (and a `Range` characteristic whose value is the literal text `"Melee"`)
- **THEN** a `MeleeWeapon` is produced with those values

### Requirement: Weapon Keyword Flag Parsing
The system SHALL parse a weapon profile's free-text `Keywords` characteristic into
`WeaponProfile`'s existing ability flags, for tokens whose mapping to an existing flag is exact
and unambiguous. A `Keywords` value of `"-"` SHALL produce no flags set. A token that does not
exactly match an existing `WeaponProfile` flag — whether because it names a mechanic with no
corresponding flag, or because it is a context-dependent alternate spelling of a mechanic an
existing flag already models — SHALL NOT be mapped to that flag by inference, and SHALL NOT cause
resolution to fail; see the Weapon Profile Verbatim Keyword Text requirement in
`datasheet-catalogue` for how such a token is preserved instead.

#### Scenario: Multiple comma-separated recognized tokens
- **WHEN** a weapon's `Keywords` characteristic is `"Anti-infantry 4+, Devastating Wounds"`
- **THEN** the produced `WeaponProfile` has `DevastatingWounds` set and its `Anti` dictionary
  contains an `"infantry"` entry with threshold `4`

#### Scenario: Value-carrying token
- **WHEN** a weapon's `Keywords` characteristic is `"Sustained Hits 1"`
- **THEN** the produced `WeaponProfile` has `SustainedHits` set to `1`

#### Scenario: No keywords
- **WHEN** a weapon's `Keywords` characteristic is `"-"`
- **THEN** the produced `WeaponProfile` has every ability flag at its default (unset) value

#### Scenario: Token with no corresponding WeaponProfile flag
- **WHEN** a weapon's `Keywords` characteristic includes a token such as `"Hazardous"`,
  `"Precision"`, or `"Heavy"`, none of which correspond to an existing `WeaponProfile` flag
- **THEN** resolution completes without error and no existing flag is set on that token's behalf

#### Scenario: Alternate spelling of an already-modeled flag is not inferred
- **WHEN** a weapon's `Keywords` characteristic includes `"Cleave"` (melee's equivalent of
  `Blast`) or `"Close Combat"` (a generalization of `Pistol`)
- **THEN** resolution completes without error, and neither `Blast` nor `Pistol` is set on that
  token's behalf — recognizing the alias is out of scope for this change

### Requirement: Ability Text Extraction
The system SHALL extract an `Ability` from each `Abilities`-typeName profile encountered while
resolving a unit, carrying the profile's `Description` characteristic through as `Ability.Text`
verbatim, without parsing it for behavioral effect, consistent with `datasheet-catalogue`'s
existing stance that ability text is never auto-parsed into behavior.

#### Scenario: Ability with descriptive text only
- **WHEN** a resolved unit carries an `Abilities`-typeName profile with a `Description`
  characteristic
- **THEN** an `Ability` is produced whose `Text` equals that characteristic's value unchanged

### Requirement: No Composition, Validation, or Points Data
The system SHALL NOT interpret BSData's composition/validation apparatus (`constraints`,
`modifiers`, `conditionGroups`, or `associations`/eligibility links) and SHALL NOT extract points
costs, consistent with `datasheet-catalogue`'s existing "No Wargear Constraint or Points Modeling"
requirement — this system resolves names to rules data; it does not validate that a chosen roster
is legal.

#### Scenario: Composition apparatus is ignored
- **WHEN** resolving a unit entry that carries `constraints`, `modifiers`, or an `associations`
  block describing which units it may lead
- **THEN** none of that data appears on the produced `Datasheet`, `Statline`, `WeaponProfile`, or
  `Ability` objects

#### Scenario: Points cost is ignored
- **WHEN** resolving a unit entry whose `costs` array includes an entry named `"pts"`
- **THEN** the produced `Datasheet` exposes no points value
