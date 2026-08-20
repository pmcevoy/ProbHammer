## Context

See `proposal.md` for motivation. This document covers the technical shape of the pipeline that
connects a pasted GW-app 11e export to `/LivePlay`, and the real-data findings (from the three
captured exports in `data/` and the live BSData clone) that shaped it.

## Goals / Non-Goals

**Goals:**
- A single real GW-app 11e export, pasted by a user, becomes the `ArmyRoster` `/LivePlay` renders
  for that user's session — replacing `Examples/View.Roster()` entirely.
- Multiple concurrent users, each with their own imported roster (session-scoped).
- Wargear/model selections are taken verbatim from the export — no curation or inference about
  which weapon a model "would actually" use.
- BSData catalogue resolution is fast enough for a per-request rebuild (every casualty tap
  rebuilds the roster fresh, per the `casualty-tracking` precedent) without repeat file I/O.
- The app runs without depending on an external, machine-local BSData clone at runtime.

**Non-Goals:**
- Attacker + defender two-roster flow, or any `Simulation/*` revival — single roster only.
- Live GitHub fetch-and-cache of BSData — a static, baked-in snapshot only.
- The ability-text "tuning" pass (flipping known `InvulnerableSave.Caveated` phrasings, etc.) —
  parked; this design leaves a slot for it but does not build it.
- Capturing a unit's `Category` (`Character`/`Battleline`/etc.) from the export — see Decisions.
- BSData cache invalidation/refresh — the app-wide catalogue cache lives until process restart.

## Decisions

**Pipeline shape.** `text → IArmyListParser → ParsedArmyList → enrich(BsdataClosure) → ArmyRoster
→ /LivePlay`, with a deliberate, unbuilt slot for the ability-text tuning pass between `enrich`
and `/LivePlay` (operating on the built `ArmyRoster`'s distinct `Datasheet`s, producing revised
copies — see `.claude/vnext-ideas.md`). The 10e `ArmyListParser`/`ArmyList` (fully archived to
`legacy/10e-pipeline/`) is not reused: the 11e `ATTACHED UNITS` structure has no 10e equivalent,
and the old parser's return type doesn't fit what enrichment needs.

**Session stores the intermediate, not the graph.** `ParsedArmyList` (not `ArmyRoster`) is what
lives in ASP.NET Core Session (in-memory store). Every request — including every casualty
adjustment — re-runs enrichment against the cached BSData closure to rebuild `ArmyRoster` fresh.
Considered caching the built `ArmyRoster` per session instead (saves the enrichment-walk cost too),
but rejected: `ArmyRoster`'s object graph (`Datasheet`/abstract `WeaponProfile`) doesn't fit
`Session`'s plain byte/string storage without a custom serializer, and rebuilding from a
consistent source of truth on every request is the same architectural bet `casualty-tracking`
already made for the fixture path — extending it one level up avoids introducing a second,
different consistency model.

**App-wide catalogue cache, separate from session.** The expensive part of enrichment is
`BsdataClosureResolver.Resolve` (multi-file JSON parse, BFS import resolution) and building
`BsdataNameResolver`'s id/group/profile indices — this is static per faction and identical for
every user, so it's cached once per starting filename in a process-wide singleton, not rebuilt
per request or per session. This is new infrastructure: nothing today holds a long-lived cache
(corpus-scan tests and one-off tools call `Resolve` exactly once per run). No invalidation logic
this round — the cache is valid for the container's lifetime, matching the baked-in-snapshot
decision below (nothing changes the underlying data while the process runs).

