## REMOVED Requirements

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

**Reason**: `WeaponProfile` no longer carries typed ability flags (see `datasheet-catalogue`'s
"Weapon Profile Verbatim Keyword Text" requirement) — a hand-maintained recognized vocabulary that
silently dropped every keyword outside it from `/LivePlay` rendering entirely (an unrecognized
token set no flag, and rendering read only the flags, never the verbatim text) is replaced by
tokenization alone, with the verbatim list resolved against `rules-glossary` at render time
instead.

**Migration**: Any code reading a `WeaponProfile` ability flag (`Torrent`, `Blast`, `Melta`,
`RapidFire`, `SustainedHits`, `LethalHits`, `DevastatingWounds`, `TwinLinked`, `IndirectFire`,
`Pistol`, `IgnoresCover`, `Assault`, `Anti`) must instead inspect `KeywordsText` directly, or
resolve a token against `RuleGlossary` to find its behavioral meaning.

## ADDED Requirements

### Requirement: Weapon Keyword Tokenization
The system SHALL parse a weapon profile's free-text `Keywords` characteristic into an ordered list
of verbatim tokens by splitting on `,` and trimming surrounding whitespace from each token,
dropping any empty token. A `Keywords` value of `"-"` (BSData's explicit "no keywords" marker), or
a blank/whitespace-only value, SHALL produce an empty token list rather than a parse failure. No
token recognition, mapping, or vocabulary check SHALL be performed at this stage — every token is
preserved exactly as written, in source order, regardless of whether its meaning is understood
elsewhere in the system.

#### Scenario: Multiple comma-separated tokens
- **WHEN** a weapon's `Keywords` characteristic is `"Anti-infantry 4+, Devastating Wounds"`
- **THEN** the produced `WeaponProfile`'s keyword token list is exactly
  `["Anti-infantry 4+", "Devastating Wounds"]`, in that order

#### Scenario: No keywords
- **WHEN** a weapon's `Keywords` characteristic is `"-"`
- **THEN** the produced `WeaponProfile`'s keyword token list is empty

#### Scenario: An unfamiliar token is preserved without error
- **WHEN** a weapon's `Keywords` characteristic includes a token this system has never seen
  before (e.g. a newly-introduced GW/BSData keyword such as a renamed `"Pistol"`)
- **THEN** resolution completes without error, and that token appears verbatim in the produced
  `WeaponProfile`'s keyword token list
