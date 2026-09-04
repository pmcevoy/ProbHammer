## MODIFIED Requirements

### Requirement: Flagged Statline Characteristic Rendering
A Statline tile whose displayed value has been flagged — either an invulnerable save left caveated
per "Footnoted Caveat Text Resolution" (`invulnerable-save`), or a characteristic mutated by a
matched rule per `statline-flag-rules` — SHALL render a footnote marker appended to its label (e.g.
"InSv*", "OC**"), and that run SHALL render a legend directly beneath its tile row, one line per
distinct marker active within that run, naming the source ability responsible. Each legend line's
ability name SHALL be an interactive trigger using the same popover mechanism as any other ability
name in the unit block (per "Ability And Rule Text Popover") — the source ability's descriptive text
is available on demand, not rendered inline by default.

Marker identity SHALL be assigned once per unit block and reused for the same source ability
wherever it recurs within that block, so a single source referenced by more than one flagged tile,
or by tiles in more than one run, keeps the same marker throughout. The legend line for a given
marker SHALL render within every run that has a tile carrying that marker, even when this means the
same legend line appears in more than one run of the same unit block — it SHALL NOT be consolidated
into a single shared location for the whole unit block, so a run's own flagged tile is never left
without a visible local explanation.

#### Scenario: A flagged tile's label carries a marker
- **WHEN** a run's Statline tile has a flagged value
- **THEN** its label renders with a footnote marker appended, distinguishing it from an unflagged
  tile of the same characteristic

#### Scenario: A run's legend names the flagged tile's source
- **WHEN** a run has one flagged tile
- **THEN** a legend line renders beneath that run's tiles, pairing the tile's marker with the source
  ability's name, rendered as a popover trigger rather than inline prose

#### Scenario: Tapping a legend entry opens the source ability's text
- **WHEN** a player taps a legend entry's ability name
- **THEN** a popover opens showing that ability's full descriptive text, per "Ability And Rule Text
  Popover"

#### Scenario: One source ability keeps the same marker across the whole unit block
- **WHEN** a source ability's flagged value affects tiles in more than one run within the same unit
  block
- **THEN** every affected tile carries the same marker, and each affected run's own legend names
  that same source

#### Scenario: A unit-wide-scoped source's legend repeats in every affected run
- **WHEN** a source ability grants a flagged value that applies to every run of a unit (e.g. a
  bearer's-unit-wide characteristic change), and that unit renders more than one statline run
- **THEN** the legend line naming that source renders within every one of those runs, not only the
  run containing the bearer

#### Scenario: A caveated invulnerable save uses the same marker-and-legend mechanism
- **WHEN** a run's invulnerable save is left caveated (its linked ability text did not match a known
  template, per "Footnoted Caveat Text Resolution")
- **THEN** that run's legend names the invulnerable save view's own contributing ability the same
  way it would name any other flagged tile's source, rather than rendering that ability's text
  inline
