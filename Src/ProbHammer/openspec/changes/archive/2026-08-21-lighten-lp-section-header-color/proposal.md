## Why

On `/LivePlay`, each unit block's Statline/Ranged Weapons/Melee Weapons `.lp-section` disclosure
bars render in the exact same `--bg3` bottle-green background and white text as the unit-name
`h2` above them. That gives the section bars the same visual priority as the unit name itself,
even though they're a subordinate level of the hierarchy (indentation/margin is the only cue
today that they're nested under the unit). Confirmed live in the browser against a real imported
army list (`data/gw-app-export.txt`) — screenshotted side by side with two candidate treatments
before writing this proposal.

## What Changes

- Add a new design token, `--bg3-tint` (`color-mix(in srgb, var(--bg3) 55%, white)`), to
  `site.css`'s `:root` block alongside the existing seven tokens in `.claude/design-tokens.md`.
- Change `.lp-section summary`'s `background` from `var(--bg3)` to `var(--bg3-tint)`. Text stays
  white (`#fff`) — contrast against the lighter tint was confirmed visually.
- No other properties of `.lp-section summary` change (font-size, padding, border-radius,
  disclosure arrow, `.filter-flag`/`.casualty-reset-btn` children) — this is a background-color
  swap only, on this one selector.
- Update `.claude/design-tokens.md` to document the new token and its one call site.

Two alternatives were considered and rejected during live comparison (see design.md):
reusing `.casualty-btn:disabled`'s `opacity: 0.4` treatment (reads washed-out/muddy, and fades
the disclosure arrow and any icons inside the bar along with the fill), and a tint mixed toward
`--bg2` instead of white (desaturates toward gray rather than reading as a lighter green).

## Capabilities

No spec-level behavior changes — the section bars' disclosure/filter/casualty-control behavior,
content, and layout are unchanged; only a background color swaps. This is a pure visual/styling
change scoped to `site.css` and its own design-token documentation, so no `specs/` deltas apply
(`skip_specs: true` set in `.openspec.yaml`).

## Impact

- `src/ProbHammer.Web/wwwroot/css/site.css` — new `--bg3-tint` token on `:root`; one property
  change on `.lp-section summary`.
- `.claude/design-tokens.md` — documents the new token per this project's "do not hardcode hex
  values, use semantic CSS custom properties" convention.
- No C#, Razor, or JS changes. No test changes (purely visual).
