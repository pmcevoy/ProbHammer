## MODIFIED Requirements

### Requirement: Statline Ability Column Rendering
Each unit block's Statline area SHALL render two additional columns alongside the statline column:
a Model Abilities column for entries whose Ability has Scope Model, and a Unit Abilities column for
entries whose Ability has Scope Unit. Within each column, an ability entry bound to a specific
statline row SHALL render beside that row only; an ability entry with no statline-row binding (a
component-wide ability) SHALL render beside the first rendered statline row belonging to its
component, and SHALL visually span every row belonging to that component. Each ability SHALL
render by Name only; ability descriptive Text is not rendered inline by this requirement, but is
available on demand via the "Ability And Rule Text Popover" requirement below.

A row-bound ability cell SHALL collapse (hide its ability names) whenever its own row's run is
collapsed, per "Statline Section Rendering" — i.e. whenever every entry in that run is either
fully deselected or fully dead. A component-wide, row-spanning ability cell SHALL collapse whenever
every run within its visual span is collapsed by that same rule. It SHALL remain fully expanded as
long as at least one entry in at least one row within its span is neither fully deselected nor
fully dead. This applies identically to the Model Abilities and Unit Abilities columns.

#### Scenario: A row-bound ability renders beside its own statline row
- **WHEN** an ability entry carries the name of a specific statline
- **THEN** it renders in its Scope's column beside that specific statline row, not beside any
  other row of the same component

#### Scenario: A component-wide ability spans every row of its component
- **WHEN** an ability entry carries no statline name (a Datasheet-sourced, component-wide ability)
  and its owning component renders three statline rows (e.g. two merged into one shared tile and
  one separate)
- **THEN** it renders once, beside the first of those three rows, visually spanning all three

#### Scenario: Column placement is determined by Ability Scope
- **WHEN** a component contributes both a Model-scoped and a Unit-scoped ability
- **THEN** the Model-scoped ability renders in the Model Abilities column and the Unit-scoped
  ability renders in the Unit Abilities column, regardless of whether either is row-bound or
  component-wide

#### Scenario: Only the ability name renders
- **WHEN** an ability entry renders in either column
- **THEN** only its Name is shown inline; its descriptive Text is not rendered inline (see
  "Ability And Rule Text Popover" for on-demand access to it)

#### Scenario: A row-bound ability cell collapses with its own row
- **WHEN** a row-bound ability entry's own row is collapsed because every entry in that row's run
  is fully deselected or fully dead
- **THEN** that ability's name is hidden along with the rest of the row's collapsed content

#### Scenario: A component-wide ability cell stays expanded while any spanned row is still selected
- **WHEN** a component-wide ability visually spans three statline rows and only two of those rows
  are collapsed (fully deselected or fully dead), with the third still having a selected or partial
  entry with a nonzero remaining count
- **THEN** the ability cell remains fully expanded

#### Scenario: A component-wide ability cell collapses once every spanned row is fully deselected
- **WHEN** a component-wide ability visually spans three statline rows and every entry in all three
  rows becomes fully deselected
- **THEN** the ability cell collapses (its ability names hide)

#### Scenario: A component-wide ability cell collapses once every spanned row is fully dead
- **WHEN** a component-wide ability visually spans three statline rows and every entry in all three
  rows reaches a remaining count of 0
- **THEN** the ability cell collapses (its ability names hide), matching the fully-deselected case

### Requirement: Weapon Section Rendering
Each unit block SHALL group the view's `Weapons` into a Ranged section and a Melee section by each
entry's `Profile.Type`, rendering the Ranged section before the Melee section, and SHALL omit
either section entirely when it has no entries. Within each section, entries SHALL be ordered by
descending expected value of their aggregated `TotalAttacks`. Each entry SHALL show the weapon's
name, Skill, Strength, AP, Damage, the aggregated total Attacks value, and any active ability
keywords, each rendered as its own bordered chip immediately alongside the weapon's name rather
than in a separate column or joined with other keywords into a single bracketed group. Ranged
weapon entries SHALL additionally show Range; Melee weapon entries SHALL NOT show a Range value,
since it is always the fixed literal "Melee" for that weapon type and carries no information
beyond the section it's already listed under. No entry's default (collapsed) rendering SHALL
display a raw per-model Attacks value alongside a separate model count.

