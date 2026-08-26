## Context

See `proposal.md` for motivation. Both BSData's `categoryLinks` and BattleScribe/NewRecruit's
`categories` already carry the fully-resolved display name directly on the link/category object
itself (`name`, plus `id`/`targetId`/`primary`) — unlike weapon profiles, rules, or InSv text, no
cross-file id resolution is needed to get a displayable value. Confirmed against the live BSData
clone and two real roster exports of the same list ("Masters of the Maelstrom", `.txt` and
NewRecruit JSON) during exploration:

```
Masters of the Maelstrom (unit)         categoryLinks/categories:
                                           Epic Hero, Infantry, Grenades, Chaos, Chaos Undivided,
                                           Masters of the Maelstrom, Faction: Heretic Astartes,
                                           Support
  ├─ Garreon the Corpsemaster (model)    (own categoryLinks/categories, no Psyker)
  ├─ Garlon Souleater (model)            categoryLinks/categories: [Psyker]   ← per-model only
  ├─ Katar Garrix (model)
  ├─ Captain Sargotta (model)
  └─ The Enforcer (model)
```

Confirmed identically shaped in both the raw BSData catalogue file and the NewRecruit roster JSON
export of the same unit — the per-model case is not a BSData-only capability.

## Goals / Non-Goals

**Goals:**
- Populate `Datasheet.Keywords` and `ModelLine.Keywords` with real data from both import
  pipelines, using one identical extraction rule applied at two tree depths.
- Make the per-model case (a keyword scoped to one named individual within a shared statline)
  work end-to-end, since it's the concrete motivating case for `ModelLine.Keywords`'s own
  existence.

**Non-Goals:**
- No Faction-vs-plain-Keyword split. Everything lands in one `Datasheet.Keywords` set; the literal
  `"Faction: "` prefix is stripped purely for display, not used to route into a separate
  collection. `FactionKeywords` stays untouched and unused, matching today.
- No exclusion of a datasheet's own name from its Keywords, even though BSData/BattleScribe also
  use that same category internally for wargear-targeting/exclusivity constraints — it doubles as
  a genuine 40k keyword real ability text references directly (e.g. "Friendly SWORD BRETHREN
  SQUAD units have +1 OC"), and this project doesn't second-guess which uses of a real keyword are
  "intended."
- No change to `KeywordResolution.EffectiveKeywords`, `AttachedUnitAggregator`, or any `/LivePlay`
  rendering — already correct once fed real data (see "Why this needs no live-update work" below).
- No curated allowlist/lookup table, unlike `ArmyRuleNameLookup` or `WeaponKeywordParser`'s known-
  token list. The extraction rule has no per-item judgment call (strip one literal prefix,
  otherwise keep verbatim), so there's nothing to curate.

## Decisions

**Extraction rule: strip a literal `"Faction: "` prefix, otherwise keep verbatim, no exclusions.**
Confirmed against real data from two unrelated factions (Black Templars, Death Guard) that BSData
consistently prefixes only the *specific* faction/chapter ("Faction: Black Templars", "Faction:
Death Guard") and leaves the *top-level* army affiliation bare ("Imperium", "Chaos") — identical in
shape to genuine keywords like "Infantry"/"Nurgle". Since Keywords is one flat set with no Faction
split (see Non-Goals), this mismatch has no behavioral consequence — "Imperium" and "Black
Templars" both simply become Keywords either way. The prefix strip is cosmetic (display only, "not
"Faction: Black Templars"), so no curated table of top-level faction names is needed to route it
correctly.

**Datasheet's own name is kept as a Keyword.** Reversed from an earlier assumption during
exploration (that this category was an internal BSData-only construct and should be excluded) —
raised and corrected by the user with a concrete real ability text example. Alternative considered:
exclude it — rejected, since it's indistinguishable, in both source formats, from any other bare
category, and excluding it would silently break a real class of ability targeting.

**New `Datasheet` accessor: per-name Keyword lookup, mirroring `GetStatline`/`TryGetStatline`.**
`Datasheet.Statlines` is already keyed by name with an internal dictionary built once at
construction; the new per-model Keyword data follows the identical shape (ordered list supplied to
the constructor, dictionary built once, empty set returned for an unknown/no-data name) rather than
folding Keywords onto `Statline` itself — a `Statline` is a value object of printed game stats
(M/T/Sv/W/Ld/Oc + InSv), and a narrative keyword set is a different kind of fact about a named
model, not a seventh characteristic. Alternative considered: add a `Keywords` field directly to
`Statline` — rejected, since `Statline` instances are already deduplicated by value when two
differently-named statlines share identical stats (`AttachedUnitAggregator.BuildStatlines`), and a
per-model keyword set would need to key off the model's *name*, not its stats, breaking that
dedup's own correctness the moment two same-stat names had different keyword data (exactly the
Masters of the Maelstrom shape, where several named individuals may well share a statline).

