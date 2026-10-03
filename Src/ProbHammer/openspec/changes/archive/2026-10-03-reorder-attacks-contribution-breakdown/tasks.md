## 1. Tests first

- [x] 1.1 Add a WebApplicationFactory render test (alongside `LivePlayFlaggedStatlineRenderingTests`) asserting that, for a weapon with a group-wide Attacks line, the `weapon-attacks-contribution-row group-wide` `<tr>` comes after every `weapon-contribution-row` sharing its `data-weapon-id`; confirm it fails before the change
- [x] 1.2 Add/keep a render assertion that a row-bound line still immediately follows the base row it belongs to

## 2. Markup order (D1)

- [x] 2.1 In `_UnitBlock.cshtml` Ranged section, move the `GroupWideAttacksLines` loop after the `Breakdown` loop (legend row stays first)
- [x] 2.2 Same for the Melee section
- [x] 2.3 Update the stale "render it once, above the breakdown" comment in `LivePlayModel` (`ResolveGroupWideAttacksPairs`)

## 3. Indent (D2)

- [x] 3.1 In `site.css`, delete the dead `.weapon-contribution-label { padding-left: 1.75rem; }` rule
- [x] 3.2 Drop the group-wide label's padding override so it inherits the base rows' td padding; set row-bound to one `1rem` step deeper (`1.3rem`); correct the comment above them

## 4. Verify

- [x] 4.1 Run the full test suite
- [x] 4.2 Rebuild (`docker compose up --build`), import `data/gw-app-export-templars-latest.txt`, and at the 667×315 landscape viewport expand Astartes Chainsword and Close combat weapon: order is legend, base rows, group-wide line; group-wide pill's left edge equals the base rows' text left edge (check with `getBoundingClientRect`); the A total is unchanged (35 / 20) after a selection-filter toggle round-trip
- [x] 4.3 Find a row-bound/partial-reach case in a real or synthetic roster and confirm it still nests under its base row

## 5. Docs and archive

- [x] 5.1 Update `.claude/design-tokens.md` with the breakdown order (base rows, then group-wide line) and the indent reference (base rows at td padding, row-bound one step deeper)
- [x] 5.2 After archive, retitle the main-spec scenario "A group-wide ability contribution renders once, above the breakdown" to "…once, after the breakdown's base rows" in `openspec/specs/live-play-view/spec.md` by hand