When the unit has more than one `ModelLine` in total across all of its components, every weapon
entry's weapon name SHALL be an interactive trigger that toggles a contribution breakdown rendered
directly beneath that entry's row, regardless of how many contributions that specific entry has -
knowing which single `ModelLine` a weapon came from is informative on its own when the unit has
other `ModelLine`s it could be distinguished from. When the unit has exactly one `ModelLine` in
total, no weapon entry SHALL render such a trigger, since there is no second source to distinguish
it from. The breakdown SHALL group contributions by `(ComponentName, StatlineName)`: when every
contribution in a group shares the same `PerModelAttacks` (a group of exactly one contribution
trivially satisfies this), the group SHALL render as one row showing that group's name, its summed
`Count`, its shared `PerModelAttacks`, and the product of the two as a subtotal in the Attacks
column, with every other stat column left blank; when a group's contributions disagree on
`PerModelAttacks`, each contributing `ModelLine` SHALL instead render as its own row within that
group, so no subtotal is ever shown that a reader could not verify from the values beside it.
Breakdown rows SHALL be styled visually distinct from primary weapon-entry rows, and SHALL share
the same odd/even row styling as the primary entry row they belong to rather than alternating
independently.

Each weapon entry's rendering in this section is additionally scoped by the unit block's current
statline/loadout selection (see "Selection-Scoped Weapon Filtering"). An entry with at least one
contribution belonging to a currently-selected statline or loadout SHALL render, with its Attacks
total recomputed from only its currently-selected contributions rather than all of them. An entry
with no contribution belonging to a currently-selected statline or loadout SHALL NOT render in this
section at all. The contribution-breakdown group-merge rule above is further scoped by selection
agreement: a group SHALL render as one collapsed row only when every contribution in it shares both
the same `PerModelAttacks` and the same current selection state; a group whose contributions
disagree on `PerModelAttacks`, on selection state, or both, SHALL render each contributing
`ModelLine` as its own row, and any contribution that is not currently selected SHALL be omitted
from the breakdown entirely rather than rendered as a struck-through or otherwise inert row. Each
section's disclosure `<summary>` SHALL render a "filtered" indicator whenever the unit block's
current selection would hide or resize at least one of that specific section's own entries, and
SHALL render no such indicator when the current selection has no effect on that section, even if it
affects the other weapon section of the same unit block.

#### Scenario: Weapon count reflects aggregation across components
- **WHEN** a unit's aggregate view has a weapon entry whose total Attacks was built from
  contributions with different per-model Attacks values
- **THEN** the entry's default (collapsed) rendering shows only that entry's aggregated total
  Attacks, and does not show any individual contribution's per-model Attacks value or a separate
  model count; that detail is available only by activating the entry's contribution breakdown

#### Scenario: Any weapon entry can expand a contribution breakdown when the unit has multiple ModelLines
- **WHEN** a unit has more than one `ModelLine` in total across all of its components
- **THEN** every one of that unit's weapon entries renders its weapon name as an interactive
  trigger, and activating it reveals a breakdown of rows beneath the entry, one row (or group of
  rows) per distinct `(ComponentName, StatlineName)` group among its contributions - including an
  entry whose `Contributions` has only one item

#### Scenario: A single contribution still renders as one breakdown row
- **WHEN** a weapon entry has exactly one contribution, and the unit has more than one `ModelLine`
  in total
- **THEN** activating the entry's trigger reveals exactly one breakdown row naming that single
  contribution's `(ComponentName, StatlineName)`, its `Count`, its `PerModelAttacks`, and their
  product as the subtotal

#### Scenario: No breakdown trigger when the unit has only one ModelLine total
- **WHEN** a unit has exactly one `ModelLine` in total across all of its components
- **THEN** none of that unit's weapon entries render an interactive trigger on their name, and no
  breakdown is available for any of them