**Extraction point: the same recursive walk that already builds each named Statline.** Both
`BsdataDatasheetMapper.BuildDatasheet` and `BattleScribeRosterMapper.BuildModelLines`/
`BuildUnit` already visit each top-level unit entry and each of its nested named model entries to
extract that entry's own statline — collecting that same entry's own `categoryLinks`/`categories`
at the same visit is additive, not a new traversal.

**BattleScribe pipeline populates `ModelLine.Keywords` directly, bypassing any per-name lookup.**
Unlike the BSData pipeline (where `ArmyRosterEnricher` resolves a `ModelLine` from a Datasheet by
name, after the Datasheet is already fully built), `BattleScribeRosterMapper.BuildModelLines`
already holds the actual nested `model` selection object in hand at the point it constructs each
`ModelLine` — so it reads that selection's own `categories` inline, with no need to round-trip
through a name-keyed Datasheet lookup the way the BSData path does. The synthesized Datasheet's own
per-model lookup (if ever added, e.g. for a future consumer that only has the Datasheet and a
name) isn't required for this pipeline to work correctly today, and isn't built — YAGNI until a
second caller needs it.

**No corpus-scan/allowlist infrastructure for this extraction.** Every other classification task
with a dedicated `bsdata-corpus-scan` entry (weapon keyword tokens, Core-vs-Army rule origin,
primary-catalogue-gated rule triage) exists because that task makes a judgment call per item that
real data has repeatedly proven wrong on first assumption. This extraction makes no such call — it
keeps every `categoryLinks`/`categories` name verbatim except one mechanical prefix strip, so
there's no meaningful "wrong classification" a scan could catch beyond confirming the literal
string `"Faction: "` is spelled consistently everywhere, which is better served by the existing
one-off smoke-test convention (`domain-model-11e.md`'s "Automated tests never read the external
local clone... A one-off smoke test... passed against the live clone during implementation") than
a new permanent scan.

**Why this needs no live-update work.** `KeywordResolution.EffectiveKeywords` (unchanged,
untouched by this design) already filters both by component presence (`Components.Where(u =>
u.IsPresent)`) and by model-line presence (`.Where(ml => ml.RemainingCount > 0)`) before unioning
Keywords — confirmed by re-reading the source directly during exploration, not assumed from
memory. Once `ModelLine.Keywords` carries real data, marking Garlon Souleater a casualty already
drops "Psyker" out of the union with zero code changes beyond what this design already does; wiping
out an entire attached component already drops that component's Keywords out too. `roster-model`'s
own spec already covers this with a PSYKER-scenario matching this exact real case.

## Risks / Trade-offs

- The `"Faction: "` prefix convention was confirmed on two factions (Black Templars, Death Guard)
  during exploration, not the whole corpus → low risk given the strip is cosmetic-only (see Non-
  Goals), mitigated by a manual smoke-test pass against the live clone during implementation
  (same convention already established for other BSData ingestion work), not a permanent scan.
- Keeping a datasheet's own name as a Keyword means the rendered Keywords pill row will include
  that name (e.g. Impulsor's own Keywords list will include "Impulsor") → accepted, since this
  matches the real printed card convention and is the whole point of the reversed decision above.

## Migration Plan

Domain-model and mapper change only — no persisted state, no session/storage shape change. A
previously-imported session's `StoredArmyImport` is unaffected; the next `/LivePlay` render or
casualty POST re-runs `IArmyRosterProvider.Build` from the stored intermediate exactly as today,
now populating real Keywords along the way.
