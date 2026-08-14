## 1. Domain data

- [x] 1.1 Add `Weapons: IReadOnlyList<string>` to `ModelLineLoadout`, populated in
      `AttachedUnitAggregator.BuildStatlines` from the same `ModelLine.Weapons` already used to
      build `WeaponsLabel`. Leave `WeaponsLabel` unchanged.

## 2. Compression logic

- [x] 2.1 Add a compression function in `LivePlayModel.cs` (e.g.
      `CompressLoadoutLabels(IReadOnlyList<ModelLineLoadout>) -> IReadOnlyList<string>`) implementing
      the multiset-intersection-then-subtract algorithm from design.md.
- [x] 2.2 Wire it into `GroupStatlines`/`BuildUnitBlock` (or wherever `StatlineBlockViewModel`'s
      entries are prepared) so each rendered loadout carries its compressed label instead of the
      raw `WeaponsLabel`, only when the entry has more than one loadout (matching the existing
      `Loadouts.Count > 1` guard already in `LivePlay.cshtml`).

## 3. Rendering

- [x] 3.1 Update `LivePlay.cshtml`'s loadout `<li>` to render the compressed label instead of
      `loadout.WeaponsLabel`.

## 4. Tests

- [x] 4.1 Unit test: real Crusader Squad "Initiate" case (Power fist vs Astartes chainsword loadouts
      sharing Bolt pistol + Heavy Bolt pistol) compresses to `"Power fist"` / `"Astartes
      chainsword"`.
- [x] 4.2 Unit test: shared weapons at different list positions in each loadout are still excluded
      from both labels (proves multiset intersection, not prefix-strip).
- [x] 4.3 Unit test: a loadout carrying two copies of a weapon its sibling carries only one of keeps
      one copy in its compressed label (proves multiset, not set, subtraction).
- [x] 4.4 Unit test: a statline entry with exactly one loadout is unaffected (no compression
      attempted, existing no-breakdown-row behavior unchanged).

## 5. Verification

- [x] 5.1 Run the full test suite; confirm no regressions. (257/257 passing)
- [x] 5.2 Visually verify via `firefox-devtools-mcp` against the real example army: Crusader Squad's
      Initiate loadout lines show compressed labels; other units' single-loadout statlines are
      unchanged.
