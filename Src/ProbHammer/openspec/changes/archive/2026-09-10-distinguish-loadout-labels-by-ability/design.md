## Context

See `proposal.md` - Why. Two collaborators currently compute a loadout's on-screen label purely
from weapons:

- `AttachedUnitAggregator.BuildStatlines` (`src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs`)
  builds one `ModelLineLoadout` per `ModelLine` under a statline entry, carrying only
  `WeaponsLabel`/`Weapons`/`RemainingCount`/`InitialCount` - a `ModelLine`'s own `Abilities` (see
  `.claude/domain-model/roster-context.md`) never reach this record at all.
- `LivePlayModel.CompressLoadoutLabels` (`src/ProbHammer.Web/Pages/LivePlay.cshtml.cs`) reduces each
  loadout's `Weapons` list by the multiset intersection shared across every sibling loadout under the
  same statline entry, leaving only the weapons that distinguish it. `LivePlayModel.BuildLoadoutLabelLookup`
  composes that same compressed label into `"{StatlineName} w/ {compressed}"` for the Ranged/Melee
  weapon-contribution breakdown rows' own labels - it has no independent computation to fix, so
  correcting `CompressLoadoutLabels` fixes both render sites together (confirmed live: today both the
  Statline section and the Melee Weapons breakdown for the same unit render the identical blank gap).

Both `ModelLine.Abilities` sources (the BSData text-export pipeline and the BattleScribe/NewRecruit
JSON pipeline) already populate a wargear-granted ability on the specific `ModelLine` that carries it
- see `.claude/domain-model/army-list-import-pipeline.md` and
`.claude/domain-model/battlescribe-import-pipeline.md`. This change only needs to route that
already-correct data through to the label computation; no import-pipeline change is needed for the
abilities tier.

A third collaborator entered the picture once the Death Guard case (below) was found: both
pipelines' own model-line-building step (`ArmyRosterEnricher.BuildModelLine` and
`BattleScribeRosterMapper.BuildModelLines`) resolves a raw, already-parsed per-loadout name down to
one of the catalogue's declared statline names, then discards the raw text - unlike the abilities
case, this DOES need an import-pipeline change (see Decisions below) to stop discarding it.

## Goals / Non-Goals

**Goals:**
- A loadout's label distinguishes it from its siblings using both weapons and ability names, not
  weapons alone.
- Preserve every existing weapons-only scenario's output unchanged (the existing CompressLoadoutLabels
  test scenarios in `openspec/specs/live-play-view/spec.md` must still hold).

**Non-Goals:**
- Rendering an ability's full descriptive text inline in the loadout label (the ability already has
  its own popover-triggering entry in the unit's ability column - this change only adds its *name* to
  the label, the same "name only" convention every other loadout-distinguishing item already follows).
- Changing how `AttachedUnitAggregator.BuildAbilities` reports the ability itself (the abilities column
  already renders it correctly and is untouched).
