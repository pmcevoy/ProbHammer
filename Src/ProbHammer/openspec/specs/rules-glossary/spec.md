# rules-glossary Specification

## Purpose

Resolves BSData's rule-text sources (`sharedRules` at the game-system level, `rules` at the
faction/library level) into a name/alias-queryable glossary over a catalogue closure, and
mechanically extracts `[BRACKET]`-style cross-reference tokens from a piece of ability/rule text
for lookup against that glossary — text lookup and display only, never behavioral interpretation.

## Requirements

### Requirement: Universal Rule Text Extraction
The system SHALL extract a `RuleDefinition` (Name, Aliases, Text) from every entry in the closure's
game-system file's top-level `sharedRules` array, carrying that entry's `description` through as
`Text` verbatim and its `alias` values through as `Aliases`, without parsing either for behavioral
effect.

#### Scenario: A universal special rule resolves with its alias
- **WHEN** the closure's game-system file's `sharedRules` array contains an entry named
  `"Lethal Hits"` with `alias: ["LETHAL HITS"]`
- **THEN** a `RuleDefinition` is produced with `Name` "Lethal Hits", `Aliases` containing
  `"LETHAL HITS"`, and `Text` equal to that entry's `description` unchanged

### Requirement: Faction and Library Rule Text Extraction
The system SHALL extract a `RuleDefinition` from every entry in the top-level `rules` array of
every catalogue file in the closure (faction and library files, not only the closure's starting
file), reached the same way any other closure-wide name is reached — no traversal beyond the
existing import-closure walk `catalogue-json-ingestion` already performs.

