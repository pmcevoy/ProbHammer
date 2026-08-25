# catalogue-json-ingestion

## Purpose

Resolves BSData 11th Edition JSON catalogue files into the existing `Domain.Catalogue` shape
(`Datasheet`, `Statline`, `WeaponProfile`, `Ability`), queryable by name, across a faction's full
transitive set of imported catalogue files.

## Requirements

### Requirement: Faction Catalogue Closure Resolution
Given a starting catalogue file, the system SHALL resolve the full transitive set of catalogue
files reachable by following import links that import all of a linked catalogue's root entries,
recursively — an imported catalogue that itself imports further catalogues SHALL have those
further catalogues included in the closure.

#### Scenario: Single-level import
- **WHEN** resolving the closure starting from a sub-faction file that imports one other catalogue
- **THEN** the closure includes the starting file and that one imported catalogue

#### Scenario: Multi-level transitive import
- **WHEN** resolving the closure starting from `Imperium - Black Templars`, which imports
  `Imperium - Space Marines`, which itself imports `Imperium - Agents of the Imperium`,
  `Library - Titans`, and `Imperium - Imperial Knights - Library`
- **THEN** the closure includes all five files, not just the directly-imported one

#### Scenario: A faction with no sub-faction split still has its own import chain
- **WHEN** resolving the closure starting from `Chaos - Chaos Space Marines` (a faction with a
  single, unsplit faction declaration)
- **THEN** the closure still includes that file's own imports (`Chaos - Chaos Knights Library`,
  `Chaos - Daemons Library`, `Library - Astartes Heresy Legends`), not just the starting file

### Requirement: Local-Over-Imported Name Precedence
When resolving a name against a closure, an entry defined in the file nearer the closure's
starting point SHALL take precedence over a same-named entry only reachable through an import
further out.

#### Scenario: Locally-defined entry is used even though the closure also carries an import
- **WHEN** resolving `"Crusader Squad"` against the Black Templars closure, where `Crusader Squad`
  is fully defined within the starting file itself
- **THEN** the starting file's own definition is returned, not a lookup into any imported file

#### Scenario: Name absent locally falls through to an imported catalogue
- **WHEN** resolving `"Assault Intercessor Squad"` or `"Scout Squad"` against the Black Templars
  closure, where neither name is defined anywhere in the starting file
- **THEN** the definition is resolved from the imported `Imperium - Space Marines` catalogue

### Requirement: Statline Extraction
The system SHALL extract one or more named `Statline`s per resolved unit entry: a single-model
unit's statline SHALL be read from characteristics on the entry itself; a squad unit's statlines
SHALL be read from characteristics on each of its distinct child model entries, preserving their
declared order, so the result satisfies `datasheet-catalogue`'s existing "Multiple Named
Statlines" requirement. Each statline's invulnerable save SHALL be resolved per "Invulnerable
Save Text Resolution" rather than read as a single value.

#### Scenario: Single-model unit
- **WHEN** extracting a statline for a single-model entry (e.g. an Epic Hero) whose `Unit`-typeName
  profile carries `M`, `T`, `Sv`, `W`, `LD`, `OC`, and `InSv` characteristics directly
- **THEN** one named `Statline` is produced, with its invulnerable save resolved from the `InSv`
  characteristic's raw text per "Invulnerable Save Text Resolution"

#### Scenario: Squad unit with multiple distinct model types
- **WHEN** extracting statlines for a squad entry whose distinct model types (e.g. Sword Brother,
  Initiate, Neophyte) each carry their own `Unit`-typeName profile nested within the squad entry
- **THEN** one named `Statline` per distinct model type is produced, in the order those model
  types are declared in the source file

### Requirement: Invulnerable Save Text Resolution
The system SHALL resolve a `Unit`-typeName profile's raw invulnerable-save characteristic text into
an `invulnerable-save` value as follows.

Text stating its attack-type restriction directly — a parenthetical annotation naming the
restricted attack type, or the unfootnoted side of a two-value split — SHALL be resolved without
consulting any ability, setting the value for the other attack type to 0.

