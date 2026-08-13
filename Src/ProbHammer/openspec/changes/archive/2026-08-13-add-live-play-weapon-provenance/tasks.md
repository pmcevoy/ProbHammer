## 1. View-model grouping

- [x] 1.1 Add a `WeaponContributionRow(Label, Count, PerModelAttacks, Subtotal)`-shaped view-model
      type (or equivalent) to `LivePlay.cshtml.cs`, alongside the existing `StatlineBlockViewModel`
      pattern.
- [x] 1.2 Implement the grouping function: for an `AggregateWeaponEntry` with
      `Contributions.Count > 1`, group by `(ComponentName, StatlineName)` in first-seen order;
      within a group, collapse to one row when all `PerModelAttacks` are structurally equal
      (summed `Count`, `PerModelAttacks.Scale(summedCount)` subtotal), else emit one row per
      `WeaponContribution`.
- [x] 1.3 Wire the grouping function into `OnGet()`'s existing per-unit view-model construction so
      each rendered `AggregateWeaponEntry` carries its (possibly empty) breakdown rows.

## 2. Razor markup

- [x] 2.1 In `LivePlay.cshtml`'s Ranged and Melee weapon tables, render the weapon-name cell as a
      `<button type="button" class="weapon-name-toggle" data-weapon-id="...">` only when the entry
      has more than one contribution; otherwise render it exactly as today (plain text).
- [x] 2.2 Render breakdown rows as sibling `<tr class="weapon-contribution-row" data-weapon-id="...">`
      elements immediately following the primary row, hidden by default, showing the group/row
      label and Attacks subtotal with all other stat columns blank.
- [x] 2.3 Confirm melee vs. ranged column-count differences (melee has no Range column) are
      respected in the breakdown rows' blank-cell colspan/alignment.

## 3. Client-side toggle

- [x] 3.1 Create `src/ProbHammer.Web/wwwroot/js/live-play.js`: on `.weapon-name-toggle` click,
      toggle visibility of every `[data-weapon-id]` breakdown row matching that button's
      `data-weapon-id`.
- [x] 3.2 Reference the new script from `LivePlay.cshtml`.

## 4. Styling

- [x] 4.1 Add `.weapon-name-toggle` styling (visually indicates it's interactive, e.g. matching
      the `.lp-section summary` disclosure-triangle idiom already used elsewhere on the page).
- [x] 4.2 Add `.weapon-contribution-row` styling — dimmer text, smaller font — scoped under
      `.live-play-page`, distinct enough from primary weapon rows that the two are never confused
      at a glance.

## 5. Tests

- [x] 5.1 Unit tests for the grouping function: uniform-`PerModelAttacks` collapse case (using the
      real `CrusaderSquadUnitWithChainSwords` fixture — `Initiate` 2×Power-fist / 3×Astartes-
      chainsword, both carrying the same `Bolt pistol` entry, expect one collapsed row summing to
      5×1); disagreeing-`PerModelAttacks` fallback case (synthetic data, since no real fixture
      currently produces it); single-contribution entry produces no breakdown rows at all. (Superseded
      by 7.3 — the trigger condition changed, see below; a single contribution now produces one
      breakdown row whenever the unit has more than one `ModelLine` in total.)
- [x] 5.2 `LivePlayModelTests.cs` case confirming `OnGet()` attaches the expected breakdown rows to
      the right `AggregateWeaponEntry` for at least one real multi-contribution weapon in the
      example army. (Bonus finding: the real fixture also exercises a cross-component merge —
      Crusade Ancient's own "Bolt Pistol" is `EqualityKey`-equal to the Crusader Squad's "Bolt
      pistol" and merges into the same row, giving 3 breakdown groups instead of 2.)

## 6. Verification & docs

- [x] 6.1 Visually verify via `firefox-devtools-mcp` against the real example army: a
      multi-contribution weapon (e.g. Crusader Squad's Bolt pistol) shows the toggle, expands to
      the expected collapsed breakdown row(s), and collapses again on a second click; a
      single-contribution weapon shows no toggle. Also verified the Melee table (6-column
      alignment): Astartes chainsword correctly breaks down into only Neophyte (4×4) and
      chainsword-loadout Initiate (3×4) — the Power-fist-loadout Initiates are correctly absent
      since they don't carry that weapon at all, confirming the CCW/Power-fist scenario discussed
      during design needed no special-casing.
- [x] 6.2 Update `PROGRESS.md` with a summary of the implemented change, following the existing
      entry format for prior `/LivePlay` changes.

## 7. Post-implementation refinement (from hands-on layout feedback)

- [x] 7.1 Fix zebra-striping continuity: drive `.weapon-row-alt` from each weapon's own index in
      `RangedWeapons`/`MeleeWeapons` (not `:nth-child`, which counts always-present-but-`[hidden]`
      breakdown rows and shifts every later primary row's parity), and apply the same class to a
      weapon's breakdown rows so an expanded row reads as one continuous block instead of a white
      gap inside a grey one (or vice versa). Also increased `.weapon-contribution-label`'s indent
      for a clearer "this is a nested/expanded section" visual.
- [x] 7.2 Change the breakdown trigger from per-weapon (`Contributions.Count > 1`) to per-unit
      (`totalModelLinesInUnit > 1`, computed once via
      `view.Statlines.Sum(s => Math.Max(s.Loadouts.Count, 1))`): a single contributor is still
      worth naming (e.g. "it was the Sword Brother") as long as the unit has other `ModelLine`s to
      distinguish it from. Only a single-`ModelLine` plain `Unit` (e.g. Impulsor) is now excluded —
      an `AttachedUnit` can never total exactly one `ModelLine` by construction.
- [x] 7.3 Update tests for the new trigger: single-contributor-in-multi-`ModelLine`-unit now
      expects one breakdown row (was: expects empty); added real-fixture coverage for the Pyre
      pistol / Sword Brother single-contributor case and the Impulsor single-`ModelLine`-total
      exclusion case (asserts no weapon on Impulsor ever shows a breakdown).
- [x] 7.4 Update `proposal.md`, `design.md`, and `specs/live-play-view/spec.md` to reflect both
      refinements (trigger condition, shared row-alt striping) discovered during hands-on use.
