## Context

See `proposal.md` for motivation. Relevant existing shape, from `.claude/domain-model-11e.md`:

- `AbilityOrigin.ArmyRule` is decided today by `BsdataDatasheetMapper`'s `HasPrimaryCatalogueScope`
  structural check — a rule's own gating carries a `primary-catalogue`-scoped condition anywhere in
  its tree. This exists because a handful of shared-library sub-faction families (Space Marine
  chapter books importing `Library - Astartes Heresy Legends.json`; Craftworlds/Drukhari importing
  `Aeldari - Aeldari Library.json`) need to pick between mutually-exclusive sibling rules for the
  resolved army.
- **That signal is unreliable, confirmed against the live BSData clone.** The same gating shape is
  used for at least three mustering/composition rules, not gameplay rules: `Assigned Agents`
  (Agents of the Imperium, referenced from ~30 datasheets including allied units in other factions'
  own files), `Disparate Paths` (Aeldari), `Corsairs and Travelling Players` (Drukhari). All three
  read "you can include X units even though they don't have your Faction's keyword" — composition
  eligibility, not a gameplay rule — yet all three carry `primary-catalogue`-scoped gating
  identical in shape to Templar Vows'. Today's classification already misclassifies these as
  `ArmyRule`; this is a live bug, independent of this change.
