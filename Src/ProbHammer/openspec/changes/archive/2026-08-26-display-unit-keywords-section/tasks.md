## 1. Markup

- [x] 1.1 In `_UnitBlock.cshtml`, replace the trailing `@if (unit.Keywords.Any()) { <div class="keywords">...</div> }` block (currently after the Melee Weapons `<details>`) with a new `<details class="lp-section" data-section="keywords">` block, guarded by the same `@if (unit.Keywords.Any())`, matching the Statline/Ranged Weapons/Melee Weapons `<details>`/`<summary>` markup shape exactly (including the `<span class="section-title">Keywords</span>` and the existing `filter-flag filtered-badge` summary markup pattern used by Ranged/Melee, omitted if not applicable to this section).
- [x] 1.2 Inside the new section, render `unit.Keywords` as `<div class="weapon-tags">` containing one `<span class="weapon-tag">@keyword</span>` per keyword, in the order the view model already provides (already alphabetically sorted).

## 2. Styling

- [x] 2.1 Delete the now-unused `.keywords` rule (`site.css` ~line 183) and `.live-play-page .keywords` rule (~line 1245).
- [x] 2.2 Check `.unit-block > .keywords` (~line 799, margin spacing for the old div) and `.unit-block > .lp-section:last-of-type` (~line 802) — update or remove the first as needed now that Keywords is an `.lp-section` like its siblings, and confirm the `:last-of-type` margin rule still applies correctly with Keywords as the new last section.

## 3. Verification

- [x] 3.1 Run the existing test suite (`dotnet test`) to confirm no regression.
- [x] 3.2 Start the dev server, open `/LivePlay` with a real imported roster, and visually confirm: the Keywords section appears after Melee Weapons, collapsed by default; each keyword renders as its own pill matching the weapon-tag chip look; a unit with no keywords renders no Keywords section at all; expanding/collapsing Keywords doesn't affect other sections' state.
