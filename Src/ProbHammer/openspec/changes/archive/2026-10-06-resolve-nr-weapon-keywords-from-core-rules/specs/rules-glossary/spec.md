## MODIFIED Requirements

### Requirement: Glossary Lookup By Normalized Name Or Alias
Given a closure, the system SHALL build one glossary queryable by a normalized-key match against
any resolved `RuleDefinition`'s `Name` or any of its `Aliases`. A candidate string SHALL be
normalized, before both indexing and lookup, via the following ordered, deterministic pipeline:

1. Lowercase the entire string.
2. Drop a colon and everything after it (a target qualifier such as `: non-MONSTER/VEHICLE`).
3. Strip a trailing value/threshold/dice/placeholder suffix: a bare `x` placeholder, a digit run,
   a digit run followed by `+`, or `d` followed by a digit run with an optional `+`-suffix (e.g.
   `d3`), each preceded by whitespace.
4. Collapse a string beginning with `anti` followed immediately by a space or hyphen to the bare
   string `anti`, discarding everything after that boundary (see the Anti scenarios below for why
   this is a named exception rather than falling out of steps 3/5).
5. Strip every remaining character that is not a lowercase letter or digit (spaces, hyphens,
   slashes, `+`, etc.).

The identical pipeline normalizes both sides of every comparison: a `RuleDefinition`'s own `Name`
and each of its `Aliases` are normalized this way when the glossary is built, and a queried string
is normalized the same way before lookup - there is no separate "raw" or case-sensitive path.

#### Scenario: Lookup by the rule's own Name
- **WHEN** querying the glossary for `"Lethal Hits"`
- **THEN** the `RuleDefinition` extracted from that `sharedRules` entry is returned

#### Scenario: Lookup by an Alias distinct from Name
- **WHEN** querying the glossary for `"LETHAL HITS"` (the alias, not the Name)
- **THEN** the same `RuleDefinition` is returned as querying by `"Lethal Hits"`

#### Scenario: A name with no matching rule returns no result
- **WHEN** querying the glossary for a string that matches neither any `RuleDefinition`'s `Name`
  nor any of its `Aliases`, once normalized
- **THEN** the lookup returns nothing, and this is not treated as an error

#### Scenario: A value-suffixed reference resolves against a bare Alias
- **WHEN** querying the glossary for `"SUSTAINED HITS 1"` and it contains a `RuleDefinition` with
  Alias `"SUSTAINED HITS"` and no exact `Name`/`Alias` of `"SUSTAINED HITS 1"` itself (confirmed
  real shape - Sustained Hits' own sharedRules entry documents `**[SUSTAINED HITS X]**` as its
  referenced form)
- **THEN** that `RuleDefinition` is returned - the trailing `" 1"` is removed by step 3 before
  comparison

#### Scenario: A colon-qualified reference resolves against its base rule
- **WHEN** querying the glossary for `"LETHAL HITS: non-MONSTER/VEHICLE"` and it contains a
  `RuleDefinition` with Alias `"LETHAL HITS"` (confirmed real shape - Ork weapons in
  `data/nr-orks.json`)
- **THEN** that `RuleDefinition` is returned - step 2 drops the qualifier before comparison

#### Scenario: A value-and-qualifier reference resolves against its base rule
- **WHEN** querying the glossary for `"SUSTAINED HITS 2: MONSTER/VEHICLE"` and it contains a
  `RuleDefinition` with Alias `"SUSTAINED HITS"`
- **THEN** that `RuleDefinition` is returned - step 2 drops the qualifier, then step 3 the value

#### Scenario: A target-category-and-value-suffixed Anti reference resolves via the named exception
- **WHEN** querying the glossary for `"ANTI-VEHICLE 3+"` and it contains a `RuleDefinition` named
  `"Anti"` with Alias `"ANTI"` (confirmed real shape - Anti's own sharedRules entry documents
  `**[ANTI-X Y+]**` as its referenced form, inserting a target category between the mechanic name
  and its value, unlike every other generic mechanic's single trailing value)
- **THEN** that `RuleDefinition` is returned - step 4 collapses the whole string to `"anti"`,
  since removing only a trailing value suffix would leave `"anti-vehicle"`, which would not match

#### Scenario: A negated Anti target also resolves via the named exception
- **WHEN** querying the glossary for `"ANTI: non‑MONSTER/VEHICLE 5+"` (confirmed real shape - a
  shared "Blood Boil" ability defined once and reached by every chapter that imports it, using a
  colon separator instead of the hyphen every other Anti reference uses)
- **THEN** the same `RuleDefinition` as the previous scenario is returned - step 2 drops the
  colon and everything after it, leaving `"anti"`

#### Scenario: An unrelated word merely starting with "anti" is not mistaken for a reference
- **WHEN** querying the glossary for `"Antimatter Drive"`
- **THEN** the lookup returns nothing - the named exception in step 4 requires a boundary
  character (space or hyphen) immediately after `anti`, which `"Antimatter"` does not have

#### Scenario: A casing or punctuation variant resolves
- **WHEN** querying the glossary for `"Twin-Linked"` (title case) and it contains a
  `RuleDefinition` named `"Twin-linked"` with Alias `"TWIN-LINKED"`
- **THEN** that `RuleDefinition` is returned - normalization lowercases and strips punctuation
  from both the query and every candidate `Name`/`Alias` before comparing, so this casing
  difference does not prevent a match

#### Scenario: A rule with no declared Alias still resolves via its own Name
- **WHEN** querying the glossary for `"CLEAVE 1"` and it contains a `RuleDefinition` named
  `"Cleave"` with no `Alias` entries at all (confirmed real BSData data gap - the "Cleave"
  sharedRules entry carries no `alias` array, even though its own description text
  self-references itself as `**[CLEAVE X]**`, the same convention every other generic mechanic
  uses)
- **THEN** that `RuleDefinition` is returned - the glossary indexes every `RuleDefinition` by its
  normalized `Name` in addition to its normalized `Aliases`, so a rule with no declared alias
  still resolves through the name it consistently references itself by