**Faction → starting BSData file: suffix match, not a literal join.** Verified against the real
clone: `data/gw-app-export.txt`'s Faction lines are `"Space Marines"` / `"Black Templars"`, but the
real file is `Imperium - Black Templars.json` — `"Imperium"` never appears in the export text at
all, so `{Faction[0]} - {Faction[^1]}.json` doesn't work. Instead: search `ListFileNames()` for a
file equal to `"{mostSpecificFaction}.json"` or ending in `" - {mostSpecificFaction}.json"`, using
only the last (most specific) `Faction` entry. Verified against every real shape in the corpus
(`Orks.json`, `Imperium - Black Templars.json`, `Aeldari - Craftworlds.json`,
`Chaos - Chaos Space Marines.json`). Must exclude any filename containing `"Library"` — verified
these are import-closure members, never a playable faction identity themselves, and can hold the
*majority* of a faction's real content (`Aeldari - Aeldari Library.json` is 6.5 MB with 479 nested
`selectionEntries`; `Aeldari - Craftworlds.json` itself is 25 KB, almost entirely `catalogueLinks`)
— so excluding them isn't about them being "support" content, it's that they carry no faction
identity of their own even when they hold most of the substance.

**`ATTACHED UNITS` parsing.** `"Attached unit N"` lines are group boundaries; each group's members
carry their own `Name (Points)` header plus a first bullet `"Attached as: Role[ (Category)]"`
(`Role` ∈ {`Leader`, `Support`, `Bodyguard`}). Confirmed across all three real exports that the
`(Category)` parenthetical is **optional** — missing on a `Bodyguard` line in one file and a
`Support` line in another, not a single-file quirk. **`Category` is not captured at all** — neither
`Unit` nor `ArmyRoster` has anywhere to put it, it's a BSData-owned concept (available via the
resolved `Datasheet`/catalogue data if ever needed), and the export's own inconsistent inclusion of
it gives no reason to invent storage for it now. Exactly one `Bodyguard`-tagged member per group
becomes `AttachedUnit.Bodyguard`; the rest become `AttachedUnit.Attached`, **preserving export
order** — this matters because `ICombatUnit.Name`'s humanized join reads directly off that list's
order (`"{Bodyguard} with {A} and {B}"`).

**Model-line count-splitting is arithmetic over the export's own weapon counts, not a BSData
name-match.** Investigated the real BSData structure for Crusader Squad's `Initiates` group
directly (`Imperium - Black Templars.json`) rather than assuming: the compound name seen in some
contexts (`"Initiate w/Chainsword & Heavy Bolt Pistol"`) belongs to an outer `model`-type
selection entry, but its embedded `:Unit`-typeName profile — the thing `BsdataDatasheetMapper
.BuildDatasheet` actually captures as a named `Statline`, deduped by name, first-occurrence-wins —
is named plain `"Initiate"` identically across all four real loadout variants. So
`Datasheet.Statlines` for Crusader Squad has exactly one `"Initiate"` entry; the compound names
aren't captured anywhere in the domain model and don't need to be. This means the enrichment stage
never needs to reconstruct or match against BSData's `"w/X & Y"` naming convention (fragile:
abbreviations, ordering, punctuation) — it resolves `StatlineName: "Initiate"` directly (trivial)
and resolves each weapon independently by its own flat name via `ResolveWeaponProfile` (already
works). The splitting itself: within one exported model-count group, a weapon whose count equals
the group's total model count is shared (duplicated onto every resulting `ModelLine`); weapons
whose counts don't match the total are mutually-exclusive alternatives whose counts must sum
exactly to the total, each producing its own `ModelLine` sharing the same `StatlineName`. Verified
independently on a second, unrelated squad (`Company Heroes` → `Company Veteran` in
`data/gw-app-export-3-dp.txt`): `2x Bolt pistol`/`2x Close combat weapon` shared, `1x Master-crafted
bolt rifle`/`1x Master-crafted heavy bolter` alternating, same shape.

  - **Explicitly accepted, unverified assumption**: a model group varies along exactly one wargear
    axis at a time (never two independently-varying option slots crossed together). Held true
    across every real example seen so far. The user is actively probing NewRecruit and the GW app
    during list-building for a counter-example; this assumption is not re-litigated by that search
    succeeding or failing on its own — a counter-example, if found, comes back as new input to this
    design, not a silent code change.
  - **Requirement**: if a group's weapon counts don't cleanly satisfy "each weapon is either shared
    (== total) or part of an exact-sum-to-total partition," the parser must throw a clear,
    diagnosable exception rather than guess a split — matching this project's existing convention
    (`AmbiguousCharacteristicException` and siblings) rather than inventing a new silent-best-guess
    failure mode.

