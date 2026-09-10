## Why

`/LivePlay`'s Statline section labels each loadout under a multi-loadout statline entry by
subtracting the weapons its siblings share, leaving only what's "distinguishing" — but when two
sibling loadouts carry identical weapons and differ only by an ability-granting wargear item (e.g.
a Custodian Warden squad's 4 plain models vs. 1 model "w/ Vexilla", both carrying the same Guardian
Spear melee+ranged profile), the weapon-only subtraction leaves nothing, so both loadout labels
render as an empty `<span class="loadout-label"></span>`. Confirmed live against a real import
(`data/gw-android-export-custodes.json`, "Custodian Wardens with Blade Champion"): the rendered
page shows `(4/4)` and `(1/1)` breakdown rows with no label text at all, next to a blank gap where
NewRecruit's own rendering of the same list shows "w/ Vexilla". The Vexilla ability itself, and its
OC+1 effect, already render and flag correctly elsewhere on the page — only the loadout-label text
that should let a player tell which specific model carries it is missing.

A second real import (`data/gw-android-export-deathguard.json`, "Plague Marines") surfaced a
deeper version of the same gap after the first fix landed: "Plague Champion" and "Plague Marine w/
boltgun" carry byte-identical weapons (Boltgun, Plague knives) and no abilities at all — nothing in
either multiset distinguishes them, so both loadouts fell back to the bare statline name ("Plague
Marine"), rendering two identically-labeled rows with genuinely no way to tell which one is the
Champion. NewRecruit itself distinguishes them fine, because the roster JSON's own per-loadout
selection already carries that distinguishing text ("Plague Champion") — both import pipelines
resolve that raw text down to a shared catalogue statline name and then discard it, never storing
it anywhere `ModelLine` keeps. The same discarded-name shape exists in the Custodian Warden case too
(its raw selection name is "Custodian Warden w/ Vexilla") and in the GW-app plain-text pipeline
(`ParsedModelGroup.ModelName`, discarded the same way by `ArmyRosterEnricher.BuildModelLine`) —
confirmed by directly comparing both pipelines' resolve-then-discard code paths.

## What Changes

- `ModelLineLoadout` gains the loadout's own ability names, sourced from that `ModelLine`'s
  `Abilities`, so a loadout's identity is no longer weapon-only at the aggregate-view layer.
- `CompressLoadoutLabels`' distinguishing computation folds ability names into the same
  weapons-based multiset (bag) intersection/subtraction it already performs, rather than adding a
  second, separate mechanism — a loadout whose weapons are shared with every sibling but whose
  ability names are not still gets a real label instead of rendering blank.
- A loadout that has nothing distinguishing it from its siblings in either weapons or abilities now
  falls back to rendering its own **DisplayName** as its label — a new `ModelLine`/`ModelLineLoadout`
  field carrying the import's own raw per-loadout selection name (e.g. "Plague Champion", "Custodian
  Warden w/ Vexilla"), populated by both import pipelines from data they already parse but
  previously discarded after resolving it to a shared catalogue statline name. When a pipeline (or a
  hand-built fixture) has no such raw text to offer, DisplayName defaults to the bare statline name
  itself, so the fallback degrades gracefully to the same "no worse than before" behavior the first
  draft of this proposal specified — an empty label reads as a rendering glitch, not "nothing to
  report" (per direct user feedback on that first draft, which had left this case rendering blank).

## Capabilities

### Modified Capabilities
- `live-play-view`: "Statline Section Rendering"'s loadout-label computation changes from a
  weapons-only distinguishing set to one that also considers each loadout's ability names, then each
  loadout's own import-side DisplayName as the final tie-break.

## Impact

- `src/ProbHammer.Core/Domain/Roster/ModelLine.cs` — gains a `DisplayName` property, defaulting to
  `StatlineName` when a pipeline supplies none.
- `src/ProbHammer.Core/Domain/Roster/ArmyRosterEnricher.cs` — `BuildModelLine` passes the parsed
  export's own `ParsedModelGroup.ModelName` through as `DisplayName` instead of discarding it once
  resolution against the catalogue completes.
- `src/ProbHammer.Core/Domain/Import/BattleScribe/BattleScribeRosterMapper.cs` — `BuildModelLines`
  passes each loadout node's own raw `name` through as `DisplayName` the same way.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — `ModelLineLoadout` construction
  in `BuildStatlines` gains each loadout's ability names and its `DisplayName`.
- `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` — `CompressLoadoutLabels` (and the
  `ModelLineLoadout` record it reads) extend to fold ability names into the distinguishing
  computation and to fall back to each loadout's own `DisplayName` when nothing distinguishes it;
  `BuildLoadoutLabelLookup`, which composes the distinguishing label into a weapon breakdown row's
  own `"{StatlineName} w/ {label}"` text, renders a fallback loadout's bare `DisplayName` instead
  (no `" w/ "` wrapping - see design.md for why a whole identity name and a genuine weapon/ability
  attribute need different treatment there).
