# Visual Design Specification

GW-datasheet-inspired light theme for `/LivePlay`, the only live page today. No web fonts —
system font throughout. No monospace — stat/weapon numbers render in the regular body weight.

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

## Mechanism

Declared by redeclaring all eight custom properties locally on the `.live-play-page` selector in
`site.css`, rather than as a separate stylesheet — every rule scoped under it (including shared
selectors like `.weapon-table`) resolves against these values. `site.css` is not split by page; if
a second page with its own theme is ever added, promote this into a proper multi-theme structure
instead of copying the pattern a second time.

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

No dedicated responsive breakpoint today — the page is a single centered column, `max-width: 900px`.
