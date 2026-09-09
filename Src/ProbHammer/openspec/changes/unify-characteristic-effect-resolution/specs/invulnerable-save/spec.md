## REMOVED Requirements

### Requirement: Footnoted Caveat Text Resolution
**Reason**: Superseded by unified Build-time resolution against the checked-in
`RuleClassificationBaseline` (see `statline-flag-rules`' "Deferred Characteristic Associations Resolve
At Their Recorded Scope" in this same change), which recognizes a broader, human-verified vocabulary
of ability text than this fixed four-template set ever could — including phrasing
`widen-baseline-generation-coverage` (Stage 1) added specifically because this requirement's own
template set already needed it, and any structurally-derived entry neither could ever match. Parse-time
resolution is retired entirely; a parser no longer classifies a footnoted or split value's linked
ability text against anything.

**Migration**: No external contract changes — every real scenario this requirement covered (a single
footnoted value or a melee/ranged pair resolving via an exact ability-text match, or staying caveated
when the text doesn't match) is still covered, at Build time instead of parse time, by
`statline-flag-rules`' generalized resolution. No caller of `Datasheet`/`Statline` observes a
difference in the final displayed/caveated result for any text this requirement's own template set
already matched.

## ADDED Requirements

### Requirement: Parse-Time Resolution Never Classifies A Caveat's Linked Ability
When a Statline's invulnerable save cannot be fully determined from its own raw catalogue
characteristic text alone (a footnote, or a melee/ranged pair with exactly one footnoted side, naming
a linked ability), the parser SHALL record only that the ability exists and is associated with that
specific invulnerable save at its own correct attachment scope. The parser SHALL NOT itself attempt to
interpret the linked ability's Text, compare it against any known phrasing, or otherwise judge whether
the caveat should resolve — that determination SHALL be made entirely by a later resolution layer, not
by parsing.

#### Scenario: An unresolvable footnote defers rather than attempting its own resolution
- **WHEN** a Statline's invulnerable save text is footnoted, naming a linked ability
- **THEN** the parser produces a caveated result carrying that ability, without evaluating the
  ability's own Text against any template or pattern

#### Scenario: A fully-determinable value never involves an ability at all
- **WHEN** a Statline's invulnerable save text is fully determinable from the raw characteristic text
  alone (a plain value, or a parenthetical melee/ranged restriction)
- **THEN** the parser produces a non-caveated result with no ability involved, exactly as it does
  today — this requirement changes only the footnoted/split, ability-linked case