Text carrying a footnote marker — a bare trailing marker on a single value, or a marker on one side
of a two-value split — SHALL always resolve as caveated: the system SHALL NOT interpret the
marker's meaning from the characteristic text or from any linked ability's own text. It SHALL
instead resolve, by id, the specific `Ability` reachable from the same source entry as the
invulnerable-save characteristic (whether defined locally within that entry or reachable through a
link) — never by matching the ability's name alone, since two abilities in the source data can
share an identical name with different meanings — and populate both the melee and ranged values
with the one value known without needing that interpretation (the unfootnoted side of a split when
one exists, otherwise the footnoted value itself).

Resolution SHALL fail, identifying the characteristic and its raw text, when the raw text matches
none of these recognized shapes, or when a footnote marker is present but no candidate ability is
reachable from the same source entry.

#### Scenario: Plain value
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic is a plain value with no
  marker (e.g. `"4+"`)
- **THEN** the resolved value is uniform and non-caveated

#### Scenario: Parenthetical attack-type restriction
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic states its restriction
  directly (e.g. `"5+ (Ranged)"`)
- **THEN** the resolved value's ranged value is 5, its melee value is 0, and it is non-caveated,
  without consulting any ability

#### Scenario: Bare footnoted value
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic is a single value with a
  trailing footnote marker (e.g. `"5+*"`)
- **THEN** the resolved value is caveated, both its melee and ranged values equal the parsed digit,
  and it carries a reference to the specific `Ability` reachable from the same entry, resolved by
  id

#### Scenario: Two-value split with one footnoted side
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic is a two-value split where
  one side carries a footnote marker (e.g. `"4+* / 5+"`)
- **THEN** the resolved value is caveated, both its melee and ranged values equal the unfootnoted
  side's parsed digit, and it carries a reference to the specific `Ability` reachable from the same
  entry, resolved by id

#### Scenario: Unrecognized raw text shape fails resolution
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic text matches none of the
  recognized shapes
- **THEN** resolution fails, identifying the characteristic and the offending text

#### Scenario: Footnoted value with no reachable candidate ability fails resolution
- **WHEN** a `Unit`-typeName profile's invulnerable-save characteristic carries a footnote marker
  but no ability matching the expected naming convention is reachable from the same source entry
- **THEN** resolution fails, identifying the characteristic and the offending text

### Requirement: Weapon Profile Extraction
The system SHALL extract a `RangedWeapon` or `MeleeWeapon` from each `Ranged Weapons`- or `Melee
Weapons`-typeName profile encountered while resolving a unit, reading Range, Attacks, Skill,
Strength, AP, and Damage from that profile's characteristics.

#### Scenario: Ranged weapon profile
- **WHEN** a resolved unit carries a `Ranged Weapons`-typeName profile with `Range`, `A`, `BS`,
  `S`, `AP`, and `D` characteristics
- **THEN** a `RangedWeapon` is produced with those values, with AP stored as the negative integer
  the source data already uses

#### Scenario: Melee weapon profile
- **WHEN** a resolved unit carries a `Melee Weapons`-typeName profile with `A`, `WS`, `S`, `AP`,
  and `D` characteristics (and a `Range` characteristic whose value is the literal text `"Melee"`)
- **THEN** a `MeleeWeapon` is produced with those values

### Requirement: Weapon Keyword Flag Parsing
The system SHALL parse a weapon profile's free-text `Keywords` characteristic into
`WeaponProfile`'s existing ability flags, for tokens whose mapping to an existing flag is exact
and unambiguous. A `Keywords` value of `"-"` SHALL produce no flags set. A token that does not
exactly match an existing `WeaponProfile` flag — whether because it names a mechanic with no
corresponding flag, or because it is a context-dependent alternate spelling of a mechanic an
existing flag already models — SHALL NOT be mapped to that flag by inference, and SHALL NOT cause
resolution to fail; see the Weapon Profile Verbatim Keyword Text requirement in
`datasheet-catalogue` for how such a token is preserved instead.