- A dedicated LoadoutIndex-scoped `AggregateAbilityEntry` (see `.claude/domain-model/roster-context.md`'s
  note on `WeaponContribution.LoadoutIndex` existing for exactly this "two loadouts otherwise
  indistinguishable by ComponentName/StatlineName" reason) - not needed here since the label
  computation only ever needs a loadout's *own* ability names, sourced directly from its own
  `ModelLine.Abilities`, not from the already-built `AggregateAbilityEntry` list.

## Decisions

**Where the ability names come from**: `ModelLineLoadout` gains an `Abilities: IReadOnlyList<string>`
field (ability *names*, not full `Ability` objects) built in `BuildStatlines` from that loadout's own
`ml.Abilities.Select(a => a.Name)`, mirroring `WeaponsLabel`'s existing "already-joined-per-loadout"
shape rather than carrying full `Ability` records the label computation doesn't need. Alternative
considered: carry the full `Ability` list (so a future feature could show ability text inline) -
rejected per the Non-Goals above; carrying more than the label needs invites the label computation to
grow scope it doesn't need yet, and a future feature can widen this field then.

**How weapons and abilities combine in one label**: `CompressLoadoutLabels` runs the exact same
multiset (bag) intersection-then-subtraction algorithm it already runs for weapons a second time,
independently, over each loadout's ability-name list - never merging weapons and abilities into one
combined multiset. The two results are joined into one comma-separated label, distinguishing weapons
before distinguishing ability names (matching the natural reading order: "what does it carry, then
what does it grant"). Alternative considered: fold ability names into the same weapon multiset before
subtracting - rejected, since a weapon and an ability could coincidentally share a name (unlikely but
not structurally impossible) and conflating the two pools would make the subtraction's behavior depend
on that coincidence.

**A loadout with nothing distinguishing in either pool falls back to its statline name, not a blank
label**: the first draft of this proposal left this case rendering an empty label, matching
pre-existing behavior for a loadout that genuinely has no distinguishing feature (e.g.
`verification-roster-ability-effects-custodes.json`'s many identical-loadout squads elsewhere in the
corpus). Direct user feedback on that draft: an empty label still looks like a rendering glitch even
when it's "correct," since every other loadout line has readable text next to its checkbox and count
— a blank one reads as broken, not as "nothing to report." `CompressLoadoutLabels` now takes the
owning entry's `StatlineName` as a second parameter and returns it verbatim for any loadout whose
combined weapons+abilities subtraction leaves nothing, rather than `""`. Alternative considered:
leave it blank and rely on the header line's own `StatlineName` (rendered once, above the whole
loadout list) to carry that information by implication - rejected per the user's own read of this as
looking like a bug, not an implied fact.

**`BuildLoadoutLabelLookup` must special-case the fallback, not just compose it in**: this lookup
builds each split-out weapon-breakdown row's label as `"{StatlineName} w/ {compressed}"` (e.g.
`"Custodian Warden w/ Vexilla"`). Left unchanged, a fallback loadout would now produce
`"Custodian Warden w/ Custodian Warden"` - `compressed[i]` and `entry.StatlineName` would be the
same string, since `CompressLoadoutLabels` now returns `StatlineName` itself in that case. Fixed by
checking `compressed[i] == entry.StatlineName` and, when true, using `entry.StatlineName` alone
(no `" w/ "` suffix) - the same shape `BuildLoadoutLabelLookup` already uses for a single-loadout
entry's own lookup row (`entry.Loadouts.Count <= 1`), so a fallback loadout's breakdown row reads
identically to how a genuinely single-loadout statline's row already reads today.

**Revised after a second real import surfaced a deeper case (added post-implementation, still
before archive)**: `data/gw-android-export-deathguard.json`'s "Plague Champion" vs "Plague Marine w/
boltgun" carry byte-identical weapons and no abilities at all - the fallback above fires for BOTH
loadouts, rendering two identically-labeled "Plague Marine" rows with no way to tell which is the
Champion. Investigated and confirmed (see `.claude/domain-model/battlescribe-import-pipeline.md`'s
own per-loadout-node `name` field, and the equivalent `ParsedModelGroup.ModelName` in the GW-app
text pipeline): both import pipelines already parse a raw, distinguishing per-loadout name straight
from the source ("Plague Champion", "Custodian Warden w/ Vexilla") - and both discard it in the same
place, immediately after resolving it against the catalogue down to a shared `ModelLine.StatlineName`
("Plague Marine", "Custodian Warden"). This is a structurally identical gap in both pipelines
(`ArmyRosterEnricher.BuildModelLine` and `BattleScribeRosterMapper.BuildModelLines`), not two
separate bugs, confirmed by directly comparing their resolve-then-discard code paths.

**`ModelLine.DisplayName`, not a fresh `ModelLineLoadout`-only field**: the raw per-loadout name is
captured on `ModelLine` itself (defaulting to `StatlineName` via a new optional constructor
parameter, so every existing call site is unaffected), then threaded through to
`ModelLineLoadout.DisplayName` in `AttachedUnitAggregator.BuildStatlines` exactly like `Abilities`
already is. Putting it on `ModelLine` (the shared domain type both pipelines already build) rather
than inventing a page-layer-only concept keeps the "import's own per-loadout identity" fact where
the rest of a `ModelLine`'s identity already lives, and makes it available to any future consumer
beyond `/LivePlay`'s label computation.

