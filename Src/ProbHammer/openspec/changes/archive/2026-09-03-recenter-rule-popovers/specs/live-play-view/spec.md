## ADDED Requirements

### Requirement: Popover Is Centered In The Viewport
A popover SHALL be positioned centered in the viewport, at a consistent size and position
regardless of which element was tapped to open it, rather than positioned adjacent to its trigger.

#### Scenario: A popover appears centered regardless of what was tapped
- **WHEN** a player taps an ability name or weapon keyword chip anywhere within a unit block
- **THEN** the resulting popover renders centered in the viewport, not positioned adjacent to that
  element

### Requirement: Popover Dims The Page Behind It
While a popover is open, the page content behind it SHALL render visibly dimmed, so the player's
attention stays on the popover rather than the surrounding page.

#### Scenario: Opening a popover dims the page behind it
- **WHEN** a player opens a popover
- **THEN** the page content behind the popover renders visibly darker than it does with no popover
  open

#### Scenario: A nested popover's own dimming does not hide its still-open parent
- **WHEN** a nested reference popover is open on top of its parent (per "Nested Reference
  Popover")
- **THEN** the parent popover's own visible portion remains legibly distinguishable from the
  dimmed page behind both, even though a nested popover's own dimming layers on top of it

## MODIFIED Requirements

### Requirement: Popover Dismissal
A popover SHALL be dismissable either by tapping anywhere outside it, or by tapping its own
explicit close control - the two are independent dismissal paths, neither required over the other.

#### Scenario: Tapping outside a popover closes it
- **WHEN** a popover is open and the player taps anywhere outside it
- **THEN** the popover closes

#### Scenario: Tapping the close control closes the popover
- **WHEN** a popover is open and the player taps its own explicit close control
- **THEN** the popover closes

### Requirement: Nested Reference Popover
Within an open popover's own text, a resolved `[BRACKET]` cross-reference (per `rules-glossary`'s
"Glossary Lookup By Normalized Name Or Alias" requirement) SHALL be an interactive trigger, unless
it resolves back to a rule already shown somewhere in that popover's own ancestor chain (a direct
self-reference or a longer cycle), in which case it SHALL be treated the same as an unresolved
token. Tapping a resolved, non-cyclic reference SHALL open a second popover, centered in the
viewport like its parent but visually offset from it so the parent's own title bar remains visible
alongside the new popover, showing the referenced rule's text, without closing the popover it was
opened from. Tapping outside the second popover but still inside the first (including on the
parent's own visible title bar) SHALL close only the second popover, leaving the first open.
Tapping outside both SHALL close both in one action, as SHALL tapping either popover's own close
control for itself.

#### Scenario: Opening a nested popover keeps the parent open
- **WHEN** a player taps a resolved cross-reference inside an open popover (e.g. tapping
  `[PRECISION]` while reading a detachment rule's own text)
- **THEN** a second popover opens showing the "Precision" rule's text, visually offset from the
  first so the first popover's own title bar remains visible, and the first popover remains open

#### Scenario: Dismissing between the two closes only the nested popover
- **WHEN** both a parent and a nested popover are open, and the player taps a location inside the
  parent's own visible content but outside the nested popover
- **THEN** only the nested popover closes; the parent popover remains open

#### Scenario: Dismissing outside both closes the whole stack
- **WHEN** both a parent and a nested popover are open, and the player taps a location outside both
- **THEN** both popovers close

#### Scenario: An unresolved reference within popover text is not interactive
- **WHEN** an open popover's text contains a `[BRACKET]` token with no matching glossary entry
- **THEN** that token is not an interactive trigger and tapping it opens no further popover

#### Scenario: A self-referencing rule's own reference to itself is not interactive
- **WHEN** a popover is showing a rule whose own text contains a `[BRACKET]` reference that
  resolves back to that same rule (confirmed real shape - the "Sustained Hits", "Anti", and
  "Cleave" sharedRules entries each reference themselves this way in their own description text)
- **THEN** that reference is not an interactive trigger and tapping it opens no further popover,
  the same treatment as an unresolved token - this also applies to a longer reference cycle back
  to any rule already open in the current popover's own ancestor chain, not only a direct
  self-reference

## REMOVED Requirements

### Requirement: Popover Is Anchored Locally To Its Trigger
**Reason**: Superseded by "Popover Is Centered In The Viewport" - a popover no longer positions
relative to its trigger at all. Local anchor-positioning was confirmed unreliable on at least one
real target browser (CSSOM-confirmed supported, yet not resolving near the trigger), and
landscape's tight vertical budget made a trigger-adjacent popover more likely to sit awkwardly
close to a viewport edge.
**Migration**: No player-facing migration - every existing trigger continues to open the same
popover content, only its on-screen position changes.

### Requirement: Popover Remains Functional Without Anchor-Positioning Support
**Reason**: Moot once popover placement no longer depends on anchor-positioning support at all -
centering works identically regardless of anchor-positioning support, so there is no longer a
distinct "unsupporting browser" case to specify.
**Migration**: No migration needed - functionality (a popover opens and can be dismissed) is now
unconditionally covered by "Popover Is Centered In The Viewport" and the revised "Popover
Dismissal", with no browser-support carve-out required.