#### Scenario: Uniform contributions within a group collapse to one row
- **WHEN** a contribution breakdown group's contributions all share the same `PerModelAttacks`
- **THEN** the group renders as a single row showing the group's `(ComponentName, StatlineName)`
  label, the contributions' summed `Count`, the shared `PerModelAttacks`, and their product as
  that row's Attacks subtotal

#### Scenario: Disagreeing contributions within a group render separately
- **WHEN** a contribution breakdown group's contributions do not all share the same
  `PerModelAttacks`
- **THEN** each contributing model-line renders as its own row within that group, each showing
  its own `Count`, its own `PerModelAttacks`, and their product as that row's Attacks subtotal

#### Scenario: Breakdown rows are visually distinct from weapon entries
- **WHEN** a contribution breakdown is expanded
- **THEN** its rows render in a style distinguishable from primary weapon-entry rows, and show no
  value in any stat column other than the Attacks subtotal

#### Scenario: Ranged weapons show their Range value
- **WHEN** a unit block renders its Ranged Weapons section
- **THEN** each entry shows that weapon's Range value

#### Scenario: Melee weapons omit the Range column
- **WHEN** a unit block renders its Melee Weapons section
- **THEN** no entry shows a Range value or column, since it would always read "Melee"

#### Scenario: Ranged weapons render before Melee weapons
- **WHEN** a unit's aggregate view has both Ranged- and Melee-typed weapon entries
- **THEN** the rendered block shows all Ranged entries in a section preceding the section
  containing all Melee entries

#### Scenario: A unit with only one weapon type omits the other section
- **WHEN** a unit's aggregate view has weapon entries of only one `WeaponType`
- **THEN** the rendered block shows only the matching section, with no empty section rendered for
  the absent type

#### Scenario: Weapons within a section are ordered by descending expected attacks
- **WHEN** a section has two weapon entries whose `TotalAttacks` expected values differ (e.g. a
  fixed value of `12` and a dice expression `D6`)
- **THEN** the entry with the higher expected value renders above the entry with the lower expected
  value

#### Scenario: Ability tags render inline next to the weapon name
- **WHEN** a weapon entry's `Profile` has more than one active ability flag (e.g. both `Torrent`
  and `Pistol` and `IgnoresCover`)
- **THEN** the rendered entry shows three separate chips immediately alongside the weapon's name —
  one per keyword — not one chip containing all three joined together

#### Scenario: A keyword chip with no matching glossary entry is not interactive
- **WHEN** a weapon keyword chip's underlying tag text has no matching entry in the glossary (per
  `rules-glossary`'s "Glossary Lookup By Normalized Name Or Alias" requirement)
- **THEN** the chip still renders showing that keyword's text, but is not an interactive trigger
  and opens no popover

#### Scenario: A weapon entry disappears when none of its contributions are selected
- **WHEN** every contribution of a weapon entry belongs to a statline or loadout that is currently
  deselected
- **THEN** that entry does not render in its Ranged or Melee section at all

#### Scenario: A weapon entry's total recomputes when only some of its contributions are selected
- **WHEN** a weapon entry has contributions from more than one statline or loadout, and only some
  of them are currently selected (e.g. Crusader Squad's Bolt Pistol, carried by Neophyte, both
  Initiate loadouts, and the attached Crusade Ancient, totalling 10 Attacks when everything is
  selected)
- **THEN** the entry stays visible with its Attacks total recomputed from only the currently
  selected contributions (e.g. deselecting only the Power-fist-armed Initiate loadout drops the
  total from 10 to 8)

#### Scenario: A deselected contribution's breakdown row disappears rather than rendering inert
- **WHEN** a weapon entry's contribution breakdown group contains a contribution that is currently
  deselected, alongside one or more contributions that remain selected
- **THEN** the deselected contribution's row is entirely absent from the breakdown; it does not
  render struck through, greyed out, or otherwise present-but-inert