#### Scenario: A detachment rule defined on an imported library file resolves
- **WHEN** the closure for a Black Templars import includes `Library - Astartes Heresy Legends`
  (reached transitively through Black Templars' import of Space Marines), and that file's `rules`
  array contains an entry named `"Templar Vows"`
- **THEN** a `RuleDefinition` named `"Templar Vows"` is produced from that entry, available for
  lookup against the Black Templars closure

### Requirement: Glossary Lookup By Normalized Name Or Alias
Given a closure, the system SHALL build one glossary queryable by a normalized-key match against
any resolved `RuleDefinition`'s `Name` or any of its `Aliases`. A candidate string SHALL be
normalized, before both indexing and lookup, via the following ordered, deterministic pipeline:

1. Lowercase the entire string.
2. Strip a trailing value/threshold/dice/placeholder suffix: a bare `x` placeholder, a digit run,
   a digit run followed by `+`, or `d` followed by a digit run with an optional `+`-suffix (e.g.
   `d3`), each preceded by whitespace.
3. Collapse a string beginning with `anti` followed immediately by a space, hyphen, or colon to
   the bare string `anti`, discarding everything after that boundary (see the Anti scenarios
   below for why this is a named exception rather than falling out of steps 2/4).
4. Strip every remaining character that is not a lowercase letter or digit (spaces, hyphens,
   colons, slashes, `+`, etc.).

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
- **THEN** that `RuleDefinition` is returned - the trailing `" 1"` is removed by step 2 before
  comparison

#### Scenario: A target-category-and-value-suffixed Anti reference resolves via the named exception
- **WHEN** querying the glossary for `"ANTI-VEHICLE 3+"` and it contains a `RuleDefinition` named
  `"Anti"` with Alias `"ANTI"` (confirmed real shape - Anti's own sharedRules entry documents
  `**[ANTI-X Y+]**` as its referenced form, inserting a target category between the mechanic name
  and its value, unlike every other generic mechanic's single trailing value)
- **THEN** that `RuleDefinition` is returned - step 3 collapses the whole string to `"anti"`,
  since removing only a trailing value suffix would leave `"anti-vehicle"`, which would not match

#### Scenario: A negated Anti target also resolves via the named exception
- **WHEN** querying the glossary for `"ANTI: non‑MONSTER/VEHICLE 5+"` (confirmed real shape - a
  shared "Blood Boil" ability defined once and reached by every chapter that imports it, using a
  colon separator instead of the hyphen every other Anti reference uses)
- **THEN** the same `RuleDefinition` as the previous scenario is returned - the string still
  begins with `anti` followed immediately by a colon, so the named exception applies regardless
  of the colon-vs-hyphen separator or the target category's own internal structure

#### Scenario: An unrelated word merely starting with "anti" is not mistaken for a reference
- **WHEN** querying the glossary for `"Antimatter Drive"`
- **THEN** the lookup returns nothing - the named exception in step 3 requires a boundary
  character (space, hyphen, or colon) immediately after `anti`, which `"Antimatter"` does not have

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

### Requirement: Bracket Cross-Reference Token Extraction
The system SHALL extract every `[...]`-delimited substring from a piece of ability/rule text as a
candidate cross-reference token, independent of any other markup (`**`/`^^`) surrounding it. Before
the token is used for resolution, the system SHALL normalize its captured text by removing any
`*`/`^` characters it contains and by replacing known Unicode whitespace/hyphen variants (a
no-break space U+00A0, a non-breaking hyphen U+2011, an en dash U+2013) with their plain-ASCII
equivalent (a regular space or hyphen, respectively). This normalization corrects confirmed
real-corpus authoring variance without discarding any other content of the token, and is
independent of glossary content — it never depends on what rules a specific glossary happens to
contain.

#### Scenario: A bracketed token embedded in running text
- **WHEN** extracting tokens from text containing `"...has the **[PRECISION]** ability..."`
  (confirmed real shape — Black Templars' "Templar Vows" rule text referencing another rule)
- **THEN** `"PRECISION"` is extracted as one candidate token, independent of the surrounding
  `**...**` emphasis markup

#### Scenario: Markup characters inside the bracket itself are stripped
- **WHEN** extracting a token from text containing `[^^Lethal Hits^^]` (confirmed real shape — a
  rule referenced by its display Name, wrapped in small-caps markup, inside the brackets
  themselves — distinct from markup wrapping a bracket from the outside, e.g. `**[PRECISION]**`)
- **THEN** the extracted token is `"Lethal Hits"`, with the `^^` characters removed

#### Scenario: A non-breaking space normalizes to a plain space
- **WHEN** extracting a token from text containing a U+00A0 no-break space where a plain ASCII
  space was intended (confirmed real shape — Astra Militarum Library's Elysian Sniper Squad
  [Legends] ability text uses `DEVASTATING<NBSP>WOUNDS` inside its brackets)
- **THEN** the extracted token has the no-break space replaced with a plain space

### Requirement: Non-Bracket Markup Is Not A Reference Signal
The system SHALL NOT treat `*italic*`, `**bold**`, or `^^small-caps^^` wrapping as indicating a
cross-reference, regardless of what text they wrap, how they nest, or how deeply a bracket token
sits inside them — only a literal `[BRACKET]` token is a candidate reference.

#### Scenario: A unit name wrapped in the same markup as a keyword is not extracted as a token
- **WHEN** extracting tokens from text containing `^^**Fabius Bile**^^` and `^^**Warlord**^^`
  (confirmed real shape — Chaos Space Marines' Fabius Bile abilities wrap a unit name and a
  generic game term identically to how a genuine keyword is wrapped elsewhere in the same corpus)
- **THEN** neither produces a candidate token, since neither is `[BRACKET]`-delimited

#### Scenario: Inconsistently nested markup does not affect extraction
- **WHEN** extracting tokens from text containing `^^Fabius Bile^^**` (confirmed real shape — the
  same name wrapped with mismatched `**`/`^^` nesting order elsewhere in the same corpus)
- **THEN** extraction is unaffected, since this requirement only ever looks for `[BRACKET]`
  delimiters, never `*`/`**`/`^^`

#### Scenario: A bracket token stays extractable inside deeply nested emphasis
- **WHEN** extracting tokens from text containing `"...cannot trigger other abilities such as
  [DEVASTATING WOUNDS]."` where that whole sentence sits inside an open italic span carried over
  from an earlier `***Designer's Note:**` (confirmed real shape — "Lethal Hits"' own description,
  where `***` opens bold+italic together, `**` two words later closes only the bold, and the
  remainder of the paragraph — including the bracketed reference — stays italic until one closing
  `*` at the very end)
- **THEN** `"DEVASTATING WOUNDS"` is still extracted as a candidate token, since extraction never
  depends on being outside any `*`/`**`/`^^` span

### Requirement: Bracket Token Resolution Reuses Glossary Lookup Directly
The system SHALL resolve an extracted, normalized bracket token (per "Bracket Cross-Reference
Token Extraction") using exactly the same normalized-key lookup as "Glossary Lookup By Normalized
Name Or Alias" — no bracket-specific matching logic of its own, and no fuzzy, partial, or
substring matching beyond what that normalization pipeline already performs. A token with no
match SHALL resolve to no reference, and SHALL NOT cause resolution of the surrounding text to
fail.

#### Scenario: A recognized token resolves
- **WHEN** the token `"PRECISION"` is resolved against a glossary containing a `RuleDefinition`
  with Alias `"PRECISION"`
- **THEN** that `RuleDefinition` is returned as the token's resolved reference

#### Scenario: An unrecognized token resolves to nothing without error
- **WHEN** a bracket token is extracted whose normalized form matches no `RuleDefinition`'s
  normalized `Name` or `Alias` in the glossary
- **THEN** it resolves to no reference, and resolution of the rest of the text continues normally

### Requirement: No Behavioral Interpretation
The system SHALL treat every `RuleDefinition.Text` and every resolved cross-reference as opaque
display text; it SHALL NOT derive game behavior, modifiers, or effects from it, consistent with
`datasheet-catalogue`'s existing stance that ability text is never auto-parsed into behavior.

#### Scenario: Glossary resolution has no effect on game state
- **WHEN** a `RuleDefinition`'s text is resolved and its cross-references extracted
- **THEN** no `WeaponProfile`, `Statline`, or other rules-bearing domain object is modified as a
  result — this system only produces text and references for display
