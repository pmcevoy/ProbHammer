## 1. Corpus Investigation (resolves design.md's Open Questions before building)

- [x] 1.1 Scan the live BSData clone for every `BsModifier`'s `Field` value; build the closed
      `Field` → Statline/WeaponProfile-characteristic allowlist from what's actually found (not
      guessed). Finding: only 6 Statline scalar fields (M/T/Sv/W/Ld/Oc) ever appear in a real,
      directly-modeled `entry.Modifiers`/`group.Modifiers` array. InSv (`55a7-5b54-c60d-11dc`) has
      real tier-1 occurrences too (e.g. "Consecrating Aura") but is deliberately excluded — its own
      dedicated resolution path already covers it, a different CharacteristicView shape. Every
      WeaponProfile-characteristic id only ever appears inside a Crusade-only `modifierGroups` block
      (unmapped/unread by this loader already) — zero real reachable occurrences, so no
      WeaponProfile-field classification/application path was built (would be untested, unreachable
      code). See `CharacteristicFieldIds`' own doc comment in BsdataDatasheetMapper.cs.
- [x] 1.2 Confirm whether a genuine tier-2 (condition scoped to the modifier's own granting entry)
      example exists anywhere in the real corpus; document the finding either way — an empty tier-2
      bucket is an acceptable outcome. Finding: empty in practice. The one self-referencing-condition
      example found (Astra Militarum's "Deficiency" Battle Scar) uses `scope: "roster"` with a
      roster-wide `affects` path and is Crusade-mode-only content — correctly excluded by requiring
      a recognized tier-2 condition's own `scope` be "self" or "parent" only. See
      `IsTier1OrTier2`'s own doc comment.
- [x] 1.3 For every real modifier that would classify tier 1 or tier 2, confirm its granting entry's
      `Name` resolves to a `WeaponProfile` or `Ability` on its own `Datasheet` (the presence-check
      assumption in design.md); document any real counterexample (a bare stat-only entry with
      neither) as a known, accepted gap rather than working around it speculatively. Both risks
      design.md anticipated are real: (a) some candidates' EntryName has no resolvable Ability
      anywhere (e.g. "Augmetic Legs" — its only nested profile is named "Leader", itself globally
      excluded; "Allarus Custodian (Vexilla & Misericordia)" — a squad-slot entry with no own-named
      profile) — fails closed, an accepted gap, never applies. (b) A genuine OVERLAP with the
      existing hand-authored `StatlineFlagRuleCatalogue` exists: Adeptus Custodes' "Vexilla" wargear
      entry carries BOTH a tier-1 structural Oc-increment modifier AND is matched by
      `VexillaStatlineFlagRule` (which fully resolves Oc, not caveating it) — this directly justified
      and confirmed the need for task 4's "skip a field already touched" guard (§4.3).

## 2. Catalogue-Level Classification (`catalogue-json-ingestion`)

- [x] 2.1 Add the `Field` → characteristic allowlist from 1.1
- [x] 2.2 Implement the closed-world condition-tier classifier: tier 1 (no
      `Conditions`/`ConditionGroups`), tier 2 (every condition scoped to the modifier's own granting
      entry) are classified; every other condition shape, and any unrecognized `Field`, is left
      unclassified
- [x] 2.3 Wire the classifier into `BsdataDatasheetMapper`'s existing entry walk, reading
      `entry.Modifiers` alongside `IsGameModeGated`'s existing read of the same data
- [x] 2.4 Unit tests: a no-condition modifier classifies; a same-entry-condition modifier classifies
      (if 1.2 found a real example — otherwise a hand-built fixture); a sibling-entry condition is
      left unclassified; an unrecognized condition shape is left unclassified; an unrecognized
      `Field` is left unclassified. `CharacteristicModifierClassificationTests.cs`, 8 tests, hand-
      built `BsSelectionEntry`s (no fixture JSON needed).

## 3. Datasheet On-Demand Exposure (`datasheet-catalogue`)

- [x] 3.1 Add a `CharacteristicModifierCandidate` type (granting entry name, targeted
      characteristic, modifier value) per design.md — shape informed by 1.3's finding on whether a
      candidate needs to carry a reference usable with no backing `Ability`. Finding: no special
      shape needed — presence-checking always requires finding a real, already-resolved `Ability` by
      name (via `AggregateAbilityEntry`) before a candidate can ever apply, so `ContributingAbilities`
      is only ever populated from a genuinely-found `Ability`; a candidate with no resolvable backing
      simply never matches (§1.3's accepted gap), by construction, not by special-casing.
- [x] 3.2 Add an on-demand accessor on `Datasheet` for its classified candidates, populated from the
      mapper's classification step; `Datasheet`'s own `Statline`/`WeaponProfile` fields are never
      mutated by this
- [x] 3.3 Unit tests: a candidate is retrievable with the Datasheet's base characteristic values
      unchanged; a Datasheet with no classifiable modifiers exposes none. Covered by
      `Classification_never_mutates_the_datasheets_own_base_statline_values` and
      `A_datasheet_with_no_classifiable_modifiers_exposes_none` in
      `CharacteristicModifierClassificationTests.cs`.

## 4. Presence-Gated Application (`characteristic-modifier-caveats`)

- [x] 4.1 Add a new `AttachedUnitAggregator.Build` step (separate from `ApplyStatlineFlagRules`, per
      design.md) that, for each present component, reads its `Datasheet`'s classified candidates
- [x] 4.2 Presence-check each candidate: its `EntryName` found among that component's present
      model-lines' `Weapons`, or among that component's present `AggregateAbilityEntry` names.
      Implemented via `AggregateAbilityEntry` names only — §1.3/§3.1 confirmed every real
      Statline-field candidate that ever resolves to anything resolves to an Ability, never a bare
      weapon name with no ability; the weapon-name branch would be untested/unreachable given this
      change's Statline-only field scope (§1.1), so was not built (avoids premature behavior on an
      unexercised path — a future WeaponProfile-field extension would add it then).
- [x] 4.3 On a present match, mutate the targeted `Statline`/`WeaponProfile` field's
      `CharacteristicView` to `IsCaveated: true` with `ContributingAbilities` referencing the
      candidate's source and `DerivedValue` absent — no value computation. Additionally skips
      mutation when the field is already touched (`ContributingAbilities.Count > 0`) — required by
      the real "Vexilla" overlap found in §1.3.
- [x] 4.4 Confirm liveness: re-evaluated on every `Build` call, no independent caching; a caveat
      disappears once its bearer's remaining count reaches zero or its owning component is no longer
      present
- [x] 4.5 Confirm no cross-unit leak: a candidate only ever applies to the unit that itself carries
      its granting selection
- [x] 4.6 Unit tests: presence produces a caveat, absence does not; a casualty removes the caveat on
      the next build; two units in a roster where only one carries the granting selection — only
      that one is caveated; a data-derived candidate and a hand-authored `StatlineFlagRule` match
      coexist correctly on the same run. `CharacteristicModifierApplicationTests.cs`, 5 tests — the
      coexistence test directly exercises the real "Vexilla" overlap found in §1.3, confirming the
      resolved (non-caveated) value survives untouched.

## 5. Full-Corpus Scan

- [x] 5.1 Add a new explicit-only `[Fact(Explicit = true)]` `CorpusScan` test: every real
      `BsModifier` in the live clone is either correctly classified tier 1/2 per the 1.1/1.2
      allowlist, or deliberately left unclassified with a documented reason — same bidirectional
      `AllowlistCheck.AssertClean` pattern (fails on an unmatched result AND on an allowlist entry
      that matches nothing) as the existing scans. `CharacteristicModifierClassificationScanTests.cs`
      + `CharacteristicModifierClassificationAllowlist.cs` — uses the real, public `BuildDatasheet`
      as the classification oracle (each interesting entry re-rooted as its own Datasheet), so the
      scan can never share an undetected bug with the classifier it's checking.
- [x] 5.2 Run the scan against the live clone; fix the classifier or allowlist any genuine surprise
      found, never silently accept an unexplained result. Ran clean on the first attempt (via
      `ProbHammer.Tests.exe -explicit only -class ...`, the documented MTP-passthrough workaround) —
      every unclassified real occurrence matched one of the two allowlist entries, confirming the
      §1.1/§1.2/§1.3 corpus investigation's findings.

## 6. Live Verification

- [x] 6.1 `dotnet run` against a real captured export that exercises at least one classified tier-1
      candidate; confirm `/LivePlay` renders the caveated tile (footnote marker + legend, no
      resolved value shown) with no rendering-layer code change, per design.md's Goals. No existing
      captured export in `data/` exercised a classified candidate, so a minimal, real-data-faithful
      export was hand-built from `data/gw-android-export-custodes.txt`'s own format, swapping its
      "Martial Philosopher" Enhancement for "Auric Mantle" (Blade Champion/Shield-Captain only, `Add
      2 to the bearer's Wounds characteristic.` — a real tier-1 candidate confirmed in §1.1/§1.3).
      **First attempt used the wrong Detachment** (kept "Auric Champions" from the source export) —
      "Auric Mantle" actually belongs to Adeptus Custodes' "Shield Host" Detachment pool (each
      Enhancement entry's own BSData `comment` field names its owning Detachment; "Martial
      Philosopher"'s is "Auric Champions", "Auric Mantle"'s is "Shield Host" — confirmed by resolving
      the entry's own gating condition ids directly: `6bfb-3b18-fd53-9ff1`/`691e-fed6-a4b4-c66` =
      Shield-Captain/Blade Champion, `cac3-71d1-ea4b-795d` = Crusade Force,
      `70eb-2978-3ad5-5901` = Shield Host). ProbHammer's own importer accepted the illegal
      Detachment/Enhancement combination anyway with no error, since Enhancement resolution
      deliberately performs no eligibility validation (see this file's own "Army List Import
      Pipeline" section) — an existing, documented, out-of-scope gap, not something this change
      introduced or needs to fix; caught by the user attempting their own NewRecruit reproduction of
      the first export and finding "Auric Mantle" wasn't offered under "Auric Champions" there.
      Re-ran with Detachment corrected to "Shield Host" (a legal roster) — confirmed live via
      `dotnet run` + chrome-devtools: Blade Champion's W tile rendered `W*` with a `* ✦ Auric Mantle`
      legend line and the source Enhancement still rendering normally in the full ability list.
      **One real rendering bug caught by direct user review of that live output**: the tile rendered
      amber-tinted (`.stat-tile-flagged`), which is wrong for a still-caveated value (shows the plain
      catalogue Value, not a computed result) — amber is reserved for a genuinely *resolved* run.
      Root cause: nothing before this change ever produced a caveated Scalar characteristic (only
      `StatlineFlagRule`'s always-resolved matches), so `_UnitBlock.cshtml`'s `marker is null ? plain
      : amber` rule had a latent, previously-unreachable bug — this change was the first thing to
      exercise it. Fixed in `RenderScalarTile`, the OC bespoke branch, and the pre-existing
      caveated-InSv branch (identical bug, just never noticed) by keying amber off `!view.IsCaveated`
      instead of marker presence alone; `design-tokens.md`'s "Flagged Statline Legend" corrected to
      match. New test `CaveatedScalarTile_RendersMarkedLabelAndLegendButNoAmberBackground` plus an
      updated `LivePlayInvulnerableSaveRenderingTests` case lock this in. Re-verified live after the
      fix — identical marker/legend, tile now plain-colored. This corrected export is the one to use
      for §6.2's NewRecruit cross-check.
- [x] 6.2 Manual NewRecruit cross-check against the same real roster(s) — confirm the caveat's
      targeted characteristic and its source genuinely match NewRecruit's own resolution, per
      `feedback_verify_bsdata_claims_precisely` memory (automated tests alone missed both of the
      reverted attempt's real bugs). Confirmed by the user: NewRecruit resolves "Auric Mantle" on
      Shield-Captain/Blade Champion under the Shield Host detachment identically (+2 Wounds).
- [x] 6.3 Full test suite green; every explicit-only corpus scan (including the new one) re-run
      manually against the live clone and clean. 515/515 non-explicit tests pass; the new
      `CharacteristicModifierClassificationScanTests` explicit scan re-run clean via
      `ProbHammer.Tests.exe -explicit only`.

## 7. Docs

- [x] 7.1 Update `.claude/domain-model-11e.md`: document the classification + presence-gated
      application mechanism (new section, or an extension alongside "Statline-Flag Rules"),
      cross-referencing the reverted attempt and this change's deliberately narrower (tier 1-2 only)
      scope. New "Characteristic-Modifier Caveats" section added.
- [x] 7.2 Update `.claude/vnext-ideas.md`: reflect that tiers 1-2 are shipped; keep tier 3+
      conditions, prose classification, and actual value resolution as the explicitly remaining gaps
