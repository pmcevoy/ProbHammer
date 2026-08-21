## Context

See proposal.md - Why/What Changes for the motivation and the final rule set. This document
exists because getting there took several live-browser iterations that each ruled out an
otherwise-reasonable-looking option for a concrete, non-obvious reason — worth keeping so a future
edit to this CSS doesn't quietly reintroduce one of these.

Two structural facts about the existing markup/CSS drove every decision below:
- `.statline-cell.col-model-abilities`/`.col-unit-abilities` cells can be `.spans-component`
  (a component-wide, Datasheet-sourced ability spanning several statline grid rows) or not. A
  `.spans-component` cell already carries `background: var(--bg)` (site.css:903); a plain cell
  carries no explicit background of its own (inherits the unit-block's `--bg2`).
  `.claude/domain-model-11e.md`'s "Statline grid" note explains why: grid row tracks are shared
  with the sibling statline column, so a spanning ability cell's rendered height is driven by
  that shared track, not by its own content.
  - Consequence: any styling applied uniformly to every `.ability-name-line` has to remain visible
    against *both* possible cell backgrounds, not just one.
- Because a `.spans-component` cell's height can exceed its own content's natural height, a short
  ability list can leave blank space below the last `.ability-name-line`, inside the cell's own
  border. This is pre-existing, unrelated to this change — it just becomes visible for the first
  time once individual rows get their own background/border treatment.

## Goals / Non-Goals

**Goals:**
- Every `.ability-name-line` gets a visible, edge-to-edge zebra stripe and row divider, correct
  and gap-free for any ability count (1, 2, 3, 4+) and in both cell backgrounds
  (`.spans-component` or not).

**Non-Goals:**
- Not touching `.statline-cell.col-statline` or any other statline-grid cell type.
- Not addressing the pre-existing "short ability list in a tall spanning cell" layout fact itself
  beyond making it invisible to the eye (the cell can still be taller than its content demands;
  the rows now simply grow to fill that height rather than leaving a gap).

## Decisions

**Zebra fill color: `color-mix(in srgb, var(--border) 70%, var(--bg2))`, not a literal reuse of
the weapon table's `var(--bg)`.** The weapon table's own zebra rule
(`.live-play-page .weapon-table tbody tr.weapon-row-alt td { background: var(--bg); }`) is exactly
what was tried first, for consistency with the existing documented convention
(`.claude/design-tokens.md`'s "Zebra striping" note). It's invisible on a `.spans-component` cell
specifically, because that cell's own background is already `var(--bg)` — stripe and container
would be the same color. A flat `var(--border)` (tried next) is visible in both cases but reads
too dark/harsh at full strength. `color-mix(border 70%, bg2)` lands as a value distinct from both
possible cell backgrounds (`--bg2` and `--bg`) while staying softer than raw `--border` — confirmed
by direct screenshot comparison against a real imported army list (`data/gw-app-export.txt`) with
multiple `.spans-component` ability cells of varying ability counts (2, 3, and 4 entries).

**`:nth-child(even)`, not a server-assigned alternating class.** The weapon table deliberately
avoids `:nth-child` (`.claude/design-tokens.md`: "driven by each row's own list index server-side,
not DOM child-position, so hidden breakdown rows can't shift the parity of rows after them") because
a weapon's contribution-breakdown row is a real, always-present DOM sibling even while `[hidden]`.
No equivalent hazard exists for `.ability-name-line`: nothing in `live-play.js` hides an individual
ability line — only the whole ability cell collapses as a unit (`.run-collapsed`, driven by every
run in the cell's span being fully dead/deselected). `:nth-child(even)` is therefore safe and
simpler; no new server-side index needed on `AggregateAbilityEntry`.

**Container goes from block+padding to `display: flex; flex-direction: column; padding: 0`, with
each row at `flex: 1 1 auto`.** This is what actually fixes the dead-space gap, and needed two
iterations to get right:
1. First attempt kept the container's horizontal padding at 0 (for edge-to-edge stripes) but left
   its *vertical* padding (`0.4rem` top/bottom) in place, with rows still sized to content
   (`flex` not yet applied). Fixed the "floating chip" look (stripes weren't edge-to-edge
   horizontally) but not the dead-space gap — a short ability list still left blank space between
   the last row and the cell's own bottom border.
2. Adding `flex: 1 1 auto` to `.ability-name-line` (rows now share the flex container's full
   height equally, whatever that height is) fixed the gap *between rows and the container's
   padding edge*, but the container's own remaining `0.4rem` vertical padding was still a separate
   band outside the flex rows. That band was invisible at the top (row 1 is always the unstriped/
   odd index, same as the padding's own implicit "no fill" appearance) but visible at the bottom
   whenever the *last* row was also odd/unstriped — the unstriped last row and the blank padding
   band read as one merged, taller blank chunk, breaking the alternating rhythm right where the
   eye would notice it most. Confirmed against a genuine 3-entry (odd) ability box before the fix.
3. Zeroing the container's padding entirely, so every bit of visible inset comes from each row's
   own `0.3rem 0.6rem` padding uniformly (row 1's top padding stands in for what used to be
   container top-padding; the last row's bottom padding stands in for container bottom-padding),
   removes the asymmetry: the same spacing rule applies regardless of parity or position.
   Confirmed gap-free against 2-, 3-, and 4-entry boxes.

Flexbox (not CSS Grid) for the column layout: a single-axis "stack these divs and let them share
the available height" is exactly flexbox's `flex: 1 1 auto` behavior; Grid would need an explicit
`grid-auto-rows` share rule for the same effect with no expressiveness benefit here, and the
existing `.statline-grid` outer layout already documents (site.css:608-616) a specific reason it
needs Grid at the *cell* level (aligning a spanning cell against sibling row heights) — that
reasoning doesn't extend to this cell's own *internal* child layout.

## Risks / Trade-offs

- [The `run-collapsed` transition (site.css:909-916) references `padding-top`/`padding-bottom` on
  the same selector, now always `0 → 0`] → No functional risk: the collapse animation still works
  via its `max-height`/`opacity` transition; the now-redundant padding transition is simply inert,
  not broken.
- [`color-mix()` browser support] → Already an established pattern on this page (the existing
  `--amber-tint` token and the sibling `lighten-lp-section-header-color` change's `--bg3-tint`
  both use `color-mix()`); no new compatibility surface introduced.

## Migration Plan

Pure CSS edit, single file (plus its own design-token doc update). No data migration, no rollback
concern beyond a normal revert.
