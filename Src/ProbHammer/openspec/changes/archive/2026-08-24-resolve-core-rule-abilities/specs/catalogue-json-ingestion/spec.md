## ADDED Requirements

### Requirement: Core Rule Ability Extraction
The system SHALL extract an `Ability` from each datasheet-wide reference to a separately-defined
Core or faction rule encountered while resolving a unit, populating the Datasheet's intrinsic
(always-exposed) ability list — the same list an Intrinsic ability populates — with Origin Core
Rule. The extracted Ability's Name SHALL be the referenced rule's own name, with any accompanying
value or threshold the reference itself specifies (e.g. a damage value, a range distance) appended
to it in the order specified, separated by a single space. The extracted Ability's Text SHALL be
resolved by looking up the referenced rule's own name against the closure's rule-text glossary
(`rules-glossary`), consistent with `datasheet-catalogue`'s existing stance that ability text is
never auto-parsed into behavior. When the referenced rule's name does not resolve against the
closure's glossary, the reference SHALL be skipped — no Ability is produced for it — without
failing resolution of the rest of the unit. A datasheet-wide rule reference found nested inside a
`"type": "upgrade"` selection entry (the same signal that distinguishes an Intrinsic ability from
an optional one, per `datasheet-catalogue`'s Ability Shape requirement) SHALL NOT produce an
Ability at all, even when its name resolves successfully against the closure's glossary — it is a
weapon or wargear option's own keyword cross-reference, not a datasheet-wide Core or faction rule.

#### Scenario: A universal Core rule with an appended value resolves
- **WHEN** a resolved unit carries a reference to the Core rule named `"Deadly Demise"` with an
  accompanying value of `"D3"`
- **THEN** an `Ability` is produced with Name `"Deadly Demise D3"`, Origin Core Rule, and Text equal
  to the "Deadly Demise" rule's resolved text

#### Scenario: A faction rule with no appended value resolves
- **WHEN** a resolved unit carries a reference to the faction rule named `"Templar Vows"` with no
  accompanying value
- **THEN** an `Ability` is produced with Name `"Templar Vows"`, Origin Core Rule, and Text equal to
  the "Templar Vows" rule's resolved text, reached via the closure's transitive import chain the
  same way any other name is reached (per the existing Faction Catalogue Closure Resolution
  requirement)

#### Scenario: An unresolvable rule reference is skipped, not failed
- **WHEN** a resolved unit carries a datasheet-wide rule reference whose name does not resolve
  against the closure's rule-text glossary
- **THEN** no `Ability` is produced for that reference, and resolution of the rest of the unit
  completes without error

#### Scenario: A reference with no appended value uses the rule's own name unchanged
- **WHEN** a resolved unit carries a reference to a Core or faction rule with no accompanying value
- **THEN** the produced `Ability`'s Name equals the referenced rule's own name, with no trailing
  space or other alteration

#### Scenario: A weapon-scoped rule reference is excluded, not extracted as a datasheet ability
- **WHEN** a rule reference is found nested inside a `"type": "upgrade"` weapon or wargear option
  entry (e.g. a weapon's own Keywords cross-reference to the "Sustained Hits" or "Anti" rule),
  rather than directly on the entry that owns the unit's resolved Statline
- **THEN** no `Ability` is produced for that reference, whether or not its name would otherwise
  resolve against the closure's glossary
