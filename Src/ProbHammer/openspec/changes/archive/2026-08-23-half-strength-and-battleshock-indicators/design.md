## Context

See `proposal.md` for motivation. Relevant existing shape:

- `ICombatUnit` (`Unit`/`AttachedUnit`) is a read-only composite over `ModelLine`s; the only
  existing mutable live-game-state field anywhere in the roster domain is `ModelLine
  .RemainingCount`, adjusted via `SetRemainingCount`/`RemoveCasualties`.
- Every `/LivePlay` request rebuilds the `ArmyRoster` fresh from the session's stored
  `ParsedArmyList` via `IArmyRosterProvider.Build` (`.claude/domain-model-11e.md`'s "Session-Backed
  Import") — there is no server-held roster state between requests. Casualty adjustments are
  reapplied onto that fresh rebuild from a `localStorage`-held map, keyed
  `"{unitIndex}::{componentName}::{statlineName}[::{loadoutIndex}]"`, POSTed in full on every
  change (`live-play.js`'s `syncCasualties`), and the returned HTML fragment for the affected unit
  replaces the old one in the DOM (`swapUnitBlock`).
- `ToughnessResolution`/`KeywordResolution` are the existing precedent for a pure function computed
  fresh from an `ICombatUnit` rather than stored state.
- The caveated-InSv tile (`_UnitBlock.cshtml`, `.insv-tile-caveated` in `site.css`) is the existing
  precedent for "a stat-tile whose background signals its value isn't the plain catalogue value."
- `.filter-flag-icon` is the existing precedent for an inline SVG icon using `fill: currentColor`
  instead of a Unicode/emoji glyph, specifically because a platform emoji's shape/style varies and
  ignores `currentColor` (see that class's own comment, and `.casualty-reset-btn-emoji`'s sibling
  comment recording a real side-by-side trial).

## Goals / Non-Goals

**Goals:**
- Compute at-or-below-half-strength for any multi-model combat unit entirely from existing
  `RemainingCount`/`Count` data, with no new input.
- Give the player a place to record the two things the app cannot compute itself (single-model
  half-strength, and a Battle-shock test's outcome) that persists exactly like casualty state does.
- Keep the OC-tile "this value is flagged" signal on a single reusable background token, not a
  per-glyph color, so a future re-theme is a token swap, not a per-element audit.

**Non-Goals:**
- Simulating the 2D6-vs-Leadership Battle-shock roll itself — always player-reported.
- Tracking partial wounds on a single-model unit — the manual override exists specifically because
  this stays out of scope.
- The "collapse all sections" idea raised during exploration — parked in `.claude/vnext-ideas.md`,
  not part of this change.

## Decisions

**Where the two new flags live.** `Unit` and `AttachedUnit` gain the player-set half-strength
override and the Battle-shocked flag as mutable properties on the `ICombatUnit` implementations
themselves (mirroring where `ModelLine.RemainingCount` lives relative to `ModelLine`), rather than
on a new wrapper type. Battle-shock and the single-model override are both properties of "the
combined unit" per the spec, and `ICombatUnit` is already the existing "combined unit" abstraction
— `AttachedUnit` carries one flag for the whole Bodyguard+Attached group, not one per component.
The computed (starting-strength >= 2) half-strength path needs no stored flag at all; it's a pure
function over `Components`, added alongside `ToughnessResolution`/`KeywordResolution`.

**Sync payload shape.** Casualty adjustments are keyed per model-line/loadout because that's the
granularity that changes. Half-strength and Battle-shocked are both unit-level, so their sync
entries are keyed by unit index alone (no component/statline/loadout suffix) — a much flatter map
than the casualty one. Extending the existing sync endpoint/payload to carry both maps in one POST
(rather than a second endpoint) avoids a race between two independent requests updating the same
unit's rendered fragment, and reuses the existing "rebuild fresh, reapply from the posted map,
re-render the fragment" flow end to end.

**½ as plain text, ⚡ as inline SVG.** U+00BD is a well-supported real character with no
outline/solid distinction to worry about, so it's plain text with a `color` swap. The Battle-shock
glyph needs two genuinely different paint states (outline vs. solid) in one color family, which is
exactly the problem `.filter-flag-icon` already solved by using an inline SVG with `fill:
currentColor`/`stroke` instead of hunting for a matched Unicode pair — same approach here: one
`<path>`, toggled between `fill: none; stroke: currentColor` and `fill: var(--amber); stroke:
none` via a CSS class.

**OC tile reuses a generalized caveated-tile class, not `.insv-tile-caveated` itself.** That class
name is InSv-specific; since it's now used by two structurally different tiles (a standalone InSv
box below the main stat row, and one of the six inline tiles in the main row), the amber-tint
background treatment moves to a shared class (e.g. `.stat-tile-flagged`) that both apply, keeping
the "flagged value" visual in one place rather than duplicating the `--amber-tint` background rule.

**`.unit-name` becomes a flex row.** Currently plain text in an `<h2>`. It adopts the same
`display: flex; align-items: center` plus `margin-left: auto` pattern `.lp-section summary` already
uses for its own right-aligned `.filter-flag` — not a new layout idea, the same one applied one
level up.

## Risks / Trade-offs

[Every `AttachedUnit` OC tile must reflect one shared Battle-shocked flag] → the rendering loop
that emits each statline run's OC tile needs the *combat unit's* flag, not anything scoped to the
run's own component; threading that value through is a rendering-plumbing detail, not a domain
ambiguity, since the spec is explicit that it's one flag per `ICombatUnit`.

[A future theme without an amber-equivalent token] → the header glyphs are controls, so they follow
the existing "controls get amber ink when active" precedent (`.casualty-reset-btn`, the filter
badge) and do use `--amber` as their own active-state color — that's unchanged, established
territory. The OC tile is the one place this design deliberately avoids amber on the glyph itself
(per the user's own preference during exploration): its `⚡` always renders in the ordinary
stat-value color, and the "flagged" signal lives only in the tile's `--amber-tint` background. A
future re-theme still only needs `--amber`/`--amber-tint` redefined, not a per-element audit — the
OC tile just has one fewer place amber could leak into.
