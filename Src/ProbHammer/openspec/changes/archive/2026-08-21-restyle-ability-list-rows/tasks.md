## 1. Container Layout

- [x] 1.1 In `src/ProbHammer.Web/wwwroot/css/site.css`, change
      `.statline-cell.col-model-abilities, .statline-cell.col-unit-abilities`'s `padding:
      0.4rem 0.5rem;` to `padding: 0;`, and add `display: flex; flex-direction: column;`.
- [x] 1.2 Confirm `.statline-cell.spans-component { background: var(--bg); }` (site.css:903) and
      the `.run-collapsed` rule (site.css:909-916) are left untouched — both still apply correctly
      to the flex container.

## 2. Row Styling

- [x] 2.1 Replace `.ability-name-line { padding: 0.1rem 0; font-weight: 600; }` with:
      `padding: 0.3rem 0.6rem;`, `font-weight: 700;`, `border-bottom: 1px solid var(--border);`,
      `flex: 1 1 auto;`, `display: flex;`, `align-items: center;`.
- [x] 2.2 Add `.ability-name-line:nth-child(even) { background: color-mix(in srgb, var(--border)
      70%, var(--bg2)); }`.

## 3. Verification

- [x] 3.1 Run the app (`docker compose up`), import a real export (e.g.
      `data/gw-app-export.txt`), expand a Statline section, and visually confirm on `/LivePlay`:
      - Zebra stripes and row dividers run edge-to-edge inside every ability box's dashed border,
        on both `.spans-component` and plain ability cells.
      - No leftover blank space below the last row, for ability counts of 1, 2, 3, and 4+ (check
        at least one odd-count and one even-count box).
      - Ability names read clearly bolder with a larger touch/click target than before.
- [x] 3.2 Confirm a run's `.run-collapsed` transition (statline entry deselected/fully dead) still
      collapses the ability cell smoothly with no visual glitch from the padding/flex changes.

## 4. Documentation

- [x] 4.1 Update `.claude/design-tokens.md`'s "Zebra striping" note to record the ability-list
      exception: it can't reuse the weapon table's literal `var(--bg)` fill because a
      `.spans-component` ability cell already uses `var(--bg)` as its own background, and instead
      uses `color-mix(in srgb, var(--border) 70%, var(--bg2))`.
