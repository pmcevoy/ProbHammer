## 1. Prerequisite

- [ ] 1.1 Confirm `compress-loadout-labels` is implemented and archived; reconcile this change's
      `specs/live-play-view/spec.md` and `design.md` against the resulting main spec (its
      compressed-label wording) via `openspec-update-change` before proceeding further.

## 2. Domain data

- [ ] 2.1 Add a `LoadoutIndex` (or equivalent) field to `WeaponContribution`, set in
      `AttachedUnitAggregator.BuildWeapons` to the contributing `ModelLine`'s position within its
      statline's `Loadouts` list; use a sentinel for statlines with only one `ModelLine`.

## 3. View-model plumbing

- [ ] 3.1 Thread `(ComponentName, StatlineName, LoadoutIndex)` identity onto rendered statline
      header/loadout elements and contribution-breakdown rows in `LivePlay.cshtml` as `data-*`
      attributes.
- [ ] 3.2 Ensure the server still renders ungrouped, per-`LoadoutIndex` contribution rows (hidden
      by default when their group would collapse under today's uniform-`PerModelAttacks` rule), so
      client-side selection changes only ever show/hide/re-merge pre-rendered rows.

## 4. Statline section UI

- [ ] 4.1 Make each statline-header line and loadout `<li>` a single click/tap target.
- [ ] 4.2 Render tri-state visual indicator (selected / deselected / partial) per statline; simple
      checked/unchecked indicator per loadout.
- [ ] 4.3 Implement click handling in `live-play.js`: loadout toggles its own selection only;
      statline header toggles all its loadouts per the fully-selected/otherwise rule from design.md.
- [ ] 4.4 Render the Statline section's live "N/M selected" badge, updated on every toggle.

## 5. Weapon sections filtering

- [ ] 5.1 Implement per-row recompute in `live-play.js`: sum currently-selected contribution
      subtotals per weapon row; hide the row when the selected sum has zero contributing entries.
- [ ] 5.2 Implement breakdown-group re-merge/split on the client: collapse when visible
      contributions agree, split when they disagree (by `PerModelAttacks`, already server-computed
      per row; by selection, computed client-side).
- [ ] 5.3 Render Ranged/Melee section "filtered" badges, computed per-section from whether any row
      in that section changed under the current selection.

## 6. Clear filter control

- [ ] 6.1 Render a per-unit-block "Clear filter" control, visible only while that block's selection
      isn't fully selected; wire it to reset the block's deselected-keys state and re-run the
      recompute/badge pass.

## 7. Tests

- [ ] 7.1 Unit test: `WeaponContribution.LoadoutIndex` is set correctly for the real Crusader Squad
      fixture (two Initiate loadouts get distinct indices).
- [ ] 7.2 Unit test: contribution-breakdown grouping/splitting logic (wherever it's tested
      server-side, e.g. `LivePlayModelTests.cs`) still behaves identically under the default
      (nothing filtered) state — no regression to `add-live-play-weapon-provenance`'s existing
      coverage.
- [ ] 7.3 Manual/scripted check of `live-play.js`'s recompute logic against the worked Crusader
      Squad Bolt Pistol example (10 → 8 total, breakdown splits to Neophyte/Astartes-chainsword-
      Initiate/Crusade-Ancient) — structure the script so this logic is isolable even without a JS
      test runner in this project today.

## 8. Verification

- [ ] 8.1 Run the full test suite; confirm no regressions.
- [ ] 8.2 Visually verify via `firefox-devtools-mcp` against the real example army: default
      (all-selected) rendering unchanged; deselecting a whole statline hides its exclusive weapons;
      deselecting one loadout within a multi-loadout statline recomputes and splits only the
      affected weapon rows (Crusader Squad Bolt Pistol case); Clear filter restores default state;
      Statline/Ranged/Melee badges appear and disappear correctly.
