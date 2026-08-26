## 1. Army Rule Name Lookup

- [x] 1.1 Add `ArmyRuleNameLookup` (`ProbHammer.Core.Domain.Catalogue`) with the curated per-faction
      inclusion table from `design.md` (Death Guard, Custodes, Aeldari, Drukhari, Chaos Daemons,
      Genestealer Cults, Chaos Knights, Imperial Knights, Black Templars, Grey Knights, and every
      other Space Marine chapter → Oath of Moment) and a
      `Resolve(IReadOnlyList<string> faction) -> IReadOnlySet<string>` method using the "most
      specific (last) entry" convention, `OrdinalIgnoreCase` name comparison.
- [x] 1.2 Add the curated exclusion list (Assigned Agents, Disparate Paths, Corsairs and Travelling
      Players) as a separate, small data set — consulted only by the corpus scan (task 4.2), never
      by `Resolve`.
- [x] 1.3 Unit tests: sub-faction resolves against its own entry not its parent codex's; unsplit
      faction resolves directly; a faction with no table entry resolves to an empty set.

## 2. BSData Pipeline Wiring

- [x] 2.1 Add a `knownArmyRuleNames: IReadOnlySet<string>` parameter (default empty) to
      `BsdataDatasheetMapper.BuildDatasheet`. Core Rule Ability Extraction's Origin classification
      becomes purely `knownArmyRuleNames.Contains(name) ? ArmyRule : CoreRule` — remove its existing
      dependency on `HasPrimaryCatalogueScope` for this decision. Leave `HasPrimaryCatalogueScope`'s
      other call site (Core Rule Chapter and Game-Mode Gating's own exclusion check) untouched.
- [x] 2.2 Thread the same parameter through `ResolvedBsdataCatalogue.ResolveDatasheet` into
      `BuildDatasheet`.
- [x] 2.3 `ArmyRosterEnricher.Enrich` resolves `ArmyRuleNameLookup.Resolve(parsedArmyList.Faction)`
      once per roster and supplies it to every `ResolveDatasheet` call it makes.
- [x] 2.4 Unit tests: a rule matching the curated lookup (with or without primary-catalogue gating)
      classifies `ArmyRule`; a rule carrying primary-catalogue gating but NOT matching the lookup
      (the Assigned-Agents shape) classifies `CoreRule`, not `ArmyRule` — this is the regression
      test for the bug that motivated this redesign; an allied/off-faction unit's own rule is
      unaffected by a different faction's curated entry even when resolved within the same roster.

## 3. BattleScribe/JSON Pipeline Wiring

- [x] 3.1 Apply the identical `ArmyRuleNameLookup.Resolve(Faction)` check at the point
      `BattleScribeRosterMapper` builds a rule ability from a selection's own `rules` entry,
      classifying `ArmyRule` on a match and `CoreRule` otherwise.
- [x] 3.2 Unit tests: a matching rule name classifies as `ArmyRule`; a non-matching one stays
      `CoreRule` exactly as today.

## 4. Corpus Scan

- [x] 4.1 Add a scan (`tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/`,
      `[Fact(Explicit = true)]`) that, for every curated inclusion-table entry, resolves that
      entry's own faction to its real catalogue closure and checks the rule name resolves via the
      closure's own `RuleGlossary`.
- [x] 4.2 Add a second scan that walks every rule definition in the live clone, collects every one
      carrying `primary-catalogue`-scoped gating, and requires each to be accounted for by either
      the inclusion table or the exclusion list — failing on anything neither covers. Follow the
      existing `AllowlistCheck.AssertClean` bidirectional pattern, treating the exclusion list as
      the allowlist for this scan.
- [x] 4.3 Run both scans against the live local BSData clone. Confirm the second scan surfaces
      exactly the three known mustering rules as pre-accounted-for (not as new gaps) and nothing
      else untriaged. Fix (or, if genuinely unresolvable, remove) any inclusion-table entry the
      first scan flags before treating this task as done.
      **Findings from the real run (see `ArmyRuleNameLookup.cs`'s own comments and the two new
      allowlist files for full detail):** (1) "Custodes"→"Adeptus Custodes" and "Aeldari"→
      "Craftworlds" — wrong faction keys, didn't match any real catalogue file. (2) "Nurgle's
      Gift"→"Nurgle's Gift (Aura)" — the rule's own declared name carries the suffix. (3) The
      triage scan surfaced two GENUINE, previously-missing army rules (Tyranids' "Synapse" and
      "Shadow in the Warp" — both open with the same "If your Army Faction is TYRANIDS" phrase
      every confirmed genuine row shares, and Tyranids is a real single-faction case of two
      simultaneous ArmyRule abilities) plus one genuine mustering-rule-shaped exclusion ("Voice Of
      Command" — a model-granted Order-issuing keyword, not a roster-wide fact). (4) Drukhari's
      "Power from Pain" and Tyranids' "Synapse"/"Shadow in the Warp" are correct names but
      currently unreachable via the BSData text pipeline's own closure resolution — a pre-existing,
      already out-of-scope `BsdataClosureResolver` gap (identical root cause to
      `DetachmentGroupNameAllowlist`'s existing Aeldari-Drukhari/Tyranids entries), allowlisted
      rather than worked around. Unaffected: the BattleScribe/JSON pipeline, which reads a
      roster's own already-resolved rule text directly (confirmed against the real
      `data/gw-android-export-deathguard.json` sample).
- [x] 4.4 Add a Full-Faction Coverage Scan (`tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/`,
      `[Fact(Explicit = true)]`) that enumerates every real playable-faction catalogue file in the
      live clone (same exclusion convention as `BsdataFactionResolver.ResolveStartingFileName` —
      skip any filename containing "Library", skip the single game-system file), derives each
      file's own most-specific Faction name (text after the last `" - "`, else the whole stem — the
      same convention `ArmyRuleNameLookup`'s own table keys already follow), and asserts every one
      of those names appears as a *key* in `ArmyRuleNameLookup.Entries` — not merely that `Resolve`
      returns something non-empty, since an absent key and a deliberately-empty entry both satisfy
      that weaker check indistinguishably. No new allowlist/deferral type — the table's own existing
      "empty list is a valid, deliberate entry" contract is the only mechanism.
      Added `ArmyRuleNameCoverageScanTests`. First real run against the live clone confirmed it
      fails on exactly 15 factions, not 13 — the 13 Full-Faction Coverage candidates design.md's
      table already lists, plus `Adeptus Titanicus`/`Titanicus Traitoris` (already known to need
      explicit empty-list entries per design.md's own existing paragraph, just not yet added to the
      shipped table) — confirming both this scan's correctness and that those two hadn't actually
      been closed out despite being "decided" in prose.
- [x] 4.5 For each of the 15 factions the scan surfaces, independently validate the candidate name
      against a second authoritative source (e.g. Wahapedia) — not just the frequency + text-shape
      signal that surfaced it — before adding a verified inclusion-table entry; where validation
      confirms no single unifying army-wide rule exists (candidates so far: Agents of the Imperium,
      Unaligned Forces, plus the already-decided Adeptus Titanicus/Titanicus Traitoris empty
      entries), add an explicit empty-list entry instead. Extend `ArmyRuleNameResolutionScanTests`/
      its allowlist to cover any newly-added row the same way the original 11 are covered. Re-run
      `ArmyRuleNameCoverageScanTests` after each batch of additions to confirm the remaining gap
      shrinks accordingly, finishing at zero.
      All 15 validated against Wahapedia and added: 12 as populated inclusion-table entries
      (Adepta Sororitas/Acts of Faith, Adeptus Mechanicus/Doctrina Imperatives, Agents of the
      Imperium/Kill Team — not the higher-frequency `Assigned Agents`, which is the composition-
      eligibility rule for OTHER factions taking these units as allies — Astra Militarum/Voice Of
      Command, Chaos Space Marines/Dark Pacts, Emperor's Children/Thrill Seekers, Leagues of
      Votann/Prioritised Efficiency, Necrons/Reanimation Protocols, Orks/Waaagh!, T'au Empire/For
      The Greater Good, Thousand Sons/Cabal of Sorcerers, World Eaters/Blessings of Khorne) and 3 as
      explicit empty entries (Unaligned Forces, Adeptus Titanicus, Titanicus Traitoris — all
      confirmed to have no single unifying army-wide rule). Astra Militarum's own closure cannot
      reach "Voice Of Command"'s text (same `BsdataClosureResolver` importRootEntries:false gap as
      Drukhari/Tyranids) — added as a fourth entry to `ArmyRuleNameResolutionAllowlist`.
      `ArmyRuleNameCoverageScanTests` and all 10 other explicit-only corpus scans confirmed green
      against the live clone; default suite unaffected (436 total, 0 failed, 11 not-run).
- [x] 4.6 Resolve the Astra Militarum / `Voice Of Command` contradiction found during 4.4/4.5's
      investigation: its own rule text opens with the identical "If your Army Faction is Astra
      Militarum..." signature every confirmed genuine row shares, yet it already sits on the
      shipped `ExclusionList` as a confirmed non-army-rule (reasoned as model-granted, not
      roster-wide). Validate against Wahapedia and either move it into the inclusion table
      (removing the `ExclusionList` entry, updating `PrimaryCatalogueGatedRuleTriageScanTests`'
      allowlist accordingly) or keep the exclusion with a corrected/confirmed reason — do not leave
      both standing without resolving which one is actually right.
      Wahapedia confirmed it IS Astra Militarum's own genuine `ArmyRule` — removed from
      `ExclusionList`, added to the inclusion table (see 4.5). `PrimaryCatalogueGatedRuleTriageScanTests`
      needed no allowlist update: a name already in the inclusion table is filtered out before ever
      becoming a scan "result" (its own pre-existing behavior, see that test's doc comment), so
      moving the name off the exclusion list and onto the inclusion table keeps it silently
      accounted for either way.

## 5. Real-Data Verification

- [x] 5.1 Re-import the Death Guard JSON export that originally surfaced this gap; confirm Nurgle's
      Gift now renders in `/LivePlay`'s Army header (via `dotnet run` + a Firefox devtools
      screenshot). Confirmed: "Nurgle's Gift (Aura)" renders as an Army-column trigger (real
      `data/gw-android-export-deathguard.json`, JSON/BattleScribe pipeline).
- [x] 5.2 Re-import a Black Templars export (BSData text pipeline) and confirm Templar Vows still
      renders correctly, and that a vanilla/other-chapter Space Marines roster still shows Oath of
      Moment — no regression from removing the structural check's runtime role. Confirmed via
      `dotnet run` + Firefox against real `data/gw-app-export-templars.txt` ("Templar Vows" in the
      Army column); the Salamanders/Oath-of-Moment side confirmed by re-running the existing live-
      clone `ChapterExclusiveCoreRuleRegressionTests` explicitly (both still pass unchanged).
- [x] 5.3 Import (or construct a fixture for) a roster that includes an allied Agents of the
      Imperium unit and confirm "Assigned Agents" does NOT appear in the header or get promoted as
      an unattached ability — the concrete bug this change fixes. No real captured sample includes
      an ally, so added a small targeted live-clone regression fact
      (`AssignedAgentsRegressionTests`, `[Fact(Explicit = true)]`) resolving a real Vindicare
      Assassin (Agents of the Imperium) through a Custodes-starting closure - the same one-closure-
      for-the-whole-roster shape `ArmyRosterEnricher` uses in production. Confirmed: "Assigned
      Agents" classifies `CoreRule`, not `ArmyRule`.
- [x] 5.4 Spot-check one more curated faction end-to-end (e.g. Custodes or Grey Knights). Confirmed
      via `dotnet run` + Firefox against the real `data/gw-android-export-custodes.json`
      (JSON/BattleScribe pipeline): "Martial Ka'tah" renders correctly in the Army column.

## 6. Documentation

- [x] 6.1 Update `.claude/domain-model-11e.md`'s "Core Versus Army Rule Origin Classification" and
      BattleScribe "Core Rule Extraction" sections to document the new name-table-driven
      classification, the removal of `HasPrimaryCatalogueScope` from that decision, and
      `ArmyRuleNameLookup`'s inclusion/exclusion lists.
- [x] 6.2 Add a PROGRESS.md entry summarizing the change, including the Assigned Agents false-
      positive bug it fixes.
- [x] 6.3 Fix `LivePlayArmyHeaderRenderingTests.TwoSimultaneousArmyRuleAbilities_
      BothRenderIndependentlyInTheArmyColumn` (already-shipped test from
      `display-army-header-and-detachment-rules`), which illustrates its scenario with "Power from
      Pain" and "Corsairs and Travelling Players" as a Drukhari army's two simultaneous ArmyRule
      abilities — this change confirms "Corsairs and Travelling Players" is a mustering rule, never
      genuinely ArmyRule-origin in real data. Swap in two real distinctly-named ArmyRule examples
      (e.g. from two different present components of a fixture roster) so the test no longer cites a
      confirmed-fake example.
      Swapped in Tyranids' "Synapse"/"Shadow in the Warp" — a genuine real pair discovered by the
      task 4.2 triage scan itself, better than the originally-planned Drukhari/Aeldari cross-faction
      pairing since it's one faction's own two simultaneous ArmyRule abilities.
- [x] 6.4 Once 4.5/4.6 land validated Full-Faction Coverage entries, update
      `.claude/domain-model-11e.md`'s `ArmyRuleNameLookup` doc comment reference and PROGRESS.md
      with the finalized, complete table — including the Astra Militarum resolution and any faction
      that resolved to a deliberate empty entry rather than a name.
