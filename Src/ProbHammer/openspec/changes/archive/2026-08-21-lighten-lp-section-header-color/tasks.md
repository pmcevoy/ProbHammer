## 1. Token & Style

- [x] 1.1 Add `--bg3-tint: color-mix(in srgb, var(--bg3) 55%, white);` to the `:root` custom
      property block in `src/ProbHammer.Web/wwwroot/css/site.css`, alongside the existing
      `--bg3`/`--accent`/etc. tokens.
- [x] 1.2 Change `.lp-section summary`'s `background` from `var(--bg3)` to `var(--bg3-tint)`.
      Leave every other property on that rule (`color`, `font-weight`, `font-size`, `padding`,
      `border-radius`, etc.) unchanged.

## 2. Verification

- [x] 2.1 Run the app (`docker compose up`), import a real export (e.g.
      `data/gw-app-export.txt`), and visually confirm on `/LivePlay` that the Statline/Ranged
      Weapons/Melee Weapons bars now read as a lighter, subordinate tier under the unit-name h2,
      while white text stays legible against the new background.
- [x] 2.2 Confirm the `.lp-section[open] summary` state (rounded-top-only border radius) and the
      `.filter-flag`/`.casualty-reset-btn` children inside the bar still render correctly against
      the new background — no other rule references `--bg3` inside `.lp-section` that also needs
      updating.

## 3. Documentation

- [x] 3.1 Update `.claude/design-tokens.md`'s Colour Palette table to add `--bg3-tint` and its
      one call site (`.lp-section summary` background), following the existing `--amber-tint` row
      as the pattern for documenting a derived/tint token.