#### Scenario: A breakdown group splits when its contributions disagree on selection state
- **WHEN** a contribution breakdown group's contributions all share the same `PerModelAttacks` but
  disagree on current selection state (e.g. Crusader Squad's Bolt Pistol "Initiate" group, after
  the Power-fist-armed Initiate loadout is deselected while the Astartes-chainsword-armed Initiate
  loadout stays selected)
- **THEN** the group no longer renders as one collapsed row; each selected contributing `ModelLine`
  renders as its own row, and the deselected one is omitted

#### Scenario: A weapon section shows a filtered indicator only when it is itself affected
- **WHEN** the unit block's current selection would hide or resize at least one entry in the
  Ranged Weapons section, but has no effect on any entry in the Melee Weapons section
- **THEN** the Ranged Weapons section's disclosure summary shows a filtered indicator and the Melee
  Weapons section's disclosure summary does not

## ADDED Requirements

### Requirement: Ability And Rule Text Popover
Tapping an ability name (in either the Model Abilities or Unit Abilities column) or a weapon
keyword chip that has a matching entry in the glossary (per `rules-glossary`) SHALL open a popover
showing that entry's full descriptive text, without navigating away from or reloading the page. A
weapon keyword chip with no matching glossary entry is not an interactive trigger and opens no
popover (see "Weapon Section Rendering").

#### Scenario: Tapping an ability name opens its rule text
- **WHEN** a player taps an ability name rendered in the Model Abilities or Unit Abilities column
- **THEN** a popover opens showing that ability's full descriptive text

#### Scenario: Tapping a resolvable weapon keyword chip opens its rule text
- **WHEN** a player taps a weapon keyword chip that has a matching glossary entry (e.g. a
  `[LETHAL HITS]` chip resolving to the universal "Lethal Hits" rule)
- **THEN** a popover opens showing that rule's full descriptive text

#### Scenario: Opening a popover does not reload the page
- **WHEN** a player taps an ability name or a resolvable weapon keyword chip
- **THEN** the page's existing state (casualty marks, current statline/loadout selection, expanded
  disclosure sections) is unchanged and no full page navigation occurs

### Requirement: Popover Is Anchored Locally To Its Trigger
A popover SHALL be positioned adjacent to the specific element that was tapped to open it, rather
than centered on the screen or otherwise placed independent of what was tapped, so the player does
not lose their place in the surrounding statline, ability column, or weapon table.

#### Scenario: A popover appears next to what was tapped
- **WHEN** a player taps an ability name or weapon keyword chip anywhere within a unit block
- **THEN** the resulting popover renders positioned adjacent to that specific element, not centered
  on the screen

### Requirement: Popover Remains Functional Without Anchor-Positioning Support
On a browser that does not support local anchor positioning, a popover SHALL still open and remain
fully dismissable exactly as elsewhere in this requirement set — only its placement relative to its
trigger is affected, never whether it appears or can be closed.

#### Scenario: A popover still opens and closes on an unsupporting browser
- **WHEN** a player taps an ability name or a resolvable weapon keyword chip on a browser without
  local anchor-positioning support
- **THEN** a popover still opens showing the expected text and can still be dismissed, even though
  it is not positioned adjacent to its trigger

### Requirement: Popover Dismissal
A popover SHALL be dismissable by tapping anywhere outside it, without requiring a dedicated close
control.

#### Scenario: Tapping outside a popover closes it
- **WHEN** a popover is open and the player taps anywhere outside it
- **THEN** the popover closes

### Requirement: Nested Reference Popover
Within an open popover's own text, a resolved `[BRACKET]` cross-reference (per `rules-glossary`'s
"Glossary Lookup By Normalized Name Or Alias" requirement) SHALL be an interactive trigger, unless
it resolves back to a rule already shown somewhere in that popover's own ancestor chain (a direct
self-reference or a longer cycle), in which case it SHALL be treated the same as an unresolved
token. Tapping a resolved, non-cyclic reference SHALL open a second popover, anchored to that
specific reference, showing the referenced rule's text, without closing the popover it was opened
from. Tapping outside the second popover but still inside the first SHALL close only the second
popover, leaving the first open. Tapping outside both SHALL close both in one action. An unresolved
bracket token (no matching glossary entry) is not an interactive trigger.

