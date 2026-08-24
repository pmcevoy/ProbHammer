# Visual Design Specification

GW-datasheet-inspired light theme, shared by every current live page (`/LivePlay`, `/Import`). No
web fonts — system font throughout. No monospace outside `/Import`'s paste textarea — stat/weapon
numbers render in the regular body weight.

---

## Colour Palette

Nine semantic colours cover the page (eight, plus `--amber-bright` added by
`consolidate-unit-toolbar`). Named as CSS custom properties — do not hardcode hex values in
components.

| Name | Value | Semantic role |
|---|---|---|
| `--bg` | `#eeece6` | Page background |
| `--bg2` | `#fbfaf7` | Card/panel surfaces |
| `--bg3` | `#24413f` | Section header bars; since `consolidate-unit-toolbar`, also a `.unit-toolbar-icon-btn`'s (unchanging) background — actionable/inert is opacity-only there, matching `.casualty-btn:disabled` |
| `--accent` | `#24413f` | Emphasis: stat sub-values (e.g. invulnerable save), primary headings, a `.rule-reference` link (a resolved `[BRACKET]` cross-reference inside popover text) |
| `--text` | `#262622` | Primary text |
| `--text-dim` | `#6f6c62` | Labels, counts, dim info — including, since `consolidate-unit-toolbar`, a `.unit-toolbar-label` caption's own (always-static) color, identical to `.stat-label`'s |
| `--amber` | `#a86a1d` | Draw-the-eye **controls** when active on this page's light (`--bg`/`--bg2`) surfaces (e.g. the filter badge, the caveated invulnerable-save box's border) — never a stat *value*'s own ink; see the "Battle-Shocked Objective Control Rendering" note below. Confirmed by `consolidate-unit-toolbar` to read poorly (~2.8:1 contrast) as icon ink directly on a solid `--bg3` fill — that context uses `--amber-bright` instead, below |
| `--amber-bright` | `color-mix(in srgb, var(--amber) 55%, white)` | Foreground only — `--amber` lightened towards white specifically for icon-on-`--bg3` legibility (~4.9:1 contrast). Added by `consolidate-unit-toolbar` for `.unit-toolbar-icon-btn.is-active`'s icon color (Half Strength/Battle-shock/Reset Casualties), after bare `--amber` was confirmed, by direct user testing, not to "pop" against the toolbar buttons' solid `--bg3` background. Not a general `--amber` replacement — every other existing `--amber` use stays on `--amber` itself, since those all sit on light surfaces where it already reads fine |
| `--amber-tint` | `color-mix(in srgb, var(--amber) 20%, white)` | Background only, for a flagged stat-tile (`.stat-tile-flagged` — originally the caveated invulnerable-save box alone, generalized by `half-strength-and-battleshock-indicators` to also cover a Battle-shocked unit's OC tile) — a separate token from `--amber` itself, not a reinterpretation of it; `--amber`'s other uses stay foreground-only |
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
  than only its name, scoped narrowly to this one case (see `.claude/domain-model-11e.md`). Since
  `rules-glossary-popovers`, a second place now shows full rule text: a popover's own body
  (`.rule-popover-text`) at `0.8rem`, with `white-space: pre-line` so a real line break in the
  source text (e.g. "Lethal Hits"' own Designer's Note, a separate paragraph from its main
  description) renders as one, rather than the existing `insv-caveat-text` box's plain run-on text
  — that box is deliberately left as-is (see `.claude/domain-model-11e.md`'s "Rules Glossary &
  Popovers"), so the two full-text-rendering sites don't share identical wrapping behavior. A
  popover's `^^small-caps^^` markup renders via `font-variant: small-caps` (`.rule-text-smallcaps`)
  — the correct CSS tool for real small caps: it shrinks only the lowercase letters, leaving an
  already-uppercase letter at full size, matching how a proper name like `^^Fabius Bile^^` is
  meant to look.

---

## Spacing & Shape

- Spacing is tight — this is a game tool, not a marketing page.
- Border radius: `4px` on unit-block cards, `3px` on section/chip/control surfaces, `2px` on the
  smallest inline controls. Since `rules-glossary-popovers`: a weapon-keyword chip (`.weapon-tag`)
  follows the `3px` chip scale; a popover panel (`.rule-popover`) follows the `4px` card scale, on
  the reasoning that a popover is a self-contained floating surface closer in weight to a
  unit-block card than to an inline control — both are existing scale steps applied to new
  surfaces, not new radius values.
- All borders use `--border`. No drop shadows — a popover panel follows this too (no `box-shadow`),
  relying on its `--border` outline and the browser's own top-layer paint order to read as
  "floating" above the page.

---

## Interaction States

- **Hover** (toggles, disclosure controls): opacity fades to `0.8`
- **Disabled** (e.g. a casualty control at its bound): opacity `0.4`–`0.55`, cursor `default`
- **Zebra striping** (weapon tables): alternate rows tinted `var(--bg)` against the card's `--bg2`
  base — driven by each row's own list index server-side, not DOM child-position, so hidden
  breakdown rows can't shift the parity of rows after them
- **Zebra striping** (Model/Unit ability lists, `.ability-name-line`): alternate rows tinted
  `color-mix(in srgb, var(--border) 70%, var(--bg2))`, via `:nth-of-type(even)` — originally plain
  `:nth-child(even)`, since no JS ever hides an individual ability line (only the whole ability
  cell collapses as a unit) so there was no hidden-row/parity hazard at the time. `rules-glossary-
  popovers` changed `.ability-name-line` from a `<div>` to a `<button popovertarget>` with its own
  paired `.rule-popover` `<div>` rendered as an interleaved *sibling* (never nested inside the
  button — a `<button>` can't validly contain flow content), which shifts `:nth-child` parity
  exactly the way the weapon table's own breakdown rows already taught this codebase to avoid (see
  below) — `:nth-of-type` sidesteps it by counting only same-tag (`<button>`) siblings, ignoring
  the interleaved `<div>` popovers entirely. Can't reuse the weapon table's literal `var(--bg)`
  fill: a `.spans-component` ability cell (one spanning several statline rows) already uses
  `var(--bg)` as its own background, which would make the stripe invisible against it — see
  `openspec/changes/restyle-ability-list-rows/design.md` for the full comparison trail (a flat
  `var(--border)` was visible but read too dark; this mix lands between the two, distinct from both
  a plain cell's `--bg2` and a `.spans-component` cell's `--bg`).
- **Weapon-keyword chip, resolved vs. unresolved** (`rules-glossary-popovers`): a chip with a
  matching glossary entry (`.weapon-tag-resolved`, itself a popover-trigger `<button>`) renders at
  full opacity with the existing Hover convention above as its only "this is tappable" cue; a chip
  with no match (`.weapon-tag-unresolved`, a plain non-interactive `<span>`) is dimmed via the
  existing Disabled convention (`opacity: 0.55`) rather than a distinct color, so it reads as "not
  tappable" without inventing a new state.
- **Unit status toolbar, and the rule it establishes** (`consolidate-unit-toolbar`, superseding
  `half-strength-and-battleshock-indicators`' original unit-header placement): Half Strength,
  Battle-shock, and Reset Casualties (the latter relocated here from the Statline section's own
  summary bar) now render in their own row, between the unit-name header and the Statline section —
  moved out of the header specifically because a long unit name wrapping onto multiple lines could
  push the old header-cornered glyphs out of view, with no amount of header-width tuning fixing it
  for an arbitrarily long name. Each is a `.unit-toolbar-item`: a small square `.unit-toolbar-
  icon-btn` (sized exactly like `.mod-step-btn`/`.casualty-btn`'s existing inc/dec controls) as the
  *only* clickable area, with a plain `.unit-toolbar-label` caption beside it, outside the button —
  never part of the click target, and never itself a status indicator (styled identically to
  `.stat-label`: `--text-dim`, `0.65rem`, uppercase, unchanging regardless of the button's state).
  Adjacent items are separated by a `border-left` rule rather than a literal "|" character — a
  bordered cell, not a punctuation mark. All three controls always render now (no more
  hide-when-irrelevant per control), and the icon button alone communicates two independent states,
  both built from existing tokens only:
  - **Actionable** (can this be clicked right now) via **opacity**: full while clickable, `0.4`
    once `disabled` — reusing `.casualty-btn:disabled`'s own exact treatment, so an inert toolbar
    control looks like every other disabled control on this page instead of inventing a second
    "this is inert" visual vocabulary. (An earlier draft of this toolbar used a `--bg3`-to-
    `--text-dim` background swap instead, specifically to keep an inert-but-active icon's amber
    fully legible — see the next point's note on the resulting trade-off.)
  - **Active** (is the fact this control represents currently true) via **icon color**: white
    (matching `.casualty-btn`'s own icon-color override) when inactive, `--amber-bright` when
    `.is-active` — bare `--amber` was tried first and confirmed, by direct testing against a real
    build, to read poorly (~2.8:1 contrast) directly on this button's solid `--bg3` fill; see
    `--amber-bright`'s own Colour Palette entry above. Reset Casualties has no independent "active"
    fact of its own — its icon is amber-bright exactly when it's actionable (there's something to
    reset). Trade-off of reusing the plain opacity fade above: an inert-but-active control (the
    multi-model Half Strength case, at or below half-strength) renders its amber-bright icon at the
    same faded `0.4` opacity as any other disabled control — a deliberate choice, made after
    hands-on comparison, to match this page's one existing "disabled" look everywhere rather than
    keep a second, toolbar-only vocabulary.

  Reset Casualties' icon is a hand-authored inline SVG skull (`SkullSvgIcon`,
  `fill-rule="evenodd"` carving the eye sockets/nasal cavity as true holes out of one silhouette),
  not the U+1F480 emoji character this button used before `consolidate-unit-toolbar` — the emoji
  ignored `color`/`currentColor` entirely on most platforms (a fixed, full-color glyph), which is
  the actual reason its icon never turned amber before this fix. Shares `.unit-toolbar-svg-icon`
  with the Battle-shock bolt, so both respond to white/`--amber-bright` identically.

  This is also where the page's controls-vs-values amber rule, first established by
  `half-strength-and-battleshock-indicators`, keeps holding: a **control** gets `--amber`(-bright)
  ink directly when active; a **stat value** never does, even when flagged — the Battle-shocked OC
  tile still reuses the same bolt glyph but renders it in the tile's ordinary `.stat-value` color,
  letting `.stat-tile-flagged`'s background (`--amber-tint`) carry that signal instead.

No dedicated responsive breakpoint today — the page is a single centered column, `max-width: 900px`.

---

## Popovers (`rules-glossary-popovers`)

Every ability name and resolvable weapon-keyword chip is a native `popover="auto"` trigger — see
`.claude/domain-model-11e.md`'s "Rules Glossary & Popovers" for the full domain-model/wiring
picture; this section covers only the visual/positioning decisions.

- **Placement**: anchored locally to its own trigger (never centered), via a per-instance
  `anchor-name`/`position-anchor` CSS custom-property pair (`--a{id}`, unique per trigger/popover
  pair), wrapped in `@supports (anchor-name: --a)`. Real device testing (the actual target
  browser, not just `CSS.supports()`) found this genuinely necessary to check, not just a
  theoretical fallback: that browser reports the relevant anchor-positioning properties as
  supported and the authored rule is confirmed matched in the CSSOM, yet the resulting position
  still doesn't resolve near the trigger — a real but immature implementation, not an absent one.
  No polyfill pursued; a popover still opens/shows/dismisses correctly either way (native
  `popover="auto"` light-dismiss, not something positioning affects), so only placement quality
  degrades on a browser like this one, never functionality.
- **Nesting**: a resolved `[BRACKET]` reference inside an open popover's own text becomes a further
  nested trigger/popover pair, using the exact same mechanism recursively — no separate visual
  treatment for a nested vs. top-level popover panel, since the Popover API's own ancestor-aware
  light-dismiss stacking (established via `popovertarget`, independent of the positioning
  properties above) is what makes "parent stays open, tap-between closes only the child, tap-
  outside-both closes both" work with no hand-rolled JS.
- **Title bar** (`restyle-rule-popover-titlebar`): every popover panel (ability, weapon-keyword
  chip, and nested `[BRACKET]` reference alike) opens with a title bar naming its subject —
  `.rule-popover-title`, reusing the exact same trigger content (`triggerHtml`) the panel's own
  trigger button already renders, so an Enhancement's `✦`-prefixed name (see
  `display-resolved-enhancements`) shows in the title too, with no separate plumbing. Styled
  identically to an `.lp-section` summary's own section-title bar (`--bg3-tint` background, white
  text, `600` weight, `0.82rem`, uppercase, `0.02em` letter-spacing) — reused as its own selector
  rather than shared with `.lp-section summary`, since the two sit on structurally different
  elements (an interactive, toggling `<summary>` vs. a plain `<div>`). `position: sticky; top: 0`
  within `.rule-popover`'s own scroll container, so the title stays visible while a long popover's
  body scrolls — matching how the page's own section headers are always visible. Padding that used
  to sit on `.rule-popover` itself moved onto `.rule-popover-text` alone, so the title bar can span
  flush to the panel's own top edge and corners.