**Label fallback order becomes three tiers, not two**: `CompressLoadoutLabels` now tries, in order,
(1) the weapons+abilities distinguishing label from before, (2) the loadout's own `DisplayName` when
that label is empty, falling through automatically to (3) the bare `StatlineName` because
`DisplayName` itself already defaults to `StatlineName` when a pipeline (or a hand-built fixture)
supplies nothing extra - collapsing what would otherwise be a third explicit tier into the second
one's own default. This means `CompressLoadoutLabels` no longer needs a separate `statlineName`
parameter at all (removed) - `loadout.DisplayName` alone already carries the correct terminal value
for the Custodian Warden 4/4 case (falls back to `"Custodian Warden"`, same as before) and the new
Plague Marine case (falls back to `"Plague Champion"` / `"Plague Marine w/ boltgun"`, each loadout's
own text) uniformly, with no extra branching at the call site.

**`BuildLoadoutLabelLookup` needs the *raw* distinguishing signal, not just the final label, to
decide its own wrapping**: a fallback loadout's label is now sometimes a genuine attribute-list
result (rendered `"{StatlineName} w/ {label}"`) and sometimes a whole-identity DisplayName that must
stand alone (`"Plague Champion"`, never `"Plague Marine w/ Plague Champion"` - DisplayName is
already a complete name, not an attribute of one). Comparing the final label against
`entry.StatlineName` (the original single-tier check) is no longer sufficient to tell these apart,
since a fallback label is now usually a DisplayName that differs from StatlineName too. Fixed by
factoring the weapons+abilities-only computation out into a shared private `DistinguishingLabels`
helper that both `CompressLoadoutLabels` and `BuildLoadoutLabelLookup` call directly: an empty
result from that raw helper unambiguously means "nothing genuinely distinguishes this loadout,"
regardless of what DisplayName ultimately gets shown - `BuildLoadoutLabelLookup` wraps only when the
raw result is non-empty, and otherwise renders `loadout.DisplayName` bare.

## Risks / Trade-offs

- [Two abilities differing only in `Text` but sharing a `Name` would still look identical in the
  label, same as they already look identical everywhere else abilities render by name] → Accepted:
  no existing rendering path in `_UnitBlock.cshtml` disambiguates abilities beyond their name either
  (see `AbilityDisplayName`), so this isn't a regression relative to the rest of the page.
- [A loadout carrying several distinguishing abilities would produce a longer, comma-joined label
  (e.g. `"Vexilla, Icon of Wrath"`)] → Accepted: matches the existing multi-weapon label precedent
  (`CompressLoadoutLabels`' own doc comment already allows a multi-weapon distinguishing label), no
  new formatting concern introduced.
- [A fallback loadout line now repeats its entry's own `StatlineName` twice on screen - once on the
  header line above the loadout list, once on the loadout line itself] → Accepted: still preferred
  over a blank label per the user's own feedback; the repetition reads as "this specific loadout is
  just an ordinary {StatlineName}, same as its header" rather than as a rendering error, and only
  arises for a statline entry with multiple loadouts where at least one is genuinely
  undistinguishable from its siblings - not the common case. Only arises when a pipeline supplies no
  DisplayName of its own (or the raw text happens to equal the statline name exactly) - both real
  imports investigated so far (Custodes, Death Guard, Black Templars) always have something more
  specific to show once DisplayName is wired through.
- [`ModelLine.DisplayName` could read as a second, competing "name" alongside `StatlineName`, adding
  a small ongoing cognitive cost for anyone reading `ModelLine`] → Accepted: the two answer genuinely
  different questions (a catalogue reference vs. this specific loadout's own import-side identity),
  the doc comment states the distinction directly on the property, and the alternative (recomputing
  or re-parsing a per-loadout name at the page layer, far from where it was originally parsed) would
  be worse - duplicating parsing logic across two pipelines' own mappers plus the page layer, instead
  of once at the point each pipeline already has the raw text in hand.

## Migration Plan

Pure additive/internal change - no persisted state, no API surface, no migration needed. Ships as one
PR; the existing `AttachedUnitAggregatorTests`/`LivePlayModelTests` (or equivalent) suites gain
regression coverage for the new scenarios per `tasks.md`.
