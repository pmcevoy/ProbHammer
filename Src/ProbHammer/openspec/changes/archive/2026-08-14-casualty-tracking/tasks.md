## 1. Domain: Two-Directional Remaining Count

- [x] 1.1 Add `ModelLine.SetRemainingCount(int value)`, clamped to `[0, Count]`
- [x] 1.2 Reimplement `RemoveCasualties(int count)` as `SetRemainingCount(RemainingCount - count)`,
      confirming existing `ModelLineTests.cs` cases still pass unchanged
- [x] 1.3 Add tests: increasing remaining count, clamping at the original `Count`, and a
      decrease-then-increase round trip returning to the starting value
- [x] 1.4 Change `AttachedUnitAggregator.BuildStatlines`' drop condition from
      `!lines.Any(ml => ml.RemainingCount > 0)` to `lines.Count == 0` (see design.md - Decisions),
      so a statline entry stays listed at 0/N once it's been referenced, and is only ever omitted
      when the roster never took it at all
- [x] 1.5 Update `AttachedUnitAggregatorTests.cs`: replace the old "dropped once dead" assertion
      with one asserting the entry persists at 0/N, and add a case confirming a never-referenced
      statline name is still omitted

## 2. Page: Extract Reusable Unit Block Rendering

- [x] 2.1 Extract `LivePlay.cshtml`'s per-unit `<section class="unit-block">` markup into a Razor
      partial (e.g. `_UnitBlock.cshtml`) taking a `UnitBlockViewModel`
- [x] 2.2 Update `LivePlay.cshtml`'s existing `@for` loop to render that partial per unit,
      confirming the page's rendered output is unchanged
- [x] 2.3 Visually verify via `firefox-devtools-mcp` that the extraction is a pure refactor (no
      visible difference on `/LivePlay`)
- [x] 2.4 Add an `IsFullyDead` computed flag to `StatlineBlockViewModel` (`Entries.All(e =>
      e.RemainingCount == 0)`, see design.md - Decisions)
- [x] 2.5 Render `.run-collapsed` (and a `data-dead="true"` marker for the client script to read)
      directly in `_UnitBlock.cshtml`'s initial markup when `IsFullyDead` is true, alongside the
      existing purely-client-driven collapse for the fully-deselected case; extend the same
      collapse to row-bound and component-wide ability cells per "Statline Ability Column
      Rendering"'s updated wording

## 3. Server: Casualty Coordinate and Roster Rebuild

- [x] 3.1 Add a `UnitIndex` qualifier alongside the existing `(ComponentName, StatlineName,
      LoadoutIndex)` coordinate (see design.md - Decisions) usable both by client-side JS and the
      new endpoint
- [x] 3.2 Add a roster-rebuild step in `LivePlayModel` (or a supporting service) that takes a
      pristine `Examples.View.MyArmy()` build plus a batch of `(coordinate, remainingCount)`
      adjustments, resolves each coordinate to its `ModelLine`, and calls `SetRemainingCount`
      before running `AttachedUnitAggregator`
- [x] 3.3 Add tests for coordinate resolution and rebuild, including an out-of-range adjustment
      (should clamp, not throw) and an adjustment for a unit/statline/loadout that doesn't exist in
      the current roster shape (should be ignored, not throw — defensive against stale
      `localStorage` from a previous army shape)

## 4. Server: Casualty Sync Endpoint

- [x] 4.1 Add a POST endpoint accepting a batch of `(coordinate, remainingCount)` adjustments
- [x] 4.2 Rebuild the roster (task 3.2), determine which `UnitIndex`es actually changed relative to
      pristine, and render each changed unit's `_UnitBlock.cshtml` partial to a string
- [x] 4.3 Return the rendered fragments keyed by `UnitIndex` (empty batch in → empty response out)
- [x] 4.4 Add an integration-style test posting a small adjustment batch and asserting the returned
      fragment(s) reflect the expected remaining counts

