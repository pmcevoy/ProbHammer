# datasheet-catalogue

## Purpose

Represents reference/rules data (the Catalogue context) for 40K unit types: Datasheets, their
named statlines, on-demand weapon profile resolution, and Abilities. Independent of any specific
army list. TBD: expand as this capability grows beyond its initial domain-model scope.

## Requirements

### Requirement: Datasheet Identity
A Datasheet SHALL represent the reference/rules data for a single named unit type, independent of any specific army list, including its name, faction keywords, general keywords, and unit-wide abilities.

#### Scenario: Basic datasheet fields are present
- **WHEN** a Datasheet is constructed from catalogue data
- **THEN** it exposes a Name, a set of Faction Keywords, a set of Keywords, and a list of unit-wide Abilities

### Requirement: Multiple Named Statlines
A Datasheet SHALL support one or more named statlines, each independently specifying Movement,
Toughness, Save, Wounds, Leadership, and Objective Control. Statlines SHALL be exposed in the
order they were supplied to the Datasheet at construction — that declared order is a load-bearing
property of the Datasheet, not an incidental collection-enumeration detail, and downstream
consumers (e.g. the Attached Unit aggregate statline view) rely on it directly rather than
deriving a display order some other way.

#### Scenario: Datasheet with a single shared statline
- **WHEN** a Datasheet like "Assault Intercessor Squad" has one statline shared by all its models
- **THEN** the Datasheet exposes exactly one named statline entry, referenced by every model-line

#### Scenario: Datasheet with multiple distinct statlines
- **WHEN** a Datasheet like a Chaos Space Marine unit defines five distinct model types, each with its own statline
- **THEN** the Datasheet exposes five named statline entries, each independently queryable by name

#### Scenario: Statline and weapon eligibility vary independently
- **WHEN** a Datasheet's Sergeant-equivalent model shares its squad's statline but has different weapon options than the rest of the squad
- **THEN** the Datasheet allows the same named statline to be referenced by model-lines with different weapon eligibility, without requiring the weapon set to be identical

#### Scenario: Declared order is preserved on enumeration
- **WHEN** a Datasheet is constructed with statlines supplied in a specific order (e.g. the
  Sergeant-equivalent entry first, followed by the rank-and-file entry)
- **THEN** enumerating the Datasheet's statlines yields them in that same order, regardless of the
  names involved

### Requirement: On-Demand Weapon Profile Resolution
A Datasheet SHALL resolve weapon profiles by name on request rather than materializing a complete list of all available wargear options.

#### Scenario: Resolving a named weapon profile
- **WHEN** a caller requests the weapon profile named "Astartes chainsword" from a Datasheet
- **THEN** the Datasheet returns that profile's characteristics (Range, Attacks, Skill, Strength, AP, Damage, Abilities) without exposing or requiring enumeration of every other weapon profile the Datasheet defines

### Requirement: Ability Shape
An Ability SHALL carry a Name, free-text Text, an optional list of structured Choices, and a Scope of either Model or Unit.

#### Scenario: Simple ability with no choices
- **WHEN** an Ability has only descriptive Text and no sub-choices
- **THEN** its Choices collection is empty and its Text is preserved as-is

#### Scenario: Ability with structured choices and retained intro text
- **WHEN** an Ability's source data describes conditional instructions followed by a "choose one of the following" list (e.g. "While on an objective marker, choose one of the following:")
- **THEN** the Ability exposes that instructional prose as Text AND exposes each option as a structured entry in Choices, rather than flattening both into one string

#### Scenario: Ability scope is explicit
- **WHEN** an Ability is defined as affecting only its bearer model
- **THEN** its Scope is Model
- **WHEN** an Ability is defined as affecting every model in its unit (and, if attached, the whole attached unit)
- **THEN** its Scope is Unit

### Requirement: No Wargear Constraint or Points Modeling
A Datasheet SHALL NOT represent wargear option constraints (minimum/maximum selection counts), unit composition rules, or points costs.

#### Scenario: Datasheet omits constraint and cost data
- **WHEN** a Datasheet is constructed from catalogue data
- **THEN** it exposes no fields for wargear selection limits, composition counts, or points values

### Requirement: Weapon Profile Verbatim Keyword Text
A WeaponProfile SHALL retain the exact keyword text supplied by its source data, in source order,
independent of whether any individual keyword also corresponds to one of WeaponProfile's existing
typed ability flags. Rendering of a weapon's keywords SHALL read this verbatim record rather than
being reconstructed from the typed ability flags, so that a keyword whose meaning is not yet
understood is never silently omitted, and a keyword that is a context-dependent alternate spelling
of an already-modeled flag is never rendered as the flag's own canonical wording instead of its
actual source wording.

#### Scenario: A keyword that also maps to an existing flag is still retained verbatim
- **WHEN** a weapon's source keyword text includes `"Devastating Wounds"`, which also sets the
  existing `DevastatingWounds` flag
- **THEN** the produced WeaponProfile both has `DevastatingWounds` set AND retains
  `"Devastating Wounds"` in its verbatim keyword text

#### Scenario: A keyword with no corresponding flag is retained verbatim
- **WHEN** a weapon's source keyword text includes a token with no corresponding WeaponProfile
  flag (e.g. `"Hazardous"`, `"Precision"`, or `"Heavy"`)
- **THEN** the produced WeaponProfile retains that token in its verbatim keyword text, without
  asserting any existing typed ability flag on its behalf

#### Scenario: A context-dependent alternate spelling of an existing flag is retained verbatim, unaltered
- **WHEN** a melee weapon's source keyword text includes `"Cleave"` (the melee-context equivalent
  of `Blast`)
- **THEN** the produced WeaponProfile retains `"Cleave"` in its verbatim keyword text exactly as
  written, and rendering of that weapon's keywords shows `"Cleave"`, never `"Blast"`, regardless
  of whether the `Blast` flag is ever set on this weapon's behalf