- `BattleScribeRosterMapper` (the JSON/NewRecruit pipeline) has no BSData involvement and no
  gating concept whatsoever; every roster-level rule it extracts is hardcoded `CoreRule` (see
  `battlescribe-roster-import`'s existing "Core Rule Extraction" requirement).
- Both existing `ArmyRule`-Origin consumers — `attached-unit-tracker`'s per-`AttachedUnit`
  promoted/unattached-row dedup, and `live-play-view`'s header rule list — key strictly off
  `Ability.Origin == ArmyRule`. Neither needs to change; they already do the right thing once
  `Origin` is set correctly.
- `Core Rule Chapter and Game-Mode Gating` (a separate, existing requirement) also reads
  `primary-catalogue`-scoped conditions, but for a different question entirely: whether a hidden
  rule reference produces an `Ability` at all. That mechanism is untouched by this change — only
  Origin *classification*'s reliance on the same structural signal is removed.
- `BuildDatasheet` (`BsdataDatasheetMapper`) already threads two caller-supplied, closure-derived,
  fail-open-default parameters into its walk: `forceEntries` and `primaryCatalogueId`. This change
  adds a third, but one sourced from the *roster* (its parsed Faction), not the closure.

## Goals / Non-Goals

**Goals:**
- Make `Origin == ArmyRule` classification driven entirely by an explicit, verified, per-faction
  name table — eliminating the confirmed false-positive class (mustering/composition rules that
  happen to share the same gating shape as a genuine army rule).
- Cover Death Guard, Custodes, and the whole Space Marine chapter family (verified directly against
  the bundled corpus, not assumed) so removing the structural check's Origin role causes no
  regression for any faction that worked correctly before this change.
- Keep the lookup itself decoupled from BSData: `battlescribe-roster-import`'s own design
  explicitly avoids any BSData dependency, so the table and its resolution logic must live
  somewhere both pipelines can use without introducing that dependency.
- Preserve some of the structural check's original auto-generalization value — not by trusting it
  at runtime, but by repurposing it as a scan-time discovery aid that surfaces new candidates for a
  human to triage, rather than either missing them or blindly trusting them.
- **Every real playable faction catalogue file in the bundled corpus has an explicit table entry.**
  Added mid-change after the original 11-faction table (below) turned out to omit roughly half the
  real corpus — Orks, Necrons, T'au Empire, Adepta Sororitas, Adeptus Mechanicus, Astra Militarum,
  Leagues of Votann, Chaos Space Marines, Emperor's Children, Thousand Sons, and World Eaters all
  had no entry at all, discovered only by manually diffing the bundled clone's own file list against
  the table, not by any automated check. A faction can still resolve to an empty rule-name set (a
  faction confirmed, after research, to have no single unifying army-wide rule) — but that MUST be
  an explicit, deliberate entry (an empty list, not an absent key), so "no entry" always means
  "nobody has checked yet," never "checked and found nothing," and the two are distinguishable by
  the new Full-Faction Coverage Scan below.

**Non-Goals:**
- Auto-detecting army-wide rules from any structural or frequency signal alone, at runtime —
  already investigated and rejected; every signal considered (structural gating presence, reference
  frequency, "If your Army Faction is..." text prefix) produces both false positives and false
  negatives against real corpus data (see Decisions below for the concrete cases).
- Full future-proofing against every possible new rule shape. The new corpus scan only discovers
  candidates that live in a *shared* library file needing gating to pick between siblings
  (Oath-of-Moment-shaped). A rule defined in its own dedicated, ungated file (Gate-of-Infinity-
  shaped — which is how most non-Space-Marines army rules are actually built) is never discovered
  by gating presence at all; those stay covered only by the "does each table entry still resolve"
  scan and by manual research when a new faction needs adding. The new Full-Faction Coverage Scan
  (below) closes a different, narrower gap than either of these: it can tell you *that* a faction
  has no entry yet, but never *what the right name is* — that step still needs the same manual
  frequency + text-shape research the original table used, just applied to every real faction
  instead of the 11 this change's own bug report happened to touch.
- Any change to `AttachedUnitAggregator`, `LivePlayModel`, `/LivePlay` rendering, or `Core Rule
  Chapter and Game-Mode Gating`'s own exclusion behavior — deliberately out of scope.

## Decisions

**Origin classification is driven entirely by the curated table; the structural check is removed
as a runtime signal.** Alternative considered and rejected: keep the structural check as an
additional, independent path to `ArmyRule` (an OR with the name-match check), as originally
designed earlier in this change's own history. Rejected once `Assigned Agents`/`Disparate
Paths`/`Corsairs and Travelling Players` were found: an OR can only ever add more things `ArmyRule`
matches, never remove a wrong match, so it cannot fix the confirmed false-positive class at all —
only removing runtime dependence on the structural signal does.

**The structural check is repurposed as a scan-time discovery aid, not discarded.** A new corpus
scan walks every rule definition in the bundled clone carrying `primary-catalogue`-scoped gating —
the exact same signal `HasPrimaryCatalogueScope` already computes — and requires each one to be
triaged into either the inclusion table (`army-rule-name-lookup`) or a new, explicit exclusion list
naming confirmed non-army-rule matches, failing the scan on anything neither accounts for. This is
what a future Space Marines-family or Aeldari-family codex update would trip: a brand-new
shared-library rule gets flagged as an untriaged candidate the next time the scan runs, rather than
silently doing nothing (this change's own starting problem) or silently doing the wrong thing
(Assigned Agents' problem).

**This discovery mechanism has a real, acknowledged limit: it only ever finds shared-library,
gated rules.** A rule living in its own dedicated file needs no gating at all (there's no sibling
to distinguish from), so it's structurally invisible to this scan. Confirmed directly: Grey
Knights' `Gate of Infinity`, Death Guard's `Nurgle's Gift`, and Custodes' `Martial Ka'tah` are all
`gated: False` in their own rule definitions. These were found (and any future one like them will
need to be found) by the same manual process already used in this change's own research: comparing
a faction's top referenced `"type": "rule"` infoLink names by frequency, and checking candidate
text against the "If your Army Faction is..." pattern every confirmed genuine army rule shares —
never fully automatable, and not attempted as such here.

**A second, separate corpus scan enforces full-faction coverage — no new "deferred" concept, the
table itself is the only source of truth.** `ArmyRuleNameLookup` already exposes `Entries` (every
`(Faction, Names)` pair in the table, including — once populated below — an entry whose `Names` is
an empty list for a faction confirmed to have no unifying army-wide rule). A new explicit-only scan
enumerates every real playable-faction catalogue file in the live clone (same file-exclusion
convention as `BsdataFactionResolver.ResolveStartingFileName`: exclude any name containing
"Library", exclude the single game-system file), derives each file's own most-specific Faction
name using the identical "text after the last `' - '`, else the whole stem" rule the table's own
keys already follow, and asserts every one of those names appears as a *key* in `Entries` — not
that `Resolve` returns something non-empty, which an absent key and a deliberately-empty one would
both satisfy indistinguishably. This is a plain `Assert.Empty` over the set-difference (every real
faction name minus every table key), no bidirectional `AllowlistCheck` needed and no separate
exclusion/deferral list — a rejected alternative from earlier in this change's own history (a
`DeferredFactions` list mirroring `ExclusionList`'s shape) was dropped once it became clear the
table's own existing "empty list is a valid entry" contract already does the job with no new type.

**Faction-Scoped Resolution reuses the existing "most specific (last) entry" convention**, matching
`BsdataFactionResolver.ResolveStartingFileName` (BSData pipeline) and `roster-model`'s own
documented Faction ordering (parent codex before sub-faction). Both pipelines already build an
ordered Faction list this way — `army-roster-enrichment`'s `ArmyListParser` output for the BSData
pipeline, `BattleScribeRosterMapper`'s `catalogueName.Split(" - ")` for the JSON pipeline — so no
new Faction-parsing logic is needed, only a shared `faction[^1]` lookup key. Confirmed this must be
per-chapter, not a single generic "Space Marines" fallback: nearly every named chapter (Blood
Angels, Dark Angels, Deathwatch, Imperial Fists, Iron Hands, Raven Guard, Salamanders, Space
Wolves, Ultramarines, White Scars) has its own dedicated catalogue file and is therefore its own
most-specific Faction entry, even though all of them currently resolve to the same rule name (Oath
of Moment).

**Name matching is exact, case-insensitive (`OrdinalIgnoreCase`)** — not `RuleGlossary`'s
aggressive value/suffix-stripping `Normalize`. These are always whole rule names, matching the
comparer this codebase already uses elsewhere for ability-name identity (`BuildArmyRules`'s
existing `GroupBy(..., StringComparer.OrdinalIgnoreCase)`).

**The known-name set is a caller-supplied parameter into `BuildDatasheet`, resolved once per
roster by the caller that owns Faction — not derived inside the mapper.** `BuildDatasheet` only
ever sees a raw `primaryCatalogueId` (a BSData id), not a human-readable Faction string. So
`ArmyRosterEnricher`, which already has `parsedArmyList.Faction` and already drives every
`Datasheet` resolution for the roster, resolves `ArmyRuleNameLookup.Resolve(parsedArmyList.Faction)`
once and threads the result through `ResolvedBsdataCatalogue.ResolveDatasheet` into
`BuildDatasheet`, alongside the existing `forceEntries`/`primaryCatalogueId` parameters. Same
fail-open-default shape: omitted → empty set → every reference classifies `CoreRule`.

**`BattleScribeRosterMapper` gets the identical check** at the point it builds an ability from a
selection's own `rules` entry: `ArmyRuleNameLookup.Resolve(Faction).Contains(name) ? ArmyRule :
CoreRule`. Its own `Faction` is already computed earlier in `Map()` from `catalogueName`, so this
is a small, local addition, not a new data flow.

**Initial table contents** — researched and verified directly against the bundled corpus during
this change's own design investigation (both frequency-of-reference and rule-text-shape checks; the
new corpus scan re-verifies this on every future run):

| Faction (most-specific entry) | Known army-wide rule name | Verification |
|---|---|---|
| Death Guard | Nurgle's Gift | own dedicated file, ungated |
| Custodes | Martial Ka'tah | own dedicated file, ungated |
| Aeldari | Battle Focus | shared library, gated (siblings with Drukhari) |
| Drukhari | Power from Pain | shared library, gated (siblings with Aeldari) |
| Chaos Daemons | The Shadow of Chaos | shared library, gated |
| Genestealer Cults | Cult Ambush | own dedicated file |
| Chaos Knights | Harbingers of Dread | shared library, gated |
| Imperial Knights | Code Chivalric | shared library, gated |
| Black Templars | Templar Vows | shared library, gated (siblings with Oath of Moment) |
| Grey Knights | Gate of Infinity | own dedicated file, ungated; 0 references to Oath of Moment |
| Space Marines *(unsplit)*, Blood Angels, Dark Angels, Deathwatch, Imperial Fists, Iron Hands, Raven Guard, Salamanders, Space Wolves, Ultramarines, White Scars | Oath of Moment | shared library, gated; 2–132 references each, no override rule found for any of them in this snapshot |

**Full-Faction Coverage candidates — validated against Wahapedia and added** (surfaced by the new
Full-Faction Coverage Scan's own diffing of the live clone's file list against the original
11-faction table above; each independently cross-checked against Wahapedia as a second source,
not just the frequency + text-shape signal that surfaced it):

| Faction (most-specific entry) | Rule name | Verification |
|---|---|---|
| Adepta Sororitas | Acts of Faith | own dedicated file, ungated, 41 refs, signature match, Wahapedia-confirmed |
| Adeptus Mechanicus | Doctrina Imperatives | own dedicated file, ungated, 41 refs, Wahapedia-confirmed |
| Agents of the Imperium | Kill Team | Wahapedia-confirmed as the faction's own army rule — `Assigned Agents` (the higher-frequency candidate) is the composition-eligibility rule other factions use to include this faction's units as allies, not this faction's own rule |
| Astra Militarum | Voice Of Command | Wahapedia-confirmed as a genuine `ArmyRule`, not the mustering/composition rule its `primary-catalogue` gating shape originally suggested (see the now-resolved Exclusion list contradiction below) — its own closure cannot reach the text (same `BsdataClosureResolver` import gap as Drukhari/Tyranids below), allowlisted in `ArmyRuleNameResolutionAllowlist` |
| Chaos Space Marines | Dark Pacts | shared library, gated, 80 refs, signature match against "Heretic Astartes", Wahapedia-confirmed — the same name this file's own history had flagged as "seen during research, not yet independently re-verified"; now verified |
| Emperor's Children | Thrill Seekers | own dedicated file, ungated, 21 refs, Wahapedia-confirmed |
| Leagues of Votann | Prioritised Efficiency | own dedicated file, ungated, 25 refs, Wahapedia-confirmed |
| Necrons | Reanimation Protocols | own dedicated file, ungated, 67 refs, Wahapedia-confirmed |
| Orks | Waaagh! | own dedicated file, ungated, 94 refs, Wahapedia-confirmed |
| T'au Empire | For The Greater Good | own dedicated file, gated, 44 refs, Wahapedia-confirmed |
| Thousand Sons | Cabal of Sorcerers | own dedicated file, ungated, 15 refs, Wahapedia-confirmed |
| Unaligned Forces | *(explicit empty entry)* | Wahapedia-confirmed: no single unifying army-wide rule exists for this catalogue (a rules-light allied/renegade bucket, not a real playable army in the same sense as every other row) |
| World Eaters | Blessings of Khorne | own dedicated file, ungated, 30 refs, Wahapedia-confirmed |

Every row above is now either a verified `ArmyRuleNameLookup` table entry or an explicit
empty-list entry (`Names: []`) — never an absent key, matching the Full-Faction Coverage Scan's own
requirement. `ArmyRuleNameCoverageScanTests` passes clean against the live clone as of this table
landing.

**Adeptus Titanicus and Titanicus Traitoris get explicit empty-list table entries, not merely
prose.** The rule name surfaced by earlier external research ("Towering Example") does not appear
anywhere in the bundled BSData corpus snapshot. Adding an unverifiable name would violate the
table's own "verified to resolve" requirement; leaving the key absent would fail the new Full-Faction
Coverage Scan. An explicit `Names: []` entry satisfies both constraints at once — this faction pair
has been checked, and confirmed to have nothing resolvable in this snapshot, which is a genuinely
different state from "nobody has looked yet."

**Exclusion list contents** — confirmed mustering/composition rules that carry `primary-catalogue`
gating but must never classify as `ArmyRule`:

| Rule name | Why excluded |
|---|---|
| Assigned Agents | Composition eligibility for allied Agents of the Imperium units, not a gameplay rule |
| Disparate Paths | Composition eligibility for allied Harlequins units (Aeldari), not a gameplay rule |
| Corsairs and Travelling Players | Composition eligibility for allied Harlequins/Anhrathe units (Drukhari), not a gameplay rule |

This list is consulted only by the corpus scan (to explain why a discovered candidate is
deliberately not in the inclusion table) — it plays no runtime role, since a name simply absent
from the inclusion table already classifies `CoreRule` by default.

`Voice Of Command` briefly sat on this list (added by task 4.3's real corpus run, reasoned as a
model-granted keyword ability, not a roster-wide fact) and has since moved to the inclusion table's
Astra Militarum row above — Wahapedia validation during Full-Faction Coverage confirmed it IS that
faction's own genuine `ArmyRule`, and its own text opens with the identical "If your Army Faction
is X" signature every other confirmed row shares, which the original exclusion reasoning hadn't
weighed against.

## Risks / Trade-offs

- **[Risk] The corpus scan cannot discover a dedicated-file, ungated rule** (see Non-Goals). →
  Accepted: this is the same class of gap this change's own research process (frequency + text-
  shape review) already closes for every faction in the table above, and it's the same effort a
  future faction addition would need regardless of this change.
- **[Risk] Mid-edition codex/BSData renames silently break a table entry.** → Mitigated by the
  "does each table entry still resolve" scan; not automatic (this project's existing scans are all
  manually-triggered, not part of CI), so it depends on being re-run periodically — same accepted
  trade-off every other corpus scan in this project already makes.
- **[Risk, resolved] Table coverage was partial at ship time** for factions beyond those originally
  verified. → Originally accepted as a deliberate Non-Goal; resolved mid-change once the
  Full-Faction Coverage Scan requirement (Goals, above) was added specifically to close this gap
  rather than accept it — confirmed the gap was real and large (15 of 37 real playable-faction
  catalogue files had no entry at all — the 13-row Full-Faction Coverage candidate table above plus
  `Adeptus Titanicus`/`Titanicus Traitoris`, whose "deliberately left unmapped" treatment had only
  ever been prose, never an enforced table entry), not hypothetical. Every candidate is now
  validated and entered (name or explicit empty list); `ArmyRuleNameCoverageScanTests` passes clean
  against the live clone. No regression from the old classification either way, since the old
  structural check never correctly classified any of them
  as `ArmyRule` either, per the original bug report.
- **[Risk] A name collision between two factions' curated entries** could theoretically misfire if
  a roster's own primary Faction resolves to the wrong table row. → Not a new risk this change
  introduces: Faction-Scoped Resolution reuses the exact same "most specific entry" convention
  already trusted for catalogue file resolution today.

## Migration Plan

No data migration — this only changes how an in-memory `Ability.Origin` is classified during a
request's own enrichment pass; nothing is persisted. Ships as a single change; no rollback concerns
beyond reverting the code.
