## ADDED Requirements

### Requirement: Detachment Name Resolution
For each Detachment entry in a parsed army list (`army-list-parsing`'s Army Metadata Extraction —
a single captured text that MAY itself name more than one Detachment in natural-language list
form), the system SHALL resolve it against the faction's catalogue closure's own Detachment
choices — nested children of the closure's Detachment selection group, not its top-level named
entries — using a greedy, longest-name-first match: trying the closure's own known Detachment
names in descending order of length, the first one found anywhere in the remaining unclaimed
portion of the text claims that portion, so no shorter name already contained within an
already-claimed portion is ever matched separately. This repeats until no further known name
matches. The system SHALL attach each claimed Detachment's own resolved rule text (per
`catalogue-json-ingestion`'s Detachment Rule Text Extraction) to the `ArmyRoster`, in the order
each was claimed in the text, and SHALL fail with a diagnostic naming the unresolved remainder when
any non-separator text (i.e. anything other than whitespace, commas, or the word `"and"`) is left
unclaimed once no further known name matches.

Longest-name-first matching, rather than a syntactic split on `"and"`/commas, is what correctly
resolves two real, confirmed collision shapes found in the BSData corpus: a single Detachment can
itself be named with the word `"and"` in it (e.g. "Legends of Saga and Song" — a syntactic split
would misidentify it as two nonexistent Detachments), and one Detachment's own name can be a literal
substring of a different, separate Detachment's name in the same faction (e.g. "Warhost" is a
substring of "Armoured Warhost" — matching shorter names first would spuriously claim "Warhost" as
a second, unselected Detachment whenever only "Armoured Warhost" was actually chosen).

#### Scenario: A single Detachment name resolves directly
- **WHEN** a parsed army list's Detachments entry is a name matching one of the faction's own
  Detachment choices, with no other detachment name present in the text (e.g. "Virulent
  Vectorium")
- **THEN** the resulting ArmyRoster carries that one Detachment's own resolved rule text

#### Scenario: A Detachment name containing "and" resolves as one whole entry, not split
- **WHEN** a parsed army list's Detachments entry is a name that itself contains the word `"and"`
  and matches one of the faction's own Detachment choices as a whole (e.g. "Legends of Saga and
  Song")
- **THEN** the resulting ArmyRoster carries that one Detachment, resolved from the whole captured
  text — it is never split into "Legends of Saga" and "Song"

#### Scenario: A shorter Detachment name that is a substring of a longer one is not spuriously matched
- **WHEN** a parsed army list's Detachments entry names only the longer of two Detachments whose
  names overlap as substrings (e.g. "Armoured Warhost", where "Warhost" is also a separate, real
  Detachment name in the same faction)
- **THEN** the resulting ArmyRoster carries only "Armoured Warhost" — "Warhost" is not also
  produced as a spurious second entry

#### Scenario: A two-Detachment joined entry resolves both
- **WHEN** a parsed army list's Detachments entry names two Detachments joined in natural-language
  form (e.g. "Cabal of Chaos and Devotees of Destruction")
- **THEN** the resulting ArmyRoster carries two Detachments, "Cabal of Chaos" and "Devotees of
  Destruction", each with its own independently resolved rule text

#### Scenario: A three-or-more-Detachment joined entry resolves all of them
- **WHEN** a parsed army list's Detachments entry names three or more Detachments joined in
  Oxford-comma-plus-"and" form (e.g. "Fulguris Task Force, Marshal's Household, and Subversion
  Assets")
- **THEN** the resulting ArmyRoster carries three Detachments, each with its own independently
  resolved rule text, in the order listed

#### Scenario: A Detachment with no rule of its own resolves with an empty rule list
- **WHEN** a parsed army list's Detachments includes a name that resolves to a Detachment declaring
  no rule of its own
- **THEN** the resulting ArmyRoster carries that Detachment with an empty rule list, rather than
  failing resolution

#### Scenario: An unresolvable Detachment name fails with a diagnostic
- **WHEN** a parsed army list's Detachments entry contains text that does not match any of the
  faction's own known Detachment names, even after every other name in the text has been claimed
- **THEN** resolution fails with a diagnostic naming the unresolved remainder, including a "did you
  mean...?" suggestion when a close match exists