## 5. Client: Casualty Controls

- [x] 5.1 Add a decrease/increase control pair to each single-loadout statline entry's header line,
      and to each loadout line of a multi-loadout entry (never to a multi-loadout entry's own
      header — see design.md), in `LivePlay.cshtml` (via `_UnitBlock.cshtml`), carrying the
      coordinate as `data-*` attributes; disable each button at its own bound (0 for decrease,
      initial count for increase)
- [x] 5.2 Wire click handlers in `live-play.js`: call `event.stopPropagation()` first (so the tap
      doesn't also fire the row's selection-toggle listener), compute the new clamped absolute
      remaining count client-side from the currently-rendered value, update that one coordinate in
      the in-memory `localStorage` map (see task 6.1), POST the *entire* current map (not just the
      one changed entry — see design.md's "every request carries the full current map" decision,
      added after the stateless-server bug this would otherwise cause), and on response swap the
      returned fragment(s) into the corresponding `.unit-block`(s)
- [x] 5.3 Consolidate `initUnitSelection` and the page-wide `.weapon-name-toggle` listener wiring
      into one `initUnitBlock(unitEl)` function (see design.md - Decisions), called per unit block
      at `DOMContentLoaded` and again on the swapped-in unit block after every casualty-triggered
      swap, leaving every other unit block's selection state and provenance-toggle listeners
      untouched
- [x] 5.4 Verify provenance breakdown expand/collapse (`add-live-play-weapon-provenance`) still
      works on a unit after a casualty has been marked on it
- [x] 5.5 Update `live-play.js`'s `updateRunCollapse()` to OR its deselection-based result with each
      run's server-rendered `data-dead` marker rather than resetting `.run-collapsed` from scratch,
      so reselecting a fully-dead run's entry cannot un-collapse it (see design.md - Decisions and
      Risks)

## 6. Client: `localStorage` Persistence

- [x] 6.1 On a successful casualty adjustment, write the coordinate → remaining-count pair into a
      `localStorage`-backed map
- [x] 6.2 On `DOMContentLoaded`, read the stored map, POST it as a batch before/alongside the rest
      of `live-play.js`'s existing init work, and swap in any returned fragments
- [x] 6.3 Confirm a browser with no stored map sends an empty batch and leaves the pristine
      GET-rendered page untouched

## 7. Verification

- [x] 7.1 Full test suite green (276/276)
- [x] 7.2 Manual `firefox-devtools-mcp` pass against the real example army: marked High Marshal
      Helbrecht as a casualty (1/1 → 0/1), confirmed the run collapsed to header-only and its
      component-wide "LEADER" ability cell collapsed with it; reloaded the page and confirmed the
      marked state (and the collapsed run) persisted without re-marking; reverted via the "+"
      control and confirmed the run and its ability cell expanded again; confirmed the Statline
      `<details>` section stayed open across the casualty-triggered swap (a real bug caught live —
      see design.md, fixed via swapUnitBlock preserving open <details> sections)
- [x] 7.3 Additional pass exercising the collapse/revert and weapon-breakdown interaction: reduced
      the Crusader Squad's "Astartes chainsword" Initiate loadout from 3/3 to 0/3 across three
      casualty taps (each triggering its own swap+re-render); confirmed the parent "Initiate" entry
      correctly showed the summed 2/5; confirmed the Bolt pistol weapon row's total dropped from
      10 to 7 and its breakdown showed no row at all for the dead loadout (not a 0× row), while the
      merged "Initiate" breakdown row correctly demoted to a single raw "Initiate w/ Power fist
      (2×1)" row now that only one contribution remained in that group; confirmed the
      weapon-name-toggle provenance expand/collapse still worked correctly after the swap
      (re-wired via initUnitBlock)
- [x] 7.4 Update `.claude/domain-model-11e.md` and `PROGRESS.md` per this project's
      documentation-maintenance convention
