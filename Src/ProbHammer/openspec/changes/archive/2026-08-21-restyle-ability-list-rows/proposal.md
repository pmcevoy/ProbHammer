## Why

On `/LivePlay`, each unit block's Model/Unit ability columns (`.statline-cell.col-model-abilities`
/ `.col-unit-abilities`, populated by `AggregateAbilityEntry` — see `.claude/domain-model-11e.md`)
render as a flat, unstyled list of `.ability-name-line` divs: tight vertical padding only, no
horizontal padding, and no row separation. That reads as visually flat next to the page's other
list-shaped content (the weapon tables, which are zebra-striped and row-bordered) and gives each
ability name too little of a touch target. Confirmed live in the browser against a real imported
army list (`data/gw-app-export.txt`), iterating through several candidate treatments before
settling on this one.

## What Changes

- `.ability-name-line` gets real padding (`0.3rem 0.6rem`, up from `0.1rem 0`) and a bolder weight
  (`font-weight: 700`, up from `600`) for a clearer, more deliberate row and a larger click/touch
  target — laying groundwork for, but not implementing, the separately-deferred "click an ability
  name to see its full text" popover (already tracked in `.claude/vnext-ideas.md`'s "Ability/
  weapon-keyword definitions popup" — out of scope here).
- Add a `border-bottom: 1px solid var(--border)` to every `.ability-name-line`, and a zebra fill
  on even rows, mirroring the existing weapon-table convention
  (`.claude/design-tokens.md`'s "Zebra striping" row) — but with two adjustments the weapon table
  doesn't need, both found live in-browser (see design.md for the full comparison trail):
  - The fill color can't reuse the weapon table's literal `var(--bg)` — a `.spans-component`
    ability cell (a component-wide ability spanning several statline rows) already has
    `background: var(--bg)` on the cell itself (site.css:903), which would make the stripe
    identical to, and therefore invisible against, its own container. Uses
    `color-mix(in srgb, var(--border) 70%, var(--bg2))` instead — visible against both a plain
    cell (implicit `--bg2` background) and a `.spans-component` cell (`--bg` background).
  - `:nth-child(even)` (not a server-assigned alternating class) is safe here, unlike the weapon
    table: no JS ever hides an individual `.ability-name-line` (only the whole ability cell
    collapses as a unit, via `.run-collapsed` on the container) — so there's no hidden-row/parity
    hazard for `:nth-child` to fall into.
- Change `.statline-cell.col-model-abilities`/`.col-unit-abilities` from block layout with
  `padding: 0.4rem 0.5rem` to `display: flex; flex-direction: column; padding: 0`, and give each
  `.ability-name-line` `flex: 1 1 auto`. Needed because these cells can be taller than their own
  content — a `.spans-component` cell's height is driven by the statline rows it spans
  (`.claude/domain-model-11e.md`'s "Statline grid" note: grid row tracks are shared with the
  sibling statline column), not by its own ability count — so a short ability list previously left
  a blank gap below the last row. Any container padding (even after moving it fully to the row
  level) still reintroduced a visible seam specifically when the last row landed on an odd
  (unstriped) index, because the padding and the unstriped row read as one merged blank region.
  Zeroing the container's own padding and letting the flex rows fill 100% of the box (each row's
  own `0.3rem 0.6rem` padding is now the only spacing, applied uniformly whether a row is first,
  last, striped, or not) removes the gap in every case — confirmed against 2-, 3-, and 4-ability
  boxes live in-browser.

## Capabilities

No spec-level behavior changes — ability content, grouping, collapse behavior, and the
Model/Unit column split are unchanged; only padding, weight, borders, and background on the
existing `.ability-name-line`/cell rendering. Pure visual/styling change, so no `specs/` deltas
apply (`skip_specs: true` set in `.openspec.yaml`).

## Impact

- `src/ProbHammer.Web/wwwroot/css/site.css` — `.ability-name-line` and
  `.statline-cell.col-model-abilities`/`.col-unit-abilities` rules.
- `.claude/design-tokens.md` — documents the new zebra-fill exception (why this list can't reuse
  the weapon table's literal `--bg` stripe) alongside the existing "Zebra striping" note.
- No C#, Razor, or JS changes. No test changes (purely visual). Not related to, and does not
  unblock, the separately-deferred ability-text popover feature.
