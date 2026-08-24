## ADDED Requirements

### Requirement: Core Rule Chapter and Game-Mode Gating
The system SHALL exclude a Core Rule Ability Extraction reference from producing an `Ability` when
the referenced rule's own `hidden` condition provably evaluates true for the current closure,
evaluated with the same condition-tree logic the system already applies to entry/group-level
game-mode gating, extended to also recognize a condition scoped to the resolved army's own primary
catalogue (chapter/sub-faction) selection. A condition scoped to the primary catalogue SHALL be
evaluated using its own comparison type against the closure's own starting catalogue identity — a
"not an instance of" condition is provably true when the referenced catalogue differs from the
closure's own starting catalogue, and provably true is provably false when they match; an
"is an instance of" condition is the inverse. A condition whose scope or comparison type is not
recognized SHALL be treated as unknowable, consistent with the system's existing conservative
default, never provably excluding content on an unrecognized shape.

#### Scenario: A chapter-exclusive rule is excluded for a non-matching chapter
- **WHEN** resolving a unit against a closure whose own starting catalogue is Black Templars, and
  a `CoreRule` reference's target rule is hidden unless the primary catalogue is an instance of one
  of eleven other named chapters, none of them Black Templars
- **THEN** no `Ability` is produced for that reference

#### Scenario: A chapter-exclusive rule is included for its own matching chapter
- **WHEN** resolving a unit against a closure whose own starting catalogue is Black Templars, and a
  `CoreRule` reference's target rule is hidden unless the primary catalogue is an instance of Black
  Templars specifically
- **THEN** an `Ability` is produced for that reference

#### Scenario: The same reference produces the opposite inclusion for a different chapter
- **WHEN** resolving the identical unit and rule reference against a closure whose own starting
  catalogue is a different chapter than the rule's own required chapter
- **THEN** the inclusion outcome for that reference is the opposite of what it was for the matching
  chapter

#### Scenario: A game-mode-gated rule reference is excluded outside its applicable mode
- **WHEN** a `CoreRule` reference's target rule is hidden when a specific game-mode force (e.g. a
  mission type) is present, evaluated the same way entry/group-level game-mode gating already is
- **THEN** no `Ability` is produced for that reference under that game mode

#### Scenario: An unrecognized gating shape does not exclude the reference
- **WHEN** a `CoreRule` reference's target rule carries a `hidden` condition whose scope or
  comparison type is not one of the recognized shapes
- **THEN** an `Ability` is still produced for that reference — an unrecognized shape never provably
  excludes content

### Requirement: Core Versus Army Rule Origin Classification
The system SHALL classify a Core Rule Ability Extraction reference's Origin as Army Rule when the
referenced rule's own gating carries a condition scoped to the resolved army's own primary
catalogue (chapter/sub-faction) selection anywhere in its condition tree, regardless of comparison
type and regardless of whether that condition currently excludes the reference — and as Core Rule
otherwise. This is a structural presence check on the referenced rule itself, independent of the
Core Rule Chapter and Game-Mode Gating requirement's own exclusion evaluation, and independent of
how many datasheets in a given roster happen to reference the same rule.

#### Scenario: A chapter-exclusive rule's Origin is Army Rule even when included
- **WHEN** a `CoreRule` reference's target rule carries a primary-catalogue-scoped condition, and
  the reference is not excluded for the resolved army's own chapter
- **THEN** the produced `Ability`'s Origin is Army Rule

#### Scenario: A rule with no chapter exclusivity at all has Core Rule Origin
- **WHEN** a `CoreRule` reference's target rule carries no primary-catalogue-scoped condition
  anywhere in its gating
- **THEN** the produced `Ability`'s Origin is Core Rule
