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
A Datasheet SHALL support one or more named statlines, each independently specifying Movement, Toughness, Save, Wounds, Leadership, and Objective Control.

#### Scenario: Datasheet with a single shared statline
- **WHEN** a Datasheet like "Assault Intercessor Squad" has one statline shared by all its models
- **THEN** the Datasheet exposes exactly one named statline entry, referenced by every model-line

#### Scenario: Datasheet with multiple distinct statlines
- **WHEN** a Datasheet like a Chaos Space Marine unit defines five distinct model types, each with its own statline
- **THEN** the Datasheet exposes five named statline entries, each independently queryable by name

#### Scenario: Statline and weapon eligibility vary independently
- **WHEN** a Datasheet's Sergeant-equivalent model shares its squad's statline but has different weapon options than the rest of the squad
- **THEN** the Datasheet allows the same named statline to be referenced by model-lines with different weapon eligibility, without requiring the weapon set to be identical

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
