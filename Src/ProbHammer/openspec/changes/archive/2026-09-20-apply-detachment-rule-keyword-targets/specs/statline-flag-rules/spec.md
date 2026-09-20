## MODIFIED Requirements

### Requirement: Target-Scoped Application
A matched baseline entry's own classified target scope SHALL determine which of a resolved unit's
Statline entries the flagged value applies to: a target scoped to the ability's own bearer SHALL
apply only to that bearer's own row(s) — a specific model-line when the matched ability is
model-line-sourced, the whole owning component when it is component-wide (the existing Bearer
scope); a target scoped to the bearer's whole attached unit SHALL apply to every row of the whole
resolved unit regardless of which component granted the matched ability (the existing WholeUnit
scope). A matched entry whose own classified target is scoped to a named keyword, or is
unconditionally roster-wide with no bearer/unit qualifier at all, SHALL NOT produce a flagged value
— the same outcome as an unmatched ability — since no roster-wide keyword-predicate evaluation
exists in this capability, with one exception: an ability whose Origin is Detachment Rule (per
`army-roster-enrichment`'s Detachment Rule Keyword Target Resolution) has already had its own
keyword-scoped target evaluated against the resolved roster before it was ever attached as a
present ability on this unit, so it SHALL be treated as WholeUnit-scoped for the purposes of this
requirement regardless of its own baseline entry's classified target.

#### Scenario: A keyword-scoped match produces no flagged value
- **WHEN** a resolved unit carries an ability whose normalized Text matches a baseline entry whose
  own classified target is scoped to a named keyword rather than to the bearer or its unit, and
  that ability's Origin is not Detachment Rule
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability, the same
  outcome as if no baseline entry had matched at all

#### Scenario: An unconditionally roster-wide match produces no flagged value
- **WHEN** a resolved unit carries an ability whose normalized Text matches a baseline entry whose
  own classified target names no bearer, unit, or keyword qualifier at all
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability

#### Scenario: A Detachment-Rule-origin ability applies as WholeUnit-scoped despite its own keyword-classified target
- **WHEN** a resolved unit carries a present ability whose Origin is Detachment Rule and whose
  matched baseline entry's own classified target is a named keyword
- **THEN** the flagged value applies to every row of the whole resolved unit, the same treatment as
  an ordinary WholeUnit-scoped match, not the "no flagged value" outcome that keyword-scoped target
  would otherwise produce
