## ADDED Requirements

### Requirement: Per-Model Keyword Lookup
A Datasheet SHALL expose, for each of its named statlines, an independently queryable set of
Keywords scoped to that specific named model — distinct from the Datasheet's own top-level
Keywords — defaulting to an empty set for a named statline with no such data of its own.

#### Scenario: A named model's own keyword is independently queryable
- **WHEN** a Datasheet defines several distinct named model types and only one of them (e.g.
  "Garlon Souleater") carries a specific keyword (e.g. Psyker) in its own source data
- **THEN** querying that one name's model-scoped Keywords returns a set containing Psyker, while
  querying any other name's model-scoped Keywords does not

#### Scenario: A named model with no distinguishing keywords has an empty set
- **WHEN** a named statline has no model-scoped keyword data of its own
- **THEN** its model-scoped Keywords set is empty, not null and not an error
