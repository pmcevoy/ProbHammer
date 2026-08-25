## ADDED Requirements

### Requirement: Detachment Rule Text Extraction
The system SHALL extract zero or more `(Name, Text)` rule pairs from a resolved Detachment entry,
covering both real shapes found in BSData catalogue data: a rule declared locally on the
Detachment entry itself, and a rule reached via a `"type": "rule"` infoLink resolved against the
closure's rule-text glossary (`rules-glossary`) — the same glossary lookup mechanism the Core Rule
Ability Extraction requirement already uses. A Detachment entry MAY declare more than one rule of
either shape; every one it declares SHALL be extracted, in declaration order. When a `"type":
"rule"` infoLink's name does not resolve against the closure's glossary, that reference SHALL be
skipped — no rule pair is produced for it — without failing extraction of the Detachment's other
rules.

#### Scenario: A locally-declared rule extracts directly
- **WHEN** a resolved Detachment entry declares a rule locally, with its own Name and Text (e.g.
  Black Templars' "Marshal's Household", declaring "Faith-Fuelled Resolve")
- **THEN** the extraction produces one `(Name, Text)` pair matching that locally-declared rule
  exactly

#### Scenario: An infoLink-referenced rule resolves via the glossary
- **WHEN** a resolved Detachment entry declares no local rules but carries a `"type": "rule"`
  infoLink referencing a separately-defined rule (e.g. Necrons' "Awakened Dynasty", referencing
  "Command Protocols")
- **THEN** the extraction produces one `(Name, Text)` pair whose Text is that referenced rule's
  resolved text

#### Scenario: A Detachment with more than one rule extracts all of them
- **WHEN** a resolved Detachment entry declares more than one rule (e.g. Space Marines' "Black
  Spear Task Force", declaring both "Kill Teams" and "Mission Tactics")
- **THEN** the extraction produces one pair per declared rule, in declaration order

#### Scenario: An unresolvable infoLink reference is skipped, not failed
- **WHEN** a resolved Detachment entry's `"type": "rule"` infoLink does not resolve against the
  closure's glossary
- **THEN** no rule pair is produced for that reference, and extraction of the Detachment's other
  rules (if any) completes without error

#### Scenario: A Detachment with no rule of its own produces no pairs
- **WHEN** a resolved Detachment entry declares no local rules and carries no `"type": "rule"`
  infoLink
- **THEN** the extraction produces zero rule pairs, rather than failing
