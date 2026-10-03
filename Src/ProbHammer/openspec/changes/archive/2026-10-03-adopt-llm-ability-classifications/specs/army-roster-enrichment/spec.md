## MODIFIED Requirements

### Requirement: Detachment Rule Keyword Target Resolution
Once every `Unit`/`AttachedUnit` and every selected Detachment's own `DetachmentRule`s are resolved
into a roster, the system SHALL, for each `DetachmentRule`, look up its own text in the
ability-classification catalogue (per `ability-classification-catalogue`'s Catalogue Lookup By
Normalized-Text Content Hash — the same lookup `statline-flag-rules` already performs, never a live
classification call at enrichment time). When a record exists and its classified target is a set of
keywords, the system SHALL attach a synthesized `Ability` (Name and Text taken verbatim from the
`DetachmentRule`, Scope Unit, Origin Detachment Rule — per `roster-model`'s Inbound Detachment-Rule
Abilities) to the `InboundAbilities` of every `ICombatUnit` in the roster whose own effective keyword
set (`roster-model`'s Attached Unit Keyword Resolution) contains every one of that target's keywords,
compared ignoring case. A `DetachmentRule` whose text has no record, or whose record's target is not
a keyword target, SHALL attach no `Ability` to any unit. This step SHALL run identically regardless
of which import pipeline produced the roster.

#### Scenario: A keyword-matched Detachment rule attaches to every unit carrying that keyword
- **WHEN** a selected Detachment's own rule text has a catalogue record whose target is the keyword
  set {SWORD BRETHREN SQUAD}, and the roster contains a Sword Brethren Squad unit
- **THEN** that unit's `InboundAbilities` gains one `Ability` with that rule's own Name and Text,
  Origin Detachment Rule, Scope Unit

#### Scenario: A multi-keyword target requires every keyword
- **WHEN** a selected Detachment's own rule text has a catalogue record whose target is the keyword
  set {ADEPTUS ASTARTES, INFANTRY}, and the roster contains one unit carrying both keywords and
  another carrying only ADEPTUS ASTARTES
- **THEN** only the unit carrying both keywords gains the `Ability`

#### Scenario: A non-matching unit is unaffected
- **WHEN** a selected Detachment's own rule text is keyword-matched as above, and the roster also
  contains a unit whose effective keyword set does not include that keyword
- **THEN** that unit's `InboundAbilities` gains no `Ability` from this rule

#### Scenario: An attached unit gains a keyword-matched rule via any present component
- **WHEN** an `AttachedUnit`'s own effective keyword set (the union across its present components)
  includes the matched keyword because one attached component carries it, even though the Bodyguard
  does not
- **THEN** the whole `AttachedUnit`'s `InboundAbilities` gains the matched `Ability`, not just the
  contributing component's

#### Scenario: A Detachment rule with no baseline entry attaches nothing
- **WHEN** a selected Detachment's own rule text has no record in the ability-classification
  catalogue
- **THEN** no unit's `InboundAbilities` gains an `Ability` from that rule

#### Scenario: A Detachment rule classified with a non-keyword target attaches nothing
- **WHEN** a selected Detachment's own rule text has a catalogue record whose classified target is
  the bearer, the bearer's unit, or unconditional
- **THEN** no unit's `InboundAbilities` gains an `Ability` from that rule — it remains reference-only
  in the Army Header, exactly as before this requirement existed

#### Scenario: Both import pipelines produce identical InboundAbilities for equivalent input
- **WHEN** the same real army list is imported once via the GW-app text pipeline and once via a
  BattleScribe/NewRecruit JSON export
- **THEN** both resulting rosters attach the same keyword-matched Detachment-rule `Ability` entries
  to the same units
