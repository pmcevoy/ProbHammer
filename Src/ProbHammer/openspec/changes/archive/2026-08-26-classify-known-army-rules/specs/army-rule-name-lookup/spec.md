## Purpose

Maintains a small, curated, per-faction table of known army-wide rule names — each verified to
resolve as a real rule against the bundled BSData corpus — that the import pipelines use as the
sole source of truth for classifying a rule ability as Army Rule. Also maintains a small exclusion
list documenting confirmed non-army-rule names that share the same BSData gating shape as a genuine
army rule, consulted only by the corpus scan, never at runtime. Classification input only: never
used for wargear/detachment composition validation or any points-related check.

## ADDED Requirements

### Requirement: Curated Per-Faction Army Rule Names
The system SHALL maintain a table mapping a faction name to zero or more known army-wide rule
names. Each name entered into the table SHALL be verified, at the time it is added, to resolve as
a real rule against the bundled BSData corpus for that faction. A faction with no table entry
SHALL resolve to an empty name set rather than an error.

#### Scenario: A faction with a known army-wide rule resolves its name
- **WHEN** the table is queried for a faction that has a curated entry (e.g. Death Guard)
- **THEN** the query returns that faction's own known army-wide rule name(s)

#### Scenario: A faction with no curated entry resolves to an empty set
- **WHEN** the table is queried for a faction that has no curated entry
- **THEN** the query returns an empty name set, not a failure

### Requirement: Faction-Scoped Resolution
Given a roster's ordered Faction list, the system SHALL resolve the table entry using the same
"most specific (last) Faction entry" convention `army-roster-enrichment`'s Faction Catalogue File
Resolution already uses, so a sub-faction (e.g. Black Templars under Space Marines) resolves
against its own most specific entry, never its parent codex's.

#### Scenario: A sub-faction resolves against its own entry, not its parent codex's
- **WHEN** the ordered Faction list is `["Space Marines", "Black Templars"]`
- **THEN** resolution uses `"Black Templars"` as the lookup key, not `"Space Marines"`

#### Scenario: An unsplit faction resolves directly
- **WHEN** the ordered Faction list is `["Death Guard"]`
- **THEN** resolution uses `"Death Guard"` as the lookup key

### Requirement: Curated Non-Army-Rule Exclusion List
The system SHALL maintain a list of rule names confirmed, at the time each is added, to carry
`primary-catalogue`-scoped gating in the bundled BSData corpus but to not represent a genuine
army-wide gameplay rule (e.g. a mustering/composition rule). This list SHALL be consulted only by
the corpus scan's own triage check (per `bsdata-corpus-scan`'s Full-Corpus Primary-Catalogue-Gated
Rule Triage Scan) and SHALL play no role in runtime Origin classification — a name absent from the
inclusion table already classifies Core Rule by default, regardless of whether it also appears on
this list.

#### Scenario: An excluded name plays no runtime role
- **WHEN** a `CoreRule` reference's target rule Name matches an entry on the exclusion list but does
  not match any entry in the inclusion table
- **THEN** runtime classification is unaffected by the exclusion list — the reference classifies
  Core Rule for the same reason any other non-matching name would (per
  `catalogue-json-ingestion`'s Core Versus Army Rule Origin Classification), not because of this
  list

#### Scenario: The exclusion list explains a triaged scan candidate
- **WHEN** the corpus scan discovers a `primary-catalogue`-gated rule definition whose Name matches
  an entry on this list
- **THEN** the scan treats that candidate as accounted for, rather than reporting it as an
  untriaged gap

### Requirement: Full-Faction Coverage
The system SHALL expose every table entry, including a faction whose known army-wide rule name set
is deliberately empty, in a way that is distinguishable from a faction with no entry at all — so a
faction confirmed to have no single unifying army-wide rule and a faction nobody has yet researched
are never conflated.

#### Scenario: A deliberately empty entry is distinguishable from an absent one
- **WHEN** the table is queried, via its entry-enumeration surface, for a faction confirmed to have
  no single unifying army-wide rule
- **THEN** that faction appears as an entry with an empty name set, distinct from a faction that has
  no entry in the table at all

#### Scenario: Resolving a faction does not distinguish the two cases
- **WHEN** the table is queried via ordinary resolution (per Curated Per-Faction Army Rule Names,
  above) for a faction with a deliberately empty entry, or for a faction with no entry at all
- **THEN** both return an empty name set — the entry-enumeration surface, not ordinary resolution,
  is what distinguishes them