#### Scenario: Multiple comma-separated recognized tokens
- **WHEN** a weapon's `Keywords` characteristic is `"Anti-infantry 4+, Devastating Wounds"`
- **THEN** the produced `WeaponProfile` has `DevastatingWounds` set and its `Anti` dictionary
  contains an `"infantry"` entry with threshold `4`

#### Scenario: Value-carrying token
- **WHEN** a weapon's `Keywords` characteristic is `"Sustained Hits 1"`
- **THEN** the produced `WeaponProfile` has `SustainedHits` set to `1`

#### Scenario: No keywords
- **WHEN** a weapon's `Keywords` characteristic is `"-"`
- **THEN** the produced `WeaponProfile` has every ability flag at its default (unset) value

#### Scenario: Token with no corresponding WeaponProfile flag
- **WHEN** a weapon's `Keywords` characteristic includes a token such as `"Hazardous"`,
  `"Precision"`, or `"Heavy"`, none of which correspond to an existing `WeaponProfile` flag
- **THEN** resolution completes without error and no existing flag is set on that token's behalf

#### Scenario: Alternate spelling of an already-modeled flag is not inferred
- **WHEN** a weapon's `Keywords` characteristic includes `"Cleave"` (melee's equivalent of
  `Blast`) or `"Close Combat"` (a generalization of `Pistol`)
- **THEN** resolution completes without error, and neither `Blast` nor `Pistol` is set on that
  token's behalf — recognizing the alias is out of scope for this change

### Requirement: Ability Text Extraction
The system SHALL extract an `Ability` from each `Abilities`-typeName profile encountered while
resolving a unit, carrying the profile's `Description` characteristic through as `Ability.Text`
verbatim, without parsing it for behavioral effect, consistent with `datasheet-catalogue`'s
existing stance that ability text is never auto-parsed into behavior. An `Abilities`-typeName
profile found directly on the entry that owns the resolved unit's `Statline` SHALL populate the
Datasheet's intrinsic (always-exposed) ability list; an `Abilities`-typeName profile found nested
inside a `"type": "upgrade"` selection entry anywhere else in the walk SHALL instead populate the
Datasheet's on-demand ability index only (per `datasheet-catalogue`'s On-Demand Ability
Resolution), classified as an Enhancement when that selection entry's own containing selection
group is named "Enhancements", and as a plain optional grant otherwise.

#### Scenario: Ability with descriptive text only
- **WHEN** a resolved unit carries an `Abilities`-typeName profile with a `Description`
  characteristic
- **THEN** an `Ability` is produced whose `Text` equals that characteristic's value unchanged

#### Scenario: An entry-direct ability is extracted as intrinsic
- **WHEN** an `Abilities`-typeName profile sits directly on the entry that owns the resolved
  unit's `Statline` profile
- **THEN** the extracted `Ability` populates the Datasheet's intrinsic ability list

#### Scenario: An ability nested inside an Enhancements selection is extracted as an Enhancement
- **WHEN** an `Abilities`-typeName profile sits on a `"type": "upgrade"` selection entry that is
  itself a member of a selection group named "Enhancements"
- **THEN** the extracted `Ability` populates the Datasheet's on-demand index only, classified as
  an Enhancement, and does NOT appear in the intrinsic ability list

