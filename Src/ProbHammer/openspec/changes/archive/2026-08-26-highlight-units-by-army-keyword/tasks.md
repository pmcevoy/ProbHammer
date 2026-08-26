## 1. Army Header Markup

- [x] 1.1 Add an "All Keywords" section to `_ArmyHeader.cshtml`, directly beneath the existing
      Rules section: an empty `<details class="lp-section" data-section="army-keywords">` (no
      `open` attribute, closed by default) with an empty pill-list container for JS to populate. No
      new render-model fields.

## 2. Styling

- [x] 2.1 Add a base chip style for the header's own keyword pills (button reset over the existing
      `.weapon-tag` pill look, cursor pointer, existing hover convention).
- [x] 2.2 Add an active/selected style for a header keyword pill using the existing `--bg3` fill +
      white text control convention (no new colour tokens).
- [x] 2.3 Add a flagged style for a per-unit Keywords pill using the existing `--amber-tint`
      background convention (`.stat-tile-flagged`'s pattern), applied only when that specific
      keyword's filter is active. No changes to `.weapon-tag`'s own base style.

## 3. Client-Side Keyword Filter (`live-play.js`)

- [x] 3.1 Add a module-scope `Set` of currently-active keyword filters, alongside
      `deselectedByUnit`, reinitialized empty on each page load (no persistence).
- [x] 3.2 Implement a scan that collects the deduplicated, case-insensitively-sorted set of keyword
      text currently rendered across every unit block's own Keywords section.
- [x] 3.3 On each rebuild, intersect the active-filter Set against the freshly-scanned keyword set
      so a keyword no longer present is dropped from the active set (no explicit "clear" step
      needed elsewhere).
- [x] 3.4 Render/replace the header's own pill list from the scanned keyword set, one button per
      keyword, marked active per the (pruned) active-filter Set.
- [x] 3.5 Wire a click handler on each header pill that toggles its keyword in the active-filter Set
      and re-runs the render/highlight steps (3.4 and 3.6) - no need to rescan keywords for a pure
      click.
- [x] 3.6 Implement the cross-unit highlighting pass: for every unit block, for every rendered
      Keywords pill, add/remove the flagged style based on active-filter Set membership; if a
      unit has at least one currently-flagged pill, force its Keywords `<details>` `open` (never
      force it closed).
- [x] 3.7 Implement one orchestrating function (scan → prune → render header pills → apply
      cross-unit highlighting) and call it once on `DOMContentLoaded`.
- [x] 3.8 Call the same orchestrating function once per `syncLivePlayState()` batch, after every
      fragment in that batch has been swapped in via `swapUnitBlock` - not once per individual
      swapped unit.

## 4. Manual Verification

- [x] 4.1 Load a real imported army with units sharing a common keyword (e.g. a faction keyword)
      and units with unique keywords; confirm the header list renders sorted, deduplicated, and
      matches what each unit's own Keywords section shows.
- [x] 4.2 Select a keyword; confirm only matching units' Keywords sections expand and only the
      matching pill inside each is flagged - other units and other pills are untouched.
- [x] 4.3 Select a second, different keyword while the first stays active; confirm OR matching -
      units matching either keyword are expanded/flagged.
- [x] 4.4 Deselect a keyword; confirm its active style and flags clear, but every section it had
      expanded remains expanded.
- [x] 4.5 Manually expand a unit's Keywords section that does not carry the keyword being toggled;
      confirm activating/deactivating that unrelated filter does not change that section's
      expanded/collapsed state.
- [x] 4.6 Mark casualties so a keyword's only carrier is no longer present; confirm that keyword
      drops out of the header list and, if it was active, its filter clears.
- [x] 4.7 Revert those casualties (e.g. Reset Casualties); confirm the keyword reappears in the
      header list in its inactive style, with no automatic re-expand/re-flag.
- [x] 4.8 Activate one or more filters, then reload `/LivePlay`; confirm no filter is active after
      reload.
- [x] 4.9 Confirm the "All Keywords" section is entirely omitted when no unit on the page currently
      has any keyword.
