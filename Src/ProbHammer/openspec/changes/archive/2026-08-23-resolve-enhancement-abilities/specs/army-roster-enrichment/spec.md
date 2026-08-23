## MODIFIED Requirements

### Requirement: Unit and Weapon Name Resolution
For each parsed unit, the system SHALL resolve its name against the faction's catalogue closure
(per `catalogue-json-ingestion`'s Local-Over-Imported Name Precedence) to obtain a `Datasheet`,
normalizing typographic punctuation (e.g. a right single quotation mark) to its plain ASCII
equivalent before resolution. Each parsed model sub-group's own name SHALL resolve to a `Statline`
declared on that `Datasheet`; each of its weapon names SHALL resolve to a `WeaponProfile` via that
`Datasheet`, falling back to that `Datasheet`'s on-demand ability index (per
`datasheet-catalogue`'s On-Demand Ability Resolution) when the name is not a weapon profile — some
wargear lines grant an ability rather than a weapon (e.g. Impulsor's "Shield Dome", a granted
invulnerable save). The system SHALL fail with a diagnostic naming the unresolved text when a name
resolves against neither.

#### Scenario: A unit name containing a typographic apostrophe resolves correctly
- **WHEN** a parsed weapon name uses a typographic right single quotation mark (e.g.
  `"Reaver's blade"`) where the catalogue's own name uses a plain ASCII apostrophe
- **THEN** the name resolves successfully against the catalogue entry

#### Scenario: An unresolvable name fails with a diagnostic
- **WHEN** a parsed unit or weapon name resolves against neither a `WeaponProfile` nor the
  on-demand ability index of the resolved catalogue closure
- **THEN** resolution fails with a diagnostic naming the unresolved text, rather than silently
  omitting that unit or weapon

#### Scenario: A wargear name resolves to a granted ability instead of a weapon
- **WHEN** a parsed model sub-group's wargear name does not match any `WeaponProfile` on its
  resolved `Datasheet`
- **THEN** the system resolves that name against the Datasheet's on-demand ability index instead,
  and attaches the resolved ability to that model-line rather than failing resolution

## ADDED Requirements

### Requirement: Enhancement Name Resolution
For each parsed unit, the system SHALL resolve every name in its parsed Enhancements against its
resolved Datasheet's on-demand ability index (per `datasheet-catalogue`'s On-Demand Ability
Resolution), attaching each resolved `Ability` to the resulting `Unit`. The system SHALL fail with
a diagnostic naming the unresolved text when an Enhancement name does not resolve. No Enhancement
ability SHALL be attached to a Unit unless its name was actually present in that unit's parsed
Enhancements — an Enhancement the Datasheet merely defines as available is never attached on its
own.

#### Scenario: A selected Enhancement resolves to its ability text
- **WHEN** a parsed unit's Enhancements list includes a name matching an Enhancement-classified
  ability on its resolved Datasheet (e.g. "Ferocity")
- **THEN** the resulting Unit carries the resolved Ability, including its full rules Text

#### Scenario: An unresolvable Enhancement name fails with a diagnostic
- **WHEN** a parsed unit's Enhancements list includes a name that does not resolve against its
  resolved Datasheet's on-demand ability index
- **THEN** resolution fails with a diagnostic naming the unresolved text

#### Scenario: A unit with no Enhancements selected resolves with an empty Enhancements list
- **WHEN** a parsed unit's Enhancements list is empty
- **THEN** the resulting Unit's Enhancements list is empty — no Enhancement-classified ability is
  attached merely because the unit's Datasheet defines one available
