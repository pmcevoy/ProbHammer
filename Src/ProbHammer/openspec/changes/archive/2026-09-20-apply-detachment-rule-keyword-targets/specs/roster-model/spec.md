## ADDED Requirements

### Requirement: Inbound Detachment-Rule Abilities
Every `ICombatUnit` SHALL expose a mutable `InboundAbilities` list of `Ability`, defaulting to
empty, set once during roster enrichment (per `army-roster-enrichment`'s Detachment Rule Keyword
Target Resolution) and never recomputed at render time — the same "settable post-construction, read
everywhere" shape already used by `IsHalfStrengthOverride`/`IsBattleShocked`. An `Ability` placed in
`InboundAbilities` SHALL carry an Origin of Detachment Rule, a Scope of Unit, and a Name/Text
matching its source `DetachmentRule` verbatim. Detachment Rule is an `AbilityOrigin` value never
produced by Datasheet resolution (`datasheet-catalogue`'s own Ability Shape requirement describes
only the Origins a Datasheet itself can resolve) — it exists only as the result of the
roster-enrichment matching step this requirement describes.

#### Scenario: Default is empty
- **WHEN** an `ICombatUnit` is constructed
- **THEN** its `InboundAbilities` is empty until the roster-enrichment matching step runs

#### Scenario: A matched ability carries the source Detachment rule's own text verbatim
- **WHEN** a Detachment rule is matched to an `ICombatUnit`
- **THEN** the resulting `Ability` in that unit's `InboundAbilities` has the exact Name and Text of
  the source `DetachmentRule`, with Origin Detachment Rule and Scope Unit

#### Scenario: InboundAbilities is not recomputed per render
- **WHEN** an `ICombatUnit`'s `InboundAbilities` is read more than once across different requests
  (e.g. after a casualty adjustment)
- **THEN** it returns the same set every time — it is set once during roster enrichment, never
  re-derived from live game state
