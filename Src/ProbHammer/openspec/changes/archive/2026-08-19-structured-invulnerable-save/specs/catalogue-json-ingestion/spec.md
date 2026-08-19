## MODIFIED Requirements

### Requirement: Statline Extraction
The system SHALL extract one or more named `Statline`s per resolved unit entry: a single-model
unit's statline SHALL be read from characteristics on the entry itself; a squad unit's statlines
SHALL be read from characteristics on each of its distinct child model entries, preserving their
declared order, so the result satisfies `datasheet-catalogue`'s existing "Multiple Named
Statlines" requirement. Each statline's invulnerable save SHALL be resolved per "Invulnerable
Save Text Resolution" rather than read as a single value.

#### Scenario: Single-model unit
- **WHEN** extracting a statline for a single-model entry (e.g. an Epic Hero) whose `Unit`-typeName
  profile carries `M`, `T`, `Sv`, `W`, `LD`, `OC`, and `InSv` characteristics directly
- **THEN** one named `Statline` is produced, with its invulnerable save resolved from the `InSv`
  characteristic's raw text per "Invulnerable Save Text Resolution"

#### Scenario: Squad unit with multiple distinct model types
- **WHEN** extracting statlines for a squad entry whose distinct model types (e.g. Sword Brother,
  Initiate, Neophyte) each carry their own `Unit`-typeName profile nested within the squad entry
- **THEN** one named `Statline` per distinct model type is produced, in the order those model
  types are declared in the source file

## ADDED Requirements

### Requirement: Invulnerable Save Text Resolution
The system SHALL resolve a `Unit`-typeName profile's raw invulnerable-save characteristic text into
an `invulnerable-save` value as follows.

Text stating its attack-type restriction directly — a parenthetical annotation naming the
restricted attack type, or the unfootnoted side of a two-value split — SHALL be resolved without
consulting any ability, setting the value for the other attack type to 0.

Text carrying a footnote marker — a bare trailing marker on a single value, or a marker on one side
of a two-value split — SHALL always resolve as caveated: the system SHALL NOT interpret the
marker's meaning from the characteristic text or from any linked ability's own text. It SHALL
instead resolve, by id, the specific `Ability` reachable from the same source entry as the
invulnerable-save characteristic (whether defined locally within that entry or reachable through a
link) — never by matching the ability's name alone, since two abilities in the source data can
share an identical name with different meanings — and populate both the melee and ranged values
with the one value known without needing that interpretation (the unfootnoted side of a split when
one exists, otherwise the footnoted value itself).

Resolution SHALL fail, identifying the characteristic and its raw text, when the raw text matches
none of these recognized shapes, or when a footnote marker is present but no candidate ability is
reachable from the same source entry.

#### Scenario: Plain value
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic is a plain value with no
  marker (e.g. `"4+"`)
- **THEN** the resolved value is uniform and non-caveated

#### Scenario: Parenthetical attack-type restriction
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic states its restriction
  directly (e.g. `"5+ (Ranged)"`)
- **THEN** the resolved value's ranged value is 5, its melee value is 0, and it is non-caveated,
  without consulting any ability

#### Scenario: Bare footnoted value
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic is a single value with a
  trailing footnote marker (e.g. `"5+*"`)
- **THEN** the resolved value is caveated, both its melee and ranged values equal the parsed digit,
  and it carries a reference to the specific `Ability` reachable from the same entry, resolved by
  id

#### Scenario: Two-value split with one footnoted side
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic is a two-value split where
  one side carries a footnote marker (e.g. `"4+* / 5+"`)
- **THEN** the resolved value is caveated, both its melee and ranged values equal the unfootnoted
  side's parsed digit, and it carries a reference to the specific `Ability` reachable from the same
  entry, resolved by id

#### Scenario: Unrecognized raw text shape fails resolution
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic text matches none of the
  recognized shapes
- **THEN** resolution fails, identifying the characteristic and the offending text

#### Scenario: Footnoted value with no reachable candidate ability fails resolution
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic carries a footnote marker
  but no ability matching the expected naming convention is reachable from the same source entry
- **THEN** resolution fails, identifying the characteristic and the offending text