#### Scenario: An ability nested inside a non-Enhancement optional selection is extracted as a plain grant
- **WHEN** an `Abilities`-typeName profile sits on a `"type": "upgrade"` selection entry that is
  not a member of any selection group named "Enhancements" (e.g. Impulsor's "Shield Dome")
- **THEN** the extracted `Ability` populates the Datasheet's on-demand index only, classified as a
  plain optional grant, and does NOT appear in the intrinsic ability list

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

### Requirement: Core Rule Chapter and Game-Mode Gating
The system SHALL exclude a Core Rule Ability Extraction reference from producing an `Ability` when
the referenced rule's own `hidden` condition provably evaluates true for the current closure,
evaluated with the same condition-tree logic the system already applies to entry/group-level
game-mode gating, extended to also recognize a condition scoped to the resolved army's own primary
catalogue (chapter/sub-faction) selection. A condition scoped to the primary catalogue SHALL be
evaluated using its own comparison type against the closure's own starting catalogue identity — a
"not an instance of" condition is provably true when the referenced catalogue differs from the
closure's own starting catalogue, and provably true is provably false when they match; an
"is an instance of" condition is the inverse. A condition whose scope or comparison type is not
recognized SHALL be treated as unknowable, consistent with the system's existing conservative
default, never provably excluding content on an unrecognized shape.

#### Scenario: A chapter-exclusive rule is excluded for a non-matching chapter
- **WHEN** resolving a unit against a closure whose own starting catalogue is Black Templars, and
  a `CoreRule` reference's target rule is hidden unless the primary catalogue is an instance of one
  of eleven other named chapters, none of them Black Templars
- **THEN** no `Ability` is produced for that reference

#### Scenario: A chapter-exclusive rule is included for its own matching chapter
- **WHEN** resolving a unit against a closure whose own starting catalogue is Black Templars, and a
  `CoreRule` reference's target rule is hidden unless the primary catalogue is an instance of Black
  Templars specifically
- **THEN** an `Ability` is produced for that reference

#### Scenario: The same reference produces the opposite inclusion for a different chapter
- **WHEN** resolving the identical unit and rule reference against a closure whose own starting
  catalogue is a different chapter than the rule's own required chapter
- **THEN** the inclusion outcome for that reference is the opposite of what it was for the matching
  chapter

#### Scenario: A game-mode-gated rule reference is excluded outside its applicable mode
- **WHEN** a `CoreRule` reference's target rule is hidden when a specific game-mode force (e.g. a
  mission type) is present, evaluated the same way entry/group-level game-mode gating already is
- **THEN** no `Ability` is produced for that reference under that game mode

#### Scenario: An unrecognized gating shape does not exclude the reference
- **WHEN** a `CoreRule` reference's target rule carries a `hidden` condition whose scope or
  comparison type is not one of the recognized shapes
- **THEN** an `Ability` is still produced for that reference — an unrecognized shape never provably
  excludes content

### Requirement: Core Versus Army Rule Origin Classification
The system SHALL classify a Core Rule Ability Extraction reference's Origin as Army Rule when the
referenced rule's own gating carries a condition scoped to the resolved army's own primary
catalogue (chapter/sub-faction) selection anywhere in its condition tree, regardless of comparison
type and regardless of whether that condition currently excludes the reference — and as Core Rule
otherwise. This is a structural presence check on the referenced rule itself, independent of the
Core Rule Chapter and Game-Mode Gating requirement's own exclusion evaluation, and independent of
how many datasheets in a given roster happen to reference the same rule.

#### Scenario: A chapter-exclusive rule's Origin is Army Rule even when included
- **WHEN** a `CoreRule` reference's target rule carries a primary-catalogue-scoped condition, and
  the reference is not excluded for the resolved army's own chapter
- **THEN** the produced `Ability`'s Origin is Army Rule

#### Scenario: A rule with no chapter exclusivity at all has Core Rule Origin
- **WHEN** a `CoreRule` reference's target rule carries no primary-catalogue-scoped condition
  anywhere in its gating
- **THEN** the produced `Ability`'s Origin is Core Rule

### Requirement: No Composition, Validation, or Points Data
The system SHALL NOT interpret BSData's composition/validation apparatus (`constraints`,
`modifiers`, `conditionGroups`, or `associations`/eligibility links) and SHALL NOT extract points
costs, consistent with `datasheet-catalogue`'s existing "No Wargear Constraint or Points Modeling"
requirement — this system resolves names to rules data; it does not validate that a chosen roster
is legal.

#### Scenario: Composition apparatus is ignored
- **WHEN** resolving a unit entry that carries `constraints`, `modifiers`, or an `associations`
  block describing which units it may lead
- **THEN** none of that data appears on the produced `Datasheet`, `Statline`, `WeaponProfile`, or
  `Ability` objects

#### Scenario: Points cost is ignored
- **WHEN** resolving a unit entry whose `costs` array includes an entry named `"pts"`
- **THEN** the produced `Datasheet` exposes no points value

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
