## MODIFIED Requirements

### Requirement: Core Versus Army Rule Origin Classification
The system SHALL classify a Core Rule Ability Extraction reference's Origin as Army Rule when the
referenced rule's own Name matches one of the known army-wide rule names resolved for the roster's
own Faction (per `army-rule-name-lookup`'s Faction-Scoped Resolution), supplied to this
classification as a caller-provided name set, and as Core Rule otherwise. This classification is
independent of the Core Rule Chapter and Game-Mode Gating requirement's own exclusion evaluation
(which decides whether an Ability is produced at all, not what Origin it carries), and independent
of how many datasheets in a given roster happen to reference the same rule. Whether the referenced
rule's own gating carries a condition scoped to the resolved army's own primary catalogue
(chapter/sub-faction) selection has no bearing on this classification — that structural shape is
not a reliable signal of a genuine army-wide rule (it is also used, in the real BSData corpus, for
mustering/composition rules that are not army-wide gameplay rules at all) and is not consulted here.

#### Scenario: A chapter-exclusive rule's Origin is Army Rule even when included
- **WHEN** a `CoreRule` reference's target rule carries a primary-catalogue-scoped condition, and
  its own Name matches one of the known army-wide rule names resolved for the roster's own Faction
  (e.g. "Templar Vows" for a Black Templars roster)
- **THEN** the produced `Ability`'s Origin is Army Rule

#### Scenario: A rule with no chapter exclusivity at all has Core Rule Origin
- **WHEN** a `CoreRule` reference's target rule carries no primary-catalogue-scoped condition
  anywhere in its gating, and its own Name does not match any of the known army-wide rule names
  resolved for the roster's own Faction
- **THEN** the produced `Ability`'s Origin is Core Rule

#### Scenario: A rule matching the roster's curated lookup has Army Rule Origin even with no structural gating
- **WHEN** a `CoreRule` reference's target rule carries no primary-catalogue-scoped condition, but
  its own Name matches one of the known army-wide rule names resolved for the roster's own Faction
  (e.g. "Nurgle's Gift" for a Death Guard roster, whose own rule definition carries no gating at
  all)
- **THEN** the produced `Ability`'s Origin is Army Rule

#### Scenario: A rule carrying primary-catalogue-scoped gating but not matching the curated lookup has Core Rule Origin
- **WHEN** a `CoreRule` reference's target rule carries a primary-catalogue-scoped condition, but its
  own Name does not match any of the known army-wide rule names resolved for the roster's own
  Faction (e.g. a mustering/composition rule such as "Assigned Agents", which is gated the same way
  a genuine army rule is but is not one)
- **THEN** the produced `Ability`'s Origin is Core Rule, not Army Rule

#### Scenario: An allied unit's own rule is unaffected by a different faction's curated entry
- **WHEN** resolving a unit that belongs to a different faction than the roster's own primary
  Faction (e.g. an allied Imperial Knight inside a Custodes roster), and that unit's own `CoreRule`
  reference's Name does not match any of the known army-wide rule names resolved for the roster's
  own (primary) Faction
- **THEN** the produced `Ability`'s Origin is Core Rule, even if that Name happens to match a
  curated entry for some other faction
