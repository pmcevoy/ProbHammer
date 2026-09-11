## 1. Domain: surface a caveated match without applying it

- [x] 1.1 Add `WeaponContribution.UnresolvedAbilities: IReadOnlyList<Ability> = []` and
  `AggregateWeaponEntry.UnresolvedAbilities: IReadOnlyList<Ability> = []` (design.md D1). Update any
  existing positional (non-named-argument) construction of these records found by the compiler.
- [x] 1.2 Add a caveated-branch counterpart to `TryGetWeaponEffectEntry` in
  `AttachedUnitAggregator.cs` — same normalized-Text baseline lookup, same `Target: SelfRuleTarget or
  AttachedUnitRuleTarget` filter, but admitting `IsCaveated: true` entries instead of excluding them.
- [x] 1.3 Add a per-contribution helper (sibling to `ResolveContributionProfile`) that, for a given
  bearer + weapon profile, finds every present ability whose normalized text matches a caveated
  baseline entry (task 1.2) with a matching `IsBearerOf` and `WeaponSelectorMatches` result (reusing
  both helpers unchanged, per design.md D5), excluding an Attacks-only effect exactly as the
  non-caveated branch already does, and returns the distinct source `Ability` list.
- [x] 1.4 Wire this helper into `BuildWeapons`'s existing per-contribution loop: set each
  `WeaponContribution.UnresolvedAbilities` from task 1.3's result, computed independently of (and
  alongside) the existing profile-mutation call — a contribution can carry both a mutation from one
  ability and an unresolved reference from a different ability (spec scenario).
- [x] 1.5 Compute `AggregateWeaponEntry.UnresolvedAbilities` in `BuildWeapons`'s grouping step the
  same way `Name` is already computed: order-preserving `Distinct()` by `(Ability.Name, Ability.Text)`
  over every contribution's own `UnresolvedAbilities`, first-encountered order.