**Enhancements reuse the 10e detection heuristic unchanged.** Real data confirms
`"• Enhancements: Touched by the Warp"` (`data/gw-app-export-masters-of-the-maelstrom.txt`) — the
same `"Enhancements:"`-prefixed-bullet shape the archived 10e parser already recognized. No new
design needed; the heuristic just wasn't exercised by the first sample file read during
exploration.

**Name normalization before BSData resolution.** Real export text uses typographic apostrophes
(U+2019 — `"Reaver's blade"`), which will not match BSData's plain-ASCII names. This was already
flagged as a known gap in `domain-model-11e.md` from the one-off resolution smoke test
(`"Emperor's Champion"`) and explicitly deferred as "the future export-parser/enrichment step's
problem" — this change is that step, and must normalize before calling `BsdataNameResolver
.Resolve`.

**BSData packaging: baked-in snapshot, cache-friendly Docker layering.** Copied into
`src/ProbHammer.Web/BsData/` (outside `wwwroot` — it's runtime catalogue data, not a
publicly-servable asset) but excluded from the `.csproj`'s default content glob
(`<Content Remove="BsData/**" />`) so `dotnet publish` never touches or duplicates it. The
Dockerfile copies it directly into the **final** stage as its own layer, independent of the
`build`/`publish` stage:
```
FROM ... AS final
COPY --link BsData/ /app/BsData/
COPY --link --from=build /app/publish .
```
This keeps the layer's Docker build-cache validity tied only to the snapshot's own content —
editing application code never invalidates it, and vice versa. No live GitHub fetch this round;
the snapshot will drift from real rules updates until that later feature exists (accepted).

## Risks / Trade-offs

- **[Risk]** The single-wargear-axis partitioning assumption is wrong for some real datasheet not
  yet seen → **Mitigation**: fail loud with a diagnosable exception instead of silently guessing a
  split; the user is actively hunting for a counter-example independent of implementation.
- **[Risk]** The baked-in BSData snapshot drifts from live rules updates over a long-running
  container's life → **Mitigation**: explicitly accepted for this round; a later change adds
  GitHub fetch-and-cache (the 10e pipeline had this capability before archival, as prior art).
- **[Risk]** Reintroducing ASP.NET Core Session could read as walking back
  `archive-10e-pipeline`'s removal of it → **Mitigation**: documented explicitly here — new purpose
  (per-session `ParsedArmyList` storage), not the old `Enricher` cache; `archive-10e-pipeline`'s
  removal was about deleting the *old* consumer, not a standing prohibition on Session itself.
- **[Risk]** The faction → file suffix match is ambiguous, or matches zero/multiple files, for a
  faction shape not yet seen in the corpus → **Mitigation**: only ~40 catalogue files exist total;
  fail loud (clear exception) rather than guess when the match isn't exactly one file.
- **[Risk]** Typographic-character mismatches beyond the one confirmed case (curly apostrophes)
  cause a silent BSData resolution failure → **Mitigation**: normalize the known case; any
  resolution failure must surface clearly through the import flow's error reporting
  (`proposal.md`), not fail silently or crash.

## Open Questions

- Exact shape of the app-wide catalogue cache (a custom keyed singleton vs. `IMemoryCache`) —
  implementation detail, doesn't affect the design's decisions or the task breakdown.
- Exact UX for surfacing a parse/resolution failure on the import page (inline error text vs. a
  redirect-with-message) — a presentation detail, not a design-level decision.
