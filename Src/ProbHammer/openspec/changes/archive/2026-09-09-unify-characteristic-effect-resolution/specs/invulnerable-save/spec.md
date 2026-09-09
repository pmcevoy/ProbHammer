## REMOVED Requirements

### Requirement: Footnoted Caveat Text Resolution
**Reason**: Superseded by unified Build-time resolution against the checked-in
`RuleClassificationBaseline` (see "A Caveated Value Is Given One Resolution Attempt During Roster
Aggregation" below), which recognizes a broader, human-verified vocabulary of ability text than this
fixed four-template set ever could — including phrasing `widen-baseline-generation-coverage` (Stage 1)
added specifically because this requirement's own template set already needed it. Parse-time
resolution is retired entirely; a parser no longer classifies a footnoted or split value's linked
ability text against anything.

**Migration**: No external contract changes — every real scenario this requirement covered (a single
footnoted value or a melee/ranged pair resolving via an exact ability-text match, or staying caveated
when the text doesn't match) is still covered, at Build time instead of parse time. No caller of
`Datasheet`/`Statline` observes a difference in the final displayed/caveated result for any text this
requirement's own template set already matched.

## ADDED Requirements

### Requirement: Parse-Time Resolution Never Classifies A Caveat's Linked Ability
When a Statline's invulnerable save cannot be fully determined from its own raw catalogue
characteristic text alone (a footnote, or a melee/ranged pair with exactly one footnoted side, naming
a linked ability), the parser SHALL produce a caveated result carrying that ability, exactly as it
does today. The parser SHALL NOT itself attempt to interpret the linked ability's Text, compare it
against any known phrasing, or otherwise judge whether the caveat should resolve — that determination
SHALL be made entirely by a later resolution step (see "A Caveated Value Is Given One Resolution
Attempt During Roster Aggregation" below), not by parsing.

#### Scenario: An unresolvable footnote defers rather than attempting its own resolution
- **WHEN** a Statline's invulnerable save text is footnoted, naming a linked ability
- **THEN** the parser produces a caveated result carrying that ability, without evaluating the
  ability's own Text against any template or pattern

#### Scenario: A fully-determinable value never involves an ability at all
- **WHEN** a Statline's invulnerable save text is fully determinable from the raw characteristic text
  alone (a plain value, or a parenthetical melee/ranged restriction)
- **THEN** the parser produces a non-caveated result with no ability involved, exactly as it does
  today — this requirement changes only the footnoted/split, ability-linked case

### Requirement: A Caveated Value Is Given One Resolution Attempt During Roster Aggregation
For every resolved unit's Statline whose invulnerable save is caveated, the system SHALL attempt to
resolve it exactly once per aggregate rebuild by matching its own single contributing ability's
normalized Text against the checked-in `RuleClassificationBaseline`, using the same resolution this
system already applies to an ordinary present ability. A successful match SHALL replace the caveated
result with a resolved value carrying that ability as its source, preserving the true pre-caveat
original value. An unsuccessful match SHALL leave the result caveated, unchanged from today's existing
fallback behavior.

#### Scenario: A caveated value resolves via a baseline match
- **WHEN** a resolved unit's Statline carries a caveated invulnerable save, and its single
  contributing ability's normalized Text matches a baseline entry
- **THEN** the invulnerable save is displayed as resolved, using the matched entry's value, still
  referencing that ability as its source

#### Scenario: A caveated value with no baseline match stays caveated
- **WHEN** a resolved unit's Statline carries a caveated invulnerable save, and its contributing
  ability's normalized Text matches no baseline entry
- **THEN** the invulnerable save remains caveated, showing the Datasheet's own base value, exactly as
  before this resolution attempt was made

#### Scenario: A resolved value is never re-caveated by a later pass
- **WHEN** a resolved unit's Statline invulnerable save has already been resolved by this requirement
  or by any other characteristic-resolution mechanism
- **THEN** no later step in the same aggregate rebuild replaces or downgrades that resolved value