- [x] 1.6 Unit tests (`AttachedUnitAggregatorTests` or a new sibling file, mirroring
  `WeaponCharacteristicEffectRosterTests`' own fixture style): a caveated match surfaces an
  unresolved reference without mutating the profile (spec scenario); an unresolved reference and an
  applied mutation coexist on one contribution from two different matched abilities (spec scenario);
  a group's `UnresolvedAbilities` aggregates across its contributions in first-encountered order; a
  contribution with no caveated match reports an empty `UnresolvedAbilities` (regression case).

## 2. View layer: shared marker registry across Statline and Weapons

- [x] 2.1 Widen `LivePlayModel.AssignFlagMarkers` (design.md D2) to also accept the unit's ordered
  Ranged and Melee weapon-row lists, assigning markers in one pass: Statline runs (existing order,
  unchanged) first, then Ranged weapon entries in rendered order, then Melee weapon entries in
  rendered order. Within a weapon entry, check `S` → `AP` → `D`'s own `ContributingAbilities` in that
  order for a resolved-value marker, then the entry's own first `UnresolvedAbilities` entry for a
  name marker. Reuse the existing `markerBySource` dictionary unchanged — a source already assigned a
  marker for a Statline tile keeps that same marker when it also flags a weapon value.
- [x] 2.2 Extend `WeaponRowViewModel` (or add a new sibling view model, whichever keeps
  `_UnitBlock.cshtml`'s existing binding shape simplest) with: a per-characteristic (S/AP/D)
  resolved-value marker string (nullable), a name-marker string (nullable) for an unresolved
  reference, and the actual source `Ability` each marker needs for its popover trigger.
- [x] 2.3 Update `BuildUnitBlock`'s `orderedWeapons` projection to call the widened
  `AssignFlagMarkers` (task 2.1) and populate task 2.2's new fields.
- [x] 2.4 Unit tests (`LivePlayModelTests`): a source ability flagging both a Statline tile and a
  weapon value in the same unit block gets one shared marker (spec scenario); a weapon entry with an
  unresolved reference gets a name marker distinct from any value-cell marker; marker assignment
  order matches the fixed Statline-then-Ranged-then-Melee sequence (regression-shape test, asserting
  which source gets `*` vs `**` when both a Statline and a weapon source are present).

## 3. View layer: per-entry breakdown-trigger gating

- [x] 3.1 Change `BuildUnitBlock`'s `showsBreakdownTrigger` from one unit-wide bool to a per-entry
  computation (design.md D4): `true` when the existing `totalModelLines > 1` condition holds, OR the
  entry's own `Profile.S/Ap/D` has a non-empty `ContributingAbilities`, OR the entry's own
  `UnresolvedAbilities` is non-empty.
- [x] 3.2 Unit tests: a single-`ModelLine` unit's unflagged weapon entry renders no trigger
  (regression, existing spec scenario); a single-`ModelLine` unit's flagged weapon entry (resolved or
  unresolved) still renders a trigger while a sibling unflagged entry on the same unit does not (new
  spec scenario).

## 4. Template: render the markers and legend

- [x] 4.1 In `_UnitBlock.cshtml`'s Ranged/Melee weapon tables: append the entry's name-marker (task
  2.2) to its rendered name/breakdown-trigger text, mirroring the Statline `.stat-label` marker
  convention (`.claude/design-tokens.md`'s "Flagged Statline Legend").
  Apply `--amber-tint` styling plus the value's own marker to each of the S/AP/D value cells that
  carry a resolved marker, mirroring the existing `.stat-tile-flagged` convention.
- [x] 4.2 Render a legend line for each distinct marker present within the unit block's weapon
  section(s), reusing `BuildRulePopover`/`ability-name-line` exactly as the Statline legend and every
  other ability-name popover on the page does — placed within (or immediately below) the
  contribution breakdown of an affected entry, per design.md's "reachable from that entry's
  contribution breakdown" requirement, not as a separate always-visible block.
- [x] 4.3 Manual verification against a running `docker compose up` instance (`firefox-devtools`
  MCP): imported the real captured `data/gw-app-export.txt` (Black Templars) via `/Import` — redirects
  to `/LivePlay` with no crash, 7 unit blocks render. `document.querySelectorAll('.weapon-value-
  flagged, .weapon-flag-legend-row').length === 0` (correct — no baseline entry matches this roster),
  and a sampled `<tr>`'s raw HTML confirms every S/AP/D `<td>` still renders its plain unmarked value
  (`<td class="">4</td>`) with zero visual change from before this change. Clicked a `weapon-name-
  toggle` button directly (Crusader Squad's Bolt pistol) and confirmed its breakdown rows still
  correctly toggle `hidden` — the `live-play.js` selector widening (task 4.2) doesn't affect the
  existing toggle behavior. **No real, currently-bundled GW-app-format export exists in-repo for a
  roster that exercises an actual flagged marker** (the only such export would need Adepta Sororitas'
  Ministorum Priest/Zealot, and fabricating GW-app export text risks testing an invented format rather
  than real user input) — that specific case is instead verified deterministically through the real
  BSData pipeline in task 5.1, which reaches the exact same `LivePlayModel.BuildUnitBlock` code path
  `_UnitBlock.cshtml` calls.

  **Real bug found post-ship, 2026-09-11 (user's own live test, not caught by this task's own
  verification)**: the user manually edited Vexilla's baseline entry to carry a real weapon-
  characteristic Effect and imported a real Custodes roster with several weapon entries. Every
  *second* flagged S/AP/D value cell rendered its marker text ("6\*"/"7\*") but not its
  `--amber-tint` background — only cells on a non-`weapon-row-alt` (unstriped) row got the tint.
  Root cause: `.live-play-page .weapon-table tbody tr.weapon-row-alt td` (the existing zebra-striping
  rule, specificity `(0,3,3)`) beats `.live-play-page .weapon-table td.weapon-value-flagged`
  (`(0,3,1)`) regardless of source order, so every striped row's own `--bg` fill silently overrode the
  marker's amber tint. This exact interaction was invisible to this task's own verification because
  the deterministic `LivePlayModel`-level checks (5.1/5.2) only assert view-model marker values, never
  render real HTML/CSS, and the live browser check (above) used a roster with zero flagged rows at
  all - neither exercised two-or-more flagged rows spanning both stripe parities. Fixed by adding a
  second, higher-specificity selector (`tbody tr.weapon-row-alt td.weapon-value-flagged`, `(0,4,3)`)
  matching the striping rule's own structure, guaranteed to win regardless of order (`site.css`).
  Verified live: injected a synthetic `tr.weapon-row-alt > td.weapon-value-flagged` into a running
  `docker compose` instance and confirmed `getComputedStyle` now resolves to the same
  `--amber-tint` color on both a striped and an unstriped flagged cell. Full suite re-run: 663 total,
  649 passed, 0 failed, 14 skipped - no regressions.

## 5. Real-corpus verification

- [x] 5.1 Re-run the real-corpus pipeline test from `resolve-weapon-characteristic-effects` (Adepta
  Sororitas' Ministorum Priest carrying Zealot) and confirm its weapon entry now reports a non-empty
  `UnresolvedAbilities` naming Zealot (a real, currently-visible result this change newly produces,
  unlike Phase 3's own "no visible result yet" finding). **Done in
  `WeaponCharacteristicEffectRealCorpusTests.cs`**: extended the existing
  `MinistorumPriestWithZealot_BuildsWithoutCrashing_AndLeavesItsWeaponUnmutated` test with a
  `weapon.UnresolvedAbilities.Should().ContainSingle(a => a.Name == "Zealot")` assertion — passing.
- [x] 5.2 Run the app locally (`docker compose up`) and confirm that datasheet's weapon table on
  `/LivePlay` now shows a name marker on the affected weapon entry, with a legend naming Zealot as an
  interactive popover trigger — the first real, visible weapon-characteristic-effect result this
  project has produced end-to-end. **Verified deterministically instead of via a live screenshot**
  (see task 4.3's note on why no real Adepta Sororitas GW-app export exists to drive this through the
  browser): added
  `MinistorumPriestWithZealot_RendersANameMarkerAndLegendNamingZealot`, which runs the exact real
  pipeline (`BsdataFactionResolver` → `ResolvedBsdataCatalogue` → `ArmyRosterEnricher.Enrich` →
  `AttachedUnitAggregator.Build`) through to `LivePlayModel.BuildUnitBlock` — the same method
  `_UnitBlock.cshtml` calls — and asserts `weaponRow.NameMarker == "*"` and `weaponRow.FlagLegend`
  names Zealot. Passing.
- [x] 5.3 Run the full test suite (`dotnet test` or the Rider MCP equivalent) and confirm no
  unrelated regression. **662 total, 648 passed, 0 failed, 14 skipped (full-corpus scan tests,
  filtered by default, unrelated to this change) — no regressions.**