#### Scenario: Opening a nested popover keeps the parent open
- **WHEN** a player taps a resolved cross-reference inside an open popover (e.g. tapping
  `[PRECISION]` while reading a detachment rule's own text)
- **THEN** a second popover opens showing the "Precision" rule's text, and the first popover
  remains open and visible

#### Scenario: Dismissing between the two closes only the nested popover
- **WHEN** both a parent and a nested popover are open, and the player taps a location inside the
  parent's own content but outside the nested popover
- **THEN** only the nested popover closes; the parent popover remains open

#### Scenario: Dismissing outside both closes the whole stack
- **WHEN** both a parent and a nested popover are open, and the player taps a location outside both
- **THEN** both popovers close

#### Scenario: An unresolved reference within popover text is not interactive
- **WHEN** an open popover's text contains a `[BRACKET]` token with no matching glossary entry
- **THEN** that token is not an interactive trigger and tapping it opens no further popover

#### Scenario: A self-referencing rule's own reference to itself is not interactive
- **WHEN** a popover is showing a rule whose own text contains a `[BRACKET]` reference that
  resolves back to that same rule (confirmed real shape — the "Sustained Hits", "Anti", and
  "Cleave" sharedRules entries each reference themselves this way in their own description text)
- **THEN** that reference is not an interactive trigger and tapping it opens no further popover,
  the same treatment as an unresolved token — this also applies to a longer reference cycle back
  to any rule already open in the current popover's own ancestor chain, not only a direct
  self-reference

### Requirement: Popover Text Preserves Paragraph Breaks
A popover's rendered text SHALL preserve real line breaks present in the source text as visual
line breaks, rather than collapsing them to a single space or run-on line.

#### Scenario: Multi-paragraph rule text renders with its paragraph break intact
- **WHEN** a popover renders rule text containing a real line break (e.g. the "Lethal Hits" rule's
  Designer's Note, which is a separate paragraph from its main description)
- **THEN** the two paragraphs render as visually distinct lines/paragraphs, not run together as one

### Requirement: Popover Renders Emphasis Markup As Styling, Not As Links
A popover SHALL render source emphasis markup as visual styling only, using three distinct styles:
`*italic*` (single asterisk) as italic text, `**bold**` (double asterisk) as bold text, and
`^^small-caps^^` (double caret) as small-caps text. None of the three SHALL make the wrapped text
an interactive/tappable element merely by virtue of that markup — only a resolved `[BRACKET]`
reference (per "Nested Reference Popover") is tappable, independent of any emphasis markup around
it.

#### Scenario: Single-asterisk text renders italic
- **WHEN** a popover renders text containing `*italic text*`
- **THEN** that text renders in italics

#### Scenario: Double-asterisk text renders bold
- **WHEN** a popover renders text containing `**bold text**`
- **THEN** that text renders in bold

#### Scenario: Double-caret text renders small caps
- **WHEN** a popover renders text containing `^^small caps text^^`
- **THEN** that text renders in small caps

#### Scenario: Bold and small-caps combine when both wrap the same text
- **WHEN** a popover renders text containing `^^**Fabius Bile**^^` (a unit name wrapped in both
  markers at once, confirmed real shape)
- **THEN** that text renders both bold and small-caps, and tapping it opens no popover

#### Scenario: Nested triple-asterisk emphasis renders as combined bold and italic
- **WHEN** a popover renders text containing `***Designer's Note:**` followed by running text that
  stays open until a single closing `*` later in the same paragraph (confirmed real shape — the
  "Lethal Hits" rule's own Designer's Note, where the bold portion closes after two words but the
  italic portion continues to the paragraph's end)
- **THEN** `"Designer's Note:"` renders both bold and italic, and the remaining running text up to
  the closing `*` renders italic only, matching the source markup's own nesting
