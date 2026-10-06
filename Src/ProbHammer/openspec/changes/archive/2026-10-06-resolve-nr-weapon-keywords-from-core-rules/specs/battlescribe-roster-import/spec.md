## ADDED Requirements

### Requirement: Rule Glossary Falls Back To Core Rules
A BattleScribe import's rule glossary SHALL resolve a name from the roster's own rule entries
first and, for a name the roster doesn't define, from the shared rules of the bundled game-system
file whose id matches the roster's `gameSystemId`. When no bundled game-system file matches, the
glossary SHALL hold the roster's own rule entries only, and the import SHALL still succeed.

#### Scenario: A weapon keyword the roster doesn't define resolves from core rules
- **WHEN** a roster JSON whose own rule entries include no "Lethal Hits" entry (e.g.
  `data/nr-orks.json`) is imported, and a weapon carries the keyword `LETHAL HITS`
- **THEN** that keyword's chip resolves to the game system's "Lethal Hits" rule text

#### Scenario: The roster's own rule text wins over core rules
- **WHEN** a roster JSON's own rule entries define a rule whose name the game system's shared
  rules also define, with different text
- **THEN** the glossary resolves that name to the roster's own text

#### Scenario: An unmatched game system leaves the roster-only glossary
- **WHEN** a roster JSON's `gameSystemId` matches no bundled game-system file
- **THEN** the import succeeds and the glossary resolves only the roster's own rule entries
