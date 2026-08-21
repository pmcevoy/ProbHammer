# Visual Design Specification

GW-datasheet-inspired light theme, shared by every current live page (`/LivePlay`, `/Import`). No
web fonts — system font throughout. No monospace outside `/Import`'s paste textarea — stat/weapon
numbers render in the regular body weight.

---

## Colour Palette

Eight semantic colours cover the page. Named as CSS custom properties — do not hardcode hex
values in components.

| Name | Value | Semantic role |
|---|---|---|
| `--bg` | `#eeece6` | Page background |
| `--bg2` | `#fbfaf7` | Card/panel surfaces |
| `--bg3` | `#24413f` | Section header bars |
| `--accent` | `#24413f` | Emphasis: stat sub-values (e.g. invulnerable save), primary headings |
| `--text` | `#262622` | Primary text |
| `--text-dim` | `#6f6c62` | Labels, counts, dim info |
| `--amber` | `#a86a1d` | Draw-the-eye controls/values (e.g. the casualty-reset icon, the filter badge, the caveated invulnerable-save box's border) |
| `--amber-tint` | `color-mix(in srgb, var(--amber) 20%, white)` | Background only, for the caveated invulnerable-save box — a separate token from `--amber` itself, not a reinterpretation of it; `--amber`'s other uses stay foreground-only |
| `--border` | `#d9d5c9` | Borders and dividers |
| `--bg3-tint` | `color-mix(in srgb, var(--bg3) 55%, white)` | Background only, for `.lp-section` summary bars (Statline/Ranged Weapons/Melee Weapons disclosure headers) — a lighter, flat step down in visual weight from the solid `--bg3` unit-name h2 above them, so the two don't read as the same priority. Same "derived tint, not an opacity fade" pattern as `--amber-tint`: a named, independently redefinable token rather than `--bg3` at reduced opacity, which would also fade the bar's own arrow/icon children and whose rendered color would depend on whatever sits behind it. |

## Mechanism

Declared once on `:root` in `site.css` (not split by page, not a separate stylesheet), so every
page shares the same theme by default — `body`'s own `background`/`color` already reference these
tokens, so a new page needs no per-page redeclaration to pick up the theme, only its own layout
rules (e.g. `.import-page`'s padding/max-width, alongside `.live-play-page`'s). Originally declared
locally on `.live-play-page` only, back when it was the sole live page; promoted to `:root` when
`/Import` (`import-army-list-for-live-play`) became a second page needing the identical theme,
per this file's own prior guidance not to copy the pattern a second time. A future page wanting a
*different* theme would still redeclare the tokens it wants to override on its own page-scoped
selector, same mechanism as before, just no longer the default case.

---

## Typography

- **Font:** `system-ui, sans-serif`
- **Scale:** compact — section headers `0.82rem` bold uppercase, stat labels `0.65rem` uppercase,
  stat values `0.95rem` bold, most secondary text in the `0.6–0.78rem` range. No more than a
  handful of distinct sizes on any one screen. The invulnerable-save box (below the main M/T/Sv/W/
  Ld/Oc row, aligned under Sv specifically — a distinct box + side label, not a value stacked
  inside a tile) reuses these same sizes: its value at `0.95rem` bold like a stat value, its label
  at `0.65rem` like a stat label. A caveated save's box gets an off-color (`--amber-tint`)
  background instead of the normal `--bg`, plus its linked ability's full text rendered in italics
  beneath the box at `0.7rem` — the one place on `/LivePlay` that shows an ability's text rather
  than only its name, scoped narrowly to this one case (see `.claude/domain-model-11e.md`).

---

## Spacing & Shape

- Spacing is tight — this is a game tool, not a marketing page.
- Border radius: `4px` on unit-block cards, `3px` on section/chip/control surfaces, `2px` on the
  smallest inline controls.
- All borders use `--border`. No drop shadows.

---

## Interaction States

- **Hover** (toggles, disclosure controls): opacity fades to `0.8`
- **Disabled** (e.g. a casualty control at its bound): opacity `0.4`–`0.55`, cursor `default`
- **Zebra striping** (weapon tables): alternate rows tinted `var(--bg)` against the card's `--bg2`
  base — driven by each row's own list index server-side, not DOM child-position, so hidden
  breakdown rows can't shift the parity of rows after them
- **Zebra striping** (Model/Unit ability lists, `.ability-name-line`): alternate rows tinted
  `color-mix(in srgb, var(--border) 70%, var(--bg2))`, via plain `:nth-child(even)` rather than a
  server-assigned class — safe here because, unlike a weapon table's breakdown rows, no JS ever
  hides an individual ability line (only the whole ability cell collapses as a unit). Can't reuse
  the weapon table's literal `var(--bg)` fill: a `.spans-component` ability cell (one spanning
  several statline rows) already uses `var(--bg)` as its own background, which would make the
  stripe invisible against it — see `openspec/changes/restyle-ability-list-rows/design.md` for the
  full comparison trail (a flat `var(--border)` was visible but read too dark; this mix lands
  between the two, distinct from both a plain cell's `--bg2` and a `.spans-component` cell's
  `--bg`).

No dedicated responsive breakpoint today — the page is a single centered column, `max-width: 900px`.
