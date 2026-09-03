## Why

Every ability/rule popover on `/LivePlay` is small and anchor-positioned next to whatever was
tapped (`live-play-view`'s "Popover Is Anchored Locally To Its Trigger" requirement), dismissed
only by tapping outside it (no dedicated close control, per "Popover Dismissal"). Two things argue
against that on a real phone: design-tokens.md's own "Popovers" section already records that real
device testing found local anchor-positioning genuinely unreliable on at least one real browser
(the CSS is confirmed matched in the CSSOM, yet the popover still doesn't resolve near its
trigger); and landscape's already-tight vertical budget (`live-play-landscape-only`) makes a small
popover spawned right at the tap point more likely to sit awkwardly close to the top/bottom edge
of a 315px-tall viewport. A centered panel sidesteps both: it needs no anchor-positioning support
at all, and its position is independent of where on the (short) screen it was triggered from.

## What Changes

- **BREAKING**: a popover no longer opens anchored next to its trigger — it opens centered in the
  viewport instead, at a consistent size regardless of where it was tapped from.
- A popover gains an explicit "×" close control, rather than relying solely on tap-outside
  dismissal — centering a panel over the middle of the content makes "tap outside" a less obvious
  affordance than it was for a small bubble sitting right next to what was tapped.
- The page content behind an open popover renders visibly dimmed (native `::backdrop`), keeping
  the player's attention on the popover rather than the surrounding page.
- Nested `[BRACKET]` reference popovers keep their existing ancestor-aware light-dismiss stacking
  (opening a nested popover doesn't close its parent; dismissing between them closes only the
  nested one) — this mechanism is independent of on-screen position, so centering does not change
  it, only how each individual panel is placed.
- The existing anchor-positioning `@supports` fallback path (kept today specifically for a browser
  that reports the feature supported but doesn't resolve it correctly) becomes unnecessary, since
  centering never depends on anchor-positioning support in the first place.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `live-play-view`: replaces "Popover Is Anchored Locally To Its Trigger" with a centered-position
  requirement; adds a "Popover Dims The Page Behind It" requirement; replaces "Popover Dismissal"'s
  tap-outside-only behavior with tap-outside-or-explicit-close; updates "Nested Reference Popover"
  for centered (rather than anchored) placement of both parent and nested panels; removes the now-
  moot "Popover Remains Functional Without Anchor-Positioning Support" requirement (centering does
  not depend on anchor-positioning support, so the distinction it describes no longer applies).

## Impact

- `src/ProbHammer.Web/wwwroot/css/site.css`: replaces the `anchor-name`/`position-anchor` CSS
  custom-property mechanism (and its `@supports (anchor-name: --a)` guard) with fixed/centered
  positioning for `.rule-popover`; adds close-button styling.
- `src/ProbHammer.Web/Rendering/RulePopoverRenderer.cs`: stops emitting per-instance
  `anchor-name`/`position-anchor` custom properties; adds a close button to each rendered popover
  panel.
- This is presentation-only — no change to `ProbHammer.Core`, `RuleGlossary` resolution, or
  bracket-token extraction.
